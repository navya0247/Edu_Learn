using EduLearn.PaymentService.Data;
using EduLearn.PaymentService.Entities;
using EduLearn.PaymentService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.PaymentService.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _db;

    public PaymentRepository(PaymentDbContext db) => _db = db;

    public async Task<Payment> CreateAsync(Payment payment)
    {
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return payment;
    }

    public async Task<Payment?> FindByIdAsync(int paymentId) =>
        await _db.Payments.FindAsync(paymentId);

    public async Task<Payment?> FindByRazorpayOrderIdAsync(string orderId) =>
        await _db.Payments.FirstOrDefaultAsync(p => p.RazorpayOrderId == orderId);

    public async Task<List<Payment>> FindByStudentIdAsync(int studentId) =>
        await _db.Payments.Where(p => p.StudentId == studentId)
                          .OrderByDescending(p => p.CreatedAt)
                          .ToListAsync();

    public async Task<List<Payment>> FindByCourseIdAsync(int courseId) =>
        await _db.Payments.Where(p => p.CourseId == courseId)
                          .OrderByDescending(p => p.CreatedAt)
                          .ToListAsync();

    public async Task UpdateAsync(Payment payment)
    {
        _db.Payments.Update(payment);
        await _db.SaveChangesAsync();
    }
}
