using EduLearn.ReviewService.DTOs;

namespace EduLearn.ReviewService.Interfaces;

// Service interface — all business logic for Review management
public interface IReviewService
{
    // Submit review — one per student per course (duplicate check)
    Task<ReviewResponseDto> SubmitReview(int studentId, CreateReviewDto dto);

    // Get single review
    Task<ReviewResponseDto?> GetReviewById(int reviewId);

    // All approved reviews for a course — shown on course detail page
    Task<IList<ReviewResponseDto>> GetApprovedReviewsByCourse(int courseId);

    // All reviews for a course — Instructor and Admin see all
    Task<IList<ReviewResponseDto>> GetAllReviewsByCourse(int courseId);

    // Course rating summary — average + star distribution
    Task<CourseRatingSummaryDto> GetRatingSummary(int courseId);

    // All reviews by a student
    Task<IList<ReviewResponseDto>> GetReviewsByStudent(int studentId);

    // Update own review — student only
    Task<ReviewResponseDto> UpdateReview(int reviewId, int studentId, UpdateReviewDto dto);

    // Delete own review — student or admin
    Task DeleteReview(int reviewId, int studentId, bool isAdmin);

    // Admin: approve review — makes visible on course page
    Task ApproveReview(int reviewId);

    // Admin: hide review — soft delete without removing data
    Task HideReview(int reviewId);

    // Admin: all pending reviews waiting for approval
    Task<IList<ReviewResponseDto>> GetPendingReviews();
}