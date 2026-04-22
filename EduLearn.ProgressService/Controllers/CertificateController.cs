using EduLearn.ProgressService.DTOs;
using EduLearn.ProgressService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduLearn.ProgressService.Controllers;

[ApiController]
[Route("api/certificates")]
[Produces("application/json")]
public class CertificateController : ControllerBase
{
    private readonly ICertificateService _certificateService;
    private readonly ILogger<CertificateController> _logger;

    public CertificateController(ICertificateService certificateService, ILogger<CertificateController> logger)
    {
        _certificateService = certificateService;
        _logger             = logger;
    }

    // ── Public Endpoints ──────────────────────────────────────────────────────

    /// <summary>Verify certificate by unique code — PUBLIC, no login required</summary>
    [HttpGet("verify/{code}")]
    [ProducesResponseType(typeof(CertificateDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Verify(string code)
    {
        var cert = await _certificateService.VerifyCertificate(code);
        if (cert == null) return NotFound(new { message = "Certificate not found or invalid code" });
        return Ok(cert);
    }

    // ── Student Endpoints ─────────────────────────────────────────────────────

    /// <summary>Get all certificates earned by current student</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpGet("my-certificates")]
    [ProducesResponseType(typeof(IList<CertificateDto>), 200)]
    public async Task<IActionResult> GetMyCertificates()
    {
        var certs = await _certificateService.GetStudentCertificates(GetCurrentUserId());
        return Ok(certs);
    }

    /// <summary>Get certificate for a specific completed course</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpGet("course/{courseId}")]
    [ProducesResponseType(typeof(CertificateDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByCourse(int courseId)
    {
        var cert = await _certificateService.GetCertificate(GetCurrentUserId(), courseId);
        if (cert == null) return NotFound(new { message = "Certificate not found for this course" });
        return Ok(cert);
    }

    /// <summary>Download certificate PDF — returns PDF file bytes</summary>
    [Authorize(Roles = "STUDENT")]
    [HttpGet("{id}/download")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Download(int id)
    {
        try
        {
            var pdfBytes = await _certificateService.GenerateCertificatePdf(id);
            return File(pdfBytes, "application/pdf", $"certificate-{id}.pdf");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── Admin / System Endpoints ──────────────────────────────────────────────

    /// <summary>Issue certificate — called when student completes course (100% progress)</summary>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(CertificateDto), 201)]
    public async Task<IActionResult> Issue([FromBody] IssueCertificateDto dto)
    {
        var cert = await _certificateService.IssueCertificate(dto);
        _logger.LogInformation("Certificate issued: {Code}", cert.CertificateCode);
        return CreatedAtAction(nameof(Verify), new { code = cert.CertificateCode }, cert);
    }

    /// <summary>Get all certificates for a student — Admin only</summary>
    [Authorize(Roles = "ADMIN")]
    [HttpGet("student/{studentId}")]
    [ProducesResponseType(typeof(IList<CertificateDto>), 200)]
    public async Task<IActionResult> GetByStudent(int studentId)
    {
        var certs = await _certificateService.GetStudentCertificates(studentId);
        return Ok(certs);
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }
}