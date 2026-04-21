using EduLearn.CourseService.DTOs;
using EduLearn.CourseService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduLearn.CourseService.Controllers;

/// <summary>
/// REST API for Course management.
/// Base route: /api/courses
/// JWT from AuthService validated here — must use same secret key.
/// </summary>
[ApiController]
[Route("api/courses")]
public class CourseController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly ILogger<CourseController> _logger;

    public CourseController(ICourseService courseService, ILogger<CourseController> logger)
    {
        _courseService = courseService;
        _logger        = logger;
    }

    // ── Public Endpoints (no JWT needed) ─────────────────────────────────────

    /// <summary>
    /// GET /api/courses/published
    /// Public catalogue — only IsPublished=true AND IsApproved=true courses.
    /// Guests and students can browse without login.
    /// </summary>
    [HttpGet("published")]
    public async Task<IActionResult> GetPublished()
    {
        var courses = await _courseService.GetPublishedCourses();
        return Ok(courses);
    }

    /// <summary>
    /// GET /api/courses/search?keyword=python
    /// Search by keyword — EF Core LIKE on title, description, category.
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new { message = "Search keyword is required" });

        var courses = await _courseService.SearchCourses(keyword);
        return Ok(courses);
    }

    /// <summary>
    /// GET /api/courses/filter?category=Programming&level=Beginner
    /// Combined filter — any combination works.
    /// </summary>
    [HttpGet("filter")]
    public async Task<IActionResult> Filter(
        [FromQuery] string? category,
        [FromQuery] string? level,
        [FromQuery] string? keyword)
    {
        var courses = await _courseService.FilterCourses(category, level, keyword);
        return Ok(courses);
    }

    /// <summary>
    /// GET /api/courses/top?count=10
    /// Top N most enrolled courses — homepage featured section.
    /// </summary>
    [HttpGet("top")]
    public async Task<IActionResult> GetTop([FromQuery] int count = 10)
    {
        var courses = await _courseService.GetTopCourses(count);
        return Ok(courses);
    }

    /// <summary>
    /// GET /api/courses/{id}
    /// Single course detail page.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var course = await _courseService.GetCourseById(id);
        if (course == null)
            return NotFound(new { message = "Course not found" });
        return Ok(course);
    }

    /// <summary>
    /// GET /api/courses/category/{category}
    /// Browse by category.
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var courses = await _courseService.FilterCourses(category, null, null);
        return Ok(courses);
    }

    // ── Instructor Endpoints ──────────────────────────────────────────────────

    /// <summary>
    /// POST /api/courses
    /// Create new course — INSTRUCTOR only.
    /// Starts as draft: IsPublished=false, IsApproved=false.
    /// </summary>
    [Authorize(Roles = "INSTRUCTOR")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCourseDto dto)
    {
        var instructorId = GetCurrentUserId();
        var created = await _courseService.CreateCourse(dto, instructorId);
        return CreatedAtAction(nameof(GetById), new { id = created.CourseId }, created);
    }

    /// <summary>
    /// PUT /api/courses/{id}
    /// Update course — only the instructor who owns it.
    /// </summary>
    [Authorize(Roles = "INSTRUCTOR")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCourseDto dto)
    {
        try
        {
            var updated = await _courseService.UpdateCourse(id, dto, GetCurrentUserId());
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// PUT /api/courses/{id}/publish
    /// Instructor submits for admin review — sets IsPublished = true.
    /// Course still NOT visible to students until admin approves.
    /// </summary>
    [Authorize(Roles = "INSTRUCTOR")]
    [HttpPut("{id}/publish")]
    public async Task<IActionResult> Publish(int id)
    {
        try
        {
            await _courseService.PublishCourse(id, GetCurrentUserId());
            return Ok(new { message = "Course submitted for admin review successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// GET /api/courses/instructor/{instructorId}
    /// All courses by instructor — includes drafts.
    /// </summary>
    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpGet("instructor/{instructorId}")]
    public async Task<IActionResult> GetByInstructor(int instructorId)
    {
        var courses = await _courseService.GetCoursesByInstructor(instructorId);
        return Ok(courses);
    }

    // ── Admin Endpoints ───────────────────────────────────────────────────────

    /// <summary>
    /// PUT /api/courses/{id}/approve
    /// Admin approves — course NOW visible in public catalogue.
    /// Sets IsApproved = true.
    /// </summary>
    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        try
        {
            await _courseService.ApproveCourse(id);
            _logger.LogInformation("Admin approved course {Id}", id);
            return Ok(new { message = "Course approved and now visible in catalogue" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/courses/{id}/reject
    /// Admin rejects — sends back to instructor as draft.
    /// </summary>
    [Authorize(Roles = "ADMIN")]
    [HttpPut("{id}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        try
        {
            await _courseService.RejectCourse(id);
            return Ok(new { message = "Course rejected and sent back to instructor" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/courses/{id}
    /// Admin deletes any course. Instructor deletes only their own.
    /// </summary>
    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var course = await _courseService.GetCourseById(id);
            if (course == null)
                return NotFound(new { message = "Course not found" });

            if (User.IsInRole("INSTRUCTOR") && course.InstructorId != GetCurrentUserId())
                return Forbid();

            await _courseService.DeleteCourse(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/courses/{id}/increment-enrollment
    /// Called by EnrollmentService when student enrolls.
    /// Atomic increment via ExecuteUpdateAsync.
    /// </summary>
    [Authorize]
    [HttpPut("{id}/increment-enrollment")]
    public async Task<IActionResult> IncrementEnrollment(int id)
    {
        await _courseService.IncrementEnrollment(id);
        return Ok(new { message = "Enrollment count updated" });
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }
}