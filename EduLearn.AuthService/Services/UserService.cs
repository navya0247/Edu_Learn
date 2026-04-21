using System.Security.Claims;
using EduLearn.AuthService.DTOs;
using EduLearn.AuthService.Entities;
using EduLearn.AuthService.Helpers;
using EduLearn.AuthService.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace EduLearn.AuthService.Services;

/// <summary>
/// Implements IUserService — all authentication business logic.
/// Uses JwtHelper for token generation and PasswordHasher for hashing.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _repo;
    private readonly JwtHelper _jwtHelper;
    private readonly ILogger<UserService> _logger;

    // PasswordHasher from ASP.NET Core Identity
    // Handles salting + PBKDF2+HMAC-SHA256 hashing automatically
    private readonly PasswordHasher<User> _hasher = new();

    public UserService(
        IUserRepository repo,
        JwtHelper jwtHelper,
        ILogger<UserService> logger)
    {
        _repo      = repo;
        _jwtHelper = jwtHelper;
        _logger    = logger;
    }

    /// <summary>
    /// Register new user — hash password, check duplicate email, save to DB.
    /// Returns UserResponseDto (no password in response).
    /// </summary>
    public async Task<UserResponseDto> Register(RegisterRequestDto dto)
    {
        // Prevent duplicate accounts
        if (await _repo.ExistsByEmail(dto.Email))
            throw new InvalidOperationException("An account with this email already exists.");

        var user = new User
        {
            FullName  = dto.FullName,
            Email     = dto.Email,
            Role      = dto.Role ?? "STUDENT",
            IsActive  = true,
            CreatedAt = DateTime.UtcNow
        };

        // Hash the plain password — stored as PBKDF2+HMAC-SHA256
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        var created = await _repo.Create(user);
        _logger.LogInformation("New user registered: {Email} as {Role}", created.Email, created.Role);

        return MapToUserResponse(created);
    }

    /// <summary>
    /// Login — verify password hash, update last login, return JWT.
    /// Throws UnauthorizedAccessException on failure.
    /// </summary>
    public async Task<LoginResponseDto> Login(LoginRequestDto dto)
    {
        var user = await _repo.FindByEmail(dto.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        // Suspended users cannot log in
        if (!user.IsActive)
            throw new UnauthorizedAccessException("Your account has been suspended. Contact admin.");

        // VerifyHashedPassword compares stored hash with provided plain password
        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid email or password.");

        // Update last login timestamp atomically
        await _repo.UpdateLastLogin(user.UserId, DateTime.UtcNow);

        _logger.LogInformation("User logged in: {Email}", user.Email);

        return new LoginResponseDto
        {
            Token    = _jwtHelper.GenerateToken(user),
            UserId   = user.UserId,
            FullName = user.FullName,
            Email    = user.Email,
            Role     = user.Role,
            Message  = "Login successful"
        };
    }

    /// <summary>
    /// Google OAuth login — auto-registers on first login.
    /// Links Google ID to existing account if email matches.
    /// </summary>
    public async Task<LoginResponseDto> LoginWithGoogle(GoogleLoginRequestDto dto)
    {
        // Check if this Google account has logged in before
        var user = await _repo.FindByGoogleId(dto.GoogleId);

        if (user == null)
        {
            // Check if email already registered via normal signup
            var existing = await _repo.FindByEmail(dto.Email);
            if (existing != null)
            {
                // Link Google ID to existing account
                existing.GoogleId = dto.GoogleId;
                user = await _repo.Update(existing);
            }
            else
            {
                // First time — auto register as STUDENT
                user = await _repo.Create(new User
                {
                    FullName      = dto.FullName,
                    Email         = dto.Email,
                    GoogleId      = dto.GoogleId,
                    PasswordHash  = string.Empty, // No password for Google users
                    Role          = "STUDENT",
                    IsActive      = true,
                    CreatedAt     = DateTime.UtcNow
                });
                _logger.LogInformation("New user via Google: {Email}", dto.Email);
            }
        }

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Your account has been suspended.");

        await _repo.UpdateLastLogin(user.UserId, DateTime.UtcNow);

        return new LoginResponseDto
        {
            Token    = _jwtHelper.GenerateToken(user),
            UserId   = user.UserId,
            FullName = user.FullName,
            Email    = user.Email,
            Role     = user.Role,
            Message  = "Google login successful"
        };
    }

    // JWT is stateless — client simply discards the token
    public Task Logout(int userId) => Task.CompletedTask;

    // Check if token is valid and not expired
    public Task<bool> ValidateToken(string token)
    {
        var principal = _jwtHelper.ValidateToken(token, validateLifetime: true);
        return Task.FromResult(principal != null);
    }

    /// <summary>
    /// Refresh — issue new token using claims from old (possibly expired) token.
    /// </summary>
    public async Task<LoginResponseDto> RefreshToken(string token)
    {
        // validateLifetime = false allows expired tokens for refresh
        var principal = _jwtHelper.ValidateToken(token, validateLifetime: false)
            ?? throw new UnauthorizedAccessException("Invalid token.");

        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Invalid token claims.");

        var user = await _repo.FindByUserId(int.Parse(userIdStr))
            ?? throw new UnauthorizedAccessException("User not found.");

        return new LoginResponseDto
        {
            Token    = _jwtHelper.GenerateToken(user),
            UserId   = user.UserId,
            FullName = user.FullName,
            Email    = user.Email,
            Role     = user.Role,
            Message  = "Token refreshed"
        };
    }

    public async Task<UserResponseDto?> GetUserById(int userId)
    {
        var user = await _repo.FindByUserId(userId);
        return user == null ? null : MapToUserResponse(user);
    }

    // Update name and avatar only
    public async Task<UserResponseDto> UpdateProfile(int userId, UpdateProfileDto dto)
    {
        var user = await _repo.FindByUserId(userId)
            ?? throw new KeyNotFoundException("User not found.");

        user.FullName = dto.FullName;
        if (!string.IsNullOrWhiteSpace(dto.AvatarUrl))
            user.AvatarUrl = dto.AvatarUrl;

        var updated = await _repo.Update(user);
        return MapToUserResponse(updated);
    }

    // Change password — verify old password first
    public async Task ChangePassword(int userId, ChangePasswordDto dto)
    {
        var user = await _repo.FindByUserId(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var check = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.OldPassword);
        if (check == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = _hasher.HashPassword(user, dto.NewPassword);
        await _repo.Update(user);
    }

    public async Task<IList<UserResponseDto>> GetAllByRole(string role)
    {
        var users = await _repo.FindAllByRole(role);
        return users.Select(MapToUserResponse).ToList();
    }

    // Soft delete — sets IsActive = false
    public async Task DeactivateAccount(int userId)
    {
        var user = await _repo.FindByUserId(userId)
            ?? throw new KeyNotFoundException("User not found.");
        user.IsActive = false;
        await _repo.Update(user);
        _logger.LogWarning("Account deactivated: UserId {UserId}", userId);
    }

    // Reactivate suspended account
    public async Task ReactivateAccount(int userId)
    {
        var user = await _repo.FindByUserId(userId)
            ?? throw new KeyNotFoundException("User not found.");
        user.IsActive = true;
        await _repo.Update(user);
        _logger.LogInformation("Account reactivated: UserId {UserId}", userId);
    }

    public async Task<IList<UserResponseDto>> SearchUsers(string searchTerm)
    {
        var users = await _repo.SearchUsers(searchTerm);
        return users.Select(MapToUserResponse).ToList();
    }

    // ── Private Helper ────────────────────────────────────────────────────────

    // Map User entity to UserResponseDto — never expose PasswordHash
    private static UserResponseDto MapToUserResponse(User user) => new()
    {
        UserId      = user.UserId,
        FullName    = user.FullName,
        Email       = user.Email,
        Role        = user.Role,
        AvatarUrl   = user.AvatarUrl,
        IsActive    = user.IsActive,
        CreatedAt   = user.CreatedAt,
        LastLoginAt = user.LastLoginAt
    };
}