using EduLearn.ProgressService.DTOs;
using EduLearn.ProgressService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduLearn.ProgressService.Controllers;

[ApiController]
[Route("api/progress")]
[Produces("application/json")]
public class ProgressController : ControllerBase
{
    private readonly IProgressService _progressService;
    private readonly ILogger<ProgressController> _logger;

    public ProgressController(IProgressService progressService, ILogger<ProgressController> logger)
    {
        _progressService = progressService;
        _logger          = logger;
    }

    /// <summary>Track lesson view — creates or updates WatchPercent for a lesson</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpPost("lesson")]
    [ProducesResponseType(typeof(LessonProgressDto), 200)]
    public async Task<IActionResult> TrackLesson([FromBody] TrackLessonDto dto)
    {
        dto.StudentId = GetCurrentUserId();
        var result = await _progressService.TrackLesson(dto);
        return Ok(result);
    }

    /// <summary>Mark lesson as fully completed — sets IsCompleted = true</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpPut("lesson/complete")]
    [ProducesResponseType(typeof(LessonProgressDto), 200)]
    public async Task<IActionResult> CompleteLesson([FromBody] CompleteLessonDto dto)
    {
        dto.StudentId = GetCurrentUserId();
        var result = await _progressService.CompleteLesson(dto);
        return Ok(result);
    }

    /// <summary>Get all lesson progress records for a student in a course</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpGet("course/{courseId}")]
    [ProducesResponseType(typeof(IList<LessonProgressDto>), 200)]
    public async Task<IActionResult> GetLessonProgress(int courseId)
    {
        var progress = await _progressService.GetLessonProgress(GetCurrentUserId(), courseId);
        return Ok(progress);
    }

    /// <summary>Get course progress summary with ProgressPercent — pass totalLessons as query param</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpGet("course/{courseId}/summary")]
    [ProducesResponseType(typeof(CourseProgressDto), 200)]
    public async Task<IActionResult> GetCourseProgress(int courseId, [FromQuery] int totalLessons)
    {
        var progress = await _progressService.GetCourseProgress(
            GetCurrentUserId(), courseId, totalLessons);
        return Ok(progress);
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }
}