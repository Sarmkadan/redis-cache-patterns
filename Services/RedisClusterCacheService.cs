// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text.Json;
using StackExchange.Redis;
using RedisCachePatterns.Configuration;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Exceptions;
using RedisCachePatterns.Infrastructure.Cache;
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
    private readonly IRedisClusterConnection _cluster;
    private readonly ClusterConfiguration _clusterConfig;
    private readonly ILogger<RedisClusterCacheService> _logger;

    // Policy store — same lock-free frozen-snapshot pattern as RedisCacheService.
    private readonly Dictionary<string, CachePolicy> _policiesMutable = new();
    private volatile FrozenDictionary<string, CachePolicy> _policies =
        FrozenDictionary<string, CachePolicy>.Empty;
    private readonly Lock _policyLock = new();

    /// <summary>
    /// Initialises the service with the cluster connection and configuration.
    /// </summary>
    public RedisClusterCacheService(
        IRedisClusterConnection cluster,
        ClusterConfiguration clusterConfig,
        ILogger<RedisClusterCacheService> logger)
    {
        _cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));
        _clusterConfig = clusterConfig ?? throw new ArgumentNullException(nameof(clusterConfig));
        _logger = logger;
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
            }

            _logger.LogDebug("Cluster cache miss: {Key} — loading from source", key);
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
    public async Task<T?> GetAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        try
        {
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

        try
        {
            var persisted = await persistFn();
            var db = _cluster.GetDatabase();
            var json = JsonSerializer.Serialize(persisted);
            await db.StringSetAsync(key, json, GetEffectiveExpiration(key, expiration));
            _logger.LogDebug("Cluster write-through completed for key: {Key}", key);
            return persisted;
        }
        catch (Exception ex) when (ex is not CacheException)
        {
            _logger.LogError(ex, "WriteAsync failed for key: {Key}", key);
            throw new CacheException("Cluster write-through operation failed", ex);
        }
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
            var current = await db.StringGetAsync(lockKey);
            if (!current.HasValue || current != lockValue) return false;

            await db.KeyDeleteAsync(lockKey);
            _logger.LogInformation("Lock released: {LockKey}", lockKey);
            return true;
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
            var current = await db.StringGetAsync(lockKey);
            if (!current.HasValue || current != lockValue) return false;

            await db.StringSetAsync(lockKey, lockValue, newDuration);
            _logger.LogInformation("Lock renewed: {LockKey} for {Duration}", lockKey, newDuration);
            return true;
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

    // ── Private helpers ───────────────────────────────────────────────────────

    // Hot path — reads the frozen snapshot; lock-free.
    private TimeSpan? GetEffectiveExpiration(string key, TimeSpan? expiration)
    {
        if (expiration.HasValue) return expiration;
        _policies.TryGetValue(key, out var policy);
        return policy?.DefaultExpiration;
    }
}
