using EduLearn.ProgressService.DTOs;
using EduLearn.ProgressService.Entities;
using EduLearn.ProgressService.Interfaces;

namespace EduLearn.ProgressService.Services;

// Business logic for lesson progress tracking
public class ProgressService : IProgressService
{
    private readonly IProgressRepository _repo;
    private readonly ILogger<ProgressService> _logger;

    public ProgressService(IProgressRepository repo, ILogger<ProgressService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    // Track lesson view — creates new record or updates existing WatchPercent
    public async Task<LessonProgressDto> TrackLesson(TrackLessonDto dto)
    {
        var existing = await _repo.FindByStudentAndLesson(dto.StudentId, dto.LessonId);

        if (existing == null)
        {
            // First time student opens this lesson
            var progress = new LessonProgress
            {
                StudentId    = dto.StudentId,
                LessonId     = dto.LessonId,
                CourseId     = dto.CourseId,
                IsCompleted  = false,
                StartedAt    = DateTime.UtcNow,
                WatchPercent = dto.WatchPercent
            };
            var created = await _repo.Create(progress);
            return MapToDto(created);
        }

        // Update watch progress — only move forward, never backward
        if (dto.WatchPercent > existing.WatchPercent)
            existing.WatchPercent = dto.WatchPercent;

        var updated = await _repo.Update(existing);
        return MapToDto(updated);
    }

    // Mark lesson as fully completed — sets IsCompleted = true
    public async Task<LessonProgressDto> CompleteLesson(CompleteLessonDto dto)
    {
        var existing = await _repo.FindByStudentAndLesson(dto.StudentId, dto.LessonId);

        if (existing == null)
        {
            // Create completed record directly
            var progress = new LessonProgress
            {
                StudentId    = dto.StudentId,
                LessonId     = dto.LessonId,
                CourseId     = dto.CourseId,
                IsCompleted  = true,
                StartedAt    = DateTime.UtcNow,
                CompletedAt  = DateTime.UtcNow,
                WatchPercent = 100
            };
            var created = await _repo.Create(progress);
            _logger.LogInformation("Student {StudentId} completed lesson {LessonId}", dto.StudentId, dto.LessonId);
            return MapToDto(created);
        }

        existing.IsCompleted  = true;
        existing.CompletedAt  = DateTime.UtcNow;
        existing.WatchPercent = 100;

        var updated = await _repo.Update(existing);
        _logger.LogInformation("Student {StudentId} completed lesson {LessonId}", dto.StudentId, dto.LessonId);
        return MapToDto(updated);
    }

    public async Task<IList<LessonProgressDto>> GetLessonProgress(int studentId, int courseId)
    {
        var records = await _repo.FindByCourseAndStudent(studentId, courseId);
        return records.Select(MapToDto).ToList();
    }

    // Compute overall course progress — totalLessons passed in from API caller
    // since LessonService is a separate database
    public async Task<CourseProgressDto> GetCourseProgress(int studentId, int courseId, int totalLessons)
    {
        var completed = await _repo.CountCompletedLessons(studentId, courseId);
        var percent   = totalLessons > 0
            ? (int)Math.Round((double)completed / totalLessons * 100)
            : 0;

        return new CourseProgressDto
        {
            StudentId        = studentId,
            CourseId         = courseId,
            TotalLessons     = totalLessons,
            CompletedLessons = completed,
            ProgressPercent  = percent,
            IsCourseComplete = percent >= 100
        };
    }

    private static LessonProgressDto MapToDto(LessonProgress lp) => new()
    {
        LessonProgressId = lp.LessonProgressId,
        StudentId        = lp.StudentId,
        LessonId         = lp.LessonId,
        CourseId         = lp.CourseId,
        IsCompleted      = lp.IsCompleted,
        StartedAt        = lp.StartedAt,
        CompletedAt      = lp.CompletedAt,
        WatchPercent     = lp.WatchPercent
    };
}