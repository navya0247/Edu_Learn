using EduLearn.ProgressService.DTOs;

namespace EduLearn.ProgressService.Interfaces;

// Service interface — lesson progress tracking
public interface IProgressService
{
    // Track lesson view — creates or updates progress record
    Task<LessonProgressDto> TrackLesson(TrackLessonDto dto);

    // Mark lesson as fully completed
    Task<LessonProgressDto> CompleteLesson(CompleteLessonDto dto);

    // Get progress for all lessons in a course for a student
    Task<IList<LessonProgressDto>> GetLessonProgress(int studentId, int courseId);

    // Get course progress summary — ProgressPercent, CompletedLessons etc.
    // totalLessons passed in since LessonService is a different DB
    Task<CourseProgressDto> GetCourseProgress(int studentId, int courseId, int totalLessons);
}

// Service interface — certificate issuance and verification
public interface ICertificateService
{
    // Issue certificate — called when course progress reaches 100%
    Task<CertificateDto> IssueCertificate(IssueCertificateDto dto);

    // Get certificate for student + course
    Task<CertificateDto?> GetCertificate(int studentId, int courseId);

    // Get all certificates for a student
    Task<IList<CertificateDto>> GetStudentCertificates(int studentId);

    // PUBLIC endpoint — verify certificate by unique code (no auth needed)
    Task<CertificateDto?> VerifyCertificate(string certificateCode);

    // Generate PDF using QuestPDF and return byte array
    Task<byte[]> GenerateCertificatePdf(int certificateId);
}