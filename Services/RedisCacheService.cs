#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Frozen;
using System.Text.Json;
using StackExchange.Redis;
using RedisCachePatterns.Infrastructure.Cache;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Exceptions;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Services;

/// <summary>
/// Redis-based cache service implementing multiple caching patterns.
///
/// Performance notes:
/// - Policy lookups use a FrozenDictionary snapshot for lock-free reads on the hot path.
/// - Lock release/renew use RedisValue == operator to avoid .ToString() allocation.
/// - RemoveByPatternAsync issues a single batch KeyDeleteAsync rather than N serial calls.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IRedisConnection _redisConnection;
    private readonly ILogger<RedisCacheService> _logger;

    // Mutable store written under _policyLock; reads always go through the frozen snapshot.
    private readonly Dictionary<string, CachePolicy> _policiesMutable = new();
    private volatile FrozenDictionary<string, CachePolicy> _policies =
        FrozenDictionary<string, CachePolicy>.Empty;
    private readonly Lock _policyLock = new();

    public RedisCacheService(IRedisConnection redisConnection, ILogger<RedisCacheService> logger)
    {
        _redisConnection = redisConnection;
        _logger = logger;
    }

    /// <summary>
    /// Cache-Aside pattern: check cache first, on miss load from <paramref name="loadFn"/> and store.
    /// Deserialization failures (e.g., schema changes between deployments) are treated as cache
    /// misses - the corrupted entry is evicted and the value is reloaded from source.
    ///
    /// <para>
    /// <b>TOCTOU safety:</b> this method issues a single <c>GET</c> command
    /// (<see cref="StackExchange.Redis.IDatabaseAsync.StringGetAsync"/>) rather than a
    /// separate <c>EXISTS</c> check followed by a <c>GET</c>. A key that expires between an
    /// existence check and the subsequent read would cause <c>StringGetAsync</c> to return
    /// <c>RedisValue.Null</c>; because <c>RedisValue.Null.HasValue</c> is <c>false</c> the code
    /// correctly treats that as a cache miss and falls through to <paramref name="loadFn"/>.
    /// </para>
    /// </summary>
    public async Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null or whitespace.");
        if (loadFn == null)
            throw new ArgumentNullException(nameof(loadFn), "Load function cannot be null.");

        try
        {
            var db = _redisConnection.GetDatabase();

            var cached = await db.StringGetAsync(key);
            if (cached.HasValue)
            {
                try
                {
                    _logger.LogInformation("Cache hit for key: {Key}", key);
                    return JsonSerializer.Deserialize<T>(cached.ToString());
                }
                catch (JsonException ex)
                {
                    // Stale or incompatible cached value - evict and fall through to reload
                    _logger.LogWarning(ex,
                        "Deserialization failed for key: {Key}. Evicting corrupted entry and reloading from source.",
                        key);
                    await db.KeyDeleteAsync(key);
                }
            }

            _logger.LogInformation("Cache miss for key: {Key}, loading from source", key);
            var value = await loadFn();

            if (value != null)
            {
                var json = JsonSerializer.Serialize(value);
                var ttl = GetEffectiveExpiration(key, expiration);
                await db.StringSetAsync(key, json, ttl);
            }

            return value;
        }
        catch (JsonException)
        {
            throw; // Already handled above; should not reach here but re-throw if it does
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrLoadAsync for key: {Key}", key);
            throw new CacheException("Cache operation failed", ex);
        }
    }

    /// <summary>
    /// Cache-aside with sliding expiration: on every cache hit <c>KeyExpireAsync</c> resets the
    /// TTL to <paramref name="slidingExpiration"/>, keeping hot entries alive while evicting
    /// entries that have not been accessed for the full window.
    /// </summary>
    public async Task<T?> GetOrLoadWithSlidingExpirationAsync<T>(
        string key, Func<Task<T>> loadFn, TimeSpan slidingExpiration)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null or whitespace.");
        if (loadFn == null)
            throw new ArgumentNullException(nameof(loadFn), "Load function cannot be null.");
        if (slidingExpiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(slidingExpiration), "Sliding expiration must be a positive duration.");

        try
        {
            var db = _redisConnection.GetDatabase();

            var cached = await db.StringGetAsync(key);
            if (cached.HasValue)
            {
                try
                {
                    var result = JsonSerializer.Deserialize<T>(cached.ToString());
                    // Reset the TTL on every hit so active entries stay warm.
                    await db.KeyExpireAsync(key, slidingExpiration);
                    _logger.LogDebug("Sliding cache hit for key: {Key} — TTL reset to {Ttl}", key, slidingExpiration);
                    return result;
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex,
                        "Deserialization failed for key: {Key}. Evicting corrupted entry and reloading from source.",
                        key);
                    await db.KeyDeleteAsync(key);
                }
            }

            _logger.LogDebug("Sliding cache miss for key: {Key} — loading from source", key);
            var value = await loadFn();

            if (value != null)
            {
                var json = JsonSerializer.Serialize(value);
                await db.StringSetAsync(key, json, slidingExpiration);
            }

            return value;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrLoadWithSlidingExpirationAsync for key: {Key}", key);
            throw new CacheException("Sliding cache operation failed", ex);
        }
    }


        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null or whitespace.");

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

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null or whitespace.");
        if (value == null)
            throw new ArgumentNullException(nameof(value), "Value to cache cannot be null.");

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
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null or whitespace.");
        if (value == null)
            throw new ArgumentNullException(nameof(value), "Value to write cannot be null.");
        if (persistFn == null)
            throw new ArgumentNullException(nameof(persistFn), "Persist function cannot be null.");

        try
        {
            var persistedValue = await persistFn();

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
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null or whitespace.");

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

    // Remove all keys matching a pattern using a single batch call
    public async Task RemoveByPatternAsync(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentNullException(nameof(pattern), "Cache pattern cannot be null or whitespace.");

        try
        {
            var keys = (await GetKeysByPatternAsync(pattern))
                .Select(k => (RedisKey)k)
                .ToArray();

            if (keys.Length > 0)
            {
                var db = _redisConnection.GetDatabase();
                // Single batch call instead of N sequential deletes
                await db.KeyDeleteAsync(keys);
                _logger.LogInformation(
                    "Removed {Count} cache keys matching pattern: {Pattern}", keys.Length, pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null or whitespace.");

        var db = _redisConnection.GetDatabase();
        return await db.KeyExistsAsync(key);
    }

    public async Task<TimeSpan?> GetExpirationAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key), "Cache key cannot be null or whitespace.");

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

    // Lua script for atomic compare-and-delete (lock release).
    // Prevents the race condition where a lock could expire and be re-acquired
    // by another client between our GET and DELETE operations.
    private static readonly LuaScript ReleaseLockScript = LuaScript.Prepare(
        "if redis.call('get', @key) == @value then return redis.call('del', @key) else return 0 end");

    // Lua script for atomic compare-and-renew (lock extension).
    private static readonly LuaScript RenewLockScript = LuaScript.Prepare(
        "if redis.call('get', @key) == @value then return redis.call('pexpire', @key, @ttl) else return 0 end");

    /// <summary>
    /// Releases a distributed lock atomically using a Lua script.
    /// The lock is only released if the current holder matches <paramref name="lockValue"/>,
    /// preventing accidental deletion of a lock held by another client.
    /// </summary>
    public async Task<bool> ReleaseLockAsync(string lockKey, string lockValue)
    {
        try
        {
            var db = _redisConnection.GetDatabase();
            var result = (int)await db.ScriptEvaluateAsync(
                ReleaseLockScript,
                new { key = (RedisKey)lockKey, value = lockValue });

            if (result == 1)
            {
                _logger.LogInformation("Lock released: {LockKey}", lockKey);
                return true;
            }

            _logger.LogWarning("Lock release failed (value mismatch or expired): {LockKey}", lockKey);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock: {LockKey}", lockKey);
            return false;
        }
    }

    /// <summary>
    /// Renews a distributed lock atomically using a Lua script.
    /// The lock TTL is only extended if the current holder matches <paramref name="lockValue"/>.
    /// </summary>
    public async Task<bool> RenewLockAsync(string lockKey, string lockValue, TimeSpan newDuration)
    {
        try
        {
            var db = _redisConnection.GetDatabase();
            var ttlMs = (long)newDuration.TotalMilliseconds;
            var result = (int)await db.ScriptEvaluateAsync(
                RenewLockScript,
                new { key = (RedisKey)lockKey, value = lockValue, ttl = ttlMs });

            if (result == 1)
            {
                _logger.LogInformation("Lock renewed: {LockKey} (TTL: {TtlMs}ms)", lockKey, ttlMs);
                return true;
            }

            _logger.LogWarning("Lock renew failed (value mismatch or expired): {LockKey}", lockKey);
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

            int totalKeys = 0;
            long memoryUsed = 0;
            int hits = 0;
            int misses = 0;

            foreach (var section in info)
            {
                if (section.Key == "Memory")
                {
                    var memEntry = section.FirstOrDefault(x => x.Key == "used_memory");
                    if (long.TryParse(memEntry.Value, out var val)) memoryUsed = val;
                }
                else if (section.Key == "Stats")
                {
                    var hitsEntry = section.FirstOrDefault(x => x.Key == "keyspace_hits");
                    if (int.TryParse(hitsEntry.Value, out var val)) hits = val;

                    var missesEntry = section.FirstOrDefault(x => x.Key == "keyspace_misses");
                    if (int.TryParse(missesEntry.Value, out var val)) misses = val;
                }
                else if (section.Key.StartsWith("Keyspace"))
                {
                    // Keyspace info example: db0:keys=100,expires=50,avg_ttl=12345
                    var dbEntry = section.FirstOrDefault(x => x.Key.StartsWith("db"));
                    if (!string.IsNullOrEmpty(dbEntry.Value))
                    {
                        var parts = dbEntry.Value.Split(',');
                        foreach (var part in parts)
                        {
                            if (part.StartsWith("keys="))
                            {
                                if (int.TryParse(part.Substring("keys=".Length), out var val))
                                {
                                    totalKeys += val;
                                }
                            }
                        }
                    }
                }
            }
            
            return new CacheStatistics
            {
                TotalKeys = totalKeys,
                MemoryUsedBytes = memoryUsed,
                Hits = hits,
                Misses = misses,
                CapturedAt = DateTime.UtcNow,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return new CacheStatistics();
        }
    }

    // ValueTask — no I/O involved; result is always synchronously available.
    public ValueTask SetPolicyAsync(CachePolicy policy)
    {
        lock (_policyLock)
        {
            _policiesMutable[policy.Key] = policy;
            _policies = _policiesMutable.ToFrozenDictionary();
        }
        _logger.LogInformation("Cache policy set for key: {Key}", policy.Key);
        return ValueTask.CompletedTask;
    }

    public ValueTask<CachePolicy?> GetPolicyAsync(string key)
    {
        _policies.TryGetValue(key, out var policy);
        return ValueTask.FromResult(policy);
    }

    // Hot path: reads the frozen snapshot — lock-free, branch-prediction-friendly.
    private TimeSpan? GetEffectiveExpiration(string key, TimeSpan? expiration)
    {
        if (expiration.HasValue) return expiration;
        _policies.TryGetValue(key, out var policy);
        return policy?.DefaultExpiration;
    }
}
