using EduLearn.LessonService.Data;
using EduLearn.LessonService.Entities;
using EduLearn.LessonService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.LessonService.Repositories;

// EF Core implementation of ILessonRepository
// Connected to EduLearn_Lesson PostgreSQL database
public class LessonRepository : ILessonRepository
{
    private readonly LessonDbContext _db;

    public LessonRepository(LessonDbContext db) => _db = db;

    public async Task<Lesson?> FindByLessonId(int lessonId)
        => await _db.Lessons.FindAsync(lessonId);

    // Ordered by DisplayOrder — course curriculum sequence
    public async Task<IList<Lesson>> FindByCourseIdOrdered(int courseId)
        => await _db.Lessons
            .Where(l => l.CourseId == courseId)
            .OrderBy(l => l.DisplayOrder)
            .ToListAsync();

    // Only preview lessons — guest access without enrollment
    public async Task<IList<Lesson>> FindPreviewLessons(int courseId)
        => await _db.Lessons
            .Where(l => l.CourseId == courseId && l.IsPreview)
            .OrderBy(l => l.DisplayOrder)
            .ToListAsync();

    public async Task<IList<Lesson>> FindByContentType(int courseId, string contentType)
        => await _db.Lessons
            .Where(l => l.CourseId == courseId && l.ContentType == contentType)
            .OrderBy(l => l.DisplayOrder)
            .ToListAsync();

    public async Task<int> CountByCourseId(int courseId)
        => await _db.Lessons.CountAsync(l => l.CourseId == courseId);

    // Bulk delete — ExecuteDeleteAsync (no entity load needed)
    public async Task DeleteByCourseId(int courseId)
        => await _db.Lessons
            .Where(l => l.CourseId == courseId)
            .ExecuteDeleteAsync();

    public async Task<Lesson> Create(Lesson lesson)
    {
        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync();
        return lesson;
    }

    public async Task<Lesson> Update(Lesson lesson)
    {
        _db.Lessons.Update(lesson);
        await _db.SaveChangesAsync();
        return lesson;
    }

    public async Task Delete(int lessonId)
        => await _db.Lessons.Where(l => l.LessonId == lessonId).ExecuteDeleteAsync();

    // Atomic DisplayOrder update — ExecuteUpdateAsync (no entity load)
    // Called in loop by ReorderLessons() for each lesson
    public async Task UpdateDisplayOrder(int lessonId, int newOrder)
        => await _db.Lessons
            .Where(l => l.LessonId == lessonId)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(l => l.DisplayOrder, newOrder));
}