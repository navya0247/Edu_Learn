using EduLearn.ProgressService.DTOs;
using EduLearn.ProgressService.Entities;
using EduLearn.ProgressService.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EduLearn.ProgressService.Services;

// Certificate issuance and PDF generation using QuestPDF
public class CertificateService : ICertificateService
{
    private readonly ICertificateRepository _repo;
    private readonly ILogger<CertificateService> _logger;

    public CertificateService(ICertificateRepository repo, ILogger<CertificateService> logger)
    {
        _repo   = repo;
        _logger = logger;

        // QuestPDF community license — free for open source projects
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // Issue certificate — one per student per course (unique index enforced in DB)
    public async Task<CertificateDto> IssueCertificate(IssueCertificateDto dto)
    {
        // Check if already issued — don't duplicate
        var existing = await _repo.FindByStudentAndCourse(dto.StudentId, dto.CourseId);
        if (existing != null)
            return MapToDto(existing);

        var certificate = new Certificate
        {
            StudentId       = dto.StudentId,
            CourseId        = dto.CourseId,
            StudentName     = dto.StudentName,
            CourseName      = dto.CourseName,
            IssuedAt        = DateTime.UtcNow,
            // Unique code: CERT-{first 8 chars of new GUID uppercase}
            CertificateCode = $"CERT-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}"
        };

        var created = await _repo.Create(certificate);
        _logger.LogInformation("Certificate {Code} issued to Student {StudentId} for Course {CourseId}",
            created.CertificateCode, dto.StudentId, dto.CourseId);

        return MapToDto(created);
    }

    public async Task<CertificateDto?> GetCertificate(int studentId, int courseId)
    {
        var cert = await _repo.FindByStudentAndCourse(studentId, courseId);
        return cert == null ? null : MapToDto(cert);
    }

    public async Task<IList<CertificateDto>> GetStudentCertificates(int studentId)
    {
        var certs = await _repo.FindByStudentId(studentId);
        return certs.Select(MapToDto).ToList();
    }

    // PUBLIC verification — no auth needed — students share this URL on LinkedIn etc.
    public async Task<CertificateDto?> VerifyCertificate(string certificateCode)
    {
        var cert = await _repo.FindByCertificateCode(certificateCode);
        return cert == null ? null : MapToDto(cert);
    }

    // Generate certificate PDF using QuestPDF
    // Returns PDF as byte array — caller decides to stream or save to Azure Blob
    public async Task<byte[]> GenerateCertificatePdf(int certificateId)
    {
        var cert = await _repo.FindByCertificateCode(certificateId.ToString())
            ?? throw new KeyNotFoundException($"Certificate {certificateId} not found.");

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(2, Unit.Centimetre);
                page.Background(Colors.White);

                page.Content().Column(col =>
                {
                    // Header
                    col.Item().AlignCenter().Text("EduLearn LMS")
                        .FontSize(28).Bold().FontColor(Colors.Blue.Darken2);

                    col.Item().Height(20);

                    // Title
                    col.Item().AlignCenter().Text("Certificate of Completion")
                        .FontSize(22).Bold();

                    col.Item().Height(30);

                    // Body
                    col.Item().AlignCenter().Text("This certifies that")
                        .FontSize(14).FontColor(Colors.Grey.Darken1);

                    col.Item().Height(10);

                    col.Item().AlignCenter().Text(cert.StudentName)
                        .FontSize(26).Bold().FontColor(Colors.Blue.Darken3);

                    col.Item().Height(10);

                    col.Item().AlignCenter().Text("has successfully completed")
                        .FontSize(14).FontColor(Colors.Grey.Darken1);

                    col.Item().Height(10);

                    col.Item().AlignCenter().Text(cert.CourseName)
                        .FontSize(20).Bold();

                    col.Item().Height(30);

                    // Date and code
                    col.Item().AlignCenter().Text($"Issued on: {cert.IssuedAt:MMMM dd, yyyy}")
                        .FontSize(12);

                    col.Item().Height(10);

                    col.Item().AlignCenter().Text($"Certificate Code: {cert.CertificateCode}")
                        .FontSize(11).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();

        await Task.CompletedTask;
        return pdfBytes;
    }

    private static CertificateDto MapToDto(Certificate c) => new()
    {
        CertificateId   = c.CertificateId,
        StudentId       = c.StudentId,
        CourseId        = c.CourseId,
        StudentName     = c.StudentName,
        CourseName      = c.CourseName,
        IssuedAt        = c.IssuedAt,
        CertificateCode = c.CertificateCode,
        PdfUrl          = c.PdfUrl
    };
}