#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text.Json;
using StackExchange.Redis;
using RedisCachePatterns.Configuration;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Exceptions;
using RedisCachePatterns.Infrastructure.Cache;
using RedisCachePatterns.Monitoring;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Services;

/// <summary>
/// Cluster-aware implementation of <see cref="ICacheService"/> that targets a Redis Cluster
/// deployment.
/// <para>
/// Standard cache operations (get, set, remove, lock) are routed automatically by
/// StackExchange.Redis through <c>MOVED</c> / <c>ASK</c> redirections — no application-level
/// slot awareness is needed for these. Cluster-specific value is delivered by:
/// <list type="bullet">
///   <item>
///     <b>Pattern scans</b> — <see cref="GetKeysByPatternAsync"/> and <see cref="RemoveByPatternAsync"/>
///     fan out a <c>SCAN</c> cursor across every master node concurrently.
///   </item>
///   <item>
///     <b>Statistics</b> — <see cref="GetStatisticsAsync"/> aggregates memory and key counts from
///     all shards into a single <see cref="CacheStatistics"/> snapshot.
///   </item>
///   <item>
///     <b>Flush</b> — <see cref="FlushAsync"/> issues <c>FLUSHDB</c> on every master in parallel.
///   </item>
/// </list>
/// Distributed locking uses the standard single-slot approach; the cluster provides
/// fault-tolerance for the lock key through automatic master/replica failover.
/// </para>
/// </summary>
public sealed class RedisClusterCacheService : ICacheService
{
    // Lua script for atomic compare-and-delete (lock release) in Redis Cluster.
    // Prevents the race condition where a lock could expire and be re-acquired
    // by another client between our GET and DELETE operations.
    private static readonly LuaScript ReleaseLockScript = LuaScript.Prepare(
        "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end");

    // Lua script for atomic compare-and-set (lock renewal) in Redis Cluster.
    // Only extends the TTL if the lock is still held by the current holder.
    private static readonly LuaScript RenewLockScript = LuaScript.Prepare(
        "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('pexpire', KEYS[1], tonumber(ARGV[2])) else return 0 end");

    private readonly IRedisClusterConnection _cluster;
    private readonly Configuration.ClusterConfiguration _clusterConfig;
    private readonly ILogger<RedisClusterCacheService> _logger;
    private readonly CacheStatisticsAggregator _statsAggregator = CacheStatisticsAggregator.Instance;

    // Policy store — same lock-free frozen-snapshot pattern as RedisCacheService.
    private readonly Dictionary<string, CachePolicy> _policiesMutable = new();
    private volatile FrozenDictionary<string, CachePolicy> _policies =
        FrozenDictionary<string, CachePolicy>.Empty;
    private readonly Lock _policyLock = new();

    // Configurable TTL jitter percentage (e.g., 0.1 for ±10%). Default is 10%.
    private readonly double _ttlJitterPercentage;

