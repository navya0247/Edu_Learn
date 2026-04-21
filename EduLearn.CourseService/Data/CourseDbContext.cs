using EduLearn.CourseService.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.CourseService.Data;

/// <summary>
/// EF Core DbContext for CourseService.
/// Uses its OWN PostgreSQL database: EduLearn_Course
/// Completely separate from EduLearn_Auth — microservices pattern.
/// </summary>
public class CourseDbContext : DbContext
{
    public CourseDbContext(DbContextOptions<CourseDbContext> options) : base(options) { }

    // Maps to public."Courses" table in PostgreSQL
    public DbSet<Course> Courses => Set<Course>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(c => c.CourseId);

            // Required string fields with max lengths
            entity.Property(c => c.Title).IsRequired().HasMaxLength(300);
            entity.Property(c => c.Description).IsRequired().HasMaxLength(5000);
            entity.Property(c => c.Category).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Level).HasMaxLength(20).HasDefaultValue("Beginner");
            entity.Property(c => c.Language).HasMaxLength(50).HasDefaultValue("English");

            // PostgreSQL numeric type
            entity.Property(c => c.Price).HasColumnType("decimal(10,2)").HasDefaultValue(0);
            entity.Property(c => c.ThumbnailUrl).HasMaxLength(1000);

            // Both flags start as false — must go through publish/approve workflow
            entity.Property(c => c.IsPublished).HasDefaultValue(false);
            entity.Property(c => c.IsApproved).HasDefaultValue(false);
            entity.Property(c => c.EnrollmentCount).HasDefaultValue(0);

            // PostgreSQL uses NOW() not GETUTCDATE() (SQL Server)
            entity.Property(c => c.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(c => c.UpdatedAt).HasDefaultValueSql("NOW()");

            // Composite index — speeds up: WHERE IsPublished=true AND IsApproved=true
            entity.HasIndex(c => new { c.IsPublished, c.IsApproved });

            // Index for instructor dashboard queries
            entity.HasIndex(c => c.InstructorId);

            // Index for category filter
            entity.HasIndex(c => c.Category);
        });
    }
}