namespace EduLearn.CourseService.Entities;

/// <summary>
/// Course entity — maps to [Courses] table in EduLearn_Course PostgreSQL database.
/// Two-step publish workflow (PDF requirement):
///   Step 1: Instructor → PublishCourse() → IsPublished = true
///   Step 2: Admin → ApproveCourse() → IsApproved = true
/// Both flags must be true for course to appear in public catalogue.
/// </summary>
public class Course
{
    // Primary key — auto increment by PostgreSQL
    public int CourseId { get; set; }

    // Course title shown in catalogue and search results
    public string Title { get; set; } = string.Empty;

    // Full description shown on course detail page
    public string Description { get; set; } = string.Empty;

    // FK to User.UserId in AuthService (different DB — no navigation property)
    public int InstructorId { get; set; }

    // Category for filtering: Programming, Design, Business etc.
    public string Category { get; set; } = string.Empty;

    // Difficulty level: Beginner | Intermediate | Advanced
    public string Level { get; set; } = "Beginner";

    // Language of instruction: English, Hindi etc.
    public string Language { get; set; } = "English";

    // 0 = free, >0 = paid course price
    public decimal Price { get; set; } = 0;

    // Thumbnail image URL — stored in Azure Blob Storage
    public string? ThumbnailUrl { get; set; }

    // STEP 1: Instructor marks course ready for admin review
    public bool IsPublished { get; set; } = false;

    // STEP 2: Admin approves — course now visible in public catalogue
    public bool IsApproved { get; set; } = false;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Total content duration in minutes
    public int TotalDuration { get; set; } = 0;

    // Atomically incremented via ExecuteUpdateAsync on every new enrollment
    public int EnrollmentCount { get; set; } = 0;
}