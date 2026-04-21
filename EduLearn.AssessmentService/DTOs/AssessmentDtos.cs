namespace EduLearn.AssessmentService.DTOs;

// ── REQUEST DTOs ──────────────────────────────────────────────────────────────

// Question model inside Quiz
public class QuestionDto
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();  // answer choices
    public string CorrectAnswer { get; set; } = string.Empty; // not sent to student
}

// POST /api/quizzes — create new quiz
public class CreateQuizDto
{
    public int CourseId { get; set; }
    public int? LessonId { get; set; }  // null = course-level quiz
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TimeLimitMinutes { get; set; } = 0;
    public int PassingScore { get; set; } = 70;
    public int MaxAttempts { get; set; } = 3;
    public List<QuestionDto> Questions { get; set; } = new();
}

// PUT /api/quizzes/{id}/attempt/submit — student submits answers
public class SubmitAttemptDto
{
    // QuestionId → selected answer: {"1":"A","2":"C"}
    public Dictionary<int, string> Answers { get; set; } = new();
}

// ── RESPONSE DTOs ─────────────────────────────────────────────────────────────

// Quiz details — correct answers excluded for students
public class QuizResponseDto
{
    public int QuizId { get; set; }
    public int CourseId { get; set; }
    public int? LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TimeLimitMinutes { get; set; }
    public int PassingScore { get; set; }
    public int MaxAttempts { get; set; }
    public bool IsPublished { get; set; }
    public int QuestionCount { get; set; }  // count only — no correct answers exposed
    public DateTime CreatedAt { get; set; }
}

// Student-facing question — correct answer hidden
public class StudentQuestionDto
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    // CorrectAnswer intentionally excluded
}

// Attempt result returned after submission
public class AttemptResponseDto
{
    public int AttemptId { get; set; }
    public int QuizId { get; set; }
    public int StudentId { get; set; }
    public int Score { get; set; }          // 0-100
    public bool IsPassed { get; set; }
    public int PassingScore { get; set; }   // so student knows the threshold
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
}