using EduLearn.CourseService.Entities;

namespace EduLearn.CourseService.Interfaces;

/// <summary>
/// Repository interface — all async DB operations for Course.
/// Implemented by CourseRepository using EF Core + PostgreSQL.
/// </summary>
public interface ICourseRepository
{
    // Find single course by PK
    Task<Course?> FindByCourseId(int courseId);

    // All courses by instructor — includes unpublished drafts
    Task<IList<Course>> FindByInstructorId(int instructorId);

    // Filter public courses by category
    Task<IList<Course>> FindByCategory(string category);

    // Full public catalogue — IsPublished AND IsApproved both true
    Task<IList<Course>> FindPublishedAndApproved();

    // EF Core LIKE search on Title, Description, Category
    Task<IList<Course>> SearchCourses(string keyword);

    // Top N by EnrollmentCount — homepage featured section
    Task<IList<Course>> FindTopCourses(int count);

    // Filter by category AND level together
    Task<IList<Course>> FindByCategoryAndLevel(string? category, string? level);

    // Count instructor's total courses
    Task<int> CountByInstructorId(int instructorId);

    // Atomic increment — ExecuteUpdateAsync (no entity load needed)
    Task IncrementEnrollment(int courseId);

    // CRUD
    Task<Course> Create(Course course);
    Task<Course> Update(Course course);
    Task Delete(int courseId);
}