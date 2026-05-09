using EduLearn.LessonService.DTOs;
using EduLearn.LessonService.Entities;
using EduLearn.LessonService.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

// Alias to avoid namespace vs class name conflict (same as CourseService fix)
using LessonServiceClass = EduLearn.LessonService.Services.LessonService;

namespace EduLearn.LessonService.Tests.UnitTests;

/// <summary>
/// Unit tests for LessonService.
/// Moq fakes the repository and Azure Blob service — no real DB or Azure needed.
/// Total: 11 tests
/// </summary>
[TestFixture]
public class LessonServiceTests
{
    // The class we are testing
    private LessonServiceClass _lessonService = null!;

    // Mocked dependencies
    private Mock<ILessonRepository> _mockRepo = null!;
    private Mock<IAzureBlobService> _mockBlob = null!;

    // A sample lesson reused across many tests
    private Lesson _sampleLesson = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<ILessonRepository>();
        _mockBlob = new Mock<IAzureBlobService>();
        var logger = new Mock<ILogger<LessonServiceClass>>().Object;

        _lessonService = new LessonServiceClass(_mockRepo.Object, _mockBlob.Object, logger);

        // Sample lesson used in most tests
        _sampleLesson = new Lesson
        {
            LessonId        = 1,
            CourseId        = 10,
            Title           = "Python Introduction",
            Description     = "Welcome to Python",
            ContentType     = "VIDEO",
            ContentUrl      = "https://blob.azure.com/video.mp4",
            DurationMinutes = 15,
            DisplayOrder    = 1,
            IsPreview       = false,
            IsPublished     = false,
            CreatedAt       = DateTime.UtcNow
        };

        // Default: blob service returns a fake SAS URL
        _mockBlob.Setup(b => b.GenerateSasUrlAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                 .ReturnsAsync("https://blob.azure.com/video.mp4?sas=token123");
    }

    // ── TEST 1: Add Lesson ────────────────────────────────────────────────────

    [Test]
    [Description("AddLesson should save lesson and return it with IsPublished = false")]
    public async Task AddLesson_ValidData_ReturnsLessonWithUnpublishedStatus()
    {
        // Arrange
        _mockRepo.Setup(r => r.CountByCourseId(10)).ReturnsAsync(0);
        _mockRepo.Setup(r => r.Create(It.IsAny<Lesson>()))
                 .ReturnsAsync((Lesson l) => { l.LessonId = 1; return l; });

        var dto = new CreateLessonDto
        {
            CourseId        = 10,
            Title           = "Python Introduction",
            ContentType     = "VIDEO",
            ContentUrl      = "https://blob.azure.com/video.mp4",
            DurationMinutes = 15,
            IsPreview       = false
        };

        // Act
        var result = await _lessonService.AddLesson(dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Python Introduction");
        result.IsPublished.Should().BeFalse("new lesson always starts unpublished");
        result.CourseId.Should().Be(10);
    }

    // ── TEST 2: Add Lesson - Auto Display Order ───────────────────────────────

    [Test]
    [Description("AddLesson with DisplayOrder=0 should auto-set order as count+1")]
    public async Task AddLesson_NoDisplayOrder_AutoSetsOrder()
    {
        // Arrange — course already has 3 lessons, so new one gets order = 4
        _mockRepo.Setup(r => r.CountByCourseId(10)).ReturnsAsync(3);
        _mockRepo.Setup(r => r.Create(It.IsAny<Lesson>()))
                 .ReturnsAsync((Lesson l) => l);

        var dto = new CreateLessonDto
        {
            CourseId     = 10,
            Title        = "Lesson 4",
            ContentType  = "VIDEO",
            DisplayOrder = 0  // not set — should auto-assign
        };

        // Act
        await _lessonService.AddLesson(dto);

        // Assert — Create must be called with DisplayOrder = 4
        _mockRepo.Verify(r => r.Create(It.Is<Lesson>(l => l.DisplayOrder == 4)), Times.Once,
            "DisplayOrder should be auto-set to existing count + 1");
    }

    // ── TEST 3: Get Lesson By ID ──────────────────────────────────────────────

    [Test]
    [Description("GetLessonById should return lesson with SAS URL when lesson exists")]
    public async Task GetLessonById_ExistingLesson_ReturnsLessonWithSasUrl()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByLessonId(1)).ReturnsAsync(_sampleLesson);

        // Act
        var result = await _lessonService.GetLessonById(1);

