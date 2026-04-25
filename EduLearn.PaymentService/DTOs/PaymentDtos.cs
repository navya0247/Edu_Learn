namespace EduLearn.PaymentService.DTOs;

/// <summary>Request to create a Razorpay order for a course</summary>
public class CreateOrderDto
{
    public int CourseId { get; set; }
    public int StudentId { get; set; }
    public decimal Amount { get; set; }   // course price in INR
}

/// <summary>Razorpay order created — send these to frontend to open payment popup</summary>
public class OrderResponseDto
{
    public int PaymentId { get; set; }
    public string RazorpayOrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string RazorpayKeyId { get; set; } = string.Empty;  // public key for frontend
}

/// <summary>Frontend sends this after user completes payment — we verify signature</summary>
public class VerifyPaymentDto
{
    public int PaymentId { get; set; }
    public string RazorpayOrderId { get; set; } = string.Empty;
    public string RazorpayPaymentId { get; set; } = string.Empty;
    public string RazorpaySignature { get; set; } = string.Empty;
}

/// <summary>Payment details returned to client</summary>
public class PaymentResponseDto
{
    public int PaymentId { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string RazorpayOrderId { get; set; } = string.Empty;
    public string? RazorpayPaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}
