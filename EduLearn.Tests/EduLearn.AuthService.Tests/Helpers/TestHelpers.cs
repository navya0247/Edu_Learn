using EduLearn.AuthService.Entities;
using EduLearn.AuthService.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace EduLearn.AuthService.Tests.Helpers;

/// <summary>
/// Shared test helpers — used across all test classes.
/// Creates fake config, sample users, and JwtHelper instances.
/// </summary>
public static class TestHelpers
{
    // ── Fake IConfiguration ──────────────────────────────────────────────────

    /// <summary>
    /// Creates an IConfiguration with in-memory JWT settings.
    /// Used by JwtHelper without needing appsettings.json.
    /// </summary>
    public static IConfiguration CreateFakeConfig(
        string jwtSecret     = "TestSecret_EduLearn_Min32Chars_ForTests!",
        string jwtExpiryHours = "24")
    {
        var data = new Dictionary<string, string?>
        {
            ["Jwt:Secret"]      = jwtSecret,
            ["Jwt:ExpiryHours"] = jwtExpiryHours
        };

        return new ConfigurationBuilder()
            .Add(new MemoryConfigurationSource { InitialData = data })
            .Build();
    }

    /// <summary>
    /// Creates a JwtHelper instance with fake config for tests.
    /// </summary>
    public static JwtHelper CreateJwtHelper() =>
        new JwtHelper(CreateFakeConfig());

    // ── Sample User Factory ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a sample STUDENT user for testing.
    /// PasswordHash is left empty — tests that need it set it manually.
    /// </summary>
    public static User CreateStudentUser(int id = 1) => new()
    {
        UserId       = id,
        FullName     = "Test Student",
        Email        = "student@test.com",
        Role         = "STUDENT",
        IsActive     = true,
        CreatedAt    = DateTime.UtcNow,
        PasswordHash = string.Empty
    };

    /// <summary>
    /// Creates a sample INSTRUCTOR user.
    /// </summary>
    public static User CreateInstructorUser(int id = 2) => new()
    {
        UserId       = id,
        FullName     = "Test Instructor",
        Email        = "instructor@test.com",
        Role         = "INSTRUCTOR",
        IsActive     = true,
        CreatedAt    = DateTime.UtcNow,
        PasswordHash = string.Empty
    };

    /// <summary>
    /// Creates a sample ADMIN user.
    /// </summary>
    public static User CreateAdminUser(int id = 3) => new()
    {
        UserId       = id,
        FullName     = "Test Admin",
        Email        = "admin@test.com",
        Role         = "ADMIN",
        IsActive     = true,
        CreatedAt    = DateTime.UtcNow,
        PasswordHash = string.Empty
    };

    /// <summary>
    /// Creates a suspended (inactive) user.
    /// </summary>
    public static User CreateSuspendedUser(int id = 4) => new()
    {
        UserId       = id,
        FullName     = "Suspended User",
        Email        = "suspended@test.com",
        Role         = "STUDENT",
        IsActive     = false,   // <-- suspended
        CreatedAt    = DateTime.UtcNow,
        PasswordHash = string.Empty
    };
}
