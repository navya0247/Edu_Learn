using System.Text.Json;
using EduLearn.AssessmentService.DTOs;
using EduLearn.AssessmentService.Entities;
using EduLearn.AssessmentService.Interfaces;

namespace EduLearn.AssessmentService.Services;

// Business logic for quiz creation and attempt lifecycle
// Answers stored/retrieved as JSON via System.Text.Json
public class QuizService : IQuizService
{
    private readonly IQuizRepository _repo;
    private readonly ILogger<QuizService> _logger;

    public QuizService(IQuizRepository repo, ILogger<QuizService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public async Task<QuizResponseDto> CreateQuiz(CreateQuizDto dto)
    {
        var quiz = new Quiz
        {
            CourseId         = dto.CourseId,
            LessonId         = dto.LessonId,
            Title            = dto.Title,
            Description      = dto.Description,
            TimeLimitMinutes = dto.TimeLimitMinutes,
            PassingScore     = dto.PassingScore,
            MaxAttempts      = dto.MaxAttempts,
            IsPublished      = false,
            // Serialize full question list including correct answers — stored securely in DB
            QuestionsJson    = JsonSerializer.Serialize(dto.Questions),
            CreatedAt        = DateTime.UtcNow
        };

        var created = await _repo.Create(quiz);
        _logger.LogInformation("Quiz created: '{Title}' for Course {CourseId}", created.Title, created.CourseId);
        return MapToResponse(created);
    }

    public async Task<QuizResponseDto?> GetQuizById(int quizId)
    {
        var quiz = await _repo.FindByQuizId(quizId);
        return quiz == null ? null : MapToResponse(quiz);
    }

    public async Task<IList<QuizResponseDto>> GetQuizzesByCourse(int courseId)
    {
        var quizzes = await _repo.FindByCourseId(courseId);
        return quizzes.Select(MapToResponse).ToList();
    }

    public async Task<QuizResponseDto?> GetQuizByLesson(int lessonId)
    {
        var quiz = await _repo.FindByLessonId(lessonId);
        return quiz == null ? null : MapToResponse(quiz);
    }

    public async Task<QuizResponseDto> UpdateQuiz(int quizId, CreateQuizDto dto)
    {
        var quiz = await _repo.FindByQuizId(quizId)
            ?? throw new KeyNotFoundException($"Quiz {quizId} not found.");

        quiz.Title            = dto.Title;
        quiz.Description      = dto.Description;
        quiz.TimeLimitMinutes = dto.TimeLimitMinutes;
        quiz.PassingScore     = dto.PassingScore;
        quiz.MaxAttempts      = dto.MaxAttempts;
        quiz.QuestionsJson    = JsonSerializer.Serialize(dto.Questions);

        var updated = await _repo.Update(quiz);
        return MapToResponse(updated);
    }

    public async Task DeleteQuiz(int quizId)
    {
        var quiz = await _repo.FindByQuizId(quizId)
            ?? throw new KeyNotFoundException($"Quiz {quizId} not found.");
        await _repo.Delete(quizId);
    }

    public async Task PublishQuiz(int quizId)
    {
        var quiz = await _repo.FindByQuizId(quizId)
            ?? throw new KeyNotFoundException($"Quiz {quizId} not found.");
        quiz.IsPublished = true;
        await _repo.Update(quiz);
    }

    // Return questions WITHOUT correct answers — for student quiz page
    public async Task<IList<StudentQuestionDto>> GetQuestionsForStudent(int quizId)
    {
        var quiz = await _repo.FindByQuizId(quizId)
            ?? throw new KeyNotFoundException($"Quiz {quizId} not found.");

        var questions = JsonSerializer.Deserialize<List<QuestionDto>>(quiz.QuestionsJson)
                        ?? new List<QuestionDto>();

        // Strip correct answers before sending to student
        return questions.Select(q => new StudentQuestionDto
        {
            Id       = q.Id,
            Question = q.Question,
            Options  = q.Options
            // CorrectAnswer intentionally not included
        }).ToList();
    }

    /// <summary>
    /// StartAttempt — checks CountAttempts less than MaxAttempts before creating.
    /// Throws if student has exhausted all attempts.
    /// </summary>
    public async Task<AttemptResponseDto> StartAttempt(int quizId, int studentId)
    {
        var quiz = await _repo.FindByQuizId(quizId)
            ?? throw new KeyNotFoundException($"Quiz {quizId} not found.");

        // MaxAttempts guard — prevent exceeding allowed retakes
        var attemptCount = await _repo.CountAttempts(studentId, quizId);
        if (attemptCount >= quiz.MaxAttempts)
            throw new InvalidOperationException(
                $"Maximum attempts ({quiz.MaxAttempts}) reached for this quiz.");

        var attempt = new QuizAttempt
        {
            QuizId    = quizId,
            StudentId = studentId,
            StartedAt = DateTime.UtcNow,
            Score     = 0,
            IsPassed  = false,
            AnswersJson = "{}"
        };

        var created = await _repo.CreateAttempt(attempt);
        _logger.LogInformation("Student {StudentId} started quiz {QuizId} (attempt {Count}/{Max})",
            studentId, quizId, attemptCount + 1, quiz.MaxAttempts);

        return MapAttemptToResponse(created, quiz.PassingScore);
    }

    /// <summary>
    /// SubmitAttempt — computes score from submitted answers vs correct answers in DB.
    /// Sets IsPassed = Score >= Quiz.PassingScore.
    /// Answers stored as JSON via System.Text.Json.
    /// </summary>
    public async Task<AttemptResponseDto> SubmitAttempt(int attemptId, SubmitAttemptDto dto)
    {
        var attempt = await _repo.FindAttemptById(attemptId)
            ?? throw new KeyNotFoundException($"Attempt {attemptId} not found.");

        if (attempt.SubmittedAt != null)
            throw new InvalidOperationException("This attempt has already been submitted.");

        var quiz = await _repo.FindByQuizId(attempt.QuizId)
            ?? throw new KeyNotFoundException("Quiz not found.");

        // Deserialize correct answers from DB
        var questions = JsonSerializer.Deserialize<List<QuestionDto>>(quiz.QuestionsJson)
                        ?? new List<QuestionDto>();

        // Compute score: correct answers / total questions * 100
        int correct = 0;
        foreach (var question in questions)
        {
            if (dto.Answers.TryGetValue(question.Id, out var studentAnswer))
            {
                if (string.Equals(studentAnswer, question.CorrectAnswer,
                    StringComparison.OrdinalIgnoreCase))
                    correct++;
            }
        }

        var score = questions.Count > 0
            ? (int)Math.Round((double)correct / questions.Count * 100)
            : 0;

        // Persist results
        attempt.Score       = score;
        attempt.IsPassed    = score >= quiz.PassingScore;
        attempt.SubmittedAt = DateTime.UtcNow;
        attempt.AnswersJson = JsonSerializer.Serialize(dto.Answers);

        var updated = await _repo.UpdateAttempt(attempt);
        _logger.LogInformation("Student {StudentId} scored {Score}% on quiz {QuizId} — {Result}",
            attempt.StudentId, score, attempt.QuizId, attempt.IsPassed ? "PASSED" : "FAILED");

        return MapAttemptToResponse(updated, quiz.PassingScore);
    }

    public async Task<IList<AttemptResponseDto>> GetAttemptsByStudent(int studentId, int quizId)
    {
        var quiz = await _repo.FindByQuizId(quizId);
        var attempts = await _repo.FindAttemptsByStudentAndQuiz(studentId, quizId);
        return attempts.Select(a => MapAttemptToResponse(a, quiz?.PassingScore ?? 70)).ToList();
    }

    public async Task<AttemptResponseDto?> GetBestAttempt(int studentId, int quizId)
    {
        var quiz    = await _repo.FindByQuizId(quizId);
        var attempt = await _repo.FindBestAttempt(studentId, quizId);
        return attempt == null ? null : MapAttemptToResponse(attempt, quiz?.PassingScore ?? 70);
    }

    public async Task<int> GetAttemptCount(int studentId, int quizId)
        => await _repo.CountAttempts(studentId, quizId);

    // ── Private Helpers 

    private static QuizResponseDto MapToResponse(Quiz q) => new()
    {
        QuizId           = q.QuizId,
        CourseId         = q.CourseId,
        LessonId         = q.LessonId,
        Title            = q.Title,
        Description      = q.Description,
        TimeLimitMinutes = q.TimeLimitMinutes,
        PassingScore     = q.PassingScore,
        MaxAttempts      = q.MaxAttempts,
        IsPublished      = q.IsPublished,
        QuestionCount    = JsonSerializer.Deserialize<List<object>>(q.QuestionsJson)?.Count ?? 0,
        CreatedAt        = q.CreatedAt
    };

    private static AttemptResponseDto MapAttemptToResponse(QuizAttempt a, int passingScore) => new()
    {
        AttemptId    = a.AttemptId,
        QuizId       = a.QuizId,
        StudentId    = a.StudentId,
        Score        = a.Score,
        IsPassed     = a.IsPassed,
        PassingScore = passingScore,
        StartedAt    = a.StartedAt,
        SubmittedAt  = a.SubmittedAt
    };
}