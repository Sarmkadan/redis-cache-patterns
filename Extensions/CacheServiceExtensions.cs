#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using RedisCachePatterns.Services;

namespace RedisCachePatterns.Extensions;

/// <summary>
/// Extension methods for ICacheService common patterns
/// </summary>
public static class CacheServiceExtensions
{
    /// <summary>
    /// Cache-Aside pattern with automatic fallback
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="key">The cache key. Must not be null or whitespace.</param>
    /// <param name="fetchFn">Factory delegate invoked on cache miss to load the value from the backing store.</param>
    /// <param name="expiration">Optional TTL for the cache entry.</param>
    /// <param name="forceRefresh">Whether to force a cache refresh by removing the existing entry first.</param>
    /// <returns>The cached or freshly loaded value, or <c>default</c> if not found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cache"/> or <paramref name="key"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="fetchFn"/> is <c>null</c>.</exception>
    public static async Task<T?> GetOrFetchAsync<T>(
        this ICacheService cache,
        string key,
        Func<Task<T>> fetchFn,
        TimeSpan? expiration = null,
        bool forceRefresh = false)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(fetchFn);

        if (forceRefresh)
            await cache.RemoveAsync(key);

        return await cache.GetOrLoadAsync(key, fetchFn, expiration);
    }

    /// <summary>
    /// Set and invalidate related cache keys
    /// </summary>
    /// <typeparam name="T">The type of the value being cached.</typeparam>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="key">The cache key to set.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="invalidatePatterns">Collection of patterns to invalidate after setting the value.</param>
    /// <param name="expiration">Optional TTL for the cache entry.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cache"/> or <paramref name="key"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="invalidatePatterns"/> is <c>null</c>.</exception>
    public static async Task SetWithInvalidationAsync<T>(
        this ICacheService cache,
        string key,
        T value,
        IEnumerable<string> invalidatePatterns,
        TimeSpan? expiration = null)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(invalidatePatterns);

        await cache.SetAsync(key, value, expiration);

        foreach (var pattern in invalidatePatterns)
        {
            await cache.RemoveByPatternAsync(pattern);
        }
    }

    /// <summary>
    /// Execute with distributed lock
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the action.</typeparam>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="lockKey">The Redis key used as the lock identifier.</param>
    /// <param name="action">The action to execute under the lock.</param>
    /// <param name="instanceId">A unique identifier for this lock instance.</param>
    /// <param name="lockDuration">Optional duration for which the lock should be held.</param>
    /// <returns>The result of the action.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cache"/> or <paramref name="lockKey"/> or <paramref name="action"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="lockKey"/> is whitespace.</exception>
    /// <exception cref="InvalidOperationException">Failed to acquire the lock.</exception>
    public static async Task<TResult> ExecuteWithLockAsync<TResult>(
        this ICacheService cache,
        string lockKey,
        Func<Task<TResult>> action,
        string instanceId,
        TimeSpan? lockDuration = null)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentException.ThrowIfNullOrWhiteSpace(lockKey);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        lockDuration ??= TimeSpan.FromSeconds(10);
        var lockValue = Guid.NewGuid().ToString();

        var lockAcquired = await cache.AcquireLockAsync(lockKey, lockValue, lockDuration.Value);
        if (!lockAcquired)
            throw new InvalidOperationException($"Failed to acquire lock for key: {lockKey}");

        try
        {
            return await action();
        }
        finally
        {
            await cache.ReleaseLockAsync(lockKey, lockValue);
        }
    }

    /// <summary>
    /// Batch set multiple cache entries
    /// </summary>
    /// <typeparam name="T">The type of the values being cached.</typeparam>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="entries">Collection of key-value pairs to set.</param>
    /// <param name="expiration">Optional TTL for the cache entries.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cache"/> or <paramref name="entries"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="entries"/> contains null keys.</exception>
    public static async Task SetBatchAsync<T>(
        this ICacheService cache,
        Dictionary<string, T> entries,
        TimeSpan? expiration = null)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(entries);

        var tasks = entries.Select(kvp => cache.SetAsync(kvp.Key, kvp.Value, expiration));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Batch get multiple cache entries
    /// </summary>
    /// <typeparam name="T">The type of the cached values.</typeparam>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="keys">Collection of keys to retrieve.</param>
    /// <returns>A dictionary mapping requested keys to their values.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cache"/> or <paramref name="keys"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="keys"/> contains null or whitespace keys.</exception>
    public static async Task<Dictionary<string, T?>> GetBatchAsync<T>(
        this ICacheService cache,
        IEnumerable<string> keys)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(keys);

        var result = new Dictionary<string, T?>();

        foreach (var key in keys)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            var value = await cache.GetAsync<T>(key);
            result[key] = value;
        }

        return result;
    }

    /// <summary>
    /// Cache with exponential backoff on errors
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="key">The cache key to retrieve.</param>
    /// <param name="loadFn">Factory delegate to load the value if not in cache.</param>
    /// <param name="expiration">Optional TTL for the cache entry.</param>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <returns>The cached or loaded value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cache"/> or <paramref name="key"/> or <paramref name="loadFn"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is whitespace.</exception>
    /// <exception cref="InvalidOperationException">All retry attempts exhausted without success.</exception>
    public static async Task<T?> GetWithRetryAsync<T>(
        this ICacheService cache,
        string key,
        Func<Task<T>> loadFn,
        TimeSpan? expiration = null,
        int maxRetries = 3)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(loadFn);

        ArgumentOutOfRangeException.ThrowIfLessThan(maxRetries, 0);

        int attempt = 0;
        Exception? lastException = null;

        while (attempt < maxRetries)
        {
            try
            {
                return await cache.GetOrLoadAsync(key, loadFn, expiration);
            }
            catch (Exception ex) when (attempt < maxRetries - 1)
            {
                lastException = ex;
                attempt++;
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100));
            }
        }

        throw new InvalidOperationException(
            $"All {maxRetries} retry attempts exhausted for key: {key}",
            lastException);
    }

    /// <summary>
    /// Warm cache by pre-loading data
    /// </summary>
    /// <typeparam name="T">The type of the values being cached.</typeparam>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="entries">Collection of key-value pairs to preload.</param>
    /// <param name="expiration">Optional TTL for the cache entries.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cache"/> or <paramref name="entries"/> is <c>null</c>.</exception>
    public static async Task WarmCacheAsync<T>(
        this ICacheService cache,
        IEnumerable<(string Key, T Value)> entries,
        TimeSpan? expiration = null)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(entries);

        var tasks = entries.Select(entry => cache.SetAsync(entry.Key, entry.Value, expiration));
        await Task.WhenAll(tasks);
    }
}