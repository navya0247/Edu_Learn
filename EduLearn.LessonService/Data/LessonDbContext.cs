using EduLearn.LessonService.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.LessonService.Data;

// EF Core DbContext for LessonService
// Own PostgreSQL database: EduLearn_Lesson — separate from Auth and Course
public class LessonDbContext : DbContext
{
    public LessonDbContext(DbContextOptions<LessonDbContext> options) : base(options) { }

    public DbSet<Lesson> Lessons => Set<Lesson>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(l => l.LessonId);
            entity.Property(l => l.Title).IsRequired().HasMaxLength(300);
            entity.Property(l => l.Description).HasMaxLength(2000);
            entity.Property(l => l.ContentType).IsRequired().HasMaxLength(20).HasDefaultValue("VIDEO");
            entity.Property(l => l.ContentUrl).HasMaxLength(2000);
            entity.Property(l => l.DurationMinutes).HasDefaultValue(0);
            entity.Property(l => l.DisplayOrder).HasDefaultValue(0);
            entity.Property(l => l.IsPreview).HasDefaultValue(false);
            entity.Property(l => l.IsPublished).HasDefaultValue(false);

            // PostgreSQL uses NOW() not GETUTCDATE()
            entity.Property(l => l.CreatedAt).HasDefaultValueSql("NOW()");

            // Composite index — speeds up ordered curriculum query
            entity.HasIndex(l => new { l.CourseId, l.DisplayOrder });

            // Index for preview lesson queries (guest access)
            entity.HasIndex(l => new { l.CourseId, l.IsPreview });
        });
    }
}