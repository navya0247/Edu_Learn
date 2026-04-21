using EduLearn.EnrollmentService.DTOs;

namespace EduLearn.EnrollmentService.Interfaces;

// Service interface — all business logic for Enrollment management
public interface IEnrollmentService
{
    // Enroll student — checks IsEnrolled first to prevent duplicates
    Task<EnrollmentResponseDto> Enroll(int studentId, EnrollRequestDto dto);

    Task<EnrollmentResponseDto?> GetEnrollmentById(int enrollmentId);

    // All courses a student is enrolled in
    Task<IList<EnrollmentResponseDto>> GetEnrollmentsByStudent(int studentId);

    // All enrollments for a course — instructor analytics
    Task<IList<EnrollmentResponseDto>> GetEnrollmentsByCourse(int courseId);

    // Check if student is enrolled — used before allowing lesson access
    Task<bool> IsEnrolled(int studentId, int courseId);

    // Update ProgressPercent — called by ProgressService after lesson completion
    Task UpdateProgress(int enrollmentId, int progressPercent);

    // Mark enrollment as COMPLETED — triggers certificate check
    Task CompleteEnrollment(int enrollmentId);

    // Student drops course — sets Status = DROPPED
    Task DropCourse(int enrollmentId, int studentId);

    // Completed courses only
    Task<IList<EnrollmentResponseDto>> GetCompletedCourses(int studentId);

    // In-progress courses only
    Task<IList<EnrollmentResponseDto>> GetInProgressCourses(int studentId);

    // Total enrollment count for a course
    Task<int> GetEnrollmentCount(int courseId);

    // Analytics: total/completed/inProgress/dropped for instructor dashboard
    Task<EnrollmentAnalyticsDto> GetCourseAnalytics(int courseId);
}