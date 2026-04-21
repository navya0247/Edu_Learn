using EduLearn.CourseService.Data;
using EduLearn.CourseService.Entities;
using EduLearn.CourseService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.CourseService.Repositories;

/// <summary>
/// EF Core implementation of ICourseRepository.
/// Connected to EduLearn_Course PostgreSQL database.
/// </summary>
public class CourseRepository : ICourseRepository
{
    private readonly CourseDbContext _db;

    public CourseRepository(CourseDbContext db) => _db = db;

    // PK lookup — EF checks change tracker cache first (faster)
    public async Task<Course?> FindByCourseId(int courseId)
        => await _db.Courses.FindAsync(courseId);

    // All courses by instructor — includes unpublished drafts
    public async Task<IList<Course>> FindByInstructorId(int instructorId)
        => await _db.Courses
            .Where(c => c.InstructorId == instructorId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    // Public courses filtered by category
    public async Task<IList<Course>> FindByCategory(string category)
        => await _db.Courses
            .Where(c => c.Category == category
                     && c.IsPublished
                     && c.IsApproved)
            .ToListAsync();

    // Full public catalogue — both flags must be true
    public async Task<IList<Course>> FindPublishedAndApproved()
        => await _db.Courses
            .Where(c => c.IsPublished && c.IsApproved)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    // EF Core LIKE — maps to SQL LIKE '%keyword%' in PostgreSQL
    public async Task<IList<Course>> SearchCourses(string keyword)
        => await _db.Courses
            .Where(c => c.IsPublished && c.IsApproved &&
                (EF.Functions.Like(c.Title, $"%{keyword}%") ||
                 EF.Functions.Like(c.Description, $"%{keyword}%") ||
                 EF.Functions.Like(c.Category, $"%{keyword}%")))
            .ToListAsync();

    // Top N by enrollment count — most popular courses
    public async Task<IList<Course>> FindTopCourses(int count)
        => await _db.Courses
            .Where(c => c.IsPublished && c.IsApproved)
            .OrderByDescending(c => c.EnrollmentCount)
            .Take(count)
            .ToListAsync();

    // Combined filter — any combination of category and level
    public async Task<IList<Course>> FindByCategoryAndLevel(string? category, string? level)
    {
        var query = _db.Courses.Where(c => c.IsPublished && c.IsApproved);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(c => c.Category == category);

        if (!string.IsNullOrEmpty(level))
            query = query.Where(c => c.Level == level);

        return await query.ToListAsync();
    }

    public async Task<int> CountByInstructorId(int instructorId)
        => await _db.Courses.CountAsync(c => c.InstructorId == instructorId);

    /// <summary>
    /// Atomic increment via ExecuteUpdateAsync.
    /// SQL: UPDATE "Courses" SET "EnrollmentCount" = "EnrollmentCount" + 1 WHERE "CourseId" = @id
    /// No entity load needed — very efficient.
    /// </summary>
    public async Task IncrementEnrollment(int courseId)
        => await _db.Courses
            .Where(c => c.CourseId == courseId)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(c => c.EnrollmentCount, c => c.EnrollmentCount + 1));

    public async Task<Course> Create(Course course)
    {
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();
        return course;
    }

    public async Task<Course> Update(Course course)
    {
        course.UpdatedAt = DateTime.UtcNow;
        _db.Courses.Update(course);
        await _db.SaveChangesAsync();
        return course;
    }

    // ExecuteDeleteAsync — direct SQL DELETE without entity load
    public async Task Delete(int courseId)
        => await _db.Courses
            .Where(c => c.CourseId == courseId)
            .ExecuteDeleteAsync();
}