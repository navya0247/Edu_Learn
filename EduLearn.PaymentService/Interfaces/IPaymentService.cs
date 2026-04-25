using EduLearn.PaymentService.DTOs;

namespace EduLearn.PaymentService.Interfaces;

/// <summary>Business logic contract for payment operations</summary>
public interface IPaymentService
{
    /// <summary>Create Razorpay order — returns order details for frontend popup</summary>
    Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto);

    /// <summary>Verify Razorpay signature after payment — marks payment SUCCESS</summary>
    Task<bool> VerifyPaymentAsync(VerifyPaymentDto dto);

    /// <summary>Get payment by internal ID</summary>
    Task<PaymentResponseDto?> GetByIdAsync(int paymentId);

    /// <summary>All payments made by a student</summary>
    Task<List<PaymentResponseDto>> GetByStudentAsync(int studentId);

    /// <summary>All payments for a course (instructor/admin view)</summary>
    Task<List<PaymentResponseDto>> GetByCourseAsync(int courseId);
}