    /// <summary>
    /// Initialises the service with the cluster connection and configuration.
    /// </summary>
    /// <param name="cluster">Cluster connection.</param>
    /// <param name="clusterConfig">Cluster configuration.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="ttlJitterPercentage">
    /// Percentage of jitter to apply to expirations (0‑1). Default is 0.1 (±10%).
    /// </param>
    public RedisClusterCacheService(
        IRedisClusterConnection cluster,
        Configuration.ClusterConfiguration clusterConfig,
        ILogger<RedisClusterCacheService> logger,
        double ttlJitterPercentage = 0.1)
    {
        _cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));
        _clusterConfig = clusterConfig ?? throw new ArgumentNullException(nameof(clusterConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ttlJitterPercentage = ttlJitterPercentage;
    }

    // ── Cache-Aside ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));
        ArgumentNullException.ThrowIfNull(loadFn);

        try
        {
            var db = _cluster.GetDatabase();
            var cached = await db.StringGetAsync(key);

            if (cached.HasValue)
            {
                _logger.LogDebug("Cluster cache hit: {Key}", key);
                return JsonSerializer.Deserialize<T>(cached.ToString());
                _statsAggregator.IncrementHits();
            }

            _logger.LogDebug("Cluster cache miss: {Key} — loading from source", key);
                _statsAggregator.IncrementMisses();
            var value = await loadFn();

            if (value is not null)
            {
                var json = JsonSerializer.Serialize(value);
                await db.StringSetAsync(key, json, GetEffectiveExpiration(key, expiration));
            }

            return value;
        }
        catch (Exception ex) when (ex is not CacheException)
        {
            _logger.LogError(ex, "GetOrLoadAsync failed for key: {Key}", key);
            throw new CacheException("Cluster cache-aside operation failed", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<T?> GetOrLoadWithSlidingExpirationAsync<T>(
        string key, Func<Task<T>> loadFn, TimeSpan slidingExpiration)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));
        ArgumentNullException.ThrowIfNull(loadFn);
        if (slidingExpiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(slidingExpiration), "Sliding expiration must be a positive duration.");

        try
        {
            var db = _cluster.GetDatabase();

            var cached = await db.StringGetAsync(key);
            if (cached.HasValue)
            {
                try
                {
                    var result = JsonSerializer.Deserialize<T>(cached.ToString());
                    await db.KeyExpireAsync(key, slidingExpiration);
                _statsAggregator.IncrementHits();
                    _logger.LogDebug("Cluster sliding cache hit: {Key} — TTL reset to {Ttl}", key, slidingExpiration);
                    return result;
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Deserialization failed for key: {Key}. Evicting and reloading.", key);
                    await db.KeyDeleteAsync(key);
                }
                _statsAggregator.IncrementMisses();
            }

            _logger.LogDebug("Cluster sliding cache miss: {Key} — loading from source", key);
            var value = await loadFn();

            if (value is not null)
            {
                var json = JsonSerializer.Serialize(value);
                await db.StringSetAsync(key, json, slidingExpiration);
            }

            return value;
        }
        catch (Exception ex) when (ex is not CacheException)
        {
            _logger.LogError(ex, "GetOrLoadWithSlidingExpirationAsync failed for key: {Key}", key);
            throw new CacheException("Cluster sliding cache operation failed", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<T?> GetAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        try
        {
                _statsAggregator.IncrementHits();
            var db = _cluster.GetDatabase();
            var value = await db.StringGetAsync(key);
            if (!value.HasValue) return default;
            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (Exception ex) when (ex is not CacheException)
        {
            _logger.LogError(ex, "GetAsync failed for key: {Key}", key);
            throw new CacheException("Cluster cache retrieval failed", ex);
        }
    }

    /// <summary>
    /// Retrieves a cached value by key and refreshes its TTL on successful read (sliding expiration).
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key to look up.</param>
    /// <param name="slidingExpiration">The TTL to apply on every successful read.</param>
    /// <returns>The deserialized value if found; otherwise <c>default</c>.</returns>
    public async Task<T?> GetWithSlidingExpirationAsync<T>(string key, TimeSpan slidingExpiration)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));
        if (slidingExpiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(slidingExpiration), "Sliding expiration must be a positive duration.");

        try
        {
            var db = _cluster.GetDatabase();
            var cached = await db.StringGetAsync(key);

            if (!cached.HasValue)
            {
                _logger.LogDebug("Cluster sliding expiration cache miss: {Key}", key);
                return default;
            }

            try
            {
                var result = JsonSerializer.Deserialize<T>(cached.ToString());
                // Reset the TTL on every hit so active entries stay warm.
                await db.KeyExpireAsync(key, slidingExpiration);
                _logger.LogDebug("Cluster sliding expiration cache hit: {Key} — TTL reset to {Ttl}", key, slidingExpiration);
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Deserialization failed for key: {Key}. Evicting corrupted entry.", key);
                await db.KeyDeleteAsync(key);
                return default;
            }
        }
        catch (Exception ex) when (ex is not CacheException)
        {
            _logger.LogError(ex, "GetWithSlidingExpirationAsync failed for key: {Key}", key);
            throw new CacheException("Cluster sliding expiration cache operation failed", ex);
        }
    }

    /// <inheritdoc/>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var db = _cluster.GetDatabase();
            var json = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, json, GetEffectiveExpiration(key, expiration));
        }
        catch (Exception ex) when (ex is not CacheException)
        {
            _logger.LogError(ex, "SetAsync failed for key: {Key}", key);
            throw new CacheException("Cluster cache set failed", ex);
        }
    }

    // ── Write-Through ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<T> WriteAsync<T>(string key, T value, Func<Task<T>> persistFn, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(persistFn);

        var persisted = await persistFn();

        IDatabase? db = null;
        try
        {
            db = _cluster.GetDatabase();
            var json = JsonSerializer.Serialize(persisted);
            await db.StringSetAsync(key, json, GetEffectiveExpiration(key, expiration));
            _logger.LogDebug("Cluster write-through completed for key: {Key}", key);
        }
        catch (Exception cacheEx) when (cacheEx is not CacheException)
        {
            _logger.LogWarning(cacheEx,
                "Cluster write-through cache update failed for key: {Key}. Invalidating key to prevent stale reads.",
                key);
            try { if (db != null) await db.KeyDeleteAsync(key); }
            catch (Exception deleteEx)
            {
                _logger.LogError(deleteEx,
                    "Failed to invalidate cache key after write-through failure: {Key}", key);
            }

            throw new CacheException(
                "Cluster write-through cache update failed after successful database persistence. " +
                "The cache key has been invalidated; the next read will reload from the database.",
                cacheEx);
        }

        return persisted;
    }

    // ── General operations ────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task RemoveAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        await _cluster.GetDatabase().KeyDeleteAsync(key);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Fans the <c>SCAN</c> cursor out to every master node concurrently, then issues a
    /// single batched <c>DEL</c> per shard for matched keys.
    /// </remarks>
    public async Task RemoveByPatternAsync(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentNullException(nameof(pattern));

        var keys = (await GetKeysByPatternAsync(pattern))
            .Select(k => (RedisKey)k)
            .ToArray();

        if (keys.Length == 0) return;

        var db = _cluster.GetDatabase();
        await db.KeyDeleteAsync(keys);
        _logger.LogInformation(
            "Cluster pattern remove: deleted {Count} key(s) matching '{Pattern}'", keys.Length, pattern);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        return await _cluster.GetDatabase().KeyExistsAsync(key);
    }

    /// <inheritdoc/>
    public async Task<TimeSpan?> GetExpirationAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        return await _cluster.GetDatabase().KeyTimeToLiveAsync(key);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Concurrently scans every master node with a <c>SCAN</c> cursor. Results are aggregated
    /// into a de-duplicated collection. Keys that exist on a node but are returned multiple
    /// times (due to migration) are naturally deduplicated by the <see cref="ConcurrentBag{T}"/>
    /// caller conversion.
    /// </remarks>
    public async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern)
    {
        var bag = new ConcurrentBag<string>();

        await _cluster.ForEachMasterAsync(async server =>
        {
            await foreach (var key in server.KeysAsync(
                               pattern: pattern,
                               pageSize: _clusterConfig.SlotScanPageSize))
            {
                bag.Add(key.ToString());
            }
        });

        return bag;
    }

