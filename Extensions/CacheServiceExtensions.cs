#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    public static async Task<T?> GetOrFetchAsync<T>(
        this ICacheService cache,
        string key,
        Func<Task<T>> fetchFn,
        TimeSpan? expiration = null,
        bool forceRefresh = false)
    {
        if (forceRefresh)
            await cache.RemoveAsync(key).ConfigureAwait(false);

        return await cache.GetOrLoadAsync(key, fetchFn, expiration).ConfigureAwait(false);
    }

    /// <summary>
    /// Set and invalidate related cache keys
    /// </summary>
    public static async Task SetWithInvalidationAsync<T>(
        this ICacheService cache,
        string key,
        T value,
        IEnumerable<string> invalidatePatterns,
        TimeSpan? expiration = null)
    {
        await cache.SetAsync(key, value, expiration).ConfigureAwait(false);

        foreach (var pattern in invalidatePatterns)
        {
            await cache.RemoveByPatternAsync(pattern).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Execute with distributed lock
    /// </summary>
    public static async Task<TResult> ExecuteWithLockAsync<TResult>(
        this ICacheService cache,
        string lockKey,
        Func<Task<TResult>> action,
        string instanceId,
        TimeSpan? lockDuration = null)
    {
        lockDuration ??= TimeSpan.FromSeconds(10);
        var lockValue = Guid.NewGuid().ToString();

        var lockAcquired = await cache.AcquireLockAsync(lockKey, lockValue, lockDuration.Value).ConfigureAwait(false);
        if (!lockAcquired)
            throw new InvalidOperationException($"Failed to acquire lock for key: {lockKey}");

        try
        {
            return await action().ConfigureAwait(false);
        }
        finally
        {
            await cache.ReleaseLockAsync(lockKey, lockValue).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Batch set multiple cache entries
    /// </summary>
    public static async Task SetBatchAsync<T>(
        this ICacheService cache,
        Dictionary<string, T> entries,
        TimeSpan? expiration = null)
    {
        var tasks = entries.Select(kvp => cache.SetAsync(kvp.Key, kvp.Value, expiration));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Batch get multiple cache entries
    /// </summary>
    public static async Task<Dictionary<string, T?>> GetBatchAsync<T>(
        this ICacheService cache,
        IEnumerable<string> keys)
    {
        var result = new Dictionary<string, T?>();

        foreach (var key in keys)
        {
            var value = await cache.GetAsync<T>(key).ConfigureAwait(false);
            result[key] = value;
        }

        return result;
    }

    /// <summary>
    /// Cache with exponential backoff on errors
    /// </summary>
    public static async Task<T?> GetWithRetryAsync<T>(
        this ICacheService cache,
        string key,
        Func<Task<T>> loadFn,
        TimeSpan? expiration = null,
        int maxRetries = 3)
    {
        int attempt = 0;
        while (attempt < maxRetries)
        {
            try
            {
                return await cache.GetOrLoadAsync(key, loadFn, expiration).ConfigureAwait(false);
            }
            catch (Exception) when (attempt < maxRetries - 1)
            {
                attempt++;
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100)).ConfigureAwait(false);
            }
        }

        return await cache.GetOrLoadAsync(key, loadFn, expiration).ConfigureAwait(false);
    }

    /// <summary>
    /// Warm cache by pre-loading data
    /// </summary>
    public static async Task WarmCacheAsync<T>(
        this ICacheService cache,
        IEnumerable<(string Key, T Value)> entries,
        TimeSpan? expiration = null)
    {
        var tasks = entries.Select(entry => cache.SetAsync(entry.Key, entry.Value, expiration));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
