namespace EduLearn.AuthService.Entities;

/// <summary>
/// User entity — maps to [Users] table in EduLearn_Auth SQL Server database.
/// Three roles supported: STUDENT, INSTRUCTOR, ADMIN.
/// </summary>
public class User
{
    // Primary key — auto increment by SQL Server
    public int UserId { get; set; }

    // Full name shown on profile, certificates, and reviews
    public string FullName { get; set; } = string.Empty;

    // Unique email — used as login username
    // Has unique index in DB to prevent duplicate accounts
    public string Email { get; set; } = string.Empty;

    // PBKDF2+HMAC-SHA256 hash via PasswordHasher<User>
    // Empty string for Google OAuth users (no password set)
    public string PasswordHash { get; set; } = string.Empty;

    // Drives [Authorize(Roles)] on all controllers
    // Values: STUDENT | INSTRUCTOR | ADMIN
    public string Role { get; set; } = "STUDENT";

    // Azure Blob Storage URL for profile picture
    public string? AvatarUrl { get; set; }

    // false = suspended by Admin — cannot login
    public bool IsActive { get; set; } = true;

    // Account creation timestamp
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Updated on every successful login
    public DateTime? LastLoginAt { get; set; }

    // Google OAuth subject ID — null for email/password users
    public string? GoogleId { get; set; }
}