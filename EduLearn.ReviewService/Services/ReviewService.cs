using EduLearn.ReviewService.DTOs;
using EduLearn.ReviewService.Entities;
using EduLearn.ReviewService.Interfaces;

namespace EduLearn.ReviewService.Services;

// Business logic for course review and rating management
public class ReviewService : IReviewService
{
    private readonly IReviewRepository _repo;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(IReviewRepository repo, ILogger<ReviewService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    // Submit review — validates rating 1-5 and prevents duplicates
    public async Task<ReviewResponseDto> SubmitReview(int studentId, CreateReviewDto dto)
    {
        // Rating must be 1-5
        if (dto.Rating < 1 || dto.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        // One review per student per course
        var existing = await _repo.FindByStudentAndCourse(studentId, dto.CourseId);
        if (existing != null)
            throw new InvalidOperationException("You have already reviewed this course.");

        var review = new Review
        {
            StudentId  = studentId,
            CourseId   = dto.CourseId,
            Rating     = dto.Rating,
            Comment    = dto.Comment,
            IsApproved = false,  // Admin must approve first
            IsHidden   = false,
            CreatedAt  = DateTime.UtcNow,
            UpdatedAt  = DateTime.UtcNow
        };

        var created = await _repo.Create(review);
        _logger.LogInformation("Review submitted by Student {StudentId} for Course {CourseId} — Rating: {Rating}",
            studentId, dto.CourseId, dto.Rating);

        return MapToResponse(created);
    }

    public async Task<ReviewResponseDto?> GetReviewById(int reviewId)
    {
        var review = await _repo.FindByReviewId(reviewId);
        return review == null ? null : MapToResponse(review);
    }

    // Only approved and not hidden — for public course detail page
    public async Task<IList<ReviewResponseDto>> GetApprovedReviewsByCourse(int courseId)
    {
        var reviews = await _repo.FindApprovedByCourseId(courseId);
        return reviews.Select(MapToResponse).ToList();
    }

    // All reviews including pending — instructor/admin dashboard
    public async Task<IList<ReviewResponseDto>> GetAllReviewsByCourse(int courseId)
    {
        var reviews = await _repo.FindByCourseId(courseId);
        return reviews.Select(MapToResponse).ToList();
    }

    // Course rating summary with star distribution
    public async Task<CourseRatingSummaryDto> GetRatingSummary(int courseId)
    {
        var reviews = await _repo.FindApprovedByCourseId(courseId);
        var average = await _repo.GetAverageRating(courseId);

        return new CourseRatingSummaryDto
        {
            CourseId      = courseId,
            AverageRating = average,
            TotalReviews  = reviews.Count,
            FiveStars     = reviews.Count(r => r.Rating == 5),
            FourStars     = reviews.Count(r => r.Rating == 4),
            ThreeStars    = reviews.Count(r => r.Rating == 3),
            TwoStars      = reviews.Count(r => r.Rating == 2),
            OneStar       = reviews.Count(r => r.Rating == 1)
        };
    }

    public async Task<IList<ReviewResponseDto>> GetReviewsByStudent(int studentId)
    {
        var reviews = await _repo.FindByStudentId(studentId);
        return reviews.Select(MapToResponse).ToList();
    }

    // Update own review — resets approval (admin must re-review)
    public async Task<ReviewResponseDto> UpdateReview(int reviewId, int studentId, UpdateReviewDto dto)
    {
        var review = await _repo.FindByReviewId(reviewId)
            ?? throw new KeyNotFoundException($"Review {reviewId} not found.");

        if (review.StudentId != studentId)
            throw new UnauthorizedAccessException("Cannot update another student's review.");

        if (dto.Rating < 1 || dto.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        review.Rating     = dto.Rating;
        review.Comment    = dto.Comment;
        review.IsApproved = false;  // Reset approval after edit

        var updated = await _repo.Update(review);
        return MapToResponse(updated);
    }

    // Delete review — student can delete own, admin can delete any
    public async Task DeleteReview(int reviewId, int studentId, bool isAdmin)
    {
        var review = await _repo.FindByReviewId(reviewId)
            ?? throw new KeyNotFoundException($"Review {reviewId} not found.");

        if (!isAdmin && review.StudentId != studentId)
            throw new UnauthorizedAccessException("Cannot delete another student's review.");

        await _repo.Delete(reviewId);
        _logger.LogWarning("Review {ReviewId} deleted", reviewId);
    }

    // Admin: approve review — makes visible on course page
    public async Task ApproveReview(int reviewId)
    {
        var review = await _repo.FindByReviewId(reviewId)
            ?? throw new KeyNotFoundException($"Review {reviewId} not found.");

        review.IsApproved = true;
        review.IsHidden   = false;
        await _repo.Update(review);
        _logger.LogInformation("Review {ReviewId} approved by admin", reviewId);
    }

    // Admin: hide review — soft delete, data preserved
    public async Task HideReview(int reviewId)
    {
        var review = await _repo.FindByReviewId(reviewId)
            ?? throw new KeyNotFoundException($"Review {reviewId} not found.");

        review.IsHidden = true;
        await _repo.Update(review);
        _logger.LogWarning("Review {ReviewId} hidden by admin", reviewId);
    }

    // Admin moderation queue — pending approval
    public async Task<IList<ReviewResponseDto>> GetPendingReviews()
    {
        var reviews = await _repo.FindPendingApproval();
        return reviews.Select(MapToResponse).ToList();
    }

    // ── Private Helper 
    private static ReviewResponseDto MapToResponse(Review r) => new()
    {
        ReviewId  = r.ReviewId,
        StudentId = r.StudentId,
        CourseId  = r.CourseId,
        Rating    = r.Rating,
        Comment   = r.Comment,
        IsApproved = r.IsApproved,
        IsHidden  = r.IsHidden,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };
}