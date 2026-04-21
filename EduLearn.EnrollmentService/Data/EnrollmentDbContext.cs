using EduLearn.EnrollmentService.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.EnrollmentService.Data;

// EF Core DbContext for EnrollmentService
// Own PostgreSQL database: EduLearn_Enrollment
public class EnrollmentDbContext : DbContext
{
    public EnrollmentDbContext(DbContextOptions<EnrollmentDbContext> options) : base(options) { }

    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId);

            entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("ACTIVE");
            entity.Property(e => e.ProgressPercent).HasDefaultValue(0);
            entity.Property(e => e.CertificateIssued).HasDefaultValue(false);
            entity.Property(e => e.PaymentId).HasMaxLength(200);

            // PostgreSQL timestamp
            entity.Property(e => e.EnrolledAt).HasDefaultValueSql("NOW()");

            // Unique index — prevents duplicate enrollments (one student per course)
            entity.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();

            // Index for student's enrolled courses list
            entity.HasIndex(e => e.StudentId);

            // Index for course analytics — instructor dashboard
            entity.HasIndex(e => e.CourseId);
        });
    }
}