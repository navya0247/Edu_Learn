using EduLearn.AuthService.DTOs;
using EduLearn.AuthService.Entities;
using EduLearn.AuthService.Interfaces;
using EduLearn.AuthService.Services;
using EduLearn.AuthService.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace EduLearn.AuthService.Tests.UnitTests;

/// <summary>
/// Unit tests for UserService.
/// Uses Moq to mock IUserRepository — no real database needed.
/// Tests each business rule in isolation.
/// </summary>
[TestFixture]
public class UserServiceTests
{
    // The class under test
    private UserService _userService = null!;

    // Mocked dependencies — injected into UserService constructor
    private Mock<IUserRepository> _mockRepo = null!;
    private PasswordHasher<User> _hasher = null!;

    [SetUp]
    public void SetUp()
    {
        // Create fresh mocks before each test
        _mockRepo = new Mock<IUserRepository>();
        _hasher   = new PasswordHasher<User>();
        var jwtHelper = TestHelpers.CreateJwtHelper();
        var logger    = new Mock<ILogger<UserService>>().Object;

        // Inject mocked repo + real JwtHelper
        _userService = new UserService(_mockRepo.Object, jwtHelper, logger);
    }

    // ── Register Tests ───────────────────────────────────────────────────────

    [Test]
    [Description("Register with new email should create and return user")]
    public async Task Register_NewEmail_ReturnsUserResponseDto()
    {
        // Arrange — email not taken
        _mockRepo.Setup(r => r.ExistsByEmail("new@test.com"))
                 .ReturnsAsync(false);

        // Repo.Create returns the saved user
        _mockRepo.Setup(r => r.Create(It.IsAny<User>()))
                 .ReturnsAsync((User u) =>
                 {
                     u.UserId = 1;
                     return u;
                 });

        var dto = new RegisterRequestDto
        {
            FullName = "New User",
            Email    = "new@test.com",
            Password = "Test@123",
            Role     = "STUDENT"
        };

        // Act
        var result = await _userService.Register(dto);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("new@test.com");
        result.FullName.Should().Be("New User");
        result.Role.Should().Be("STUDENT");
        result.UserId.Should().Be(1);
    }

    [Test]
    [Description("Register with duplicate email should throw InvalidOperationException")]
    public async Task Register_DuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange — email already exists
        _mockRepo.Setup(r => r.ExistsByEmail("taken@test.com"))
                 .ReturnsAsync(true);

        var dto = new RegisterRequestDto
        {
            FullName = "Another User",
            Email    = "taken@test.com",
            Password = "Test@123"
        };

        // Act & Assert
        var act = async () => await _userService.Register(dto);

