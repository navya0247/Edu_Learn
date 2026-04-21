using EduLearn.AuthService.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.AuthService.Data;

/// <summary>
/// EF Core DbContext for AuthService.
/// Connected to EduLearn_Auth SQL Server database.
/// Each microservice has its OWN database — this is the microservices pattern.
/// </summary>
public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    // Maps to [dbo].[Users] table in SQL Server
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            // Primary key
            entity.HasKey(u => u.UserId);

            // Required fields with nvarchar max lengths
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(300);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);
            entity.Property(u => u.Role).IsRequired().HasMaxLength(20)
                  .HasDefaultValue("STUDENT");

            // Optional fields
            entity.Property(u => u.AvatarUrl).HasMaxLength(1000);
            entity.Property(u => u.GoogleId).HasMaxLength(200);

            // SQL Server default values
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(u => u.IsActive).HasDefaultValue(true);

            // Unique index on Email — prevents duplicate accounts
            entity.HasIndex(u => u.Email).IsUnique();

            // Index on GoogleId — fast lookup during Google OAuth
            entity.HasIndex(u => u.GoogleId);

            // Index on Role — fast query for GetAllByRole (Admin feature)
            entity.HasIndex(u => u.Role);
        });
    }
}