using EduLearn.ReviewService.Data;
using EduLearn.ReviewService.Entities;
using EduLearn.ReviewService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.ReviewService.Repositories;

// EF Core implementation of IReviewRepository
public class ReviewRepository : IReviewRepository
{
    private readonly ReviewDbContext _db;

    public ReviewRepository(ReviewDbContext db) => _db = db;

    public async Task<Review?> FindByReviewId(int reviewId)
        => await _db.Reviews.FindAsync(reviewId);

    // Duplicate check — one review per student per course
    public async Task<Review?> FindByStudentAndCourse(int studentId, int courseId)
        => await _db.Reviews
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.CourseId == courseId);

    // All reviews for a course (admin and instructor view)
    public async Task<IList<Review>> FindByCourseId(int courseId)
        => await _db.Reviews
            .Where(r => r.CourseId == courseId && !r.IsHidden)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    // Only approved reviews — public course detail page
    public async Task<IList<Review>> FindApprovedByCourseId(int courseId)
        => await _db.Reviews
            .Where(r => r.CourseId == courseId && r.IsApproved && !r.IsHidden)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<IList<Review>> FindByStudentId(int studentId)
        => await _db.Reviews
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    // Admin moderation queue — pending approval
    public async Task<IList<Review>> FindPendingApproval()
        => await _db.Reviews
            .Where(r => !r.IsApproved && !r.IsHidden)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();

    // Average star rating — approved reviews only
    public async Task<double> GetAverageRating(int courseId)
    {
        var ratings = await _db.Reviews
            .Where(r => r.CourseId == courseId && r.IsApproved && !r.IsHidden)
            .Select(r => r.Rating)
            .ToListAsync();

        return ratings.Any() ? Math.Round(ratings.Average(), 1) : 0;
    }

    public async Task<int> GetTotalReviews(int courseId)
        => await _db.Reviews
            .CountAsync(r => r.CourseId == courseId && r.IsApproved && !r.IsHidden);

    public async Task<Review> Create(Review review)
    {
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();
        return review;
    }

    public async Task<Review> Update(Review review)
    {
        review.UpdatedAt = DateTime.UtcNow;
        _db.Reviews.Update(review);
        await _db.SaveChangesAsync();
        return review;
    }

    public async Task Delete(int reviewId)
        => await _db.Reviews.Where(r => r.ReviewId == reviewId).ExecuteDeleteAsync();
}