        await act.Should()
                 .ThrowAsync<InvalidOperationException>()
                 .WithMessage("*already exists*");
    }

    [Test]
    [Description("Register with null Role should default to STUDENT")]
    public async Task Register_NullRole_DefaultsToStudent()
    {
        // Arrange
        _mockRepo.Setup(r => r.ExistsByEmail(It.IsAny<string>()))
                 .ReturnsAsync(false);

        _mockRepo.Setup(r => r.Create(It.IsAny<User>()))
                 .ReturnsAsync((User u) => { u.UserId = 5; return u; });

        var dto = new RegisterRequestDto
        {
            FullName = "Default Role User",
            Email    = "default@test.com",
            Password = "Test@123",
            Role     = null  // not specified
        };

        // Act
        var result = await _userService.Register(dto);

        // Assert
        result.Role.Should().Be("STUDENT", "default role when not specified should be STUDENT");
    }

    [Test]
    [Description("Register should NOT expose password hash in response")]
    public async Task Register_Always_DoesNotExposePasswordHash()
    {
        // Arrange
        _mockRepo.Setup(r => r.ExistsByEmail(It.IsAny<string>()))
                 .ReturnsAsync(false);
        _mockRepo.Setup(r => r.Create(It.IsAny<User>()))
                 .ReturnsAsync((User u) => { u.UserId = 1; return u; });

        var dto = new RegisterRequestDto
        {
            FullName = "Safe User",
            Email    = "safe@test.com",
            Password = "Secret@123"
        };

        // Act
        var result = await _userService.Register(dto);

        // Assert — UserResponseDto has no PasswordHash field — this is correct by design
        // The type itself should not contain password info
        var resultType = result.GetType();
        var passwordProp = resultType.GetProperty("PasswordHash");
        passwordProp.Should().BeNull("UserResponseDto must never expose PasswordHash");
    }

    // ── Login Tests ──────────────────────────────────────────────────────────

    [Test]
    [Description("Login with correct credentials should return LoginResponseDto with token")]
    public async Task Login_CorrectCredentials_ReturnsLoginResponseWithToken()
    {
        // Arrange — create user with hashed password
        var user = TestHelpers.CreateStudentUser();
        user.PasswordHash = _hasher.HashPassword(user, "Correct@123");

        _mockRepo.Setup(r => r.FindByEmail("student@test.com"))
                 .ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateLastLogin(It.IsAny<int>(), It.IsAny<DateTime>()))
                 .Returns(Task.CompletedTask);

        var dto = new LoginRequestDto
        {
            Email    = "student@test.com",
            Password = "Correct@123"
        };

        // Act
        var result = await _userService.Login(dto);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty("login must return a JWT token");
        result.Email.Should().Be("student@test.com");
        result.Role.Should().Be("STUDENT");
    }

    [Test]
    [Description("Login with wrong password should throw UnauthorizedAccessException")]
    public async Task Login_WrongPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = TestHelpers.CreateStudentUser();
        user.PasswordHash = _hasher.HashPassword(user, "RealPassword@123");

        _mockRepo.Setup(r => r.FindByEmail("student@test.com"))
                 .ReturnsAsync(user);

        var dto = new LoginRequestDto
        {
            Email    = "student@test.com",
            Password = "WrongPassword@999"
        };

        // Act & Assert
        var act = async () => await _userService.Login(dto);

        await act.Should()
                 .ThrowAsync<UnauthorizedAccessException>()
                 .WithMessage("*Invalid email or password*");
    }

    [Test]
    [Description("Login with non-existent email should throw UnauthorizedAccessException")]
    public async Task Login_EmailNotFound_ThrowsUnauthorizedException()
    {
        // Arrange — user doesn't exist
        _mockRepo.Setup(r => r.FindByEmail("notfound@test.com"))
                 .ReturnsAsync((User?)null);

        var dto = new LoginRequestDto
        {
            Email    = "notfound@test.com",
            Password = "Any@123"
        };

        // Act & Assert
        var act = async () => await _userService.Login(dto);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    [Description("Login with suspended account should throw UnauthorizedAccessException")]
    public async Task Login_SuspendedAccount_ThrowsUnauthorizedException()
    {
        // Arrange — suspended user
        var user = TestHelpers.CreateSuspendedUser();
        user.PasswordHash = _hasher.HashPassword(user, "Any@123");

        _mockRepo.Setup(r => r.FindByEmail("suspended@test.com"))
                 .ReturnsAsync(user);

        var dto = new LoginRequestDto
        {
            Email    = "suspended@test.com",
            Password = "Any@123"
        };

        // Act & Assert
        var act = async () => await _userService.Login(dto);

        await act.Should()
                 .ThrowAsync<UnauthorizedAccessException>()
                 .WithMessage("*suspended*");
    }

    [Test]
    [Description("Login success should call UpdateLastLogin on repository")]
    public async Task Login_Success_CallsUpdateLastLogin()
    {
        // Arrange
        var user = TestHelpers.CreateStudentUser();
        user.PasswordHash = _hasher.HashPassword(user, "Test@123");

        _mockRepo.Setup(r => r.FindByEmail(It.IsAny<string>()))
                 .ReturnsAsync(user);
        _mockRepo.Setup(r => r.UpdateLastLogin(It.IsAny<int>(), It.IsAny<DateTime>()))
                 .Returns(Task.CompletedTask);

        var dto = new LoginRequestDto { Email = "student@test.com", Password = "Test@123" };

        // Act
        await _userService.Login(dto);

        // Assert — verify UpdateLastLogin was called exactly once
        _mockRepo.Verify(r => r.UpdateLastLogin(user.UserId, It.IsAny<DateTime>()), Times.Once,
            "UpdateLastLogin must be called on successful login");
    }

    // ── GetUserById Tests ────────────────────────────────────────────────────

    [Test]
    [Description("GetUserById with valid ID returns UserResponseDto")]
    public async Task GetUserById_ExistingUser_ReturnsUserDto()
    {
        // Arrange
        var user = TestHelpers.CreateStudentUser(id: 10);
        _mockRepo.Setup(r => r.FindByUserId(10)).ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserById(10);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(10);
        result.Email.Should().Be("student@test.com");
    }

    [Test]
    [Description("GetUserById with non-existent ID returns null")]
    public async Task GetUserById_NotFound_ReturnsNull()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByUserId(999)).ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetUserById(999);

        // Assert
        result.Should().BeNull("non-existent user ID should return null");
    }

    // ── UpdateProfile Tests ──────────────────────────────────────────────────

    [Test]
    [Description("UpdateProfile should update name and return updated user")]
    public async Task UpdateProfile_ValidData_ReturnsUpdatedUser()
    {
        // Arrange
        var user = TestHelpers.CreateStudentUser(id: 1);
        _mockRepo.Setup(r => r.FindByUserId(1)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.Update(It.IsAny<User>()))
                 .ReturnsAsync((User u) => u);

        var dto = new UpdateProfileDto
        {
            FullName  = "Updated Name",
            AvatarUrl = "https://example.com/avatar.jpg"
        };

        // Act
        var result = await _userService.UpdateProfile(1, dto);

        // Assert
        result.FullName.Should().Be("Updated Name");
        result.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
    }

    [Test]
    [Description("UpdateProfile for non-existent user should throw KeyNotFoundException")]
    public async Task UpdateProfile_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByUserId(999)).ReturnsAsync((User?)null);

        var dto = new UpdateProfileDto { FullName = "Ghost" };

        // Act & Assert
        var act = async () => await _userService.UpdateProfile(999, dto);

        await act.Should().ThrowAsync<KeyNotFoundException>()
                 .WithMessage("*not found*");
    }

    // ── DeactivateAccount Tests ──────────────────────────────────────────────

    [Test]
    [Description("DeactivateAccount should set IsActive to false")]
    public async Task DeactivateAccount_ExistingUser_SetsIsActiveFalse()
    {
        // Arrange
        var user = TestHelpers.CreateStudentUser(id: 1);
        user.IsActive = true;

        _mockRepo.Setup(r => r.FindByUserId(1)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.Update(It.IsAny<User>()))
                 .ReturnsAsync((User u) => u);

        // Act
        await _userService.DeactivateAccount(1);

        // Assert — verify Update was called with IsActive = false
        _mockRepo.Verify(r => r.Update(It.Is<User>(u => u.IsActive == false)), Times.Once,
            "DeactivateAccount must set IsActive = false before saving");
    }

    [Test]
    [Description("DeactivateAccount for non-existent user should throw KeyNotFoundException")]
    public async Task DeactivateAccount_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindByUserId(999)).ReturnsAsync((User?)null);

        // Act & Assert
        var act = async () => await _userService.DeactivateAccount(999);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── ReactivateAccount Tests ──────────────────────────────────────────────

    [Test]
    [Description("ReactivateAccount should set IsActive back to true")]
    public async Task ReactivateAccount_SuspendedUser_SetsIsActiveTrue()
    {
        // Arrange
        var user = TestHelpers.CreateSuspendedUser(id: 4);
        user.IsActive = false;

        _mockRepo.Setup(r => r.FindByUserId(4)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.Update(It.IsAny<User>()))
                 .ReturnsAsync((User u) => u);

        // Act
        await _userService.ReactivateAccount(4);

        // Assert
        _mockRepo.Verify(r => r.Update(It.Is<User>(u => u.IsActive == true)), Times.Once,
            "ReactivateAccount must set IsActive = true");
    }

    // ── ChangePassword Tests ─────────────────────────────────────────────────

    [Test]
    [Description("ChangePassword with correct old password should succeed")]
    public async Task ChangePassword_CorrectOldPassword_Succeeds()
    {
        // Arrange
        var user = TestHelpers.CreateStudentUser(id: 1);
        user.PasswordHash = _hasher.HashPassword(user, "OldPassword@123");

        _mockRepo.Setup(r => r.FindByUserId(1)).ReturnsAsync(user);
        _mockRepo.Setup(r => r.Update(It.IsAny<User>()))
                 .ReturnsAsync((User u) => u);

        var dto = new ChangePasswordDto
        {
            OldPassword = "OldPassword@123",
            NewPassword = "NewPassword@456"
        };

        // Act & Assert — should NOT throw
        var act = async () => await _userService.ChangePassword(1, dto);
        await act.Should().NotThrowAsync();

        // Verify Update was called — password was changed
        _mockRepo.Verify(r => r.Update(It.IsAny<User>()), Times.Once);
    }

    [Test]
    [Description("ChangePassword with wrong old password should throw UnauthorizedAccessException")]
    public async Task ChangePassword_WrongOldPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = TestHelpers.CreateStudentUser(id: 1);
        user.PasswordHash = _hasher.HashPassword(user, "RealOldPassword@123");

        _mockRepo.Setup(r => r.FindByUserId(1)).ReturnsAsync(user);

        var dto = new ChangePasswordDto
        {
            OldPassword = "WrongOldPassword@999",
            NewPassword = "NewPassword@456"
        };

        // Act & Assert
        var act = async () => await _userService.ChangePassword(1, dto);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
                 .WithMessage("*incorrect*");
    }

    // ── SearchUsers Tests ────────────────────────────────────────────────────

    [Test]
    [Description("SearchUsers returns matching users mapped to DTOs")]
    public async Task SearchUsers_MatchingTerm_ReturnsMappedDtos()
    {
        // Arrange
        var users = new List<User>
        {
            TestHelpers.CreateStudentUser(1),
            TestHelpers.CreateInstructorUser(2)
        };
        _mockRepo.Setup(r => r.SearchUsers("test"))
                 .ReturnsAsync(users);

        // Act
        var result = await _userService.SearchUsers("test");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(u => u.Should().NotBeNull());
    }

    [Test]
    [Description("SearchUsers with no matches returns empty list")]
    public async Task SearchUsers_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        _mockRepo.Setup(r => r.SearchUsers("zzznomatch"))
                 .ReturnsAsync(new List<User>());

        // Act
        var result = await _userService.SearchUsers("zzznomatch");

        // Assert
        result.Should().BeEmpty("no matches should return an empty list, not null");
    }

    // ── GetAllByRole Tests ───────────────────────────────────────────────────

    [Test]
    [Description("GetAllByRole returns only users with matching role")]
    public async Task GetAllByRole_StudentRole_ReturnsStudentUsers()
    {
        // Arrange
        var students = new List<User>
        {
            TestHelpers.CreateStudentUser(1),
            TestHelpers.CreateStudentUser(5)
        };
        _mockRepo.Setup(r => r.FindAllByRole("STUDENT")).ReturnsAsync(students);

        // Act
        var result = await _userService.GetAllByRole("STUDENT");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(u => u.Role.Should().Be("STUDENT"));
    }

    // ── Google Login Tests ───────────────────────────────────────────────────

    [Test]
    [Description("Google login with new email auto-registers user as STUDENT")]
    public async Task LoginWithGoogle_NewUser_AutoRegistersAsStudent()
    {
        // Arrange — no existing account
        _mockRepo.Setup(r => r.FindByGoogleId("google123")).ReturnsAsync((User?)null);
        _mockRepo.Setup(r => r.FindByEmail("google@test.com")).ReturnsAsync((User?)null);
        _mockRepo.Setup(r => r.Create(It.IsAny<User>()))
                 .ReturnsAsync((User u) => { u.UserId = 99; return u; });
        _mockRepo.Setup(r => r.UpdateLastLogin(It.IsAny<int>(), It.IsAny<DateTime>()))
                 .Returns(Task.CompletedTask);

        var dto = new GoogleLoginRequestDto
        {
            GoogleId = "google123",
            Email    = "google@test.com",
            FullName = "Google User"
        };

        // Act
        var result = await _userService.LoginWithGoogle(dto);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.Role.Should().Be("STUDENT", "new Google users default to STUDENT");

        // Verify Create was called — user was saved to DB
        _mockRepo.Verify(r => r.Create(It.IsAny<User>()), Times.Once);
    }

    [Test]
    [Description("Google login with existing Google ID returns token without re-registering")]
    public async Task LoginWithGoogle_ExistingGoogleId_ReturnsTokenWithoutCreating()
    {
        // Arrange — user already exists with this Google ID
        var existingUser = TestHelpers.CreateStudentUser(id: 7);
        existingUser.GoogleId = "google_existing";

        _mockRepo.Setup(r => r.FindByGoogleId("google_existing"))
                 .ReturnsAsync(existingUser);
        _mockRepo.Setup(r => r.UpdateLastLogin(It.IsAny<int>(), It.IsAny<DateTime>()))
                 .Returns(Task.CompletedTask);

        var dto = new GoogleLoginRequestDto
        {
            GoogleId = "google_existing",
            Email    = "student@test.com",
            FullName = "Test Student"
        };

        // Act
        var result = await _userService.LoginWithGoogle(dto);

        // Assert
        result.Token.Should().NotBeNullOrEmpty();

        // Verify Create was NOT called — user already exists
        _mockRepo.Verify(r => r.Create(It.IsAny<User>()), Times.Never,
            "existing Google user should not be re-created");
    }
}
