using EduLearn.ReviewService.Entities;

namespace EduLearn.ReviewService.Interfaces;

// Repository interface — all DB operations for Review
public interface IReviewRepository
{
    Task<Review?> FindByReviewId(int reviewId);
    Task<Review?> FindByStudentAndCourse(int studentId, int courseId); // duplicate check
    Task<IList<Review>> FindByCourseId(int courseId);                  // public course reviews
    Task<IList<Review>> FindApprovedByCourseId(int courseId);          // only approved
    Task<IList<Review>> FindByStudentId(int studentId);                // student's reviews
    Task<IList<Review>> FindPendingApproval();                         // admin moderation queue
    Task<double> GetAverageRating(int courseId);                       // course star average
    Task<int> GetTotalReviews(int courseId);
    Task<Review> Create(Review review);
    Task<Review> Update(Review review);
    Task Delete(int reviewId);
}