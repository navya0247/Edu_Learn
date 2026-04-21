namespace EduLearn.AssessmentService.Entities;

// Quiz entity — maps to Quizzes table in EduLearn_Assessment database
// Can be attached to a course OR a specific lesson
// Questions stored as JSON string — parsed by System.Text.Json
// PassingScore = minimum score % to pass (0-100)
// MaxAttempts = how many times student can retake the quiz
public class Quiz
{
    public int QuizId { get; set; }

    // FK to Course in CourseService (different DB)
    public int CourseId { get; set; }

    // Optional FK to Lesson — null means course-level quiz
    public int? LessonId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Time limit in minutes — 0 means no time limit
    public int TimeLimitMinutes { get; set; } = 0;

    // Minimum score to pass (0-100)
    public int PassingScore { get; set; } = 70;

    // Max retake attempts — checked by StartAttempt() before creating new attempt
    public int MaxAttempts { get; set; } = 3;

    // true = visible to enrolled students
    public bool IsPublished { get; set; } = false;

    // Questions stored as JSON: [{"id":1,"question":"...","options":["a","b"],"correct":"a"}]
    public string QuestionsJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}