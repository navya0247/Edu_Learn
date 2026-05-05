namespace EduLearn.LessonService.DTOs;

// ── REQUEST DTOs

// Used for POST /api/lessons and PUT /api/lessons/{id}
public class CreateLessonDto
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ContentType { get; set; } = "VIDEO"; // VIDEO|ARTICLE|PDF|QUIZ_LINK
    public string? ContentUrl { get; set; }
    public int DurationMinutes { get; set; } = 0;
    public int DisplayOrder { get; set; } = 0;
    public bool IsPreview { get; set; } = false;
}

// Used for PUT /api/lessons/reorder
public class ReorderLessonsDto
{
    public int CourseId { get; set; }

    // Ordered list of lesson IDs — new sequence [3,1,2] = lesson3 first, lesson1 second
    public List<int> LessonIds { get; set; } = new();
}

// ── RESPONSE DTOs 

// Full lesson details — includes SAS URL for content access
public class LessonResponseDto
{
    public int LessonId { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? ContentUrl { get; set; } // Time-limited SAS URL
    public int DurationMinutes { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPreview { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Light version for curriculum list — no content URL
public class LessonSummaryDto
{
    public int LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPreview { get; set; }
    public bool IsPublished { get; set; }
}