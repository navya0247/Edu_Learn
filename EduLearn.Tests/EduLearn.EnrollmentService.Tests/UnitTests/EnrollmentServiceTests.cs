using EduLearn.EnrollmentService.DTOs;
using EduLearn.EnrollmentService.Entities;
using EduLearn.EnrollmentService.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

// Alias to avoid namespace vs class name conflict
using EnrollmentServiceClass = EduLearn.EnrollmentService.Services.EnrollmentService;

namespace EduLearn.EnrollmentService.Tests.UnitTests;

/// <summary>
/// Unit tests for EnrollmentService.
/// Moq fakes the repository — no real database needed.
/// Total: 11 tests
/// </summary>
[TestFixture]
public class EnrollmentServiceTests
{
    private EnrollmentServiceClass _enrollmentService = null!;
    private Mock<IEnrollmentRepository> _mockRepo = null!;

    // A sample enrollment reused across many tests
    private Enrollment _sampleEnrollment = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IEnrollmentRepository>();
        var logger = new Mock<ILogger<EnrollmentServiceClass>>().Object;

        _enrollmentService = new EnrollmentServiceClass(_mockRepo.Object, logger);

        // Sample active enrollment used in most tests
        _sampleEnrollment = new Enrollment
        {
            EnrollmentId    = 1,
            StudentId       = 5,
            CourseId        = 10,
            EnrolledAt      = DateTime.UtcNow,
            Status          = "ACTIVE",
            ProgressPercent = 0,
            CertificateIssued = false
        };
    }

    // ── TEST 1: Enroll Student ────────────────────────────────────────────────

    [Test]
    [Description("Enroll should create enrollment with ACTIVE status and 0% progress")]
    public async Task Enroll_NewStudent_CreatesActiveEnrollment()
    {
        // Arrange — student is NOT already enrolled
        _mockRepo.Setup(r => r.IsEnrolled(5, 10)).ReturnsAsync(false);
        _mockRepo.Setup(r => r.Create(It.IsAny<Enrollment>()))
                 .ReturnsAsync((Enrollment e) => { e.EnrollmentId = 1; return e; });

        var dto = new EnrollRequestDto { CourseId = 10, PaymentId = null };

        // Act
        var result = await _enrollmentService.Enroll(studentId: 5, dto: dto);

        // Assert
        result.Should().NotBeNull();
        result.StudentId.Should().Be(5);
        result.CourseId.Should().Be(10);
        result.Status.Should().Be("ACTIVE");
        result.ProgressPercent.Should().Be(0);
    }

    // ── TEST 2: Enroll - Duplicate Prevention ─────────────────────────────────

    [Test]
    [Description("Enroll should throw if student is already enrolled in the same course")]
    public async Task Enroll_AlreadyEnrolled_ThrowsInvalidOperationException()
    {
        // Arrange — student IS already enrolled
        _mockRepo.Setup(r => r.IsEnrolled(5, 10)).ReturnsAsync(true);

        var dto = new EnrollRequestDto { CourseId = 10 };

        // Act & Assert
        var act = async () => await _enrollmentService.Enroll(studentId: 5, dto: dto);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*already enrolled*");
    }

    // ── TEST 3: Enroll - Paid Course ──────────────────────────────────────────

    [Test]
    [Description("Enroll for paid course should save PaymentId on enrollment")]
    public async Task Enroll_PaidCourse_SavesPaymentId()
    {
        // Arrange
        _mockRepo.Setup(r => r.IsEnrolled(5, 10)).ReturnsAsync(false);
        _mockRepo.Setup(r => r.Create(It.IsAny<Enrollment>()))
                 .ReturnsAsync((Enrollment e) => e);

        var dto = new EnrollRequestDto { CourseId = 10, PaymentId = "pay_razorpay_123" };

        // Act
        var result = await _enrollmentService.Enroll(studentId: 5, dto: dto);

        // Assert
        result.PaymentId.Should().Be("pay_razorpay_123", "paid course must save PaymentId");
    }

    // ── TEST 4: Get Enrollment By ID ──────────────────────────────────────────

    [Test]
    [Description("GetEnrollmentById should return enrollment when it exists")]
    public async Task GetEnrollmentById_Existing_ReturnsEnrollment()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByEnrollmentId(1)).ReturnsAsync(_sampleEnrollment);

        // Act
        var result = await _enrollmentService.GetEnrollmentById(1);

        // Assert
        result.Should().NotBeNull();
        result!.EnrollmentId.Should().Be(1);
        result.Status.Should().Be("ACTIVE");
    }

    // ── TEST 5: Get Enrollment By ID - Not Found ──────────────────────────────

    [Test]
    [Description("GetEnrollmentById should return null for non-existent enrollment")]
    public async Task GetEnrollmentById_NotFound_ReturnsNull()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByEnrollmentId(999)).ReturnsAsync((Enrollment?)null);

        // Act
        var result = await _enrollmentService.GetEnrollmentById(999);

        // Assert
        result.Should().BeNull();
    }

    // ── TEST 6: Update Progress ───────────────────────────────────────────────

    [Test]
    [Description("UpdateProgress should save new progress percentage on enrollment")]
    public async Task UpdateProgress_ValidPercent_SavesNewProgress()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByEnrollmentId(1)).ReturnsAsync(_sampleEnrollment);
        _mockRepo.Setup(r => r.Update(It.IsAny<Enrollment>()))
                 .ReturnsAsync((Enrollment e) => e);

        // Act — student completed 60% of the course
        await _enrollmentService.UpdateProgress(enrollmentId: 1, progressPercent: 60);

        // Assert — verify Update called with ProgressPercent = 60
        _mockRepo.Verify(r => r.Update(It.Is<Enrollment>(e => e.ProgressPercent == 60)),
            Times.Once, "Progress must be saved as 60");
    }

    // ── TEST 7: Update Progress to 100% - Auto Complete ───────────────────────

    [Test]
    [Description("UpdateProgress to 100 should auto-complete the enrollment")]
    public async Task UpdateProgress_100Percent_AutoCompletesEnrollment()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByEnrollmentId(1)).ReturnsAsync(_sampleEnrollment);
        _mockRepo.Setup(r => r.Update(It.IsAny<Enrollment>()))
                 .ReturnsAsync((Enrollment e) => e);

        // Act — student reaches 100%
        await _enrollmentService.UpdateProgress(enrollmentId: 1, progressPercent: 100);

        // Assert — enrollment should be COMPLETED, not just updated
        _mockRepo.Verify(r => r.Update(It.Is<Enrollment>(e =>
            e.Status == "COMPLETED" && e.ProgressPercent == 100)), Times.Once,
            "100% progress must auto-complete the enrollment");
    }

    // ── TEST 8: Drop Course ───────────────────────────────────────────────────

    [Test]
    [Description("DropCourse should set enrollment status to DROPPED")]
    public async Task DropCourse_ActiveEnrollment_SetsDroppedStatus()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByEnrollmentId(1)).ReturnsAsync(_sampleEnrollment);
        _mockRepo.Setup(r => r.Update(It.IsAny<Enrollment>()))
                 .ReturnsAsync((Enrollment e) => e);

        // Act — student 5 drops their own enrollment
        await _enrollmentService.DropCourse(enrollmentId: 1, studentId: 5);

        // Assert
        _mockRepo.Verify(r => r.Update(It.Is<Enrollment>(e => e.Status == "DROPPED")),
            Times.Once, "Dropped enrollment must have DROPPED status");
    }

    // ── TEST 9: Drop Course - Wrong Student ───────────────────────────────────

    [Test]
    [Description("DropCourse by a different student should throw UnauthorizedAccessException")]
    public async Task DropCourse_WrongStudent_ThrowsUnauthorized()
    {
        // Arrange — enrollment belongs to student 5, but student 99 tries to drop it
        _mockRepo.Setup(r => r.FindByEnrollmentId(1)).ReturnsAsync(_sampleEnrollment);

        // Act & Assert
        var act = async () => await _enrollmentService.DropCourse(enrollmentId: 1, studentId: 99);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
                 .WithMessage("*another student*");
    }

    // ── TEST 10: Drop Completed Course ────────────────────────────────────────

    [Test]
    [Description("DropCourse on a completed enrollment should throw InvalidOperationException")]
    public async Task DropCourse_CompletedEnrollment_ThrowsInvalidOperation()
    {
        // Arrange — enrollment is already COMPLETED
        _sampleEnrollment.Status = "COMPLETED";
        _mockRepo.Setup(r => r.FindByEnrollmentId(1)).ReturnsAsync(_sampleEnrollment);

        // Act & Assert
        var act = async () => await _enrollmentService.DropCourse(enrollmentId: 1, studentId: 5);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*completed course*");
    }

    // ── TEST 11: Course Analytics ─────────────────────────────────────────────

    [Test]
    [Description("GetCourseAnalytics should correctly count enrolled, completed, dropped students")]
    public async Task GetCourseAnalytics_CourseWithMixedStatuses_ReturnsCorrectCounts()
    {
        // Arrange — 4 enrollments: 2 active, 1 completed, 1 dropped
        var enrollments = new List<Enrollment>
        {
            new Enrollment { StudentId = 1, CourseId = 10, Status = "ACTIVE",    ProgressPercent = 50 },
            new Enrollment { StudentId = 2, CourseId = 10, Status = "ACTIVE",    ProgressPercent = 20 },
            new Enrollment { StudentId = 3, CourseId = 10, Status = "COMPLETED", ProgressPercent = 100 },
            new Enrollment { StudentId = 4, CourseId = 10, Status = "DROPPED",   ProgressPercent = 10 }
        };

        _mockRepo.Setup(r => r.FindByCourseId(10)).ReturnsAsync(enrollments);

        // Act
        var result = await _enrollmentService.GetCourseAnalytics(courseId: 10);

        // Assert
        result.TotalEnrolled.Should().Be(3, "3 students are enrolled (active + completed, not dropped)");
        result.TotalCompleted.Should().Be(1, "1 student completed");
        result.TotalDropped.Should().Be(1, "1 student dropped");
        result.TotalInProgress.Should().Be(2, "2 students are actively in progress");
    }
}
