using EduLearn.AuthService.Entities;

namespace EduLearn.AuthService.Interfaces;

/// <summary>
/// Repository interface — defines all async DB operations for User.
/// Implemented by UserRepository using EF Core.
/// Registered in DI: builder.Services.AddScoped&lt;IUserRepository, UserRepository&gt;()
/// </summary>
public interface IUserRepository
{
    // Find by email — used in Login() to look up user
    Task<User?> FindByEmail(string email);

    // Find by PK — used in GetProfile, UpdateProfile etc.
    Task<User?> FindByUserId(int userId);

    // Check if email taken — used in Register() to prevent duplicates
    Task<bool> ExistsByEmail(string email);

    // Get all users by role — Admin: list all students or instructors
    Task<IList<User>> FindAllByRole(string role);

    // Get all active users — Admin dashboard
    Task<IList<User>> FindAllActive();

    // Atomic update of LastLoginAt — uses ExecuteUpdateAsync
    Task UpdateLastLogin(int userId, DateTime loginTime);

    // EF Core LIKE search on name and email — Admin search feature
    Task<IList<User>> SearchUsers(string searchTerm);

    // Find by Google subject ID — Google OAuth login
    Task<User?> FindByGoogleId(string googleId);

    // CRUD
    Task<User> Create(User user);
    Task<User> Update(User user);
    Task Delete(int userId);
}