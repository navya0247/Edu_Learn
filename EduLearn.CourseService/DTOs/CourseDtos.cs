namespace EduLearn.CourseService.DTOs;

// ── REQUEST DTOs ──────────────────────────────────────────────────────────────

/// <summary>
/// POST /api/courses — Create new course
/// PUT /api/courses/{id} — Update existing course
/// </summary>
public class CreateCourseDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    // Beginner | Intermediate | Advanced
    public string Level { get; set; } = "Beginner";
    public string Language { get; set; } = "English";

    // 0 = free course
    public decimal Price { get; set; } = 0;
    public string? ThumbnailUrl { get; set; }
}

// ── RESPONSE DTOs ─────────────────────────────────────────────────────────────

/// <summary>
/// Full course details — returned for single course GET.
/// </summary>
public class CourseResponseDto
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int InstructorId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsPublished { get; set; }
    public bool IsApproved { get; set; }
    public int EnrollmentCount { get; set; }
    public int TotalDuration { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Light version — returned for course list/catalogue endpoints.
/// Less data = faster response for lists.
/// </summary>
public class CourseSummaryDto
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int EnrollmentCount { get; set; }
    public int InstructorId { get; set; }
    public bool IsPublished { get; set; }
    public bool IsApproved { get; set; }
}