namespace EduLearn.EnrollmentService.Entities;

// Enrollment entity — maps to Enrollments table in EduLearn_Enrollment PostgreSQL database
// Represents the student-course relationship
// Status: ACTIVE | COMPLETED | DROPPED
// ProgressPercent = (completedLessons / totalLessons) * 100
public class Enrollment
{
    public int EnrollmentId { get; set; }

    // FK to User in AuthService (different DB — no navigation property)
    public int StudentId { get; set; }

    // FK to Course in CourseService (different DB — no navigation property)
    public int CourseId { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    // Set when Status becomes COMPLETED
    public DateTime? CompletedAt { get; set; }

    // ACTIVE = currently enrolled
    // COMPLETED = finished all lessons and quizzes
    // DROPPED = student unenrolled before completion
    public string Status { get; set; } = "ACTIVE";

    // 0-100 — updated every time student marks a lesson complete
    public int ProgressPercent { get; set; } = 0;

    // Last time student accessed the course — used for resume feature
    public DateTime? LastAccessedAt { get; set; }

    // true when certificate has been issued
    public bool CertificateIssued { get; set; } = false;

    // Payment reference for paid courses — null for free courses
    public string? PaymentId { get; set; }
}