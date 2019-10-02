#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Domain;

namespace RedisCachePatterns.Services;

/// <summary>
/// Core caching service interface implementing cache-aside, write-through, and distributed lock patterns.
/// All methods are designed for concurrent access and handle Redis connection failures gracefully.
/// </summary>
public interface ICacheService
{
    // -------------------------------------------------------------------------
    // Cache-Aside Pattern
    // -------------------------------------------------------------------------

    /// <summary>
    /// Cache-aside pattern: returns the cached value for <paramref name="key"/> if present;
    /// otherwise invokes <paramref name="loadFn"/> to load the value from the source of truth,
    /// stores it in cache, and returns it.
    /// </summary>
    /// <typeparam name="T">The type of the cached value. Must be JSON-serializable.</typeparam>
    /// <param name="key">The cache key. Must not be null or whitespace.</param>
    /// <param name="loadFn">A factory delegate invoked on cache miss to load the value from the backing store.</param>
    /// <param name="expiration">
    /// Optional TTL for the cache entry. When <c>null</c>, the key-specific <see cref="CachePolicy"/>
    /// is used if configured; otherwise the entry does not expire.
    /// </param>
    /// <returns>The cached or freshly loaded value, or <c>default</c> if <paramref name="loadFn"/> returns null.</returns>
    Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan? expiration = null);

    /// <summary>
    /// Retrieves a cached value by key without triggering a load on miss.
    /// </summary>
    /// <typeparam name="T">The expected type of the cached value.</typeparam>
    /// <param name="key">The cache key to look up.</param>
    /// <returns>The deserialized value if found; otherwise <c>default</c>.</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Stores a value in cache, overwriting any existing entry for the given key.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key. Must not be null or whitespace.</param>
    /// <param name="value">The value to store. Must not be null.</param>
    /// <param name="expiration">Optional TTL. Falls back to the key's <see cref="CachePolicy"/> when null.</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    // -------------------------------------------------------------------------
    // Write-Through Pattern
    // -------------------------------------------------------------------------

    /// <summary>
    /// Write-through pattern: persists the value via <paramref name="persistFn"/> first,
    /// then updates the cache with the persisted result. Ensures cache consistency with
    /// the backing store by writing to the database before the cache.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to persist and cache.</param>
    /// <param name="persistFn">A delegate that writes the value to the backing store and returns the persisted entity.</param>
    /// <param name="expiration">Optional TTL for the cache entry.</param>
    /// <returns>The value as returned by <paramref name="persistFn"/> after successful persistence.</returns>
    Task<T> WriteAsync<T>(string key, T value, Func<Task<T>> persistFn, TimeSpan? expiration = null);

    // -------------------------------------------------------------------------
    // General Operations
    // -------------------------------------------------------------------------

    /// <summary>
    /// Removes a single cache entry by its exact key.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    Task RemoveAsync(string key);

    /// <summary>
    /// Removes all cache entries whose keys match the given glob-style pattern (e.g., "user:*").
    /// Uses SCAN internally to avoid blocking the Redis server on large keyspaces.
    /// </summary>
    /// <param name="pattern">A Redis glob pattern (supports *, ?, and []).</param>
    Task RemoveByPatternAsync(string pattern);

    /// <summary>
    /// Checks whether a cache entry exists for the given key without retrieving its value.
    /// </summary>
    /// <param name="key">The cache key to check.</param>
    /// <returns><c>true</c> if the key exists in cache; otherwise <c>false</c>.</returns>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Returns the remaining time-to-live for a cache key.
    /// </summary>
    /// <param name="key">The cache key to inspect.</param>
    /// <returns>The remaining TTL, or <c>null</c> if the key has no expiration or does not exist.</returns>
    Task<TimeSpan?> GetExpirationAsync(string key);

    /// <summary>
    /// Returns all cache keys matching the given glob-style pattern.
    /// </summary>
    /// <param name="pattern">A Redis glob pattern.</param>
    /// <returns>An enumerable of matching key strings.</returns>
    Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern);

    // -------------------------------------------------------------------------
    // Distributed Locks
    // -------------------------------------------------------------------------

    /// <summary>
    /// Acquires a distributed lock using Redis SET NX with automatic expiration.
    /// The lock is only granted if no other client holds it.
    /// </summary>
    /// <param name="lockKey">The Redis key used as the lock identifier.</param>
    /// <param name="lockValue">A unique value identifying this lock holder (typically a GUID).</param>
    /// <param name="duration">The maximum time the lock is held before automatic release.</param>
    /// <returns><c>true</c> if the lock was acquired; <c>false</c> if it is already held.</returns>
    Task<bool> AcquireLockAsync(string lockKey, string lockValue, TimeSpan duration);

    /// <summary>
    /// Releases a distributed lock atomically. The lock is only released if the stored value
    /// matches <paramref name="lockValue"/>, preventing accidental release of another client's lock.
    /// </summary>
    /// <param name="lockKey">The Redis key used as the lock identifier.</param>
    /// <param name="lockValue">The unique value that was used when acquiring the lock.</param>
    /// <returns><c>true</c> if the lock was released; <c>false</c> if the value did not match or the lock expired.</returns>
    Task<bool> ReleaseLockAsync(string lockKey, string lockValue);

    /// <summary>
    /// Extends the TTL of a distributed lock atomically. Only succeeds if the lock is still
    /// held by the caller (value matches).
    /// </summary>
    /// <param name="lockKey">The Redis key used as the lock identifier.</param>
    /// <param name="lockValue">The unique value that was used when acquiring the lock.</param>
    /// <param name="newDuration">The new TTL to set on the lock.</param>
    /// <returns><c>true</c> if the lock was renewed; <c>false</c> if the value did not match or the lock expired.</returns>
    Task<bool> RenewLockAsync(string lockKey, string lockValue, TimeSpan newDuration);

    // -------------------------------------------------------------------------
    // Cache Management
    // -------------------------------------------------------------------------

    /// <summary>
    /// Removes all entries from the cache database. Use with caution in production environments.
    /// </summary>
    Task FlushAsync();

    /// <summary>
    /// Retrieves cache statistics including total key count, memory usage, and hit/miss rates.
    /// </summary>
    /// <returns>A <see cref="CacheStatistics"/> snapshot captured at the current moment.</returns>
    Task<CacheStatistics> GetStatisticsAsync();

    /// <summary>
    /// Registers or updates a cache policy for a specific key pattern. Policies define
    /// default TTL values that apply when no explicit expiration is provided.
    /// </summary>
    /// <param name="policy">The policy to register, keyed by <see cref="CachePolicy.Key"/>.</param>
    /// <remarks>
    /// Uses <see cref="ValueTask"/> because no I/O is involved - policy storage is purely in-memory.
    /// </remarks>
    ValueTask SetPolicyAsync(CachePolicy policy);

    /// <summary>
    /// Retrieves the cache policy configured for a specific key.
    /// </summary>
    /// <param name="key">The policy key to look up.</param>
    /// <returns>The matching <see cref="CachePolicy"/>, or <c>null</c> if none is configured.</returns>
    ValueTask<CachePolicy?> GetPolicyAsync(string key);
}

/// <summary>
/// Point-in-time snapshot of cache performance metrics.
/// </summary>
public class CacheStatistics
{
    /// <summary>Total number of keys currently in the cache.</summary>
    public int TotalKeys { get; set; }

    /// <summary>Redis memory consumption in bytes.</summary>
    public long MemoryUsedBytes { get; set; }

    /// <summary>Number of cache hits since the last server restart.</summary>
    public int Hits { get; set; }

    /// <summary>Number of cache misses since the last server restart.</summary>
    public int Misses { get; set; }

    /// <summary>Cache hit rate as a percentage (0-100). Returns 0 when no operations have occurred.</summary>
    public double HitRate => (Hits + Misses) > 0 ? (double)Hits / (Hits + Misses) * 100 : 0;

    /// <summary>UTC timestamp when these statistics were captured.</summary>
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
}
