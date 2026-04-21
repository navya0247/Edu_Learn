using EduLearn.LessonService.DTOs;

namespace EduLearn.LessonService.Interfaces;

// Service interface — all business logic for Lesson management
public interface ILessonService
{
    Task<LessonResponseDto> AddLesson(CreateLessonDto dto);
    Task<LessonResponseDto?> GetLessonById(int lessonId);
    Task<IList<LessonSummaryDto>> GetLessonsByCourse(int courseId);
    Task<IList<LessonSummaryDto>> GetPreviewLessons(int courseId);  // no auth needed
    Task<LessonResponseDto> UpdateLesson(int lessonId, CreateLessonDto dto);
    Task ReorderLessons(ReorderLessonsDto dto);                      // batch reorder
    Task PublishLesson(int lessonId);
    Task DeleteLesson(int lessonId);
    Task DeleteAllForCourse(int courseId);
    Task<int> GetLessonCount(int courseId);
    Task<string?> GenerateSasUrl(int lessonId);                     // Azure Blob SAS URL
}

// Azure Blob Storage interface — handles file upload and secure URL generation
public interface IAzureBlobService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task<string> GenerateSasUrlAsync(string blobUrl, TimeSpan expiry);  // time-limited URL
    Task DeleteFileAsync(string blobUrl);
}