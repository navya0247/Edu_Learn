using EduLearn.AuthService.Data;
using EduLearn.AuthService.Entities;
using EduLearn.AuthService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.AuthService.Repositories;

/// <summary>
/// EF Core implementation of IUserRepository.
/// All methods async — uses AuthDbContext → EduLearn_Auth SQL Server database.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _db;

    // AuthDbContext injected by DI — scoped lifetime (one per HTTP request)
    public UserRepository(AuthDbContext db) => _db = db;

    // Case-insensitive email lookup
    public async Task<User?> FindByEmail(string email)
        => await _db.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

    // PK lookup — EF checks change tracker cache first (faster than DB hit)
    public async Task<User?> FindByUserId(int userId)
        => await _db.Users.FindAsync(userId);

    // Returns true if email already registered
    public async Task<bool> ExistsByEmail(string email)
        => await _db.Users
            .AnyAsync(u => u.Email.ToLower() == email.ToLower());

    // Filter by role — only active accounts
    public async Task<IList<User>> FindAllByRole(string role)
        => await _db.Users
            .Where(u => u.Role == role && u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync();

    // All non-suspended users
    public async Task<IList<User>> FindAllActive()
        => await _db.Users
            .Where(u => u.IsActive)
            .ToListAsync();

    /// <summary>
    /// ExecuteUpdateAsync — atomic update without loading the entity.
    /// Generates: UPDATE Users SET LastLoginAt = @time WHERE UserId = @id
    /// </summary>
    public async Task UpdateLastLogin(int userId, DateTime loginTime)
        => await _db.Users
            .Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(u => u.LastLoginAt, loginTime));

    // EF Core LIKE search on FullName and Email
    public async Task<IList<User>> SearchUsers(string searchTerm)
    {
        var term = searchTerm.ToLower();
        return await _db.Users
            .Where(u => EF.Functions.Like(u.FullName.ToLower(), $"%{term}%") ||
                        EF.Functions.Like(u.Email.ToLower(), $"%{term}%"))
            .ToListAsync();
    }

    // Google OAuth — match by Google subject ID
    public async Task<User?> FindByGoogleId(string googleId)
        => await _db.Users
            .FirstOrDefaultAsync(u => u.GoogleId == googleId);

    // Add new user — EF Core sets UserId after SaveChanges (SQL identity)
    public async Task<User> Create(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    // Update user — EF Core change tracking generates SQL UPDATE
    public async Task<User> Update(User user)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
        return user;
    }

    // Hard delete — Admin permanent delete
    public async Task Delete(int userId)
        => await _db.Users
            .Where(u => u.UserId == userId)
            .ExecuteDeleteAsync();
}