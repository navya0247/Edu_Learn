namespace EduLearn.ProgressService.Entities;

// LessonProgress — tracks which lessons a student has completed
// One record per student per lesson
// Used to compute overall course progress % in EnrollmentService
public class LessonProgress
{
    public int LessonProgressId { get; set; }

    // FK to User in AuthService (different DB)
    public int StudentId { get; set; }

    // FK to Lesson in LessonService (different DB)
    public int LessonId { get; set; }

    // FK to Course in CourseService (different DB)
    public int CourseId { get; set; }

    // true = student marked this lesson as complete
    public bool IsCompleted { get; set; } = false;

    // When student first opened/watched this lesson
    public DateTime? StartedAt { get; set; }

    // When student marked lesson as complete
    public DateTime? CompletedAt { get; set; }

    // How far through video student watched (0-100%)
    public int WatchPercent { get; set; } = 0;
}