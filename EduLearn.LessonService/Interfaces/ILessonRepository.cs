using EduLearn.LessonService.Entities;

namespace EduLearn.LessonService.Interfaces;

// Repository interface — all DB operations for Lesson
// Implemented by LessonRepository using EF Core + PostgreSQL
public interface ILessonRepository
{
    Task<Lesson?> FindByLessonId(int lessonId);
    Task<IList<Lesson>> FindByCourseIdOrdered(int courseId);   // ordered by DisplayOrder
    Task<IList<Lesson>> FindPreviewLessons(int courseId);       // IsPreview = true only
    Task<IList<Lesson>> FindByContentType(int courseId, string contentType);
    Task<int> CountByCourseId(int courseId);
    Task DeleteByCourseId(int courseId);                        // bulk delete
    Task<Lesson> Create(Lesson lesson);
    Task<Lesson> Update(Lesson lesson);
    Task Delete(int lessonId);
    Task UpdateDisplayOrder(int lessonId, int newOrder);        // ExecuteUpdateAsync
}