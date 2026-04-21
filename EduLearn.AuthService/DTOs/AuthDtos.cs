namespace EduLearn.AuthService.DTOs;

// ── REQUEST DTOs ──────────────────────────────────────────────────────────────
// These define what the frontend sends in the request body (JSON).
// Kept separate from Entity classes — entities are for DB, DTOs are for API.

/// <summary>
/// POST /api/auth/register
/// Frontend sends this to create a new account.
/// </summary>
public class RegisterRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Optional — defaults to STUDENT if not provided
    // Values: STUDENT | INSTRUCTOR
    // ADMIN accounts are created manually — not via register endpoint
    public string? Role { get; set; }
}

/// <summary>
/// POST /api/auth/login
/// Frontend sends email and password to get JWT token.
/// </summary>
public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// POST /api/auth/google-login
/// Called after Google returns user info to frontend.
/// </summary>
public class GoogleLoginRequestDto
{
    public string GoogleId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

/// <summary>
/// POST /api/auth/refresh and /validate-token
/// </summary>
public class TokenRequestDto
{
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// PUT /api/auth/users/{id}/profile
/// </summary>
public class UpdateProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// PUT /api/auth/users/{id}/change-password
/// Old password verified before new one is set.
/// </summary>
public class ChangePasswordDto
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

// ── RESPONSE DTOs ─────────────────────────────────────────────────────────────
// These define what the API sends back to the frontend.
// Never expose PasswordHash in responses.

/// <summary>
/// Returned after successful login or token refresh.
/// Frontend stores this token in localStorage or sessionStorage.
/// </summary>
public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Message { get; set; } = "Login successful";
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// Returned after register and for profile GET endpoints.
/// PasswordHash intentionally excluded.
/// </summary>
public class UserResponseDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}