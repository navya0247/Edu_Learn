using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EduLearn.AuthService.Entities;
using Microsoft.IdentityModel.Tokens;

namespace EduLearn.AuthService.Helpers;

/// <summary>
/// Helper class for JWT token operations.
/// Separated from UserService to keep concerns clean.
/// Generates and validates JWT Bearer tokens.
/// </summary>
public class JwtHelper
{
    private readonly string _secret;
    private readonly int _expiryHours;

    public JwtHelper(IConfiguration config)
    {
        _secret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret missing in appsettings.json");
        _expiryHours = int.Parse(config["Jwt:ExpiryHours"] ?? "24");
    }

    /// <summary>
    /// Generate signed JWT token with user claims embedded.
    /// Claims readable in any controller via User.FindFirstValue().
    /// Token used as: Authorization: Bearer {token}
    /// </summary>
    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims stored inside JWT — no DB lookup needed on each request
        var claims = new[]
        {
            // User ID — read via User.FindFirstValue(ClaimTypes.NameIdentifier)
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),

            // Email — read via User.FindFirstValue(ClaimTypes.Email)
            new Claim(ClaimTypes.Email, user.Email),

            // Name — read via User.FindFirstValue(ClaimTypes.Name)
            new Claim(ClaimTypes.Name, user.FullName),

            // Role — drives [Authorize(Roles = "ADMIN")] on controllers
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(_expiryHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validate token signature and optionally check expiry.
    /// validateLifetime = false used for refresh (allow expired tokens).
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token, bool validateLifetime = true)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secret);

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey        = new SymmetricSecurityKey(key),
                ValidateIssuer          = false,
                ValidateAudience        = false,
                ValidateLifetime        = validateLifetime,
                ClockSkew               = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            // Return null instead of throwing — caller checks for null
            return null;
        }
    }
}