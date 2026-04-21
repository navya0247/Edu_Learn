using EduLearn.EnrollmentService.Entities;

namespace EduLearn.EnrollmentService.Interfaces;

// Repository interface — all DB operations for Enrollment
// Implemented by EnrollmentRepository using EF Core + PostgreSQL
public interface IEnrollmentRepository
{
    Task<Enrollment?> FindByEnrollmentId(int enrollmentId);
    Task<Enrollment?> FindByStudentAndCourse(int studentId, int courseId);
    Task<bool> IsEnrolled(int studentId, int courseId);             // prevents duplicate enrollment
    Task<IList<Enrollment>> FindByStudentId(int studentId);          // student's course list
    Task<IList<Enrollment>> FindByCourseId(int courseId);           // instructor analytics
    Task<IList<Enrollment>> FindCompletedByStudent(int studentId);
    Task<IList<Enrollment>> FindInProgressByStudent(int studentId);
    Task<int> CountByCourseId(int courseId);
    Task<Enrollment> Create(Enrollment enrollment);
    Task<Enrollment> Update(Enrollment enrollment);
}