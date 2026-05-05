using System.Text.Json;
using EduLearn.CourseService.DTOs;
using EduLearn.CourseService.Entities;
using EduLearn.CourseService.Interfaces;

namespace EduLearn.CourseService.Services;

/// <summary>
/// CourseService with Redis caching added.
/// PDF Non-Functional: Redis IDistributedCache for popular course list (5-min TTL).
/// Cache is invalidated when a course is approved, rejected, or deleted.
/// </summary>
public class CourseService : ICourseService
{
    private readonly ICourseRepository _repo;
    private readonly ICacheService _cache;
    private readonly ILogger<CourseService> _logger;

    // Redis cache keys
    private const string PublishedCoursesKey = "courses:published";
    private const string TopCoursesKey       = "courses:top";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5); // PDF: 5-min TTL

    public CourseService(ICourseRepository repo, ICacheService cache, ILogger<CourseService> logger)
    {
        _repo   = repo;
        _cache  = cache;
        _logger = logger;
    }

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
            IsPublished     = false,
            IsApproved      = false,
            EnrollmentCount = 0,
            CreatedAt       = DateTime.UtcNow,
            UpdatedAt       = DateTime.UtcNow
        };

        var created = await _repo.Create(course);
        _logger.LogInformation("Course created: '{Title}' by Instructor {Id}", created.Title, created.InstructorId);
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

    /// <summary>
    /// Get all published and approved courses.
    /// ✅ Redis cached with 5-min TTL — PDF Non-Functional requirement.
    /// Cache miss → DB query → store in Redis → return.
    /// Cache hit → return from Redis directly (no DB call).
    /// </summary>
    public async Task<IList<CourseSummaryDto>> GetPublishedCourses()
    {
        // 1. Try Redis cache first
        var cached = await _cache.GetAsync(PublishedCoursesKey);
        if (cached != null)
        {
            _logger.LogInformation("Redis HIT: returning published courses from cache");
            return JsonSerializer.Deserialize<List<CourseSummaryDto>>(cached)!;
        }

        // 2. Cache miss — query DB
        _logger.LogInformation("Redis MISS: querying DB for published courses");
        var courses = await _repo.FindPublishedAndApproved();
        var dtos    = courses.Select(MapToSummary).ToList();

        // 3. Store in Redis for 5 minutes
        await _cache.SetAsync(
            PublishedCoursesKey,
            JsonSerializer.Serialize(dtos),
            CacheTtl);

        return dtos;
    }

    /// <summary>
    /// Get top N courses by enrollment count.
    /// ✅ Redis cached with 5-min TTL.
    /// </summary>
    public async Task<IList<CourseSummaryDto>> GetTopCourses(int count = 10)
    {
        var cacheKey = $"{TopCoursesKey}:{count}";
        var cached   = await _cache.GetAsync(cacheKey);

        if (cached != null)
        {
            _logger.LogInformation("Redis HIT: returning top {Count} courses from cache", count);
            return JsonSerializer.Deserialize<List<CourseSummaryDto>>(cached)!;
        }

        var courses = await _repo.FindTopCourses(count);
        var dtos    = courses.Select(MapToSummary).ToList();

        await _cache.SetAsync(cacheKey, JsonSerializer.Serialize(dtos), CacheTtl);
        return dtos;
    }

    public async Task<IList<CourseSummaryDto>> SearchCourses(string keyword)
    {
        // Search is not cached — always fresh results
        var courses = await _repo.SearchCourses(keyword);
        return courses.Select(MapToSummary).ToList();
    }

    public async Task<CourseResponseDto> UpdateCourse(int courseId, CreateCourseDto dto, int instructorId)
    {
        var course = await _repo.FindByCourseId(courseId)
            ?? throw new KeyNotFoundException($"Course {courseId} not found.");

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

        if (course.IsApproved)
        {
            course.IsApproved  = false;
            course.IsPublished = false;
        }

        var updated = await _repo.Update(course);

        // Invalidate cache — course list changed
        await _cache.RemoveAsync(PublishedCoursesKey);
        await _cache.RemoveAsync(TopCoursesKey + ":10");

        return MapToResponse(updated);
    }

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

    public async Task ApproveCourse(int courseId)
    {
        var course = await _repo.FindByCourseId(courseId)
            ?? throw new KeyNotFoundException($"Course {courseId} not found.");

        if (!course.IsPublished)
            throw new InvalidOperationException("Course must be published before approval.");

        course.IsApproved = true;
        await _repo.Update(course);

        // ✅ Invalidate cache — new course now in catalogue
        await _cache.RemoveAsync(PublishedCoursesKey);
        await _cache.RemoveAsync(TopCoursesKey + ":10");

        _logger.LogInformation("Course {Id} approved — cache invalidated", courseId);
    }

    public async Task RejectCourse(int courseId)
    {
        var course = await _repo.FindByCourseId(courseId)
            ?? throw new KeyNotFoundException($"Course {courseId} not found.");

        course.IsApproved  = false;
        course.IsPublished = false;
        await _repo.Update(course);

        // Invalidate cache
        await _cache.RemoveAsync(PublishedCoursesKey);
        await _cache.RemoveAsync(TopCoursesKey + ":10");

        _logger.LogWarning("Course {Id} rejected — cache invalidated", courseId);
    }

    public async Task DeleteCourse(int courseId)
    {
        await _repo.Delete(courseId);

        // Invalidate cache
        await _cache.RemoveAsync(PublishedCoursesKey);
        await _cache.RemoveAsync(TopCoursesKey + ":10");

        _logger.LogWarning("Course {Id} deleted — cache invalidated", courseId);
    }

    public async Task IncrementEnrollment(int courseId)
    {
        await _repo.IncrementEnrollment(courseId);

        // Invalidate top courses cache — enrollment count changed
        await _cache.RemoveAsync(TopCoursesKey + ":10");
    }

    public async Task<IList<CourseSummaryDto>> FilterCourses(string? category, string? level, string? keyword)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var results = await _repo.SearchCourses(keyword);
            return results.Select(MapToSummary).ToList();
        }

        var courses = await _repo.FindByCategoryAndLevel(category, level);
        return courses.Select(MapToSummary).ToList();
    }

    // ── Mapping Helpers ───────────────────────────────────────────────────────

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
