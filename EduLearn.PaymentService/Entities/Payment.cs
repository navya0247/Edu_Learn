namespace EduLearn.PaymentService.Entities;

/// <summary>Payment transaction entity — tracks Razorpay orders and status</summary>
public class Payment
{
    public int PaymentId { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }

    // Razorpay order created on server
    public string RazorpayOrderId { get; set; } = string.Empty;

    // Filled after successful payment by frontend
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpaySignature { get; set; }

    public decimal Amount { get; set; }        // in INR
    public string Currency { get; set; } = "INR";

    // PENDING → SUCCESS → FAILED → REFUNDED
    public string Status { get; set; } = "PENDING";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public DateTime? FailedAt { get; set; }
}