/// <summary>
/// Retrieves multiple cached values by their keys in a single batch operation.
/// Uses Redis pipelining via StringGetAsync(IEnumerable<RedisKey>) for efficiency.
/// </summary>
/// <typeparam name="T">The type of the cached values.</typeparam>
/// <param name="keys">Collection of keys to retrieve.</param>
/// <returns>A dictionary mapping keys to their cached values (null if not found).</returns>
public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys)
{
if (keys == null)
throw new ArgumentNullException(nameof(keys), "Keys collection cannot be null.");

try
{
var db = _cluster.GetDatabase();

// Convert string keys to RedisKey array for batch operation
var redisKeys = keys.Select(k => (RedisKey)k).ToArray();

// Use StackExchange.Redis batch GET operation for efficiency
var values = await db.StringGetAsync(redisKeys);

// Build result dictionary preserving key order
var result = new Dictionary<string, T?>();
var index = 0;
foreach (var key in keys)
{
var value = values[index];
index++;

if (value.HasValue)
{
try
{
var deserialized = JsonSerializer.Deserialize<T>(value.ToString());
result[key] = deserialized;
}
catch (JsonException ex)
{
_logger.LogWarning(ex, "Deserialization failed for key: {Key}. Evicting corrupted entry.", key);
await db.KeyDeleteAsync(key);
result[key] = default;
}
}
else
{
result[key] = default;
}
}

return result;
}
catch (Exception ex)
{
_logger.LogError(ex, "Error in GetManyAsync");
throw new CacheException("Cluster batch get operation failed", ex);
}
}

