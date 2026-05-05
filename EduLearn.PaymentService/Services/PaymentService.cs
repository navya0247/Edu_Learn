using System.Security.Cryptography;
using System.Text;
using EduLearn.PaymentService.DTOs;
using EduLearn.PaymentService.Entities;
using EduLearn.PaymentService.Interfaces;
using EduLearn.PaymentService.Messaging;

namespace EduLearn.PaymentService.Services;

/// <summary>
/// PaymentService with RabbitMQ publisher.
/// After VerifyPayment succeeds → publishes PaymentSuccessEvent to RabbitMQ.
/// EnrollmentService consumes it and auto-enrolls student (Saga pattern).
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository  _repo;
    private readonly IConfiguration     _config;
    private readonly RabbitMqPublisher  _publisher;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository repo,
        IConfiguration config,
        RabbitMqPublisher publisher,
        ILogger<PaymentService> logger)
    {
        _repo      = repo;
        _config    = config;
        _publisher = publisher;
        _logger    = logger;
    }

    public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto)
    {
        var keyId     = _config["Razorpay:KeyId"]     ?? "rzp_test_MOCK";
        var keySecret = _config["Razorpay:KeySecret"] ?? "MOCK_SECRET";

        string razorpayOrderId;

        if (keyId == "rzp_test_MOCK")
        {
            razorpayOrderId = $"order_MOCK_{Guid.NewGuid():N}".Substring(0, 30);
            _logger.LogWarning("Using MOCK Razorpay order");
        }
        else
        {
            razorpayOrderId = await CreateRazorpayOrderAsync(dto.Amount, keyId, keySecret);
        }

        var payment = new Payment
        {
            StudentId       = dto.StudentId,
            CourseId        = dto.CourseId,
            Amount          = dto.Amount,
            RazorpayOrderId = razorpayOrderId,
            Status          = "PENDING"
        };

        await _repo.CreateAsync(payment);
        _logger.LogInformation("Order created: {OrderId} for Student {S}", razorpayOrderId, dto.StudentId);

        return new OrderResponseDto
        {
            PaymentId       = payment.PaymentId,
            RazorpayOrderId = razorpayOrderId,
            Amount          = dto.Amount,
            Currency        = "INR",
            RazorpayKeyId   = keyId
        };
    }

    /// <summary>
    /// Verify Razorpay signature → mark SUCCESS → publish to RabbitMQ.
    /// EnrollmentService listens and auto-enrolls student (async Saga).
    /// </summary>
    public async Task<bool> VerifyPaymentAsync(VerifyPaymentDto dto)
    {
        var payment = await _repo.FindByIdAsync(dto.PaymentId);
        if (payment == null) return false;

        var keySecret = _config["Razorpay:KeySecret"] ?? "MOCK_SECRET";

        bool isValid;
        if (keySecret == "MOCK_SECRET")
        {
            isValid = true;
            _logger.LogWarning("Skipping signature verification in MOCK mode");
        }
        else
        {
            var payload     = $"{dto.RazorpayOrderId}|{dto.RazorpayPaymentId}";
            var computedSig = ComputeHmacSha256(payload, keySecret);
            isValid = computedSig == dto.RazorpaySignature;
        }

        if (isValid)
        {
            payment.RazorpayPaymentId = dto.RazorpayPaymentId;
            payment.RazorpaySignature = dto.RazorpaySignature;
            payment.Status            = "SUCCESS";
            payment.PaidAt            = DateTime.UtcNow;
            await _repo.UpdateAsync(payment);

            // ✅ Publish to RabbitMQ → EnrollmentService auto-enrolls student
            _publisher.PublishPaymentSuccess(new PaymentSuccessEvent
            {
                PaymentId = payment.PaymentId,
                StudentId = payment.StudentId,
                CourseId  = payment.CourseId,
                Amount    = payment.Amount,
                OrderId   = payment.RazorpayOrderId,
                PaidAt    = payment.PaidAt.Value
            });

            _logger.LogInformation("Payment {Id} verified — RabbitMQ event published", dto.PaymentId);
        }
        else
        {
            payment.Status   = "FAILED";
            payment.FailedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(payment);
            _logger.LogWarning("Payment {Id} verification FAILED", dto.PaymentId);
        }

        return isValid;
    }

    public async Task<PaymentResponseDto?> GetByIdAsync(int paymentId)
    {
        var p = await _repo.FindByIdAsync(paymentId);
        return p == null ? null : ToDto(p);
    }

    public async Task<List<PaymentResponseDto>> GetByStudentAsync(int studentId)
        => (await _repo.FindByStudentIdAsync(studentId)).Select(ToDto).ToList();

    public async Task<List<PaymentResponseDto>> GetByCourseAsync(int courseId)
        => (await _repo.FindByCourseIdAsync(courseId)).Select(ToDto).ToList();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }

    private static async Task<string> CreateRazorpayOrderAsync(decimal amount, string keyId, string keySecret)
    {
        using var client = new HttpClient();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));
        client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");

        var body    = new { amount = (int)(amount * 100), currency = "INR", receipt = $"rcpt_{Guid.NewGuid():N}".Substring(0, 20) };
        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://api.razorpay.com/v1/orders", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(result);
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    private static PaymentResponseDto ToDto(Payment p) => new()
    {
        PaymentId         = p.PaymentId,
        StudentId         = p.StudentId,
        CourseId          = p.CourseId,
        RazorpayOrderId   = p.RazorpayOrderId,
        RazorpayPaymentId = p.RazorpayPaymentId,
        Amount            = p.Amount,
        Status            = p.Status,
        CreatedAt         = p.CreatedAt,
        PaidAt            = p.PaidAt
    };
}
