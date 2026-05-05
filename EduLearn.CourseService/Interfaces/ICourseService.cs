using EduLearn.CourseService.DTOs;

namespace EduLearn.CourseService.Interfaces;

/// <summary>
/// Service interface — all business logic for Course management.
/// Injected into CourseController via constructor DI.
/// </summary>
public interface ICourseService
{
    // Create course — always starts as draft (IsPublished=false, IsApproved=false)
    Task<CourseResponseDto> CreateCourse(CreateCourseDto dto, int instructorId);

    // Get single course — for course detail page
    Task<CourseResponseDto?> GetCourseById(int courseId);

    // All courses by instructor — includes drafts (instructor sees their own)
    Task<IList<CourseSummaryDto>> GetCoursesByInstructor(int instructorId);

    // Public catalogue — both flags must be true
    Task<IList<CourseSummaryDto>> GetPublishedCourses();

    // Keyword search using EF Core LIKE
    Task<IList<CourseSummaryDto>> SearchCourses(string keyword);

    // Update course — resets approval if already approved
    Task<CourseResponseDto> UpdateCourse(int courseId, CreateCourseDto dto, int instructorId);

    // INSTRUCTOR: Submit for admin review — sets IsPublished = true
    Task PublishCourse(int courseId, int instructorId);

    // ADMIN: Approve course — sets IsApproved = true, now visible in catalogue
    Task ApproveCourse(int courseId);

    // ADMIN: Reject course — both flags false, back to draft
    Task RejectCourse(int courseId);

    // Delete course
    Task DeleteCourse(int courseId);

    // Top N most enrolled courses — for homepage
    Task<IList<CourseSummaryDto>> GetTopCourses(int count = 10);

    // Called by EnrollmentService when student enrolls
    Task IncrementEnrollment(int courseId);

    // Combined filter: category + level + keyword
    Task<IList<CourseSummaryDto>> FilterCourses(string? category, string? level, string? keyword);
}