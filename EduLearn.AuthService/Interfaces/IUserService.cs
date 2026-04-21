using EduLearn.AuthService.DTOs;
using EduLearn.AuthService.Entities;

namespace EduLearn.AuthService.Interfaces;

/// <summary>
/// Service interface — all business logic for user auth.
/// Implemented by UserService. Injected into UserController via DI.
/// </summary>
public interface IUserService
{
    // Register new user — hashes password, checks duplicate email
    Task<UserResponseDto> Register(RegisterRequestDto dto);

    // Login — verifies hash, returns JWT token + user info
    Task<LoginResponseDto> Login(LoginRequestDto dto);

    // Google OAuth login — auto registers on first login
    Task<LoginResponseDto> LoginWithGoogle(GoogleLoginRequestDto dto);

    // JWT is stateless — client discards token on logout
    Task Logout(int userId);

    // Validate JWT signature and expiry
    Task<bool> ValidateToken(string token);

    // Issue new JWT from existing (possibly expired) token
    Task<LoginResponseDto> RefreshToken(string token);

    // Get user profile by ID
    Task<UserResponseDto?> GetUserById(int userId);

    // Update name and avatar URL
    Task<UserResponseDto> UpdateProfile(int userId, UpdateProfileDto dto);

    // Change password — verifies old password first
    Task ChangePassword(int userId, ChangePasswordDto dto);

    // Get all users with given role — Admin only
    Task<IList<UserResponseDto>> GetAllByRole(string role);

    // Suspend account — sets IsActive = false
    Task DeactivateAccount(int userId);

    // Reactivate suspended account
    Task ReactivateAccount(int userId);

    // Search by name or email — Admin only
    Task<IList<UserResponseDto>> SearchUsers(string searchTerm);
}