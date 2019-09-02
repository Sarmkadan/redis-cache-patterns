// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using StackExchange.Redis;
using RedisCachePatterns.Infrastructure.Cache;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Exceptions;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Services;

/// <summary>
/// Redis-based cache service implementing multiple caching patterns
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IRedisConnection _redisConnection;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly Dictionary<string, CachePolicy> _policies = new();

    public RedisCacheService(IRedisConnection redisConnection, ILogger<RedisCacheService> logger)
    {
        _redisConnection = redisConnection;
        _logger = logger;
    }

    // Cache-Aside Pattern: Check cache, if miss load from source and store
    public async Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan? expiration = null)
    {
        // Fix: Validate key and loadFn inputs to prevent null or empty values.
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null or whitespace.");
        }
        if (loadFn == null)
        {
            throw new ArgumentNullException(nameof(loadFn), "Load function cannot be null.");
        }

        try
        {
            var db = _redisConnection.GetDatabase();

            // Check cache first
            var cached = await db.StringGetAsync(key);
            if (cached.HasValue)
            {
                _logger.LogInformation("Cache hit for key: {Key}", key);
                return JsonSerializer.Deserialize<T>(cached.ToString());
            }

            // Cache miss - load from source
            _logger.LogInformation("Cache miss for key: {Key}, loading from source", key);
            var value = await loadFn();

            // Store in cache
            if (value != null)
            {
                var json = JsonSerializer.Serialize(value);
                var ttl = GetEffectiveExpiration(key, expiration);
                await db.StringSetAsync(key, json, ttl);
            }

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrLoadAsync for key: {Key}", key);
            throw new CacheException("Cache operation failed", ex);
        }
    }

    // Get value from cache without loading from source
    public async Task<T?> GetAsync<T>(string key)
    {
        // Fix: Validate key input to prevent null or empty values.
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null or whitespace.");
        }

        try
        {
            var db = _redisConnection.GetDatabase();
            var cached = await db.StringGetAsync(key);

            if (!cached.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(cached.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache for key: {Key}", key);
            throw new CacheException("Cache retrieval failed", ex);
        }
    }

    // Simple set operation
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        // Fix: Validate key and value inputs to prevent null or empty values.
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null or whitespace.");
        }
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value), "Value to cache cannot be null.");
        }

        try
        {
            var db = _redisConnection.GetDatabase();
            var json = JsonSerializer.Serialize(value);
            var ttl = GetEffectiveExpiration(key, expiration);
            await db.StringSetAsync(key, json, ttl);
            _logger.LogDebug("Cached value for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", key);
            throw new CacheException("Cache set operation failed", ex);
        }
    }

    // Write-Through Pattern: Update cache and database atomically
    public async Task<T> WriteAsync<T>(string key, T value, Func<Task<T>> persistFn, TimeSpan? expiration = null)
    {
        // Fix: Validate key, value, and persistFn inputs to prevent null or empty values.
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null or whitespace.");
        }
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value), "Value to write cannot be null.");
        }
        if (persistFn == null)
        {
            throw new ArgumentNullException(nameof(persistFn), "Persist function cannot be null.");
        }

        try
        {
            // First persist to database
            var persistedValue = await persistFn();

            // Then update cache
            var json = JsonSerializer.Serialize(persistedValue);
            var db = _redisConnection.GetDatabase();
            var ttl = GetEffectiveExpiration(key, expiration);
            await db.StringSetAsync(key, json, ttl);

            _logger.LogInformation("Write-through completed for key: {Key}", key);
            return persistedValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in write-through for key: {Key}", key);
            throw new CacheException("Write-through operation failed", ex);
        }
    }

    public async Task RemoveAsync(string key)
    {
        // Fix: Validate key input to prevent null or empty values.
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null or whitespace.");
        }

        try
        {
            var db = _redisConnection.GetDatabase();
            await db.KeyDeleteAsync(key);
            _logger.LogDebug("Removed cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
        }
    }

    // Remove all keys matching a pattern
    public async Task RemoveByPatternAsync(string pattern)
    {
        // Fix: Validate pattern input to prevent null or empty values.
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentNullException(nameof(pattern), "Cache pattern cannot be null or whitespace.");
        }

        try
        {
            var connection = _redisConnection.GetConnection();
            var keys = await GetKeysByPatternAsync(pattern);
            if (keys.Any())
            {
                var db = _redisConnection.GetDatabase();
                foreach (var key in keys)
                {
                    await db.KeyDeleteAsync(key);
                }
                _logger.LogInformation("Removed {Count} cache keys matching pattern: {Pattern}", keys.Count(), pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        // Fix: Validate key input to prevent null or empty values.
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null or whitespace.");
        }

        var db = _redisConnection.GetDatabase();
        return await db.KeyExistsAsync(key);
    }

    public async Task<TimeSpan?> GetExpirationAsync(string key)
    {
        // Fix: Validate key input to prevent null or empty values.
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null or whitespace.");
        }

        var db = _redisConnection.GetDatabase();
        return await db.KeyTimeToLiveAsync(key);
    }

    // Distributed Lock - Acquire lock with automatic expiration
    public async Task<bool> AcquireLockAsync(string lockKey, string lockValue, TimeSpan duration)
    {
        try
        {
            var db = _redisConnection.GetDatabase();
            var acquired = await db.StringSetAsync(lockKey, lockValue, duration, When.NotExists);
            if (acquired)
                _logger.LogInformation("Lock acquired: {LockKey}", lockKey);
            return acquired;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring lock: {LockKey}", lockKey);
            return false;
        }
    }

    // Release lock only if held by same holder
    public async Task<bool> ReleaseLockAsync(string lockKey, string lockValue)
    {
        try
        {
            var db = _redisConnection.GetDatabase();
            var currentValue = await db.StringGetAsync(lockKey);

            if (currentValue.HasValue && currentValue.ToString() == lockValue)
            {
                await db.KeyDeleteAsync(lockKey);
                _logger.LogInformation("Lock released: {LockKey}", lockKey);
                return true;
            }

            _logger.LogWarning("Lock value mismatch for key: {LockKey}", lockKey);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock: {LockKey}", lockKey);
            return false;
        }
    }

    // Renew lock expiration
    public async Task<bool> RenewLockAsync(string lockKey, string lockValue, TimeSpan newDuration)
    {
        try
        {
            var db = _redisConnection.GetDatabase();
            var currentValue = await db.StringGetAsync(lockKey);

            if (currentValue.HasValue && currentValue.ToString() == lockValue)
            {
                await db.StringSetAsync(lockKey, lockValue, newDuration);
                _logger.LogInformation("Lock renewed: {LockKey}", lockKey);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing lock: {LockKey}", lockKey);
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern)
    {
        try
        {
            var connection = _redisConnection.GetConnection();
            var server = connection.GetServer(connection.GetEndPoints().First());
            var keyList = new List<string>();
            await foreach (var key in server.KeysAsync(pattern: pattern))
                keyList.Add(key.ToString());
            return keyList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting keys by pattern: {Pattern}", pattern);
            return Enumerable.Empty<string>();
        }
    }

    public async Task FlushAsync()
    {
        try
        {
            var connection = _redisConnection.GetConnection();
            var server = connection.GetServer(connection.GetEndPoints().First());
            await server.FlushDatabaseAsync();
            _logger.LogWarning("Cache flushed completely");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing cache");
        }
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        try
        {
            var db = _redisConnection.GetDatabase();
            var connection = _redisConnection.GetConnection();
            var server = connection.GetServer(connection.GetEndPoints().First());

            var info = await server.InfoAsync();
            long memoryUsed = 0;
            var memSection = info.FirstOrDefault();
            if (memSection != null)
            {
                var memEntry = memSection.FirstOrDefault(x => x.Key == "used_memory");
                long.TryParse(memEntry.Value, out memoryUsed);
            }

            var keys = await GetKeysByPatternAsync("*");

            return new CacheStatistics
            {
                TotalKeys = keys.Count(),
                MemoryUsedBytes = (long)memoryUsed,
                CapturedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return new CacheStatistics();
        }
    }

    public async Task SetPolicyAsync(CachePolicy policy)
    {
        _policies[policy.Key] = policy;
        _logger.LogInformation("Cache policy set for key: {Key}", policy.Key);
    }

    public async Task<CachePolicy?> GetPolicyAsync(string key)
    {
        return _policies.TryGetValue(key, out var policy) ? policy : null;
    }

    private TimeSpan? GetEffectiveExpiration(string key, TimeSpan? expiration)
    {
        // Check if there's a policy for this key
        var policy = _policies.TryGetValue(key, out var p) ? p : null;
        return expiration ?? policy?.DefaultExpiration;
    }
}
