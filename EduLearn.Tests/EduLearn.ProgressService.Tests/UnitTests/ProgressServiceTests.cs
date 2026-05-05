using EduLearn.ProgressService.DTOs;
using EduLearn.ProgressService.Entities;
using EduLearn.ProgressService.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Net.Http;

// Alias to avoid namespace vs class name conflict
using ProgressServiceClass = EduLearn.ProgressService.Services.ProgressService;

namespace EduLearn.ProgressService.Tests.UnitTests;

/// <summary>
/// Unit tests for ProgressService.
/// Moq fakes the repository — no real database needed.
/// Total: 11 tests
/// </summary>
[TestFixture]
public class ProgressServiceTests
{
    private ProgressServiceClass _progressService = null!;
    private Mock<IProgressRepository> _mockRepo = null!;
    private Mock<ICertificateRepository> _mockCertRepo = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo     = new Mock<IProgressRepository>();
        _mockCertRepo = new Mock<ICertificateRepository>();
        var logger    = new Mock<ILogger<ProgressServiceClass>>().Object;

        // Mock IHttpClientFactory — ProgressService uses it to call EnrollmentService
        var mockHttpFactory = new Mock<IHttpClientFactory>();
        var mockHttpClient  = new Mock<HttpMessageHandler>();
        var httpClient      = new HttpClient(mockHttpClient.Object)
        {
            BaseAddress = new Uri("http://localhost:5004")
        };
        mockHttpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Create ProgressService with all 4 required dependencies
        _progressService = new ProgressServiceClass(
            _mockRepo.Object,
            _mockCertRepo.Object,
            logger,
            mockHttpFactory.Object
        );
    }

    // ── TEST 1: Track Lesson - First Time ─────────────────────────────────────

    [Test]
    [Description("TrackLesson creates a new record when student opens a lesson for the first time")]
    public async Task TrackLesson_FirstTime_CreatesNewProgressRecord()
    {
        // Arrange — no existing progress for this student+lesson
        _mockRepo.Setup(r => r.FindByStudentAndLesson(5, 1))
                 .ReturnsAsync((LessonProgress?)null);

        _mockRepo.Setup(r => r.Create(It.IsAny<LessonProgress>()))
                 .ReturnsAsync((LessonProgress lp) => { lp.LessonProgressId = 1; return lp; });

        var dto = new TrackLessonDto
        {
            StudentId    = 5,
            LessonId     = 1,
            CourseId     = 10,
            WatchPercent = 30
        };

        // Act
        var result = await _progressService.TrackLesson(dto);

        // Assert
        result.Should().NotBeNull();
        result.StudentId.Should().Be(5);
        result.LessonId.Should().Be(1);
        result.WatchPercent.Should().Be(30);
        result.IsCompleted.Should().BeFalse("just started watching — not completed yet");
    }

    // ── TEST 2: Track Lesson - Move Forward Only ──────────────────────────────

    [Test]
    [Description("TrackLesson should only update WatchPercent if new value is higher")]
    public async Task TrackLesson_LowerPercent_DoesNotGoBackward()
    {
        // Arrange — student already watched 70%
        var existing = new LessonProgress
        {
            LessonProgressId = 1,
            StudentId = 5, LessonId = 1, CourseId = 10,
            WatchPercent = 70, IsCompleted = false
        };

        _mockRepo.Setup(r => r.FindByStudentAndLesson(5, 1)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.Update(It.IsAny<LessonProgress>()))
                 .ReturnsAsync((LessonProgress lp) => lp);

        // Act — student sends 40% (they rewound the video)
        var dto = new TrackLessonDto { StudentId = 5, LessonId = 1, CourseId = 10, WatchPercent = 40 };
        await _progressService.TrackLesson(dto);

        // Assert — WatchPercent must stay at 70, not go back to 40
        _mockRepo.Verify(r => r.Update(It.Is<LessonProgress>(lp => lp.WatchPercent == 70)),
            Times.Once, "Progress must never go backward");
    }

    // ── TEST 3: Track Lesson - Move Forward ───────────────────────────────────

    [Test]
    [Description("TrackLesson updates WatchPercent when new value is higher than existing")]
    public async Task TrackLesson_HigherPercent_UpdatesWatchPercent()
    {
        // Arrange — student already at 40%
        var existing = new LessonProgress
        {
            LessonProgressId = 1,
            StudentId = 5, LessonId = 1, CourseId = 10,
            WatchPercent = 40, IsCompleted = false
        };

        _mockRepo.Setup(r => r.FindByStudentAndLesson(5, 1)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.Update(It.IsAny<LessonProgress>()))
                 .ReturnsAsync((LessonProgress lp) => lp);

        // Act — student progressed to 80%
        var dto = new TrackLessonDto { StudentId = 5, LessonId = 1, CourseId = 10, WatchPercent = 80 };
        await _progressService.TrackLesson(dto);

        // Assert — WatchPercent updated to 80
        _mockRepo.Verify(r => r.Update(It.Is<LessonProgress>(lp => lp.WatchPercent == 80)),
            Times.Once, "Progress must update to 80 since it is higher");
    }

    // ── TEST 4: Complete Lesson - First Time ──────────────────────────────────

    [Test]
    [Description("CompleteLesson creates completed record when no existing progress")]
    public async Task CompleteLesson_NoExistingRecord_CreatesCompletedRecord()
    {
        // Arrange — no existing progress
        _mockRepo.Setup(r => r.FindByStudentAndLesson(5, 1))
                 .ReturnsAsync((LessonProgress?)null);

        _mockRepo.Setup(r => r.Create(It.IsAny<LessonProgress>()))
                 .ReturnsAsync((LessonProgress lp) => lp);

        var dto = new CompleteLessonDto { StudentId = 5, LessonId = 1, CourseId = 10 };

        // Act
        var result = await _progressService.CompleteLesson(dto);

        // Assert
        result.IsCompleted.Should().BeTrue("lesson should be marked complete");
        result.WatchPercent.Should().Be(100, "completed lesson means 100% watched");

        // Verify Create was called
        _mockRepo.Verify(r => r.Create(It.Is<LessonProgress>(lp =>
            lp.IsCompleted == true && lp.WatchPercent == 100)), Times.Once);
    }

    // ── TEST 5: Complete Lesson - Existing Record ─────────────────────────────

    [Test]
    [Description("CompleteLesson updates existing record to set IsCompleted = true")]
    public async Task CompleteLesson_ExistingRecord_UpdatesToCompleted()
    {
        // Arrange — student was at 60%
        var existing = new LessonProgress
        {
            LessonProgressId = 1,
            StudentId = 5, LessonId = 1, CourseId = 10,
            WatchPercent = 60, IsCompleted = false
        };

        _mockRepo.Setup(r => r.FindByStudentAndLesson(5, 1)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.Update(It.IsAny<LessonProgress>()))
                 .ReturnsAsync((LessonProgress lp) => lp);

        var dto = new CompleteLessonDto { StudentId = 5, LessonId = 1, CourseId = 10 };

        // Act
        var result = await _progressService.CompleteLesson(dto);

        // Assert
        result.IsCompleted.Should().BeTrue();
        result.WatchPercent.Should().Be(100);

        // Verify Update was called (not Create) since record existed
        _mockRepo.Verify(r => r.Update(It.Is<LessonProgress>(lp =>
            lp.IsCompleted == true)), Times.Once);
    }

    // ── TEST 6: Get Course Progress - Partial ─────────────────────────────────

    [Test]
    [Description("GetCourseProgress calculates correct percentage when some lessons done")]
    public async Task GetCourseProgress_TwoOfFiveDone_Returns40Percent()
    {
        // Arrange — student completed 2 out of 5 lessons
        _mockRepo.Setup(r => r.CountCompletedLessons(5, 10)).ReturnsAsync(2);

        // Act — total lessons = 5 (passed in from caller)
        var result = await _progressService.GetCourseProgress(studentId: 5, courseId: 10, totalLessons: 5);

        // Assert
        result.CompletedLessons.Should().Be(2);
        result.TotalLessons.Should().Be(5);
        result.ProgressPercent.Should().Be(40, "2 of 5 = 40%");
        result.IsCourseComplete.Should().BeFalse("not all lessons done");
    }

    // ── TEST 7: Get Course Progress - All Done ────────────────────────────────

    [Test]
    [Description("GetCourseProgress returns 100% and IsCourseComplete=true when all lessons done")]
    public async Task GetCourseProgress_AllLessonsDone_Returns100Percent()
    {
        // Arrange — student completed all 4 lessons
        _mockRepo.Setup(r => r.CountCompletedLessons(5, 10)).ReturnsAsync(4);

        // Act
        var result = await _progressService.GetCourseProgress(studentId: 5, courseId: 10, totalLessons: 4);

        // Assert
        result.ProgressPercent.Should().Be(100);
        result.IsCourseComplete.Should().BeTrue("all lessons completed means course is done");
    }

    // ── TEST 8: Get Course Progress - No Lessons ──────────────────────────────

    [Test]
    [Description("GetCourseProgress returns 0% when course has no lessons")]
    public async Task GetCourseProgress_ZeroTotalLessons_Returns0Percent()
    {
        // Arrange
        _mockRepo.Setup(r => r.CountCompletedLessons(5, 10)).ReturnsAsync(0);

        // Act — edge case: totalLessons = 0
        var result = await _progressService.GetCourseProgress(studentId: 5, courseId: 10, totalLessons: 0);

        // Assert — should not divide by zero
        result.ProgressPercent.Should().Be(0, "0 lessons means 0% — no divide by zero error");
        result.IsCourseComplete.Should().BeFalse();
    }

    // ── TEST 9: Get Lesson Progress List ──────────────────────────────────────

    [Test]
    [Description("GetLessonProgress returns all lesson progress records for a student in a course")]
    public async Task GetLessonProgress_StudentWithProgress_ReturnsAllRecords()
    {
        // Arrange — 3 lessons tracked for this student
        var records = new List<LessonProgress>
        {
            new LessonProgress { LessonProgressId = 1, StudentId = 5, LessonId = 1, CourseId = 10, IsCompleted = true,  WatchPercent = 100 },
            new LessonProgress { LessonProgressId = 2, StudentId = 5, LessonId = 2, CourseId = 10, IsCompleted = false, WatchPercent = 50  },
            new LessonProgress { LessonProgressId = 3, StudentId = 5, LessonId = 3, CourseId = 10, IsCompleted = false, WatchPercent = 20  }
        };

        _mockRepo.Setup(r => r.FindByCourseAndStudent(5, 10)).ReturnsAsync(records);

        // Act
        var result = await _progressService.GetLessonProgress(studentId: 5, courseId: 10);

        // Assert
        result.Should().HaveCount(3, "all 3 lesson records should be returned");
        result.Count(r => r.IsCompleted).Should().Be(1, "only lesson 1 is completed");
    }

    // ── TEST 10: Get Lesson Progress - Empty ──────────────────────────────────

    [Test]
    [Description("GetLessonProgress returns empty list when student has no progress yet")]
    public async Task GetLessonProgress_NoProgress_ReturnsEmptyList()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByCourseAndStudent(5, 10))
                 .ReturnsAsync(new List<LessonProgress>());

        // Act
        var result = await _progressService.GetLessonProgress(studentId: 5, courseId: 10);

        // Assert
        result.Should().BeEmpty("new student has no progress yet");
    }

    // ── TEST 11: Complete Lesson Sets CompletedAt ─────────────────────────────

    [Test]
    [Description("CompleteLesson should set CompletedAt timestamp when marking done")]
    public async Task CompleteLesson_NewRecord_SetsCompletedAtTimestamp()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByStudentAndLesson(5, 1))
                 .ReturnsAsync((LessonProgress?)null);

        LessonProgress? captured = null;
        _mockRepo.Setup(r => r.Create(It.IsAny<LessonProgress>()))
                 .Callback<LessonProgress>(lp => captured = lp)
                 .ReturnsAsync((LessonProgress lp) => lp);

        var dto = new CompleteLessonDto { StudentId = 5, LessonId = 1, CourseId = 10 };

        // Act
        await _progressService.CompleteLesson(dto);

        // Assert — CompletedAt must be set
        captured.Should().NotBeNull();
        captured!.CompletedAt.Should().NotBeNull("CompletedAt must be set when lesson is completed");
        captured.CompletedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}