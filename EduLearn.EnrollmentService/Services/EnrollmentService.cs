using EduLearn.EnrollmentService.DTOs;
using EduLearn.EnrollmentService.Entities;
using EduLearn.EnrollmentService.Interfaces;

namespace EduLearn.EnrollmentService.Services;

// Business logic for student-course enrollment management
public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _repo;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(IEnrollmentRepository repo, ILogger<EnrollmentService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    // Enroll student — checks for duplicate enrollment first
    public async Task<EnrollmentResponseDto> Enroll(int studentId, EnrollRequestDto dto)
    {
        // Prevent duplicate enrollments — unique index in DB also enforces this
        if (await _repo.IsEnrolled(studentId, dto.CourseId))
            throw new InvalidOperationException("Student is already enrolled in this course.");

        var enrollment = new Enrollment
        {
            StudentId   = studentId,
            CourseId    = dto.CourseId,
            EnrolledAt  = DateTime.UtcNow,
            Status      = "ACTIVE",
            ProgressPercent = 0,
            PaymentId   = dto.PaymentId
        };

        var created = await _repo.Create(enrollment);
        _logger.LogInformation("Student {StudentId} enrolled in Course {CourseId}",
            studentId, dto.CourseId);

        return MapToResponse(created);
    }

    public async Task<EnrollmentResponseDto?> GetEnrollmentById(int enrollmentId)
    {
        var enrollment = await _repo.FindByEnrollmentId(enrollmentId);
        return enrollment == null ? null : MapToResponse(enrollment);
    }

    public async Task<IList<EnrollmentResponseDto>> GetEnrollmentsByStudent(int studentId)
    {
        var enrollments = await _repo.FindByStudentId(studentId);
        return enrollments.Select(MapToResponse).ToList();
    }

    public async Task<IList<EnrollmentResponseDto>> GetEnrollmentsByCourse(int courseId)
    {
        var enrollments = await _repo.FindByCourseId(courseId);
        return enrollments.Select(MapToResponse).ToList();
    }

    public async Task<bool> IsEnrolled(int studentId, int courseId)
        => await _repo.IsEnrolled(studentId, courseId);

    // Update progress — called by ProgressService after each lesson completion
    public async Task UpdateProgress(int enrollmentId, int progressPercent)
    {
        var enrollment = await _repo.FindByEnrollmentId(enrollmentId)
            ?? throw new KeyNotFoundException($"Enrollment {enrollmentId} not found.");

        enrollment.ProgressPercent = Math.Clamp(progressPercent, 0, 100);
        enrollment.LastAccessedAt  = DateTime.UtcNow;

        // Auto-complete if 100%
        if (enrollment.ProgressPercent == 100)
            await CompleteEnrollment(enrollmentId);
        else
            await _repo.Update(enrollment);
    }

    // Mark enrollment as COMPLETED — triggers certificate eligibility
    public async Task CompleteEnrollment(int enrollmentId)
    {
        var enrollment = await _repo.FindByEnrollmentId(enrollmentId)
            ?? throw new KeyNotFoundException($"Enrollment {enrollmentId} not found.");

        enrollment.Status          = "COMPLETED";
        enrollment.ProgressPercent = 100;
        enrollment.CompletedAt     = DateTime.UtcNow;

        await _repo.Update(enrollment);
        _logger.LogInformation("Enrollment {EnrollmentId} completed — student {StudentId}",
            enrollmentId, enrollment.StudentId);
    }

    // Drop course — only ACTIVE enrollments can be dropped
    public async Task DropCourse(int enrollmentId, int studentId)
    {
        var enrollment = await _repo.FindByEnrollmentId(enrollmentId)
            ?? throw new KeyNotFoundException($"Enrollment {enrollmentId} not found.");

        // Only the enrolled student can drop their own course
        if (enrollment.StudentId != studentId)
            throw new UnauthorizedAccessException("Cannot drop another student's enrollment.");

        if (enrollment.Status == "COMPLETED")
            throw new InvalidOperationException("Cannot drop a completed course.");

        enrollment.Status = "DROPPED";
        await _repo.Update(enrollment);
        _logger.LogInformation("Student {StudentId} dropped Course {CourseId}",
            studentId, enrollment.CourseId);
    }

    public async Task<IList<EnrollmentResponseDto>> GetCompletedCourses(int studentId)
    {
        var enrollments = await _repo.FindCompletedByStudent(studentId);
        return enrollments.Select(MapToResponse).ToList();
    }

    public async Task<IList<EnrollmentResponseDto>> GetInProgressCourses(int studentId)
    {
        var enrollments = await _repo.FindInProgressByStudent(studentId);
        return enrollments.Select(MapToResponse).ToList();
    }

    public async Task<int> GetEnrollmentCount(int courseId)
        => await _repo.CountByCourseId(courseId);

    // Course analytics for instructor dashboard
    public async Task<EnrollmentAnalyticsDto> GetCourseAnalytics(int courseId)
    {
        var all = await _repo.FindByCourseId(courseId);
        return new EnrollmentAnalyticsDto
        {
            CourseId       = courseId,
            TotalEnrolled  = all.Count(e => e.Status != "DROPPED"),
            TotalCompleted = all.Count(e => e.Status == "COMPLETED"),
            TotalInProgress = all.Count(e => e.Status == "ACTIVE" && e.ProgressPercent > 0),
            TotalDropped   = all.Count(e => e.Status == "DROPPED")
        };
    }

    // ── Private Helper ────────────────────────────────────────────────────────
    private static EnrollmentResponseDto MapToResponse(Enrollment e) => new()
    {
        EnrollmentId      = e.EnrollmentId,
        StudentId         = e.StudentId,
        CourseId          = e.CourseId,
        EnrolledAt        = e.EnrolledAt,
        CompletedAt       = e.CompletedAt,
        Status            = e.Status,
        ProgressPercent   = e.ProgressPercent,
        LastAccessedAt    = e.LastAccessedAt,
        CertificateIssued = e.CertificateIssued,
        PaymentId         = e.PaymentId
    };
}