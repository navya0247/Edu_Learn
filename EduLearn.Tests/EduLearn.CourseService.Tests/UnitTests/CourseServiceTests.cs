using EduLearn.CourseService.DTOs;
using EduLearn.CourseService.Entities;
using EduLearn.CourseService.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

// Use full namespace to avoid conflict between CourseService (namespace) and CourseService (class)
using CourseServiceClass = EduLearn.CourseService.Services.CourseService;

namespace EduLearn.CourseService.Tests.UnitTests;

/// <summary>
/// Unit tests for CourseService.
/// We use Moq to fake the repository and cache — no real database needed.
/// Total: 11 tests
/// </summary>
[TestFixture]
public class CourseServiceTests
{
    // The class we are testing
    private CourseServiceClass _courseService = null!;

    // Fake dependencies — Moq creates these automatically
    private Mock<ICourseRepository> _mockRepo  = null!;
    private Mock<ICacheService>     _mockCache = null!;

    // A sample course used across tests
    private Course _sampleCourse = null!;

    [SetUp]
    public void SetUp()
    {
        // Create fresh mocks before every single test
        _mockRepo  = new Mock<ICourseRepository>();
        _mockCache = new Mock<ICacheService>();
        var logger = new Mock<ILogger<CourseServiceClass>>().Object;

        // Create the real CourseService but with fake (mocked) dependencies
        _courseService = new CourseServiceClass(_mockRepo.Object, _mockCache.Object, logger);

        // Setup a sample course we'll reuse in many tests
        _sampleCourse = new Course
        {
            CourseId     = 1,
            Title        = "Python for Beginners",
            Description  = "Learn Python from scratch",
            Category     = "Programming",
            Level        = "Beginner",
            Language     = "English",
            Price        = 0,
            InstructorId = 10,
            IsPublished  = false,
            IsApproved   = false,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        };

        // Default cache setup — always return null (cache miss) unless overridden
        _mockCache.Setup(c => c.GetAsync(It.IsAny<string>()))
                  .ReturnsAsync((string?)null);
        _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                  .Returns(Task.CompletedTask);
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>()))
                  .Returns(Task.CompletedTask);
    }

    // ── TEST 1: Create Course ─────────────────────────────────────────────────

    [Test]
    [Description("CreateCourse should save course and return it with IsPublished = false")]
    public async Task CreateCourse_ValidData_ReturnsCourseWithDraftStatus()
    {
        // Arrange — fake repo returns our sample course when Create is called
        _mockRepo.Setup(r => r.Create(It.IsAny<Course>()))
                 .ReturnsAsync((Course c) => { c.CourseId = 1; return c; });

        var dto = new CreateCourseDto
        {
            Title       = "Python for Beginners",
            Description = "Learn Python from scratch",
            Category    = "Programming",
            Level       = "Beginner",
            Language    = "English",
            Price       = 0
        };

        // Act
        var result = await _courseService.CreateCourse(dto, instructorId: 10);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Python for Beginners");
        result.IsPublished.Should().BeFalse("new course always starts as draft");
        result.IsApproved.Should().BeFalse("new course needs admin approval");
        result.InstructorId.Should().Be(10);
    }

    // ── TEST 2: Create Course - Free Course ───────────────────────────────────

    [Test]
    [Description("CreateCourse with price 0 should be saved as free course")]
    public async Task CreateCourse_FreePrice_SavedAsFree()
    {
        // Arrange
        _mockRepo.Setup(r => r.Create(It.IsAny<Course>()))
                 .ReturnsAsync((Course c) => c);

        var dto = new CreateCourseDto
        {
            Title    = "Free React Course",
            Category = "Web Dev",
            Price    = 0   // free course
        };

        // Act
        var result = await _courseService.CreateCourse(dto, instructorId: 5);

        // Assert
        result.Price.Should().Be(0, "free courses should have price 0");
    }

    // ── TEST 3: Get Course By ID ──────────────────────────────────────────────

    [Test]
    [Description("GetCourseById should return course details when course exists")]
    public async Task GetCourseById_ExistingCourse_ReturnsCourseDto()
    {
        // Arrange — repo returns our sample course for ID 1
        _mockRepo.Setup(r => r.FindByCourseId(1))
                 .ReturnsAsync(_sampleCourse);

        // Act
        var result = await _courseService.GetCourseById(1);

        // Assert
        result.Should().NotBeNull();
        result!.CourseId.Should().Be(1);
        result.Title.Should().Be("Python for Beginners");
    }

    // ── TEST 4: Get Course By ID - Not Found ──────────────────────────────────

    [Test]
    [Description("GetCourseById should return null when course does not exist")]
    public async Task GetCourseById_NotFound_ReturnsNull()
    {
        // Arrange — repo returns null for ID 999
        _mockRepo.Setup(r => r.FindByCourseId(999))
                 .ReturnsAsync((Course?)null);

        // Act
        var result = await _courseService.GetCourseById(999);

        // Assert
        result.Should().BeNull("non-existent course should return null");
    }

    // ── TEST 5: Publish Course ────────────────────────────────────────────────

    [Test]
    [Description("PublishCourse should set IsPublished = true for course owner")]
    public async Task PublishCourse_ByOwner_SetsIsPublishedTrue()
    {
        // Arrange — course belongs to instructor 10
        _mockRepo.Setup(r => r.FindByCourseId(1))
                 .ReturnsAsync(_sampleCourse);
        _mockRepo.Setup(r => r.Update(It.IsAny<Course>()))
                 .ReturnsAsync((Course c) => c);

        // Act — instructor 10 publishes their own course
        await _courseService.PublishCourse(courseId: 1, instructorId: 10);

        // Assert — verify Update was called with IsPublished = true
        _mockRepo.Verify(r => r.Update(It.Is<Course>(c => c.IsPublished == true)), Times.Once,
            "PublishCourse must set IsPublished = true");
    }

    // ── TEST 6: Publish Course - Wrong Instructor ─────────────────────────────

    [Test]
    [Description("PublishCourse by wrong instructor should throw UnauthorizedAccessException")]
    public async Task PublishCourse_WrongInstructor_ThrowsUnauthorized()
    {
        // Arrange — course belongs to instructor 10, but instructor 99 tries to publish
        _mockRepo.Setup(r => r.FindByCourseId(1))
                 .ReturnsAsync(_sampleCourse);

        // Act & Assert
        var act = async () => await _courseService.PublishCourse(courseId: 1, instructorId: 99);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
                 .WithMessage("*own courses*");
    }

    // ── TEST 7: Approve Course ────────────────────────────────────────────────

    [Test]
    [Description("ApproveCourse should set IsApproved = true for a published course")]
    public async Task ApproveCourse_PublishedCourse_SetsIsApprovedTrue()
    {
        // Arrange — course is published (IsPublished = true) but not yet approved
        _sampleCourse.IsPublished = true;
        _mockRepo.Setup(r => r.FindByCourseId(1)).ReturnsAsync(_sampleCourse);
        _mockRepo.Setup(r => r.Update(It.IsAny<Course>())).ReturnsAsync((Course c) => c);

        // Act — admin approves the course
        await _courseService.ApproveCourse(courseId: 1);

        // Assert — IsApproved must be set to true
        _mockRepo.Verify(r => r.Update(It.Is<Course>(c => c.IsApproved == true)), Times.Once,
            "ApproveCourse must set IsApproved = true");
    }

    // ── TEST 8: Approve Course - Not Published Yet ────────────────────────────

    [Test]
    [Description("ApproveCourse on a draft course should throw InvalidOperationException")]
    public async Task ApproveCourse_NotPublished_ThrowsInvalidOperation()
    {
        // Arrange — course is still a draft (IsPublished = false)
        _sampleCourse.IsPublished = false;
        _mockRepo.Setup(r => r.FindByCourseId(1)).ReturnsAsync(_sampleCourse);

        // Act & Assert — admin cannot approve a course that is not published yet
        var act = async () => await _courseService.ApproveCourse(courseId: 1);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*published before approval*");
    }

    // ── TEST 9: Reject Course ─────────────────────────────────────────────────

    [Test]
    [Description("RejectCourse should set both IsPublished and IsApproved to false")]
    public async Task RejectCourse_PublishedCourse_ResetsBothFlags()
    {
        // Arrange — course was published and approved
        _sampleCourse.IsPublished = true;
        _sampleCourse.IsApproved  = true;
        _mockRepo.Setup(r => r.FindByCourseId(1)).ReturnsAsync(_sampleCourse);
        _mockRepo.Setup(r => r.Update(It.IsAny<Course>())).ReturnsAsync((Course c) => c);

        // Act
        await _courseService.RejectCourse(courseId: 1);

        // Assert — both flags should be reset to false
        _mockRepo.Verify(r => r.Update(It.Is<Course>(c =>
            c.IsPublished == false && c.IsApproved == false)), Times.Once,
            "RejectCourse must reset both IsPublished and IsApproved to false");
    }

    // ── TEST 10: Get Published Courses - Cache Miss ───────────────────────────

    [Test]
    [Description("GetPublishedCourses should fetch from DB when Redis cache is empty")]
    public async Task GetPublishedCourses_CacheMiss_QueriesDatabase()
    {
        // Arrange — cache returns null (miss), repo returns 2 courses
        _mockCache.Setup(c => c.GetAsync("courses:published"))
                  .ReturnsAsync((string?)null);  // cache miss

        var publishedCourses = new List<Course>
        {
            new Course { CourseId = 1, Title = "Python", Category = "Programming",
                         IsPublished = true, IsApproved = true, Level = "Beginner",
                         Language = "English", InstructorId = 10 },
            new Course { CourseId = 2, Title = "React", Category = "Web Dev",
                         IsPublished = true, IsApproved = true, Level = "Intermediate",
                         Language = "English", InstructorId = 11 }
        };

        _mockRepo.Setup(r => r.FindPublishedAndApproved())
                 .ReturnsAsync(publishedCourses);

        // Act
        var result = await _courseService.GetPublishedCourses();

        // Assert
        result.Should().HaveCount(2, "both approved courses should be returned");

        // Verify the DB was actually called (not served from cache)
        _mockRepo.Verify(r => r.FindPublishedAndApproved(), Times.Once,
            "DB must be called when cache is empty");
    }

    // ── TEST 11: Update Course - Resets Approval ──────────────────────────────

    [Test]
    [Description("UpdateCourse on an already-approved course should reset IsApproved to false")]
    public async Task UpdateCourse_AlreadyApproved_ResetsApproval()
    {
        // Arrange — course was already approved
        _sampleCourse.IsPublished = true;
        _sampleCourse.IsApproved  = true;

        _mockRepo.Setup(r => r.FindByCourseId(1)).ReturnsAsync(_sampleCourse);
        _mockRepo.Setup(r => r.Update(It.IsAny<Course>())).ReturnsAsync((Course c) => c);

        var dto = new CreateCourseDto
        {
            Title       = "Python for Beginners - Updated",
            Description = "Updated description",
            Category    = "Programming",
            Level       = "Beginner",
            Language    = "English",
            Price       = 0
        };

        // Act — instructor 10 updates their own course
        await _courseService.UpdateCourse(courseId: 1, dto: dto, instructorId: 10);

        // Assert — updating an approved course must reset approval
        _mockRepo.Verify(r => r.Update(It.Is<Course>(c =>
            c.IsApproved == false && c.IsPublished == false)), Times.Once,
            "Updating an approved course must reset IsApproved and IsPublished to false");
    }
}