using System.Text.Json;
using EduLearn.AssessmentService.DTOs;
using EduLearn.AssessmentService.Entities;
using EduLearn.AssessmentService.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

// Alias to avoid namespace vs class name conflict
using QuizServiceClass = EduLearn.AssessmentService.Services.QuizService;

namespace EduLearn.AssessmentService.Tests.UnitTests;

/// <summary>
/// Unit tests for QuizService (AssessmentService).
/// Moq fakes the repository — no real database needed.
/// Total: 11 tests
/// </summary>
[TestFixture]
public class QuizServiceTests
{
    private QuizServiceClass _quizService = null!;
    private Mock<IQuizRepository> _mockRepo = null!;

    // Sample quiz and attempt reused across tests
    private Quiz _sampleQuiz = null!;
    private QuizAttempt _sampleAttempt = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IQuizRepository>();
        var logger = new Mock<ILogger<QuizServiceClass>>().Object;

        _quizService = new QuizServiceClass(_mockRepo.Object, logger);

        // Sample quiz with 2 questions
        var questions = new List<QuestionDto>
        {
            new QuestionDto { Id = 1, Question = "What is Python?", Options = new List<string> { "Language", "Framework" }, CorrectAnswer = "Language" },
            new QuestionDto { Id = 2, Question = "What is OOP?",    Options = new List<string> { "Paradigm", "Library"   }, CorrectAnswer = "Paradigm"  }
        };

        _sampleQuiz = new Quiz
        {
            QuizId           = 1,
            CourseId         = 10,
            Title            = "Python Basics Quiz",
            Description      = "Test your Python knowledge",
            TimeLimitMinutes = 30,
            PassingScore     = 70,
            MaxAttempts      = 3,
            IsPublished      = false,
            QuestionsJson    = JsonSerializer.Serialize(questions),
            CreatedAt        = DateTime.UtcNow
        };

