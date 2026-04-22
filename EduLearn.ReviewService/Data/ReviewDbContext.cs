using EduLearn.ReviewService.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.ReviewService.Data;

// EF Core DbContext for ReviewService
// Own PostgreSQL database: EduLearn_Review
public class ReviewDbContext : DbContext
{
    public ReviewDbContext(DbContextOptions<ReviewDbContext> options) : base(options) { }

    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(r => r.ReviewId);

            // Rating must be 1-5
            entity.Property(r => r.Rating).IsRequired();
            entity.Property(r => r.Comment).HasMaxLength(2000);
            entity.Property(r => r.IsApproved).HasDefaultValue(false);
            entity.Property(r => r.IsHidden).HasDefaultValue(false);
            entity.Property(r => r.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(r => r.UpdatedAt).HasDefaultValueSql("NOW()");

            // One review per student per course — prevents duplicate reviews
            entity.HasIndex(r => new { r.StudentId, r.CourseId }).IsUnique();

            // Index for fetching all reviews for a course
            entity.HasIndex(r => r.CourseId);

            // Index for fetching all reviews by a student
            entity.HasIndex(r => r.StudentId);
        });
    }
}