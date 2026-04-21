namespace EduLearn.LessonService.Entities;

// Lesson entity — maps to Lessons table in EduLearn_Lesson PostgreSQL database
// ContentType: VIDEO | ARTICLE | PDF | QUIZ_LINK
// IsPreview = true allows guests to view without enrollment (free trial)
// DisplayOrder controls the sequence in course curriculum
public class Lesson
{
    public int LessonId { get; set; }

    // FK to Course in CourseService (different DB — no navigation property)
    public int CourseId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // VIDEO | ARTICLE | PDF | QUIZ_LINK
    public string ContentType { get; set; } = "VIDEO";

    // Azure Blob SAS URL or external video link
    public string? ContentUrl { get; set; }

    public int DurationMinutes { get; set; } = 0;

    // Position in course curriculum (1, 2, 3...)
    public int DisplayOrder { get; set; } = 0;

    // true = guests can view without enrollment
    public bool IsPreview { get; set; } = false;

    // true = visible to enrolled students
    public bool IsPublished { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}