        _sampleAttempt = new QuizAttempt
        {
            AttemptId   = 1,
            QuizId      = 1,
            StudentId   = 5,
            StartedAt   = DateTime.UtcNow,
            Score       = 0,
            IsPassed    = false,
            AnswersJson = "{}"
        };
    }

    // ── TEST 1: Create Quiz ───────────────────────────────────────────────────

    [Test]
    [Description("CreateQuiz should save quiz and return it with IsPublished = false")]
    public async Task CreateQuiz_ValidData_ReturnsQuizWithUnpublishedStatus()
    {
        // Arrange
        _mockRepo.Setup(r => r.Create(It.IsAny<Quiz>()))
                 .ReturnsAsync((Quiz q) => { q.QuizId = 1; return q; });

        var dto = new CreateQuizDto
        {
            CourseId         = 10,
            Title            = "Python Basics Quiz",
            PassingScore     = 70,
            MaxAttempts      = 3,
            TimeLimitMinutes = 30,
            Questions        = new List<QuestionDto>
            {
                new QuestionDto { Id = 1, Question = "What is Python?", CorrectAnswer = "Language" }
            }
        };

        // Act
        var result = await _quizService.CreateQuiz(dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Python Basics Quiz");
        result.IsPublished.Should().BeFalse("new quiz always starts unpublished");
        result.PassingScore.Should().Be(70);
        result.MaxAttempts.Should().Be(3);
    }

    // ── TEST 2: Get Quiz By ID ────────────────────────────────────────────────

    [Test]
    [Description("GetQuizById returns quiz when it exists")]
    public async Task GetQuizById_ExistingQuiz_ReturnsQuizDto()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByQuizId(1)).ReturnsAsync(_sampleQuiz);

        // Act
        var result = await _quizService.GetQuizById(1);

        // Assert
        result.Should().NotBeNull();
        result!.QuizId.Should().Be(1);
        result.Title.Should().Be("Python Basics Quiz");
        result.QuestionCount.Should().Be(2, "sample quiz has 2 questions");
    }

    // ── TEST 3: Get Quiz By ID - Not Found ────────────────────────────────────

    [Test]
    [Description("GetQuizById returns null when quiz does not exist")]
    public async Task GetQuizById_NotFound_ReturnsNull()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByQuizId(999)).ReturnsAsync((Quiz?)null);

        // Act
        var result = await _quizService.GetQuizById(999);

        // Assert
        result.Should().BeNull("non-existent quiz must return null");
    }

    // ── TEST 4: Publish Quiz ──────────────────────────────────────────────────

    [Test]
    [Description("PublishQuiz should set IsPublished = true")]
    public async Task PublishQuiz_UnpublishedQuiz_SetsIsPublishedTrue()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByQuizId(1)).ReturnsAsync(_sampleQuiz);
        _mockRepo.Setup(r => r.Update(It.IsAny<Quiz>())).ReturnsAsync((Quiz q) => q);

        // Act
        await _quizService.PublishQuiz(quizId: 1);

        // Assert
        _mockRepo.Verify(r => r.Update(It.Is<Quiz>(q => q.IsPublished == true)), Times.Once,
            "PublishQuiz must set IsPublished = true");
    }

    // ── TEST 5: Start Attempt ─────────────────────────────────────────────────

    [Test]
    [Description("StartAttempt creates a new attempt when student is within MaxAttempts")]
    public async Task StartAttempt_WithinMaxAttempts_CreatesAttempt()
    {
        // Arrange — student has 0 previous attempts, max is 3
        _mockRepo.Setup(r => r.FindByQuizId(1)).ReturnsAsync(_sampleQuiz);
        _mockRepo.Setup(r => r.CountAttempts(5, 1)).ReturnsAsync(0);
        _mockRepo.Setup(r => r.CreateAttempt(It.IsAny<QuizAttempt>()))
                 .ReturnsAsync((QuizAttempt a) => { a.AttemptId = 1; return a; });

        // Act
        var result = await _quizService.StartAttempt(quizId: 1, studentId: 5);

        // Assert
        result.Should().NotBeNull();
        result.QuizId.Should().Be(1);
        result.StudentId.Should().Be(5);
        result.Score.Should().Be(0, "new attempt starts with 0 score");
        result.IsPassed.Should().BeFalse("not submitted yet");
    }

    // ── TEST 6: Start Attempt - Max Attempts Reached ──────────────────────────

    [Test]
    [Description("StartAttempt throws when student has used all allowed attempts")]
    public async Task StartAttempt_MaxAttemptsReached_ThrowsInvalidOperation()
    {
        // Arrange — student already used all 3 attempts
        _mockRepo.Setup(r => r.FindByQuizId(1)).ReturnsAsync(_sampleQuiz);
        _mockRepo.Setup(r => r.CountAttempts(5, 1)).ReturnsAsync(3); // = MaxAttempts

        // Act & Assert
        var act = async () => await _quizService.StartAttempt(quizId: 1, studentId: 5);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*Maximum attempts*");
    }

    // ── TEST 7: Submit Attempt - All Correct ──────────────────────────────────

    [Test]
    [Description("SubmitAttempt with all correct answers should give 100% and IsPassed = true")]
    public async Task SubmitAttempt_AllCorrect_Returns100PercentAndPassed()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindAttemptById(1)).ReturnsAsync(_sampleAttempt);
        _mockRepo.Setup(r => r.FindByQuizId(1)).ReturnsAsync(_sampleQuiz);
        _mockRepo.Setup(r => r.UpdateAttempt(It.IsAny<QuizAttempt>()))
                 .ReturnsAsync((QuizAttempt a) => a);

        // Student answers both questions correctly
        var dto = new SubmitAttemptDto
        {
            Answers = new Dictionary<int, string>
            {
                { 1, "Language" },  // correct
                { 2, "Paradigm" }   // correct
            }
        };

        // Act
        var result = await _quizService.SubmitAttempt(attemptId: 1, dto: dto);

        // Assert
        result.Score.Should().Be(100, "all answers correct = 100%");
        result.IsPassed.Should().BeTrue("100% is above passing score of 70");
    }

    // ── TEST 8: Submit Attempt - All Wrong ────────────────────────────────────

    [Test]
    [Description("SubmitAttempt with all wrong answers should give 0% and IsPassed = false")]
    public async Task SubmitAttempt_AllWrong_Returns0PercentAndFailed()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindAttemptById(1)).ReturnsAsync(_sampleAttempt);
        _mockRepo.Setup(r => r.FindByQuizId(1)).ReturnsAsync(_sampleQuiz);
        _mockRepo.Setup(r => r.UpdateAttempt(It.IsAny<QuizAttempt>()))
                 .ReturnsAsync((QuizAttempt a) => a);

        // Student answers both questions wrongly
        var dto = new SubmitAttemptDto
        {
            Answers = new Dictionary<int, string>
            {
                { 1, "Framework" },  // wrong
                { 2, "Library"   }   // wrong
            }
        };

        // Act
        var result = await _quizService.SubmitAttempt(attemptId: 1, dto: dto);

        // Assert
        result.Score.Should().Be(0, "no correct answers = 0%");
        result.IsPassed.Should().BeFalse("0% is below passing score of 70");
    }

    // ── TEST 9: Submit Attempt - Already Submitted ────────────────────────────

    [Test]
    [Description("SubmitAttempt on already submitted attempt should throw InvalidOperationException")]
    public async Task SubmitAttempt_AlreadySubmitted_ThrowsInvalidOperation()
    {
        // Arrange — attempt was already submitted
        _sampleAttempt.SubmittedAt = DateTime.UtcNow.AddMinutes(-5);
        _mockRepo.Setup(r => r.FindAttemptById(1)).ReturnsAsync(_sampleAttempt);

        var dto = new SubmitAttemptDto { Answers = new Dictionary<int, string>() };

        // Act & Assert
        var act = async () => await _quizService.SubmitAttempt(attemptId: 1, dto: dto);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*already been submitted*");
    }

    // ── TEST 10: Get Questions For Student - No Correct Answers ───────────────

    [Test]
    [Description("GetQuestionsForStudent should return questions WITHOUT correct answers")]
    public async Task GetQuestionsForStudent_ValidQuiz_HidesCorrectAnswers()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByQuizId(1)).ReturnsAsync(_sampleQuiz);

        // Act
        var result = await _quizService.GetQuestionsForStudent(quizId: 1);

        // Assert
        result.Should().HaveCount(2, "quiz has 2 questions");

        // StudentQuestionDto has no CorrectAnswer property — it's hidden by design
        // Verify the returned type does NOT expose correct answers
        var firstQuestion = result.First();
        var correctAnswerProp = firstQuestion.GetType().GetProperty("CorrectAnswer");
        correctAnswerProp.Should().BeNull("StudentQuestionDto must never expose CorrectAnswer");
    }

    // ── TEST 11: Get Best Attempt ─────────────────────────────────────────────

    [Test]
    [Description("GetBestAttempt returns the attempt with the highest score")]
    public async Task GetBestAttempt_StudentWithAttempts_ReturnsBestScore()
    {
        // Arrange — best attempt has score 80
        var bestAttempt = new QuizAttempt
        {
            AttemptId   = 3,
            QuizId      = 1,
            StudentId   = 5,
            Score       = 80,
            IsPassed    = true,
            StartedAt   = DateTime.UtcNow,
            AnswersJson = "{}"
        };

        _mockRepo.Setup(r => r.FindByQuizId(1)).ReturnsAsync(_sampleQuiz);
        _mockRepo.Setup(r => r.FindBestAttempt(5, 1)).ReturnsAsync(bestAttempt);

        // Act
        var result = await _quizService.GetBestAttempt(studentId: 5, quizId: 1);

        // Assert
        result.Should().NotBeNull();
        result!.Score.Should().Be(80, "best attempt had score 80");
        result.IsPassed.Should().BeTrue("80 is above passing score of 70");
    }
}
