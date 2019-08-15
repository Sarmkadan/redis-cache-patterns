// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Domain;

namespace RedisCachePatterns.Services;

/// <summary>
/// Core caching service interface implementing cache-aside, write-through, and distributed lock patterns
/// </summary>
public interface ICacheService
{
    // Cache-Aside Pattern
    Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan? expiration = null);
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    // Write-Through Pattern
    Task<T> WriteAsync<T>(string key, T value, Func<Task<T>> persistFn, TimeSpan? expiration = null);

    // General operations
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task<bool> ExistsAsync(string key);
    Task<TimeSpan?> GetExpirationAsync(string key);
    Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern);

    // Distributed Locks
    Task<bool> AcquireLockAsync(string lockKey, string lockValue, TimeSpan duration);
    Task<bool> ReleaseLockAsync(string lockKey, string lockValue);
    Task<bool> RenewLockAsync(string lockKey, string lockValue, TimeSpan newDuration);

    // Cache Management
    Task FlushAsync();
    Task<CacheStatistics> GetStatisticsAsync();
    // ValueTask avoids Task allocation for the common case where policies are set
    // synchronously at startup and the result is already available.
    ValueTask SetPolicyAsync(CachePolicy policy);
    ValueTask<CachePolicy?> GetPolicyAsync(string key);
}

/// <summary>
/// Cache statistics for monitoring and diagnostics
/// </summary>
public class CacheStatistics
{
    public int TotalKeys { get; set; }
    public long MemoryUsedBytes { get; set; }
    public int Hits { get; set; }
    public int Misses { get; set; }
    public double HitRate => (Hits + Misses) > 0 ? (double)Hits / (Hits + Misses) * 100 : 0;
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
}
