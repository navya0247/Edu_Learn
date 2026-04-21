using EduLearn.CourseService.DTOs;
using EduLearn.CourseService.Entities;
using EduLearn.CourseService.Interfaces;

namespace EduLearn.CourseService.Services;

/// <summary>
/// Implements ICourseService — course management business logic.
/// Enforces two-step publish workflow from PDF requirements.
/// Maps between Entity and DTO — controllers never touch raw entities.
/// </summary>
public class CourseService : ICourseService
{
    private readonly ICourseRepository _repo;
    private readonly ILogger<CourseService> _logger;

    public CourseService(ICourseRepository repo, ILogger<CourseService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    /// <summary>
    /// Create new course — always starts as unpublished draft.
    /// Instructor must call PublishCourse() when ready for review.
    /// </summary>
    public async Task<CourseResponseDto> CreateCourse(CreateCourseDto dto, int instructorId)
    {
        var course = new Course
        {
            Title           = dto.Title,
            Description     = dto.Description,
            Category        = dto.Category,
            Level           = dto.Level,
            Language        = dto.Language,
            Price           = dto.Price,
            ThumbnailUrl    = dto.ThumbnailUrl,
            InstructorId    = instructorId,
            IsPublished     = false,   // Always starts as draft
            IsApproved      = false,
            EnrollmentCount = 0,
            CreatedAt       = DateTime.UtcNow,
            UpdatedAt       = DateTime.UtcNow
        };

        var created = await _repo.Create(course);
        _logger.LogInformation("Course created: '{Title}' by Instructor {Id}",
            created.Title, created.InstructorId);

        return MapToResponse(created);
    }

    public async Task<CourseResponseDto?> GetCourseById(int courseId)
    {
        var course = await _repo.FindByCourseId(courseId);
        return course == null ? null : MapToResponse(course);
    }

    public async Task<IList<CourseSummaryDto>> GetCoursesByInstructor(int instructorId)
    {
        var courses = await _repo.FindByInstructorId(instructorId);
        return courses.Select(MapToSummary).ToList();
    }

    public async Task<IList<CourseSummaryDto>> GetPublishedCourses()
    {
        var courses = await _repo.FindPublishedAndApproved();
        return courses.Select(MapToSummary).ToList();
    }

    public async Task<IList<CourseSummaryDto>> SearchCourses(string keyword)
    {
        var courses = await _repo.SearchCourses(keyword);
        return courses.Select(MapToSummary).ToList();
    }

    /// <summary>
    /// Update course — resets approval if already approved.
    /// Admin must re-review after instructor edits.
    /// </summary>
    public async Task<CourseResponseDto> UpdateCourse(int courseId, CreateCourseDto dto, int instructorId)
    {
        var course = await _repo.FindByCourseId(courseId)
            ?? throw new KeyNotFoundException($"Course {courseId} not found.");

        // Only owner instructor can update
        if (course.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You can only update your own courses.");

        course.Title       = dto.Title;
        course.Description = dto.Description;
        course.Category    = dto.Category;
        course.Level       = dto.Level;
        course.Language    = dto.Language;
        course.Price       = dto.Price;

        if (!string.IsNullOrWhiteSpace(dto.ThumbnailUrl))
            course.ThumbnailUrl = dto.ThumbnailUrl;

        // Reset approval if already approved — admin must re-review after edits
        if (course.IsApproved)
        {
            course.IsApproved  = false;
            course.IsPublished = false;
            _logger.LogInformation("Course {Id} reset to draft after instructor edit", courseId);
        }

        var updated = await _repo.Update(course);
        return MapToResponse(updated);
    }

    /// <summary>
    /// INSTRUCTOR ACTION — Submit for admin review.
    /// Sets IsPublished = true. Course NOT yet visible to students.
    /// </summary>
    public async Task PublishCourse(int courseId, int instructorId)
    {
        var course = await _repo.FindByCourseId(courseId)
            ?? throw new KeyNotFoundException($"Course {courseId} not found.");

        if (course.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You can only publish your own courses.");

        course.IsPublished = true;
        await _repo.Update(course);
        _logger.LogInformation("Course {Id} submitted for admin approval", courseId);
    }

    /// <summary>
    /// ADMIN ACTION — Approve course for public listing.
    /// Sets IsApproved = true. Course NOW visible in catalogue.
    /// </summary>
    public async Task ApproveCourse(int courseId)
    {
        var course = await _repo.FindByCourseId(courseId)
            ?? throw new KeyNotFoundException($"Course {courseId} not found.");

        if (!course.IsPublished)
            throw new InvalidOperationException("Course must be published by instructor before admin approves.");

        course.IsApproved = true;
        await _repo.Update(course);
        _logger.LogInformation("Course {Id} approved by admin — now in public catalogue", courseId);
    }

    /// <summary>
    /// ADMIN ACTION — Reject course, send back to draft.
    /// Both flags = false. Instructor must revise and re-publish.
    /// </summary>
    public async Task RejectCourse(int courseId)
    {
        var course = await _repo.FindByCourseId(courseId)
            ?? throw new KeyNotFoundException($"Course {courseId} not found.");

        course.IsApproved  = false;
        course.IsPublished = false;
        await _repo.Update(course);
        _logger.LogWarning("Course {Id} rejected by admin", courseId);
    }

    public async Task DeleteCourse(int courseId)
    {
        var course = await _repo.FindByCourseId(courseId)
            ?? throw new KeyNotFoundException($"Course {courseId} not found.");

        await _repo.Delete(courseId);
        _logger.LogWarning("Course {Id} deleted", courseId);
    }

    public async Task<IList<CourseSummaryDto>> GetTopCourses(int count = 10)
    {
        var courses = await _repo.FindTopCourses(count);
        return courses.Select(MapToSummary).ToList();
    }

    public async Task IncrementEnrollment(int courseId)
        => await _repo.IncrementEnrollment(courseId);

    public async Task<IList<CourseSummaryDto>> FilterCourses(
        string? category, string? level, string? keyword)
    {
        // Keyword search takes priority
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var results = await _repo.SearchCourses(keyword);
            return results.Select(MapToSummary).ToList();
        }

        var courses = await _repo.FindByCategoryAndLevel(category, level);
        return courses.Select(MapToSummary).ToList();
    }

    // ── Private Mapping Helpers ───────────────────────────────────────────────

    private static CourseResponseDto MapToResponse(Course c) => new()
    {
        CourseId        = c.CourseId,
        Title           = c.Title,
        Description     = c.Description,
        InstructorId    = c.InstructorId,
        Category        = c.Category,
        Level           = c.Level,
        Language        = c.Language,
        Price           = c.Price,
        ThumbnailUrl    = c.ThumbnailUrl,
        IsPublished     = c.IsPublished,
        IsApproved      = c.IsApproved,
        EnrollmentCount = c.EnrollmentCount,
        TotalDuration   = c.TotalDuration,
        CreatedAt       = c.CreatedAt,
        UpdatedAt       = c.UpdatedAt
    };

    private static CourseSummaryDto MapToSummary(Course c) => new()
    {
        CourseId        = c.CourseId,
        Title           = c.Title,
        Category        = c.Category,
        Level           = c.Level,
        Price           = c.Price,
        ThumbnailUrl    = c.ThumbnailUrl,
        EnrollmentCount = c.EnrollmentCount,
        InstructorId    = c.InstructorId,
        IsPublished     = c.IsPublished,
        IsApproved      = c.IsApproved
    };
}