// ── Distributed Locks

    // ── Distributed Locks ─────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// Uses the standard <c>SET … NX PX</c> approach; StackExchange.Redis routes the lock
    /// key to the correct master via hash-slot computation. The cluster provides fault-tolerance
    /// for the lock through automatic master/replica failover.
    /// Retries up to <see cref="ClusterConfiguration.RedlockRetryCount"/> times with
    /// <see cref="ClusterConfiguration.RedlockRetryDelay"/> between attempts.
    /// </remarks>
    public async Task<bool> AcquireLockAsync(string lockKey, string lockValue, TimeSpan duration)
    {
        var db = _cluster.GetDatabase();

        for (var attempt = 0; attempt < _clusterConfig.RedlockRetryCount; attempt++)
        {
            try
            {
                if (await db.StringSetAsync(lockKey, lockValue, duration, When.NotExists))
                {
                    _logger.LogInformation("Lock acquired on cluster shard: {LockKey}", lockKey);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Lock acquisition attempt {Attempt}/{Max} failed for: {LockKey}",
                    attempt + 1, _clusterConfig.RedlockRetryCount, lockKey);
            }

            if (attempt < _clusterConfig.RedlockRetryCount - 1)
                await Task.Delay(_clusterConfig.RedlockRetryDelay);
        }

        _logger.LogWarning("Failed to acquire lock after {Max} attempt(s): {LockKey}",
            _clusterConfig.RedlockRetryCount, lockKey);
        return false;
    }

    /// <inheritdoc/>
    public async Task<bool> ReleaseLockAsync(string lockKey, string lockValue)
    {
    try
    {
        var db = _cluster.GetDatabase();
        var result = (int)await db.ScriptEvaluateAsync(
            ReleaseLockScript,
            new { keys = new RedisKey[] { lockKey }, args = new RedisValue[] { lockValue } });

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
        _logger.LogError(ex, "ReleaseLockAsync failed for: {LockKey}", lockKey);
        return false;
    }
}

    /// <inheritdoc/>
    public async Task<bool> RenewLockAsync(string lockKey, string lockValue, TimeSpan newDuration)
    {
    try
    {
        var db = _cluster.GetDatabase();
        var ttlMs = (long)newDuration.TotalMilliseconds;
        var result = (int)await db.ScriptEvaluateAsync(
            RenewLockScript,
            new { keys = new RedisKey[] { lockKey }, args = new RedisValue[] { lockValue, ttlMs } });

        if (result == 1)
        {
            _logger.LogInformation("Lock renewed: {LockKey} for {Duration}", lockKey, newDuration);
            return true;
        }

        _logger.LogWarning("Lock renew failed (value mismatch or expired): {LockKey}", lockKey);
        return false;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "RenewLockAsync failed for: {LockKey}", lockKey);
        return false;
    }
}

    // ── Cache Management ──────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// Issues <c>FLUSHDB</c> on every master node concurrently.
    /// All replicas will converge to empty via replication.
    /// </remarks>
    public async Task FlushAsync()
    {
        await _cluster.ForEachMasterAsync(server => server.FlushDatabaseAsync(0));
        _logger.LogWarning("Cluster cache flushed across all master nodes");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Aggregates <c>used_memory</c> and total key count across all master shards.
    /// Hit/miss counters are not maintained server-side per node in cluster mode;
    /// they must be tracked at the application layer via <see cref="Events.CacheEventListener"/>.
    /// </remarks>
    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        var memoryPerShard = new ConcurrentBag<long>();
        var keysPerShard = new ConcurrentBag<long>();

        try
        {
            await _cluster.ForEachMasterAsync(async server =>
            {
                var info = await server.InfoAsync("memory");
                var memSection = info.FirstOrDefault();
                if (memSection is not null)
                {
                    var entry = memSection.FirstOrDefault(x => x.Key == "used_memory");
                    if (long.TryParse(entry.Value, out var mem))
                        memoryPerShard.Add(mem);
                }

                // DBSIZE on each shard for a precise key count without a full SCAN.
                var dbSize = await server.DatabaseSizeAsync(0);
                keysPerShard.Add(dbSize);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetStatisticsAsync failed — returning partial stats");
        }

        return new CacheStatistics
        {
            TotalKeys = (int)keysPerShard.Sum(),
            MemoryUsedBytes = memoryPerShard.Sum(),
            CapturedAt = DateTime.UtcNow
        };
    }

    // ── Policy management (lock-free frozen-snapshot) ─────────────────────────

    /// <inheritdoc/>
    public ValueTask SetPolicyAsync(CachePolicy policy)
    {
        lock (_policyLock)
        {
            _policiesMutable[policy.Key] = policy;
            _policies = _policiesMutable.ToFrozenDictionary();
        }
        _logger.LogDebug("Cache policy set for key: {PolicyKey}", policy.Key);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask<CachePolicy?> GetPolicyAsync(string key)
    {
        _policies.TryGetValue(key, out var policy);
        return ValueTask.FromResult(policy);
    }

    // ── XFetch: probabilistic early expiration ────────────────────────────────

    // Per-key recompute-time estimates (seconds) for the XFetch algorithm.
    private readonly ConcurrentDictionary<string, double> _recomputeTimesSeconds = new();

    private const string MetaKeySuffix = ":meta";

    /// <inheritdoc/>
    public async Task<T?> GetOrLoadWithEarlyExpirationAsync<T>(
        string key,
        Func<Task<T>> loadFn,
        TimeSpan expiration,
        double beta = 1.0)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));
        ArgumentNullException.ThrowIfNull(loadFn);
        if (expiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(expiration), "Expiration must be a positive duration.");

        try
        {
            var db = _cluster.GetDatabase();
            var cached = await db.StringGetAsync(key);

            if (cached.HasValue)
            {
                var ttl = await db.KeyTimeToLiveAsync(key);
                var remainingSecs = ttl?.TotalSeconds ?? 0;
                var delta = _recomputeTimesSeconds.TryGetValue(key, out var d) ? d : 0.001;
                var earlyRefreshScore = delta * beta * (-Math.Log(Random.Shared.NextDouble()));

                if (remainingSecs > 0 && earlyRefreshScore < remainingSecs)
                {
                    try { return JsonSerializer.Deserialize<T>(cached.ToString()); }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex,
                            "Deserialization failed for key: {Key}. Evicting and reloading.", key);
                        await db.KeyDeleteAsync(key);
                    }
                }
                else
                {
                    _logger.LogDebug(
                        "XFetch early refresh triggered for key: {Key} (remaining: {Remaining:F1}s)",
                        key, remainingSecs);
                }
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var value = await loadFn();
            sw.Stop();
            _recomputeTimesSeconds[key] = sw.Elapsed.TotalSeconds;

            if (value is not null)
                await db.StringSetAsync(key, JsonSerializer.Serialize(value), expiration);

            return value;
        }
        catch (Exception ex) when (ex is not CacheException)
        {
            _logger.LogError(ex, "GetOrLoadWithEarlyExpirationAsync failed for key: {Key}", key);
            throw new CacheException("Cluster early-expiration cache operation failed", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<CacheKeyMetadata?> GetKeyMetadataAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        try
        {
            var db = _cluster.GetDatabase();
            var entries = await db.HashGetAllAsync($"{key}{MetaKeySuffix}");
            if (entries.Length == 0) return null;

            var map = entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
            return new CacheKeyMetadata
            {
                Key = key,
                HitCount = map.TryGetValue("hitCount", out var hc) && long.TryParse(hc, out var h) ? h : 0,
                LastAccessed = map.TryGetValue("lastAccessed", out var la) && long.TryParse(la, out var laMs)
                    ? DateTimeOffset.FromUnixTimeMilliseconds(laMs).UtcDateTime : null,
                CreatedAt = map.TryGetValue("createdAt", out var ca) && long.TryParse(ca, out var caMs)
                    ? DateTimeOffset.FromUnixTimeMilliseconds(caMs).UtcDateTime : null,
                SizeBytes = map.TryGetValue("size", out var sz) && long.TryParse(sz, out var s) ? s : 0,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetKeyMetadataAsync failed for key: {Key}", key);
            return null;
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    // Hot path — reads the frozen snapshot; lock-free.
    private TimeSpan? GetEffectiveExpiration(string key, TimeSpan? expiration)
    {
        // Resolve the base expiration (explicit or policy‑based)
        TimeSpan? baseExpiration = expiration;
        if (!baseExpiration.HasValue)
        {
            _policies.TryGetValue(key, out var policy);
            baseExpiration = policy?.DefaultExpiration;
        }

        // Apply jitter if we have a concrete value
        return ApplyJitter(baseExpiration);
    }

    /// <summary>
    /// Applies a random jitter of ±<c>_ttlJitterPercentage</c> to the supplied expiration.
    /// If <paramref name="expiration"/> is <c>null</c>, <c>null</c> is returned unchanged.
    /// </summary>
    private TimeSpan? ApplyJitter(TimeSpan? expiration)
    {
        if (!expiration.HasValue) return null;

        // Convert to milliseconds for easier calculation
        double baseMs = expiration.Value.TotalMilliseconds;
        double jitterRange = baseMs * _ttlJitterPercentage;
        double min = baseMs - jitterRange;
        double max = baseMs + jitterRange;

        // Ensure we never produce a non‑positive duration
        double jitteredMs = Math.Max(1, Random.Shared.NextDouble() * (max - min) + min);
        return TimeSpan.FromMilliseconds(jitteredMs);
    }
}
