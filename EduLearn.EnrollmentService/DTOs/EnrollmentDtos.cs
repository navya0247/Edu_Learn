namespace EduLearn.EnrollmentService.DTOs;

// ── REQUEST DTOs ──────────────────────────────────────────────────────────────

// POST /api/enrollments — enroll student in a course
public class EnrollRequestDto
{
    public int CourseId { get; set; }
    public string? PaymentId { get; set; } // null for free courses
}

// PUT /api/enrollments/{id}/progress — update progress percentage
public class UpdateProgressDto
{
    public int ProgressPercent { get; set; } // 0-100
}

// ── RESPONSE DTOs ─────────────────────────────────────────────────────────────

// Full enrollment details
public class EnrollmentResponseDto
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateTime EnrolledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = string.Empty;  // ACTIVE|COMPLETED|DROPPED
    public int ProgressPercent { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public bool CertificateIssued { get; set; }
    public string? PaymentId { get; set; }
}

// Analytics summary for instructor dashboard
public class EnrollmentAnalyticsDto
{
    public int CourseId { get; set; }
    public int TotalEnrolled { get; set; }
    public int TotalCompleted { get; set; }
    public int TotalInProgress { get; set; }
    public int TotalDropped { get; set; }
}