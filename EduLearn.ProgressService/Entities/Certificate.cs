namespace EduLearn.ProgressService.Entities;

// Certificate — issued when student completes a course (100% progress)
// CertificateCode is unique public verification code
// VerifyCertificate endpoint is PUBLIC — no auth needed (for sharing/verification)
public class Certificate
{
    public int CertificateId { get; set; }

    // FK to User in AuthService (different DB)
    public int StudentId { get; set; }

    // FK to Course in CourseService (different DB)
    public int CourseId { get; set; }

    // Student name printed on certificate
    public string StudentName { get; set; } = string.Empty;

    // Course title printed on certificate
    public string CourseName { get; set; } = string.Empty;

    // When certificate was generated
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    // Unique public code — used for verification without login
    // Format: CERT-{GUID} e.g. CERT-A1B2C3D4
    public string CertificateCode { get; set; } = string.Empty;

    // Azure Blob URL of generated PDF — null until GeneratePdf() is called
    public string? PdfUrl { get; set; }
}