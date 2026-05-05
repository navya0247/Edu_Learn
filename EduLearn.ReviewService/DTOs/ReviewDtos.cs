namespace EduLearn.ReviewService.DTOs;

// ── REQUEST DTOs 

// POST /api/reviews — submit a review
public class CreateReviewDto
{
    public int CourseId { get; set; }
    public int Rating { get; set; }       // 1-5
    public string? Comment { get; set; }
}

// PUT /api/reviews/{id} — update own review
public class UpdateReviewDto
{
    public int Rating { get; set; }       // 1-5
    public string? Comment { get; set; }
}

//  RESPONSE DTOs 

// Full review details
public class ReviewResponseDto
{
    public int ReviewId { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }
    public bool IsHidden { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Course rating summary — shown on course detail page
public class CourseRatingSummaryDto
{
    public int CourseId { get; set; }
    public double AverageRating { get; set; }   
    public int TotalReviews { get; set; }
    public int FiveStars { get; set; }
    public int FourStars { get; set; }
    public int ThreeStars { get; set; }
    public int TwoStars { get; set; }
    public int OneStar { get; set; }
}