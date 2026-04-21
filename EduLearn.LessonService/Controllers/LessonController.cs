using EduLearn.LessonService.DTOs;
using EduLearn.LessonService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduLearn.LessonService.Controllers;

[ApiController]
[Route("api/lessons")]
[Produces("application/json")]
public class LessonController : ControllerBase
{
    private readonly ILessonService _lessonService;
    private readonly ILogger<LessonController> _logger;

    public LessonController(ILessonService lessonService, ILogger<LessonController> logger)
    {
        _lessonService = lessonService;
        _logger        = logger;
    }

    // ── Public Endpoints ──────────────────────────────────────────────────────

    /// <summary>Get free preview lessons for a course — no login required</summary>
    [HttpGet("preview/{courseId}")]
    [ProducesResponseType(typeof(IList<LessonSummaryDto>), 200)]
    public async Task<IActionResult> GetPreview(int courseId)
    {
        var lessons = await _lessonService.GetPreviewLessons(courseId);
        return Ok(lessons);
    }

    /// <summary>Get total lesson count for a course</summary>
    [HttpGet("count/{courseId}")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetCount(int courseId)
    {
        var count = await _lessonService.GetLessonCount(courseId);
        return Ok(new { courseId, totalLessons = count });
    }

    // ── Student Endpoints ─────────────────────────────────────────────────────

    /// <summary>Get all lessons for a course ordered by display sequence</summary>
    [Authorize]
    [HttpGet("course/{courseId}")]
    [ProducesResponseType(typeof(IList<LessonSummaryDto>), 200)]
    public async Task<IActionResult> GetByCourse(int courseId)
    {
        var lessons = await _lessonService.GetLessonsByCourse(courseId);
        return Ok(lessons);
    }

    /// <summary>Get single lesson with secure Azure Blob content URL</summary>
    [Authorize]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LessonResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        var lesson = await _lessonService.GetLessonById(id);
        if (lesson == null) return NotFound(new { message = "Lesson not found" });
        return Ok(lesson);
    }

    /// <summary>Get time-limited SAS URL for secure lesson content access</summary>
    [Authorize]
    [HttpGet("{id}/content-url")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetContentUrl(int id)
    {
        var sasUrl = await _lessonService.GenerateSasUrl(id);
        if (sasUrl == null) return NotFound(new { message = "Content not found" });
        return Ok(new { contentUrl = sasUrl });
    }

    // ── Instructor Endpoints ──────────────────────────────────────────────────

    /// <summary>Add a new lesson to a course — Instructor only</summary>
    [Authorize(Roles = "INSTRUCTOR")]
    [HttpPost]
    [ProducesResponseType(typeof(LessonResponseDto), 201)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Add([FromBody] CreateLessonDto dto)
    {
        var created = await _lessonService.AddLesson(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.LessonId }, created);
    }

    /// <summary>Update lesson content or details — Instructor only</summary>
    [Authorize(Roles = "INSTRUCTOR")]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(LessonResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] CreateLessonDto dto)
    {
        try
        {
            var updated = await _lessonService.UpdateLesson(id, dto);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Reorder lessons in a course — Instructor only (drag and drop)</summary>
    [Authorize(Roles = "INSTRUCTOR")]
    [HttpPut("reorder")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Reorder([FromBody] ReorderLessonsDto dto)
    {
        await _lessonService.ReorderLessons(dto);
        return Ok(new { message = $"Reordered {dto.LessonIds.Count} lessons successfully" });
    }

    /// <summary>Publish lesson — makes visible to enrolled students</summary>
    [Authorize(Roles = "INSTRUCTOR")]
    [HttpPut("{id}/publish")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Publish(int id)
    {
        try
        {
            await _lessonService.PublishLesson(id);
            return Ok(new { message = "Lesson published successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Delete a single lesson — Instructor or Admin</summary>
    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _lessonService.DeleteLesson(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Delete all lessons in a course — Admin only</summary>
    [Authorize(Roles = "ADMIN")]
    [HttpDelete("course/{courseId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> DeleteAllForCourse(int courseId)
    {
        await _lessonService.DeleteAllForCourse(courseId);
        return NoContent();
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }
}