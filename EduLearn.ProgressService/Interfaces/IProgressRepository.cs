using EduLearn.ProgressService.Entities;

namespace EduLearn.ProgressService.Interfaces;

// Repository interface — DB operations for LessonProgress
public interface IProgressRepository
{
    Task<LessonProgress?> FindByStudentAndLesson(int studentId, int lessonId);
    Task<IList<LessonProgress>> FindByCourseAndStudent(int studentId, int courseId);
    Task<int> CountCompletedLessons(int studentId, int courseId);   // for progress %
    Task<LessonProgress> Create(LessonProgress progress);
    Task<LessonProgress> Update(LessonProgress progress);
}

// Repository interface — DB operations for Certificate
public interface ICertificateRepository
{
    Task<Certificate?> FindById(int certificateId);  // ✅ ADDED - Fix for download
    Task<Certificate?> FindByStudentAndCourse(int studentId, int courseId);
    Task<Certificate?> FindByCertificateCode(string code);          // public verification
    Task<IList<Certificate>> FindByStudentId(int studentId);
    Task<Certificate> Create(Certificate certificate);
    Task<Certificate> Update(Certificate certificate);
}