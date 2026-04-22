using EduLearn.ProgressService.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.ProgressService.Data;

// EF Core DbContext for ProgressService
// Own PostgreSQL database: EduLearn_Progress
public class ProgressDbContext : DbContext
{
    public ProgressDbContext(DbContextOptions<ProgressDbContext> options) : base(options) { }

    public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LessonProgress>(entity =>
        {
            entity.HasKey(lp => lp.LessonProgressId);
            entity.Property(lp => lp.IsCompleted).HasDefaultValue(false);
            entity.Property(lp => lp.WatchPercent).HasDefaultValue(0);

            // Unique index — one progress record per student per lesson
            entity.HasIndex(lp => new { lp.StudentId, lp.LessonId }).IsUnique();

            // Index for fetching all lessons completed by student in a course
            entity.HasIndex(lp => new { lp.StudentId, lp.CourseId });
        });

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasKey(c => c.CertificateId);
            entity.Property(c => c.StudentName).IsRequired().HasMaxLength(200);
            entity.Property(c => c.CourseName).IsRequired().HasMaxLength(300);
            entity.Property(c => c.IssuedAt).HasDefaultValueSql("NOW()");

            // CertificateCode must be unique — used for public verification
            entity.Property(c => c.CertificateCode).IsRequired().HasMaxLength(50);
            entity.HasIndex(c => c.CertificateCode).IsUnique();
            entity.Property(c => c.PdfUrl).HasMaxLength(2000);

            // One certificate per student per course
            entity.HasIndex(c => new { c.StudentId, c.CourseId }).IsUnique();
        });
    }
}