using EduLearn.AssessmentService.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.AssessmentService.Data;

// EF Core DbContext for AssessmentService
// Own PostgreSQL database: EduLearn_Assessment
public class AssessmentDbContext : DbContext
{
    public AssessmentDbContext(DbContextOptions<AssessmentDbContext> options) : base(options) { }

    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(q => q.QuizId);
            entity.Property(q => q.Title).IsRequired().HasMaxLength(300);
            entity.Property(q => q.Description).HasMaxLength(2000);
            entity.Property(q => q.TimeLimitMinutes).HasDefaultValue(0);
            entity.Property(q => q.PassingScore).HasDefaultValue(70);
            entity.Property(q => q.MaxAttempts).HasDefaultValue(3);
            entity.Property(q => q.IsPublished).HasDefaultValue(false);

            // Questions stored as JSON text column
            entity.Property(q => q.QuestionsJson).HasColumnType("text").HasDefaultValue("[]");
            entity.Property(q => q.CreatedAt).HasDefaultValueSql("NOW()");

            // Index for fetching quizzes by course
            entity.HasIndex(q => q.CourseId);

            // Index for lesson-specific quizzes
            entity.HasIndex(q => q.LessonId);
        });

        modelBuilder.Entity<QuizAttempt>(entity =>
        {
            entity.HasKey(a => a.AttemptId);
            entity.Property(a => a.Score).HasDefaultValue(0);
            entity.Property(a => a.IsPassed).HasDefaultValue(false);
            entity.Property(a => a.StartedAt).HasDefaultValueSql("NOW()");

            // Answers stored as JSON text column
            entity.Property(a => a.AnswersJson).HasColumnType("text").HasDefaultValue("{}");

            // FK to Quiz — cascade delete removes attempts when quiz is deleted
            entity.HasOne(a => a.Quiz)
                  .WithMany()
                  .HasForeignKey(a => a.QuizId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Index for fetching attempts by student and quiz
            entity.HasIndex(a => new { a.StudentId, a.QuizId });
        });
    }
}