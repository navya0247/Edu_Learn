namespace EduLearn.CourseService.Interfaces;

/// <summary>
/// Redis cache abstraction — used by CourseService to cache popular course list.
/// PDF Non-Functional: Redis IDistributedCache with 5-min TTL for popular courses.
/// </summary>
public interface ICacheService
{
    /// <summary>Get value from Redis by key. Returns null if not found or expired.</summary>
    Task<string?> GetAsync(string key);

    /// <summary>Set value in Redis with TTL (time-to-live before auto expiry).</summary>
    Task SetAsync(string key, string value, TimeSpan expiry);

    /// <summary>Remove a key from Redis — called when data changes.</summary>
    Task RemoveAsync(string key);
}
