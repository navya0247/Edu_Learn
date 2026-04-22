namespace EduLearn.ReviewService.Entities;

// Review entity — maps to Reviews table in EduLearn_Review PostgreSQL database
// One review per student per course (unique index enforced)
// Rating: 1-5 stars
// IsApproved: Admin must approve before review shows publicly
// IsHidden: Admin can hide inappropriate reviews without deleting
public class Review
{
    public int ReviewId { get; set; }

    // FK to User in AuthService (different DB)
    public int StudentId { get; set; }

    // FK to Course in CourseService (different DB)
    public int CourseId { get; set; }

    // 1-5 star rating
    public int Rating { get; set; }

    // Written review text — optional
    public string? Comment { get; set; }

    // Admin must approve before showing publicly
    public bool IsApproved { get; set; } = false;

    // Admin can hide without deleting — soft delete
    public bool IsHidden { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}