        // Assert
        result.Should().NotBeNull();
        result!.LessonId.Should().Be(1);
        result.Title.Should().Be("Python Introduction");
        // SAS URL should be generated (not the raw blob URL)
        result.ContentUrl.Should().Contain("sas=token123", "SAS URL must be generated for content access");
    }

    // ── TEST 4: Get Lesson By ID - Not Found ──────────────────────────────────

    [Test]
    [Description("GetLessonById should return null when lesson does not exist")]
    public async Task GetLessonById_NotFound_ReturnsNull()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByLessonId(999)).ReturnsAsync((Lesson?)null);

        // Act
        var result = await _lessonService.GetLessonById(999);

        // Assert
        result.Should().BeNull("missing lesson must return null");
    }

    // ── TEST 5: Get Preview Lessons ───────────────────────────────────────────

    [Test]
    [Description("GetPreviewLessons should return only lessons where IsPreview = true")]
    public async Task GetPreviewLessons_CourseWithPreviews_ReturnsOnlyPreviewLessons()
    {
        // Arrange — 2 preview lessons in course 10
        var previewLessons = new List<Lesson>
        {
            new Lesson { LessonId = 1, Title = "Preview 1", IsPreview = true,
                         ContentType = "VIDEO", CourseId = 10 },
            new Lesson { LessonId = 2, Title = "Preview 2", IsPreview = true,
                         ContentType = "VIDEO", CourseId = 10 }
        };

        _mockRepo.Setup(r => r.FindPreviewLessons(10)).ReturnsAsync(previewLessons);

        // Act
        var result = await _lessonService.GetPreviewLessons(courseId: 10);

        // Assert
        result.Should().HaveCount(2, "only the 2 preview lessons should be returned");
        result.Should().AllSatisfy(l => l.IsPreview.Should().BeTrue());
    }

    // ── TEST 6: Get Lessons By Course ─────────────────────────────────────────

    [Test]
    [Description("GetLessonsByCourse should return all lessons for a course in order")]
    public async Task GetLessonsByCourse_ValidCourse_ReturnsAllLessons()
    {
        // Arrange — 3 lessons in display order
        var lessons = new List<Lesson>
        {
            new Lesson { LessonId = 1, Title = "Lesson 1", DisplayOrder = 1, ContentType = "VIDEO", CourseId = 10 },
            new Lesson { LessonId = 2, Title = "Lesson 2", DisplayOrder = 2, ContentType = "VIDEO", CourseId = 10 },
            new Lesson { LessonId = 3, Title = "Lesson 3", DisplayOrder = 3, ContentType = "PDF",   CourseId = 10 }
        };

        _mockRepo.Setup(r => r.FindByCourseIdOrdered(10)).ReturnsAsync(lessons);

        // Act
        var result = await _lessonService.GetLessonsByCourse(courseId: 10);

        // Assert
        result.Should().HaveCount(3);
    }

    // ── TEST 7: Publish Lesson ────────────────────────────────────────────────

    [Test]
    [Description("PublishLesson should set IsPublished = true")]
    public async Task PublishLesson_UnpublishedLesson_SetsIsPublishedTrue()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByLessonId(1)).ReturnsAsync(_sampleLesson);
        _mockRepo.Setup(r => r.Update(It.IsAny<Lesson>())).ReturnsAsync((Lesson l) => l);

        // Act
        await _lessonService.PublishLesson(lessonId: 1);

        // Assert — verify Update was called with IsPublished = true
        _mockRepo.Verify(r => r.Update(It.Is<Lesson>(l => l.IsPublished == true)), Times.Once,
            "PublishLesson must set IsPublished = true");
    }

    // ── TEST 8: Delete Lesson - Not Found ─────────────────────────────────────

    [Test]
    [Description("DeleteLesson with non-existent ID should throw KeyNotFoundException")]
    public async Task DeleteLesson_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByLessonId(999)).ReturnsAsync((Lesson?)null);

        // Act & Assert
        var act = async () => await _lessonService.DeleteLesson(lessonId: 999);

        await act.Should().ThrowAsync<KeyNotFoundException>()
                 .WithMessage("*999*");
    }

    // ── TEST 9: Reorder Lessons ───────────────────────────────────────────────

    [Test]
    [Description("ReorderLessons should call UpdateDisplayOrder for each lesson in new order")]
    public async Task ReorderLessons_ThreeLessons_UpdatesEachInOrder()
    {
        // Arrange — new order: lesson 3 first, then 1, then 2
        _mockRepo.Setup(r => r.UpdateDisplayOrder(It.IsAny<int>(), It.IsAny<int>()))
                 .Returns(Task.CompletedTask);

        var dto = new ReorderLessonsDto
        {
            CourseId  = 10,
            LessonIds = new List<int> { 3, 1, 2 }  // new desired order
        };

        // Act
        await _lessonService.ReorderLessons(dto);

        // Assert — lesson 3 → order 1, lesson 1 → order 2, lesson 2 → order 3
        _mockRepo.Verify(r => r.UpdateDisplayOrder(3, 1), Times.Once,
            "Lesson 3 should become position 1");
        _mockRepo.Verify(r => r.UpdateDisplayOrder(1, 2), Times.Once,
            "Lesson 1 should become position 2");
        _mockRepo.Verify(r => r.UpdateDisplayOrder(2, 3), Times.Once,
            "Lesson 2 should become position 3");
    }

    // ── TEST 10: Update Lesson ────────────────────────────────────────────────

    [Test]
    [Description("UpdateLesson should update title and return updated lesson")]
    public async Task UpdateLesson_ValidData_ReturnsUpdatedLesson()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByLessonId(1)).ReturnsAsync(_sampleLesson);
        _mockRepo.Setup(r => r.Update(It.IsAny<Lesson>())).ReturnsAsync((Lesson l) => l);

        var dto = new CreateLessonDto
        {
            CourseId        = 10,
            Title           = "Python Introduction - Updated",
            ContentType     = "VIDEO",
            DurationMinutes = 20,
            IsPreview       = true
        };

        // Act
        var result = await _lessonService.UpdateLesson(lessonId: 1, dto: dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Python Introduction - Updated");
        result.DurationMinutes.Should().Be(20);
        result.IsPreview.Should().BeTrue();
    }

    // ── TEST 11: Get Lesson Count ─────────────────────────────────────────────

    [Test]
    [Description("GetLessonCount should return the total number of lessons in a course")]
    public async Task GetLessonCount_CourseWithLessons_ReturnsCorrectCount()
    {
        // Arrange — course 10 has 5 lessons
        _mockRepo.Setup(r => r.CountByCourseId(10)).ReturnsAsync(5);

        // Act
        var count = await _lessonService.GetLessonCount(courseId: 10);

        // Assert
        count.Should().Be(5, "course 10 has exactly 5 lessons");
    }
}
