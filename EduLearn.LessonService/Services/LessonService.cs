using EduLearn.LessonService.DTOs;
using EduLearn.LessonService.Entities;
using EduLearn.LessonService.Interfaces;

namespace EduLearn.LessonService.Services;

// Business logic for lesson management
// Handles CRUD, reordering, preview access and Azure Blob SAS URL generation
public class LessonService : ILessonService
{
    private readonly ILessonRepository _repo;
    private readonly IAzureBlobService _blobService;
    private readonly ILogger<LessonService> _logger;

    public LessonService(ILessonRepository repo, IAzureBlobService blobService, ILogger<LessonService> logger)
    {
        _repo        = repo;
        _blobService = blobService;
        _logger      = logger;
    }

    public async Task<LessonResponseDto> AddLesson(CreateLessonDto dto)
    {
        // Auto-set display order if not provided — append to end of course
        if (dto.DisplayOrder == 0)
            dto.DisplayOrder = await _repo.CountByCourseId(dto.CourseId) + 1;

        var lesson = new Lesson
        {
            CourseId        = dto.CourseId,
            Title           = dto.Title,
            Description     = dto.Description,
            ContentType     = dto.ContentType,
            ContentUrl      = dto.ContentUrl,
            DurationMinutes = dto.DurationMinutes,
            DisplayOrder    = dto.DisplayOrder,
            IsPreview       = dto.IsPreview,
            IsPublished     = false,
            CreatedAt       = DateTime.UtcNow
        };

        var created = await _repo.Create(lesson);
        _logger.LogInformation("Lesson '{Title}' added to Course {CourseId}", created.Title, created.CourseId);
        return await MapToResponse(created);
    }

    public async Task<LessonResponseDto?> GetLessonById(int lessonId)
    {
        var lesson = await _repo.FindByLessonId(lessonId);
        return lesson == null ? null : await MapToResponse(lesson);
    }

    public async Task<IList<LessonSummaryDto>> GetLessonsByCourse(int courseId)
    {
        var lessons = await _repo.FindByCourseIdOrdered(courseId);
        return lessons.Select(MapToSummary).ToList();
    }

    // Returns only IsPreview=true lessons — no auth needed for guests
    public async Task<IList<LessonSummaryDto>> GetPreviewLessons(int courseId)
    {
        var lessons = await _repo.FindPreviewLessons(courseId);
        return lessons.Select(MapToSummary).ToList();
    }

    public async Task<LessonResponseDto> UpdateLesson(int lessonId, CreateLessonDto dto)
    {
        var lesson = await _repo.FindByLessonId(lessonId)
            ?? throw new KeyNotFoundException($"Lesson {lessonId} not found.");

        lesson.Title           = dto.Title;
        lesson.Description     = dto.Description;
        lesson.ContentType     = dto.ContentType;
        lesson.DurationMinutes = dto.DurationMinutes;
        lesson.IsPreview       = dto.IsPreview;

        if (!string.IsNullOrWhiteSpace(dto.ContentUrl))
            lesson.ContentUrl = dto.ContentUrl;

        var updated = await _repo.Update(lesson);
        return await MapToResponse(updated);
    }

    // Reorder lessons — batch ExecuteUpdateAsync for each lesson
    // LessonIds = [3,1,2] → lesson3 gets order=1, lesson1 gets order=2 etc.
    public async Task ReorderLessons(ReorderLessonsDto dto)
    {
        for (int i = 0; i < dto.LessonIds.Count; i++)
            await _repo.UpdateDisplayOrder(dto.LessonIds[i], i + 1);

        _logger.LogInformation("Reordered {Count} lessons in Course {CourseId}",
            dto.LessonIds.Count, dto.CourseId);
    }

    public async Task PublishLesson(int lessonId)
    {
        var lesson = await _repo.FindByLessonId(lessonId)
            ?? throw new KeyNotFoundException($"Lesson {lessonId} not found.");
        lesson.IsPublished = true;
        await _repo.Update(lesson);
    }

    public async Task DeleteLesson(int lessonId)
    {
        var lesson = await _repo.FindByLessonId(lessonId)
            ?? throw new KeyNotFoundException($"Lesson {lessonId} not found.");
        await _repo.Delete(lessonId);
    }

    public async Task DeleteAllForCourse(int courseId)
        => await _repo.DeleteByCourseId(courseId);

    public async Task<int> GetLessonCount(int courseId)
        => await _repo.CountByCourseId(courseId);

    // Generate time-limited SAS URL for content access
    // Videos get 24h expiry — student might watch for long time
    // Other content gets 1h expiry
    public async Task<string?> GenerateSasUrl(int lessonId)
    {
        var lesson = await _repo.FindByLessonId(lessonId);
        if (lesson?.ContentUrl == null) return null;

        var expiry = lesson.ContentType == "VIDEO"
            ? TimeSpan.FromHours(24)
            : TimeSpan.FromHours(1);

        return await _blobService.GenerateSasUrlAsync(lesson.ContentUrl, expiry);
    }

    // ── Private Mapping Helpers ───────────────────────────────────────────────

    private async Task<LessonResponseDto> MapToResponse(Lesson l)
    {
        // Generate fresh SAS URL for content — expires based on content type
        string? contentUrl = null;
        if (!string.IsNullOrEmpty(l.ContentUrl))
        {
            var expiry = l.ContentType == "VIDEO" ? TimeSpan.FromHours(24) : TimeSpan.FromHours(1);
            contentUrl = await _blobService.GenerateSasUrlAsync(l.ContentUrl, expiry);
        }

        return new LessonResponseDto
        {
            LessonId        = l.LessonId,
            CourseId        = l.CourseId,
            Title           = l.Title,
            Description     = l.Description,
            ContentType     = l.ContentType,
            ContentUrl      = contentUrl,
            DurationMinutes = l.DurationMinutes,
            DisplayOrder    = l.DisplayOrder,
            IsPreview       = l.IsPreview,
            IsPublished     = l.IsPublished,
            CreatedAt       = l.CreatedAt
        };
    }

    private static LessonSummaryDto MapToSummary(Lesson l) => new()
    {
        LessonId        = l.LessonId,
        Title           = l.Title,
        ContentType     = l.ContentType,
        DurationMinutes = l.DurationMinutes,
        DisplayOrder    = l.DisplayOrder,
        IsPreview       = l.IsPreview,
        IsPublished     = l.IsPublished
    };
}