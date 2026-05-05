using EduLearn.AuthService.Entities;
using EduLearn.AuthService.Helpers;
using EduLearn.AuthService.Tests.Helpers;
using FluentAssertions;
using NUnit.Framework;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EduLearn.AuthService.Tests.UnitTests;

/// <summary>
/// Unit tests for JwtHelper.
/// Tests token generation, claim extraction, and token validation.
/// No mocking needed — JwtHelper is a pure utility class with no DB dependency.
/// </summary>
[TestFixture]
public class JwtHelperTests
{
    private JwtHelper _jwtHelper = null!;
    private User _testUser = null!;

    [SetUp]
    public void SetUp()
    {
        // Create JwtHelper with fake config (no appsettings.json needed)
        _jwtHelper = TestHelpers.CreateJwtHelper();
        _testUser  = TestHelpers.CreateStudentUser();
    }

    // ── GenerateToken Tests ──────────────────────────────────────────────────

    [Test]
    [Description("GenerateToken should return a non-empty string for a valid user")]
    public void GenerateToken_ValidUser_ReturnsNonEmptyString()
    {
        // Act
        var token = _jwtHelper.GenerateToken(_testUser);

        // Assert
        token.Should().NotBeNullOrEmpty("token must be a non-empty JWT string");
    }

    [Test]
    [Description("GenerateToken should produce a valid JWT with 3 dot-separated parts")]
    public void GenerateToken_ValidUser_ProducesValidJwtFormat()
    {
        // Act
        var token = _jwtHelper.GenerateToken(_testUser);

        // Assert — JWT format: header.payload.signature
        var parts = token.Split('.');
        parts.Should().HaveCount(3, "JWT tokens always have 3 dot-separated parts");
    }

    [Test]
    [Description("GenerateToken embeds correct UserId in claims")]
    public void GenerateToken_ValidUser_ContainsCorrectUserIdClaim()
    {
        // Act
        var token = _jwtHelper.GenerateToken(_testUser);

        // Decode the JWT to inspect claims (without verifying signature)
        var handler  = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var userIdClaim = jwtToken.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier
                              || c.Type == "nameid");

        userIdClaim.Should().NotBeNull("JWT must contain NameIdentifier claim");
        userIdClaim!.Value.Should().Be(_testUser.UserId.ToString());
    }

    [Test]
    [Description("GenerateToken embeds correct Role in claims")]
    public void GenerateToken_ValidUser_ContainsCorrectRoleClaim()
    {
        // Act
        var token = _jwtHelper.GenerateToken(_testUser);

        var handler  = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert — role claim key varies: "role" or ClaimTypes.Role
        var roleClaim = jwtToken.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.Role
                              || c.Type == "role");

        roleClaim.Should().NotBeNull("JWT must contain Role claim");
        roleClaim!.Value.Should().Be("STUDENT");
    }

    [Test]
    [Description("GenerateToken embeds correct Email in claims")]
    public void GenerateToken_ValidUser_ContainsCorrectEmailClaim()
    {
        // Act
        var token = _jwtHelper.GenerateToken(_testUser);

        var handler  = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var emailClaim = jwtToken.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.Email
                              || c.Type == "email");

        emailClaim.Should().NotBeNull("JWT must contain Email claim");
        emailClaim!.Value.Should().Be(_testUser.Email);
    }

    [Test]
    [Description("Tokens for different users should be different")]
    public void GenerateToken_DifferentUsers_ProduceDifferentTokens()
    {
        // Arrange
        var instructor = TestHelpers.CreateInstructorUser();

        // Act
        var studentToken     = _jwtHelper.GenerateToken(_testUser);
        var instructorToken  = _jwtHelper.GenerateToken(instructor);

        // Assert
        studentToken.Should().NotBe(instructorToken,
            "different users must produce different JWT tokens");
    }

    // ── ValidateToken Tests ──────────────────────────────────────────────────

    [Test]
    [Description("ValidateToken with a freshly generated token should return ClaimsPrincipal")]
    public void ValidateToken_ValidToken_ReturnsPrincipal()
    {
        // Arrange
        var token = _jwtHelper.GenerateToken(_testUser);

        // Act
        var principal = _jwtHelper.ValidateToken(token, validateLifetime: true);

        // Assert
        principal.Should().NotBeNull("valid token must return a non-null ClaimsPrincipal");
    }

    [Test]
    [Description("ValidateToken with a garbage string should return null")]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        // Act
        var principal = _jwtHelper.ValidateToken("this.is.not.a.valid.token", validateLifetime: true);

        // Assert
        principal.Should().BeNull("invalid token must return null");
    }

    [Test]
    [Description("ValidateToken with empty string should return null")]
    public void ValidateToken_EmptyToken_ReturnsNull()
    {
        // Act
        var principal = _jwtHelper.ValidateToken(string.Empty, validateLifetime: true);

        // Assert
        principal.Should().BeNull("empty token must return null");
    }

    [Test]
    [Description("ValidateToken with validateLifetime=false should accept token (used in refresh)")]
    public void ValidateToken_WithoutLifetimeCheck_ReturnsValidPrincipal()
    {
        // Arrange
        var token = _jwtHelper.GenerateToken(_testUser);

        // Act — validateLifetime=false is used by RefreshToken()
        var principal = _jwtHelper.ValidateToken(token, validateLifetime: false);

        // Assert
        principal.Should().NotBeNull();
    }

    [Test]
    [Description("INSTRUCTOR token should contain INSTRUCTOR role claim")]
    public void GenerateToken_InstructorUser_HasInstructorRoleClaim()
    {
        // Arrange
        var instructor = TestHelpers.CreateInstructorUser();

        // Act
        var token    = _jwtHelper.GenerateToken(instructor);
        var handler  = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var roleClaim = jwtToken.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role");

        roleClaim!.Value.Should().Be("INSTRUCTOR");
    }
}
