using EduLearn.EnrollmentService.DTOs;
using EduLearn.EnrollmentService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduLearn.EnrollmentService.Controllers;

[ApiController]
[Route("api/enrollments")]
[Produces("application/json")]
public class EnrollmentController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly ILogger<EnrollmentController> _logger;

    public EnrollmentController(IEnrollmentService enrollmentService, ILogger<EnrollmentController> logger)
    {
        _enrollmentService = enrollmentService;
        _logger            = logger;
    }

    // ── Student Endpoints ─────────────────────────────────────────────────────

    /// <summary>Enroll current student in a course — Student only</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpPost]
    [ProducesResponseType(typeof(EnrollmentResponseDto), 201)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Enroll([FromBody] EnrollRequestDto dto)
    {
        try
        {
            var result = await _enrollmentService.Enroll(GetCurrentUserId(), dto);
            _logger.LogInformation("Student {Id} enrolled in course {CourseId}",
                GetCurrentUserId(), dto.CourseId);
            return CreatedAtAction(nameof(GetById), new { id = result.EnrollmentId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Get all courses current student is enrolled in</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpGet("my-courses")]
    [ProducesResponseType(typeof(IList<EnrollmentResponseDto>), 200)]
    public async Task<IActionResult> GetMyCourses()
    {
        var enrollments = await _enrollmentService.GetEnrollmentsByStudent(GetCurrentUserId());
        return Ok(enrollments);
    }

    /// <summary>Get only completed courses for current student</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpGet("my-courses/completed")]
    [ProducesResponseType(typeof(IList<EnrollmentResponseDto>), 200)]
    public async Task<IActionResult> GetCompleted()
    {
        var enrollments = await _enrollmentService.GetCompletedCourses(GetCurrentUserId());
        return Ok(enrollments);
    }

    /// <summary>Get only in-progress courses for current student</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpGet("my-courses/in-progress")]
    [ProducesResponseType(typeof(IList<EnrollmentResponseDto>), 200)]
    public async Task<IActionResult> GetInProgress()
    {
        var enrollments = await _enrollmentService.GetInProgressCourses(GetCurrentUserId());
        return Ok(enrollments);
    }

    /// <summary>Drop a course — sets Status to DROPPED (Student only, own enrollments)</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpPut("{id}/drop")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Drop(int id)
    {
        try
        {
            await _enrollmentService.DropCourse(id, GetCurrentUserId());
            return Ok(new { message = "Course dropped successfully" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    /// <summary>Check if current student is enrolled in a course</summary>
    [Authorize]
    [HttpGet("is-enrolled/{courseId}")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> IsEnrolled(int courseId)
    {
        var enrolled = await _enrollmentService.IsEnrolled(GetCurrentUserId(), courseId);
        return Ok(new { isEnrolled = enrolled, courseId });
    }

    // ── Shared Endpoints ──────────────────────────────────────────────────────

    /// <summary>Get enrollment by ID</summary>
    [Authorize]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EnrollmentResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        var enrollment = await _enrollmentService.GetEnrollmentById(id);
        if (enrollment == null) return NotFound(new { message = "Enrollment not found" });
        return Ok(enrollment);
    }

    /// <summary>Update enrollment progress percent — called by ProgressService</summary>
    [Authorize]
    [HttpPut("{id}/progress")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateProgress(int id, [FromBody] UpdateProgressDto dto)
    {
        try
        {
            await _enrollmentService.UpdateProgress(id, dto.ProgressPercent);
            return Ok(new { message = "Progress updated", progressPercent = dto.ProgressPercent });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Mark enrollment as COMPLETED — called when all lessons done</summary>
    [Authorize]
    [HttpPut("{id}/complete")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Complete(int id)
    {
        try
        {
            await _enrollmentService.CompleteEnrollment(id);
            return Ok(new { message = "Enrollment completed — certificate eligible!" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── Instructor / Admin Endpoints 

    /// <summary>Get all enrollments for a course — Instructor or Admin</summary>
    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpGet("course/{courseId}")]
    [ProducesResponseType(typeof(IList<EnrollmentResponseDto>), 200)]
    public async Task<IActionResult> GetByCourse(int courseId)
    {
        var enrollments = await _enrollmentService.GetEnrollmentsByCourse(courseId);
        return Ok(enrollments);
    }

    /// <summary>Get enrollment analytics for a course — Instructor or Admin</summary>
    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpGet("course/{courseId}/analytics")]
    [ProducesResponseType(typeof(EnrollmentAnalyticsDto), 200)]
    public async Task<IActionResult> GetAnalytics(int courseId)
    {
        var analytics = await _enrollmentService.GetCourseAnalytics(courseId);
        return Ok(analytics);
    }

    /// <summary>Get total enrollment count for a course</summary>
    [HttpGet("course/{courseId}/count")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetCount(int courseId)
    {
        var count = await _enrollmentService.GetEnrollmentCount(courseId);
        return Ok(new { courseId, totalEnrollments = count });
    }

    /// <summary>Get all enrollments for a specific student — Admin only</summary>
    [Authorize(Roles = "ADMIN")]
    [HttpGet("student/{studentId}")]
    [ProducesResponseType(typeof(IList<EnrollmentResponseDto>), 200)]
    public async Task<IActionResult> GetByStudent(int studentId)
    {
        var enrollments = await _enrollmentService.GetEnrollmentsByStudent(studentId);
        return Ok(enrollments);
    }

    // ── Helper 
    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }
}