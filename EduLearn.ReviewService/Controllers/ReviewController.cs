using EduLearn.ReviewService.DTOs;
using EduLearn.ReviewService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduLearn.ReviewService.Controllers;

[ApiController]
[Route("api/reviews")]
[Produces("application/json")]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly ILogger<ReviewController> _logger;

    public ReviewController(IReviewService reviewService, ILogger<ReviewController> logger)
    {
        _reviewService = reviewService;
        _logger        = logger;
    }

    // ── Public Endpoints 

    /// <summary>Get all approved reviews for a course — public, no login required</summary>
    [HttpGet("course/{courseId}")]
    [ProducesResponseType(typeof(IList<ReviewResponseDto>), 200)]
    public async Task<IActionResult> GetApprovedByCourse(int courseId)
    {
        var reviews = await _reviewService.GetApprovedReviewsByCourse(courseId);
        return Ok(reviews);
    }

    /// <summary>Get course rating summary — average stars and distribution</summary>
    [HttpGet("course/{courseId}/summary")]
    [ProducesResponseType(typeof(CourseRatingSummaryDto), 200)]
    public async Task<IActionResult> GetRatingSummary(int courseId)
    {
        var summary = await _reviewService.GetRatingSummary(courseId);
        return Ok(summary);
    }

    // ── Student Endpoints 

    /// <summary>Submit a review for a course — Student only, one per course</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpPost]
    [ProducesResponseType(typeof(ReviewResponseDto), 201)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Submit([FromBody] CreateReviewDto dto)
    {
        try
        {
            var result = await _reviewService.SubmitReview(GetCurrentUserId(), dto);
            return CreatedAtAction(nameof(GetById), new { id = result.ReviewId }, result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Get all reviews submitted by current student</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpGet("my-reviews")]
    [ProducesResponseType(typeof(IList<ReviewResponseDto>), 200)]
    public async Task<IActionResult> GetMyReviews()
    {
        var reviews = await _reviewService.GetReviewsByStudent(GetCurrentUserId());
        return Ok(reviews);
    }

    /// <summary>Update own review — resets admin approval after edit</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ReviewResponseDto), 200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReviewDto dto)
    {
        try
        {
            var updated = await _reviewService.UpdateReview(id, GetCurrentUserId(), dto);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Delete own review — Student deletes own, Admin deletes any</summary>
    [Authorize]
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var isAdmin = User.IsInRole("ADMIN");
            await _reviewService.DeleteReview(id, GetCurrentUserId(), isAdmin);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // ── Shared Endpoints

    /// <summary>Get review by ID</summary>
    [Authorize]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReviewResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        var review = await _reviewService.GetReviewById(id);
        if (review == null) return NotFound(new { message = "Review not found" });
        return Ok(review);
    }

    // ── Instructor / Admin Endpoints 

    /// <summary>Get all reviews for a course including pending — Instructor or Admin</summary>
    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpGet("course/{courseId}/all")]
    [ProducesResponseType(typeof(IList<ReviewResponseDto>), 200)]
    public async Task<IActionResult> GetAllByCourse(int courseId)
    {
        var reviews = await _reviewService.GetAllReviewsByCourse(courseId);
        return Ok(reviews);
    }

    /// <summary>Get all reviews by a student — Admin only</summary>
    [Authorize(Roles = "ADMIN")]
    [HttpGet("student/{studentId}")]
    [ProducesResponseType(typeof(IList<ReviewResponseDto>), 200)]
    public async Task<IActionResult> GetByStudent(int studentId)
    {
        var reviews = await _reviewService.GetReviewsByStudent(studentId);
        return Ok(reviews);
    }

    /// <summary>Get all pending reviews waiting for approval — Admin only</summary>
    [Authorize(Roles = "ADMIN")]
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IList<ReviewResponseDto>), 200)]
    public async Task<IActionResult> GetPending()
    {
        var reviews = await _reviewService.GetPendingReviews();
        return Ok(reviews);
    }

    /// <summary>Approve review — makes visible on course page — Admin only</summary>
    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id}/approve")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Approve(int id)
    {
        try
        {
            await _reviewService.ApproveReview(id);
            return Ok(new { message = "Review approved and now visible" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    /// <summary>Hide review without deleting — soft delete — Admin only</summary>
    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id}/hide")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Hide(int id)
    {
        try
        {
            await _reviewService.HideReview(id);
            return Ok(new { message = "Review hidden successfully" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // ── Helper 
    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }
}