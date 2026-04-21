using EduLearn.AssessmentService.DTOs;
using EduLearn.AssessmentService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduLearn.AssessmentService.Controllers;

[ApiController]
[Route("api/quizzes")]
[Produces("application/json")]
public class QuizController : ControllerBase
{
    private readonly IQuizService _quizService;
    private readonly ILogger<QuizController> _logger;

    public QuizController(IQuizService quizService, ILogger<QuizController> logger)
    {
        _quizService = quizService;
        _logger      = logger;
    }

    // ── Public / Student Endpoints 

    /// <summary>Get quiz details by ID — question count only, no correct answers</summary>
    [Authorize]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(QuizResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        var quiz = await _quizService.GetQuizById(id);
        if (quiz == null) return NotFound(new { message = "Quiz not found" });
        return Ok(quiz);
    }

    /// <summary>Get all quizzes for a course</summary>
    [Authorize]
    [HttpGet("course/{courseId}")]
    [ProducesResponseType(typeof(IList<QuizResponseDto>), 200)]
    public async Task<IActionResult> GetByCourse(int courseId)
    {
        var quizzes = await _quizService.GetQuizzesByCourse(courseId);
        return Ok(quizzes);
    }

    /// <summary>Get quiz for a specific lesson</summary>
    [Authorize]
    [HttpGet("lesson/{lessonId}")]
    [ProducesResponseType(typeof(QuizResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByLesson(int lessonId)
    {
        var quiz = await _quizService.GetQuizByLesson(lessonId);
        if (quiz == null) return NotFound(new { message = "No quiz for this lesson" });
        return Ok(quiz);
    }

    /// <summary>Get quiz questions for student — correct answers hidden</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpGet("{id}/questions")]
    [ProducesResponseType(typeof(IList<StudentQuestionDto>), 200)]
    public async Task<IActionResult> GetQuestions(int id)
    {
        try
        {
            var questions = await _quizService.GetQuestionsForStudent(id);
            return Ok(questions);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Start a quiz attempt — checks MaxAttempts limit first</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpPost("{id}/start")]
    [ProducesResponseType(typeof(AttemptResponseDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> StartAttempt(int id)
    {
        try
        {
            var attempt = await _quizService.StartAttempt(id, GetCurrentUserId());
            return CreatedAtAction(nameof(GetById), new { id }, attempt);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Submit quiz answers — computes score and sets IsPassed</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpPut("attempt/{attemptId}/submit")]
    [ProducesResponseType(typeof(AttemptResponseDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SubmitAttempt(int attemptId, [FromBody] SubmitAttemptDto dto)
    {
        try
        {
            var result = await _quizService.SubmitAttempt(attemptId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Get all attempts by current student for a quiz</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpGet("{id}/my-attempts")]
    [ProducesResponseType(typeof(IList<AttemptResponseDto>), 200)]
    public async Task<IActionResult> GetMyAttempts(int id)
    {
        var attempts = await _quizService.GetAttemptsByStudent(GetCurrentUserId(), id);
        return Ok(attempts);
    }

    /// <summary>Get best (highest score) attempt for current student</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpGet("{id}/best-attempt")]
    [ProducesResponseType(typeof(AttemptResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetBestAttempt(int id)
    {
        var attempt = await _quizService.GetBestAttempt(GetCurrentUserId(), id);
        if (attempt == null) return NotFound(new { message = "No attempts found" });
        return Ok(attempt);
    }

    /// <summary>Get remaining attempt count for current student</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpGet("{id}/attempt-count")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetAttemptCount(int id)
    {
        var quiz  = await _quizService.GetQuizById(id);
        var count = await _quizService.GetAttemptCount(GetCurrentUserId(), id);
        return Ok(new
        {
            quizId          = id,
            attemptsUsed    = count,
            maxAttempts     = quiz?.MaxAttempts ?? 0,
            attemptsLeft    = Math.Max(0, (quiz?.MaxAttempts ?? 0) - count)
        });
    }

    // ── Instructor Endpoints 

    /// <summary>Create a new quiz — Instructor only</summary>
    [Authorize(Roles = "INSTRUCTOR")]
    [HttpPost]
    [ProducesResponseType(typeof(QuizResponseDto), 201)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Create([FromBody] CreateQuizDto dto)
    {
        var created = await _quizService.CreateQuiz(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.QuizId }, created);
    }

    /// <summary>Update quiz questions and settings — Instructor only</summary>
    [Authorize(Roles = "INSTRUCTOR")]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(QuizResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] CreateQuizDto dto)
    {
        try
        {
            var updated = await _quizService.UpdateQuiz(id, dto);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Publish quiz — makes visible to enrolled students</summary>
    [Authorize(Roles = "INSTRUCTOR")]
    [HttpPut("{id}/publish")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Publish(int id)
    {
        try
        {
            await _quizService.PublishQuiz(id);
            return Ok(new { message = "Quiz published successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Delete a quiz — Instructor or Admin</summary>
    [Authorize(Roles = "INSTRUCTOR,ADMIN")]
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _quizService.DeleteQuiz(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── Helper 
    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }
}