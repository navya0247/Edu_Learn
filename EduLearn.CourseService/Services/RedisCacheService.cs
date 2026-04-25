using EduLearn.CourseService.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace EduLearn.CourseService.Services;

/// <summary>
/// Redis cache implementation using IDistributedCache.
/// In dev mode (no Redis running) — falls back silently, app still works.
/// PDF requirement: 5-min TTL for popular course list.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await _cache.GetStringAsync(key);
        }
        catch (Exception ex)
        {
            // Redis not running — log warning and return null (cache miss)
            _logger.LogWarning("Redis GET failed for key '{Key}': {Msg}", key, ex.Message);
            return null;
        }
    }

    public async Task SetAsync(string key, string value, TimeSpan expiry)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            };
            await _cache.SetStringAsync(key, value, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Redis SET failed for key '{Key}': {Msg}", key, ex.Message);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Redis REMOVE failed for key '{Key}': {Msg}", key, ex.Message);
        }
    }
}
