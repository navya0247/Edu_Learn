using EduLearn.EnrollmentService.Data;
using EduLearn.EnrollmentService.Entities;
using EduLearn.EnrollmentService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.EnrollmentService.Repositories;

// EF Core implementation of IEnrollmentRepository
// Connected to EduLearn_Enrollment PostgreSQL database
public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly EnrollmentDbContext _db;

    public EnrollmentRepository(EnrollmentDbContext db) => _db = db;

    public async Task<Enrollment?> FindByEnrollmentId(int enrollmentId)
        => await _db.Enrollments.FindAsync(enrollmentId);

    // Used to get existing enrollment for progress update or drop
    public async Task<Enrollment?> FindByStudentAndCourse(int studentId, int courseId)
        => await _db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);

    // Quick boolean check — used before allowing lesson access
    public async Task<bool> IsEnrolled(int studentId, int courseId)
        => await _db.Enrollments
            .AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId
                        && e.Status != "DROPPED");

    // All enrollments for a student — for My Courses page
    public async Task<IList<Enrollment>> FindByStudentId(int studentId)
        => await _db.Enrollments
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();

    // All enrollments for a course — for instructor analytics dashboard
    public async Task<IList<Enrollment>> FindByCourseId(int courseId)
        => await _db.Enrollments
            .Where(e => e.CourseId == courseId)
            .ToListAsync();

    public async Task<IList<Enrollment>> FindCompletedByStudent(int studentId)
        => await _db.Enrollments
            .Where(e => e.StudentId == studentId && e.Status == "COMPLETED")
            .ToListAsync();

    public async Task<IList<Enrollment>> FindInProgressByStudent(int studentId)
        => await _db.Enrollments
            .Where(e => e.StudentId == studentId && e.Status == "ACTIVE"
                     && e.ProgressPercent > 0)
            .ToListAsync();

    public async Task<int> CountByCourseId(int courseId)
        => await _db.Enrollments
            .CountAsync(e => e.CourseId == courseId && e.Status != "DROPPED");

    public async Task<Enrollment> Create(Enrollment enrollment)
    {
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();
        return enrollment;
    }

    public async Task<Enrollment> Update(Enrollment enrollment)
    {
        _db.Enrollments.Update(enrollment);
        await _db.SaveChangesAsync();
        return enrollment;
    }
}