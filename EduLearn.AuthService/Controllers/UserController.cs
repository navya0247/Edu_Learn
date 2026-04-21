using EduLearn.AuthService.DTOs;
using EduLearn.AuthService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduLearn.AuthService.Controllers;

/// <summary>
/// REST API controller for Auth service.
/// Base route: /api/auth
/// All protected endpoints require JWT Bearer token from Login.
/// </summary>
[ApiController]
[Route("api/auth")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger      = logger;
    }

    //  Public Endpoints (no JWT needed) 

    /// <summary>
    /// POST /api/auth/register
    /// Create new Student or Instructor account.
    /// Returns 201 Created with user info (no password).
    /// Returns 409 Conflict if email already registered.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        try
        {
            var result = await _userService.Register(dto);
            return CreatedAtAction(nameof(GetProfile),
                new { id = result.UserId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/auth/login
    /// Login with email + password.
    /// Returns JWT token — frontend stores this and sends as:
    /// Authorization: Bearer {token}
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        try
        {
            var result = await _userService.Login(dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/auth/google-login
    /// Google OAuth — called after Google returns user info to frontend.
    /// Auto-registers if first time. Returns JWT token.
    /// </summary>
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto dto)
    {
        try
        {
            var result = await _userService.LoginWithGoogle(dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/auth/validate-token
    /// Check if a JWT is valid. Used by API Gateway or frontend.
    /// </summary>
    [HttpPost("validate-token")]
    public async Task<IActionResult> ValidateToken([FromBody] TokenRequestDto dto)
    {
        var isValid = await _userService.ValidateToken(dto.Token);
        return Ok(new { isValid });
    }

    /// <summary>
    /// POST /api/auth/refresh
    /// Get new JWT from existing (possibly expired) token.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] TokenRequestDto dto)
    {
        try
        {
            var result = await _userService.RefreshToken(dto.Token);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    //  Authenticated Endpoints (valid JWT required) 

    /// <summary>
    /// POST /api/auth/logout
    /// JWT is stateless — signals client to discard token.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _userService.Logout(GetCurrentUserId());
        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// GET /api/auth/users/{id}/profile
    /// Get user profile — password never returned.
    /// </summary>
    [Authorize]
    [HttpGet("users/{id}/profile")]
    public async Task<IActionResult> GetProfile(int id)
    {
        var user = await _userService.GetUserById(id);
        if (user == null) return NotFound(new { message = "User not found" });
        return Ok(user);
    }

    /// <summary>
    /// PUT /api/auth/users/{id}/profile
    /// Update name and avatar. Only the user or Admin can update.
    /// </summary>
    [Authorize]
    [HttpPut("users/{id}/profile")]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileDto dto)
    {
        // Only self or Admin can update
        if (GetCurrentUserId() != id && !User.IsInRole("ADMIN"))
            return Forbid();

        try
        {
            var result = await _userService.UpdateProfile(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/auth/users/{id}/change-password
    /// Must provide old password. Only user themselves can change.
    /// </summary>
    [Authorize]
    [HttpPut("users/{id}/change-password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto dto)
    {
        if (GetCurrentUserId() != id) return Forbid();

        try
        {
            await _userService.ChangePassword(id, dto);
            return Ok(new { message = "Password changed successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    //  Admin Only Endpoints 

    /// <summary>
    /// GET /api/auth/users/by-role/{role}
    /// List all users with given role. Admin only.
    /// Example: GET /api/auth/users/by-role/STUDENT
    /// </summary>
    [Authorize(Roles = "ADMIN")]
    [HttpGet("users/by-role/{role}")]
    public async Task<IActionResult> GetByRole(string role)
    {
        var users = await _userService.GetAllByRole(role);
        return Ok(users);
    }

    /// <summary>
    /// GET /api/auth/users/search?term=navya
    /// Search users by name or email. Admin only.
    /// </summary>
    [Authorize(Roles = "ADMIN")]
    [HttpGet("users/search")]
    public async Task<IActionResult> Search([FromQuery] string term)
    {
        var users = await _userService.SearchUsers(term);
        return Ok(users);
    }

    /// <summary>
    /// PUT /api/auth/users/{id}/suspend
    /// Suspend user — sets IsActive = false. Admin only.
    /// </summary>
    [Authorize(Roles = "ADMIN")]
    [HttpPut("users/{id}/suspend")]
    public async Task<IActionResult> Suspend(int id)
    {
        try
        {
            await _userService.DeactivateAccount(id);
            _logger.LogWarning("Admin suspended user {UserId}", id);
            return Ok(new { message = "User suspended successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/auth/users/{id}/reactivate
    /// Reactivate suspended account. Admin only.
    /// </summary>
    [Authorize(Roles = "ADMIN")]
    [HttpPut("users/{id}/reactivate")]
    public async Task<IActionResult> Reactivate(int id)
    {
        try
        {
            await _userService.ReactivateAccount(id);
            _logger.LogInformation("Admin reactivated user {UserId}", id);
            return Ok(new { message = "User reactivated successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/auth/users/{id}
    /// Permanently delete user. Admin only.
    /// </summary>
    [Authorize(Roles = "ADMIN")]
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _userService.DeactivateAccount(id);
            _logger.LogWarning("Admin deleted user {UserId}", id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    //  Helper 

    // Extract UserId integer from JWT NameIdentifier claim
    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }
}