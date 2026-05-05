using EduLearn.AssessmentService.Data;
using EduLearn.AssessmentService.Entities;
using EduLearn.AssessmentService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.AssessmentService.Repositories;

// EF Core implementation of IQuizRepository
// Connected to EduLearn_Assessment PostgreSQL database
public class QuizRepository : IQuizRepository
{
    private readonly AssessmentDbContext _db;

    public QuizRepository(AssessmentDbContext db) => _db = db;

    // ── Quiz Operations ───────────────────────────────────────────────────────

    public async Task<Quiz?> FindByQuizId(int quizId)
        => await _db.Quizzes.FindAsync(quizId);

    public async Task<IList<Quiz>> FindByCourseId(int courseId)
        => await _db.Quizzes
            .Where(q => q.CourseId == courseId)
            .OrderBy(q => q.CreatedAt)
            .ToListAsync();

    // Find the quiz attached to a specific lesson
    public async Task<Quiz?> FindByLessonId(int lessonId)
        => await _db.Quizzes
            .FirstOrDefaultAsync(q => q.LessonId == lessonId);

    public async Task<Quiz> Create(Quiz quiz)
    {
        _db.Quizzes.Add(quiz);
        await _db.SaveChangesAsync();
        return quiz;
    }

    public async Task<Quiz> Update(Quiz quiz)
    {
        _db.Quizzes.Update(quiz);
        await _db.SaveChangesAsync();
        return quiz;
    }

    public async Task Delete(int quizId)
        => await _db.Quizzes.Where(q => q.QuizId == quizId).ExecuteDeleteAsync();

    // ── Attempt Operations ────────────────────────────────────────────────────

    public async Task<QuizAttempt?> FindAttemptById(int attemptId)
        => await _db.QuizAttempts.Include(a => a.Quiz).FirstOrDefaultAsync(a => a.AttemptId == attemptId);

    // All attempts by a student for a specific quiz — ordered by start time
    public async Task<IList<QuizAttempt>> FindAttemptsByStudentAndQuiz(int studentId, int quizId)
        => await _db.QuizAttempts
            .Where(a => a.StudentId == studentId && a.QuizId == quizId)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync();

    // Count attempts — checked against MaxAttempts before allowing new attempt
    public async Task<int> CountAttempts(int studentId, int quizId)
        => await _db.QuizAttempts
            .CountAsync(a => a.StudentId == studentId && a.QuizId == quizId);

    // Best attempt = highest score
    public async Task<QuizAttempt?> FindBestAttempt(int studentId, int quizId)
        => await _db.QuizAttempts
            .Where(a => a.StudentId == studentId && a.QuizId == quizId
                     && a.SubmittedAt != null)
            .OrderByDescending(a => a.Score)
            .FirstOrDefaultAsync();

    public async Task<QuizAttempt> CreateAttempt(QuizAttempt attempt)
    {
        _db.QuizAttempts.Add(attempt);
        await _db.SaveChangesAsync();
        return attempt;
    }

    public async Task<QuizAttempt> UpdateAttempt(QuizAttempt attempt)
    {
        _db.QuizAttempts.Update(attempt);
        await _db.SaveChangesAsync();
        return attempt;
    }
}