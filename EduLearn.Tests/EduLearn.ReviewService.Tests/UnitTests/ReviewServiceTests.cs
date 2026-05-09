using EduLearn.ReviewService.DTOs;
using EduLearn.ReviewService.Entities;
using EduLearn.ReviewService.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

// Alias to avoid namespace vs class name conflict
using ReviewServiceClass = EduLearn.ReviewService.Services.ReviewService;

namespace EduLearn.ReviewService.Tests.UnitTests;

/// <summary>
/// Unit tests for ReviewService.
/// Moq fakes the repository — no real database needed.
/// Total: 11 tests
/// </summary>
[TestFixture]
public class ReviewServiceTests
{
    private ReviewServiceClass _reviewService = null!;
    private Mock<IReviewRepository> _mockRepo = null!;

    // Sample review reused across tests
    private Review _sampleReview = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IReviewRepository>();
        var logger = new Mock<ILogger<ReviewServiceClass>>().Object;

        _reviewService = new ReviewServiceClass(_mockRepo.Object, logger);

        // Sample approved review
        _sampleReview = new Review
        {
            ReviewId   = 1,
            StudentId  = 5,
            CourseId   = 10,
            Rating     = 4,
            Comment    = "Great course!",
            IsApproved = false,
            IsHidden   = false,
            CreatedAt  = DateTime.UtcNow,
            UpdatedAt  = DateTime.UtcNow
        };
    }

    // ── TEST 1: Submit Review ─────────────────────────────────────────────────

    [Test]
    [Description("SubmitReview should create review with IsApproved = false (needs admin approval)")]
    public async Task SubmitReview_ValidRating_CreatesUnapprovedReview()
    {
        // Arrange — no existing review by this student for this course
        _mockRepo.Setup(r => r.FindByStudentAndCourse(5, 10))
                 .ReturnsAsync((Review?)null);
        _mockRepo.Setup(r => r.Create(It.IsAny<Review>()))
                 .ReturnsAsync((Review r) => { r.ReviewId = 1; return r; });

        var dto = new CreateReviewDto { CourseId = 10, Rating = 4, Comment = "Great course!" };

        // Act
        var result = await _reviewService.SubmitReview(studentId: 5, dto: dto);

        // Assert
        result.Should().NotBeNull();
        result.Rating.Should().Be(4);
        result.IsApproved.Should().BeFalse("new reviews need admin approval first");
        result.StudentId.Should().Be(5);
        result.CourseId.Should().Be(10);
    }

    // ── TEST 2: Submit Review - Duplicate Prevention ───────────────────────────

    [Test]
    [Description("SubmitReview should throw if student already reviewed this course")]
    public async Task SubmitReview_AlreadyReviewed_ThrowsInvalidOperation()
    {
        // Arrange — student already has a review for this course
        _mockRepo.Setup(r => r.FindByStudentAndCourse(5, 10))
                 .ReturnsAsync(_sampleReview);

        var dto = new CreateReviewDto { CourseId = 10, Rating = 5, Comment = "Second review attempt" };

        // Act & Assert
        var act = async () => await _reviewService.SubmitReview(studentId: 5, dto: dto);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*already reviewed*");
    }

    // ── TEST 3: Submit Review - Invalid Rating ─────────────────────────────────

    [Test]
    [Description("SubmitReview with rating outside 1-5 should throw ArgumentException")]
    public async Task SubmitReview_InvalidRating_ThrowsArgumentException()
    {
        // Arrange — no existing review
        _mockRepo.Setup(r => r.FindByStudentAndCourse(5, 10))
                 .ReturnsAsync((Review?)null);

        // Rating 6 is outside allowed range 1-5
        var dto = new CreateReviewDto { CourseId = 10, Rating = 6, Comment = "Bad rating value" };

        // Act & Assert
        var act = async () => await _reviewService.SubmitReview(studentId: 5, dto: dto);

        await act.Should().ThrowAsync<ArgumentException>()
                 .WithMessage("*between 1 and 5*");
    }

    // ── TEST 4: Approve Review ────────────────────────────────────────────────

    [Test]
    [Description("ApproveReview should set IsApproved = true and IsHidden = false")]
    public async Task ApproveReview_PendingReview_SetsApprovedTrue()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByReviewId(1)).ReturnsAsync(_sampleReview);
        _mockRepo.Setup(r => r.Update(It.IsAny<Review>())).ReturnsAsync((Review r) => r);

        // Act — admin approves the review
        await _reviewService.ApproveReview(reviewId: 1);

        // Assert — IsApproved must be true
        _mockRepo.Verify(r => r.Update(It.Is<Review>(rv =>
            rv.IsApproved == true && rv.IsHidden == false)), Times.Once,
            "ApproveReview must set IsApproved=true and IsHidden=false");
    }

    // ── TEST 5: Hide Review ───────────────────────────────────────────────────

    [Test]
    [Description("HideReview should set IsHidden = true (soft delete — data preserved)")]
    public async Task HideReview_ApprovedReview_SetsHiddenTrue()
    {
        // Arrange — review is approved and visible
        _sampleReview.IsApproved = true;
        _mockRepo.Setup(r => r.FindByReviewId(1)).ReturnsAsync(_sampleReview);
        _mockRepo.Setup(r => r.Update(It.IsAny<Review>())).ReturnsAsync((Review r) => r);

        // Act — admin hides the review
        await _reviewService.HideReview(reviewId: 1);

        // Assert
        _mockRepo.Verify(r => r.Update(It.Is<Review>(rv => rv.IsHidden == true)), Times.Once,
            "HideReview must set IsHidden = true");
    }

    // ── TEST 6: Update Review - Own Review ───────────────────────────────────

    [Test]
    [Description("UpdateReview should update comment and reset approval for re-moderation")]
    public async Task UpdateReview_OwnReview_UpdatesAndResetsApproval()
    {
        // Arrange — review was already approved
        _sampleReview.IsApproved = true;
        _mockRepo.Setup(r => r.FindByReviewId(1)).ReturnsAsync(_sampleReview);
        _mockRepo.Setup(r => r.Update(It.IsAny<Review>())).ReturnsAsync((Review r) => r);

        var dto = new UpdateReviewDto { Rating = 5, Comment = "Updated — even better course!" };

        // Act — student 5 updates their own review
        var result = await _reviewService.UpdateReview(reviewId: 1, studentId: 5, dto: dto);

        // Assert
        result.Rating.Should().Be(5);
        result.IsApproved.Should().BeFalse("edited review must be re-approved by admin");
    }

    // ── TEST 7: Update Review - Wrong Student ─────────────────────────────────

    [Test]
    [Description("UpdateReview by a different student should throw UnauthorizedAccessException")]
    public async Task UpdateReview_WrongStudent_ThrowsUnauthorized()
    {
        // Arrange — review belongs to student 5, but student 99 tries to update
        _mockRepo.Setup(r => r.FindByReviewId(1)).ReturnsAsync(_sampleReview);

        var dto = new UpdateReviewDto { Rating = 1, Comment = "Trying to vandalize review" };

        // Act & Assert
        var act = async () => await _reviewService.UpdateReview(reviewId: 1, studentId: 99, dto: dto);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
                 .WithMessage("*another student's review*");
    }

    // ── TEST 8: Delete Review - By Owner ─────────────────────────────────────

    [Test]
    [Description("DeleteReview should succeed when student deletes their own review")]
    public async Task DeleteReview_OwnReview_DeletesSuccessfully()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByReviewId(1)).ReturnsAsync(_sampleReview);
        _mockRepo.Setup(r => r.Delete(1)).Returns(Task.CompletedTask);

        // Act — student 5 deletes their own review
        var act = async () => await _reviewService.DeleteReview(reviewId: 1, studentId: 5, isAdmin: false);

        // Assert — no exception
        await act.Should().NotThrowAsync();
        _mockRepo.Verify(r => r.Delete(1), Times.Once);
    }

    // ── TEST 9: Delete Review - By Admin ─────────────────────────────────────

    [Test]
    [Description("DeleteReview by admin should succeed even for another student's review")]
    public async Task DeleteReview_ByAdmin_DeletesAnyReview()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByReviewId(1)).ReturnsAsync(_sampleReview);
        _mockRepo.Setup(r => r.Delete(1)).Returns(Task.CompletedTask);

        // Act — admin (studentId = 99 does not matter when isAdmin = true)
        var act = async () => await _reviewService.DeleteReview(reviewId: 1, studentId: 99, isAdmin: true);

        // Assert — admin can delete any review
        await act.Should().NotThrowAsync("admin can delete any review regardless of owner");
        _mockRepo.Verify(r => r.Delete(1), Times.Once);
    }

    // ── TEST 10: Get Rating Summary ───────────────────────────────────────────

    [Test]
    [Description("GetRatingSummary should correctly count star distribution and average")]
    public async Task GetRatingSummary_CourseWithReviews_ReturnsCorrectSummary()
    {
        // Arrange — 4 approved reviews: two 5-stars, one 4-star, one 3-star
        var reviews = new List<Review>
        {
            new Review { ReviewId = 1, CourseId = 10, Rating = 5, IsApproved = true, StudentId = 1 },
            new Review { ReviewId = 2, CourseId = 10, Rating = 5, IsApproved = true, StudentId = 2 },
            new Review { ReviewId = 3, CourseId = 10, Rating = 4, IsApproved = true, StudentId = 3 },
            new Review { ReviewId = 4, CourseId = 10, Rating = 3, IsApproved = true, StudentId = 4 }
        };

        _mockRepo.Setup(r => r.FindApprovedByCourseId(10)).ReturnsAsync(reviews);
        _mockRepo.Setup(r => r.GetAverageRating(10)).ReturnsAsync(4.25); // (5+5+4+3)/4

        // Act
        var result = await _reviewService.GetRatingSummary(courseId: 10);

        // Assert
        result.TotalReviews.Should().Be(4);
        result.FiveStars.Should().Be(2, "two 5-star reviews");
        result.FourStars.Should().Be(1, "one 4-star review");
        result.ThreeStars.Should().Be(1, "one 3-star review");
        result.TwoStars.Should().Be(0);
        result.OneStar.Should().Be(0);
        result.AverageRating.Should().Be(4.25);
    }

    // ── TEST 11: Get Approved Reviews Only ────────────────────────────────────

    [Test]
    [Description("GetApprovedReviewsByCourse should return only approved and visible reviews")]
    public async Task GetApprovedReviewsByCourse_MixedReviews_ReturnsOnlyApproved()
    {
        // Arrange — 2 approved reviews (pending ones not in this list — repo filters them)
        var approvedReviews = new List<Review>
        {
            new Review { ReviewId = 1, CourseId = 10, Rating = 5, IsApproved = true,  IsHidden = false, StudentId = 1 },
            new Review { ReviewId = 2, CourseId = 10, Rating = 4, IsApproved = true,  IsHidden = false, StudentId = 2 }
        };

        _mockRepo.Setup(r => r.FindApprovedByCourseId(10)).ReturnsAsync(approvedReviews);

        // Act
        var result = await _reviewService.GetApprovedReviewsByCourse(courseId: 10);

        // Assert
        result.Should().HaveCount(2, "only approved reviews should be returned");
        result.Should().AllSatisfy(r => r.IsApproved.Should().BeTrue());
    }
}
