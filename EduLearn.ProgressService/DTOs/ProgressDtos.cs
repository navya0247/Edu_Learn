namespace EduLearn.ProgressService.DTOs;

// ── REQUEST DTOs ──────────────────────────────────────────────────────────────

// POST /api/progress/lesson — mark lesson as started or update watch percent
public class TrackLessonDto
{
    public int StudentId { get; set; }
    public int LessonId { get; set; }
    public int CourseId { get; set; }
    public int WatchPercent { get; set; } = 0;  // 0-100
}

// PUT /api/progress/lesson/{id}/complete — mark lesson as done
public class CompleteLessonDto
{
    public int StudentId { get; set; }
    public int LessonId { get; set; }
    public int CourseId { get; set; }
}

// POST /api/certificates — issue certificate after course completion
public class IssueCertificateDto
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
}

// ── RESPONSE DTOs ─────────────────────────────────────────────────────────────

// Lesson progress details
public class LessonProgressDto
{
    public int LessonProgressId { get; set; }
    public int StudentId { get; set; }
    public int LessonId { get; set; }
    public int CourseId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int WatchPercent { get; set; }
}

// Course progress summary — used to compute progress % for EnrollmentService
public class CourseProgressDto
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public int ProgressPercent { get; set; }  // (completed/total)*100
    public bool IsCourseComplete { get; set; } // true when ProgressPercent == 100
}

// Certificate details
public class CertificateDto
{
    public int CertificateId { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public string CertificateCode { get; set; } = string.Empty;  // for public verification
    public string? PdfUrl { get; set; }
}