namespace EduLearn.AssessmentService.Entities;

// QuizAttempt entity — records each student quiz submission
// Answers stored as JSON: {"1":"optionA","2":"optionC"} — QuestionId → selected answer
// Score computed by SubmitAttempt() by comparing with correct answers in Quiz.QuestionsJson
public class QuizAttempt
{
    public int AttemptId { get; set; }

    // FK to Quiz
    public int QuizId { get; set; }

    // FK to User in AuthService (different DB)
    public int StudentId { get; set; }

    // Score percentage (0-100) — computed on submission
    public int Score { get; set; } = 0;

    // true when Score >= Quiz.PassingScore
    public bool IsPassed { get; set; } = false;

    // Set when StartAttempt() is called
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    // Set when SubmitAttempt() is called — null if still in progress
    public DateTime? SubmittedAt { get; set; }

    // Student answers as JSON — Dictionary<int,string> serialized via System.Text.Json
    // Format: {"1":"A","2":"C","3":"B"}
    public string AnswersJson { get; set; } = "{}";

    // Navigation property
    public Quiz? Quiz { get; set; }
}