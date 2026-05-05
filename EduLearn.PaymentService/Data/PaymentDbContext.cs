using EduLearn.PaymentService.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.PaymentService.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.PaymentId);

            // Razorpay order ID must be unique
            entity.HasIndex(p => p.RazorpayOrderId).IsUnique();

            entity.Property(p => p.Amount).HasColumnType("decimal(10,2)");
            entity.Property(p => p.Status).HasDefaultValue("PENDING");
            entity.Property(p => p.Currency).HasDefaultValue("INR");
            entity.Property(p => p.CreatedAt).HasDefaultValueSql("NOW()");
        });
    }
}
