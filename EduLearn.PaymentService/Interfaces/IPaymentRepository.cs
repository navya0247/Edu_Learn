using EduLearn.PaymentService.Entities;

namespace EduLearn.PaymentService.Interfaces;

/// <summary>DB contract for payment data access</summary>
public interface IPaymentRepository
{
    Task<Payment> CreateAsync(Payment payment);
    Task<Payment?> FindByIdAsync(int paymentId);
    Task<Payment?> FindByRazorpayOrderIdAsync(string orderId);
    Task<List<Payment>> FindByStudentIdAsync(int studentId);
    Task<List<Payment>> FindByCourseIdAsync(int courseId);
    Task UpdateAsync(Payment payment);
}
