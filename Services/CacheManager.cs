#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedisCachePatterns.Configuration;
using RedisCachePatterns.Domain;
using StackExchange.Redis;

namespace RedisCachePatterns.Services;

/// <summary>
/// Central cache manager that orchestrates cache operations, eviction, and statistics tracking
/// </summary>
public class CacheManager : IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CacheManager> _logger;
    private readonly CacheSerializer _serializer;
    private readonly EvictionPolicy _evictionPolicy;
    private readonly ConcurrentDictionary<string, CacheEntry> _entryTracker = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheManager"/> class
    /// </summary>
    /// <param name="redis">Redis connection multiplexer</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="serializer">Cache serializer for value conversion</param>
    /// <param name="evictionPolicy">Policy controlling cache eviction behavior</param>
    public CacheManager(
        IConnectionMultiplexer redis,
        ILogger<CacheManager> logger,
        CacheSerializer serializer,
        EvictionPolicy evictionPolicy)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _evictionPolicy = evictionPolicy ?? throw new ArgumentNullException(nameof(evictionPolicy));
    }

    /// <summary>
    /// Retrieves a value from the cache, returning default if not found or expired
    /// </summary>
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cached value or default</returns>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(key);

        if (_entryTracker.TryGetValue(key, out var entry))
        {
            if (value.HasValue)
                entry.RecordHit();
            else
                entry.RecordMiss();
        }

        if (!value.HasValue)
        {
            _logger.LogDebug("Cache miss for key {Key}", key);
            return default;
        }

        _logger.LogDebug("Cache hit for key {Key}", key);
        return _serializer.Deserialize<T>(value!);
    }

    /// <summary>
    /// Stores a value in the cache with an optional TTL
    /// </summary>
    /// <typeparam name="T">Type of the value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to store</param>
    /// <param name="ttl">Time to live; null for no expiration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        var serialized = _serializer.Serialize(value);
        var db = _redis.GetDatabase();
        await db.StringSetAsync(key, serialized, ttl);

        var entry = new CacheEntry
        {
            Key = key,
            DataType = typeof(T).Name,
            SizeInBytes = serialized.Length,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : null
        };

        _entryTracker.AddOrUpdate(key, entry, (_, _) => entry);
        _evictionPolicy.RecordAccess(key);

        _logger.LogDebug("Cached key {Key} with TTL {Ttl}", key, ttl);
    }

    /// <summary>
    /// Removes a key from the cache
    /// </summary>
    /// <param name="key">Cache key to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the key was removed</returns>
    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        var db = _redis.GetDatabase();
        var removed = await db.KeyDeleteAsync(key);

        _entryTracker.TryRemove(key, out _);
        _evictionPolicy.Remove(key);

        _logger.LogInformation("Removed cache key {Key}, existed: {Existed}", key, removed);
        return removed;
    }

    /// <summary>
    /// Evicts entries based on the configured eviction policy until the target count is reached
    /// </summary>
    /// <param name="maxEntries">Maximum number of entries to keep</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of evicted entries</returns>
    public async Task<int> EvictAsync(int maxEntries, CancellationToken cancellationToken = default)
    {
        var keysToEvict = _evictionPolicy.SelectForEviction(_entryTracker.Keys, maxEntries);
        var evicted = 0;

        foreach (var key in keysToEvict)
        {
            if (await RemoveAsync(key, cancellationToken))
                evicted++;
        }

        _logger.LogInformation("Evicted {Count} cache entries", evicted);
        return evicted;
    }

    /// <summary>
    /// Returns statistics about the current cache state
    /// </summary>
    /// <returns>Dictionary of metric names to values</returns>
    public Dictionary<string, object> GetStatistics()
    {
        var entries = _entryTracker.Values.ToList();
        return new Dictionary<string, object>
        {
            ["total_entries"] = entries.Count,
            ["total_size_bytes"] = entries.Sum(e => e.SizeInBytes),
            ["average_hit_rate"] = entries.Count > 0 ? entries.Average(e => e.HitRate) : 0,
            ["expired_entries"] = entries.Count(e => e.IsExpired),
            ["active_entries"] = entries.Count(e => !e.IsExpired)
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _entryTracker.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
