#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Domain;

namespace RedisCachePatterns.Services;

/// <summary>
/// Decorator for <see cref="ICacheService"/> that protects against cache stampede
/// by ensuring that only one caller populates a missing cache entry while other
/// callers wait for the result. It uses a per‑key <see cref="SemaphoreSlim"/>
/// stored in a <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </summary>
public sealed class StampedeProtectedCacheService : ICacheService
{
    private readonly ICacheService _innerCache;
    private readonly ILogger<StampedeProtectedCacheService> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();

    public StampedeProtectedCacheService(ICacheService innerCache, ILogger<StampedeProtectedCacheService> logger)
    {
        _innerCache = innerCache;
        _logger = logger;
    }

    #region Helper – per‑key lock handling

    private SemaphoreSlim GetLock(string key) =>
        _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

    private void ReleaseLock(string key, SemaphoreSlim semaphore)
    {
        semaphore.Release();

        // If nobody is waiting, clean up the semaphore to avoid unbounded growth.
        if (semaphore.CurrentCount == 1)
        {
            if (_keyLocks.TryRemove(key, out var removed) && removed == semaphore)
            {
                semaphore.Dispose();
            }
        }
    }

    #endregion

    #region ICacheService implementation – protected GetOrLoad methods

    public async Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan? expiration = null)
    {
        // Fast path – try to get the cached value first.
        var cached = await _innerCache.GetAsync<T>(key);
        if (cached != null) return cached;

        var semaphore = GetLock(key);
        await semaphore.WaitAsync();
        try
        {
            // Double‑check after acquiring the lock.
            var cachedAgain = await _innerCache.GetAsync<T>(key);
            if (cachedAgain != null) return cachedAgain;

            var loaded = await loadFn();
            if (loaded != null)
                await _innerCache.SetAsync(key, loaded, expiration);

            return loaded;
        }
        finally
        {
            ReleaseLock(key, semaphore);
        }
    }

    public async Task<T?> GetOrLoadWithSlidingExpirationAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan slidingExpiration)
    {
        var cached = await _innerCache.GetAsync<T>(key);
        if (cached != null) return cached;

        var semaphore = GetLock(key);
        await semaphore.WaitAsync();
        try
        {
            var cachedAgain = await _innerCache.GetAsync<T>(key);
            if (cachedAgain != null) return cachedAgain;

            var loaded = await loadFn();
            if (loaded != null)
                await _innerCache.SetAsync(key, loaded, slidingExpiration);

            return loaded;
        }
        finally
        {
            ReleaseLock(key, semaphore);
        }
    }

    public async Task<T?> GetOrLoadWithEarlyExpirationAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan expiration, double beta = 1.0)
    {
        var semaphore = GetLock(key);
        await semaphore.WaitAsync();
        try
        {
            // Delegate to the inner cache's implementation which already handles early expiration.
            return await _innerCache.GetOrLoadWithEarlyExpirationAsync(key, loadFn, expiration, beta);
        }
        finally
        {
            ReleaseLock(key, semaphore);
        }
    }

    #endregion

    #region ICacheService – pass‑through members

    public async Task<T?> GetAsync<T>(string key) => await _innerCache.GetAsync<T>(key);

public async Task<T?> GetWithSlidingExpirationAsync<T>(string key, TimeSpan slidingExpiration)
{
    // Fast path – try to get the cached value first.
    var cached = await _innerCache.GetWithSlidingExpirationAsync<T>(key, slidingExpiration);
    if (cached != null) return cached;
    return default;
}

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) =>
        await _innerCache.SetAsync(key, value, expiration);

    public async Task<T> WriteAsync<T>(string key, T value, Func<Task<T>> persistFn, TimeSpan? expiration = null) =>
        await _innerCache.WriteAsync(key, value, persistFn, expiration);

    public async Task RemoveAsync(string key) => await _innerCache.RemoveAsync(key);

    public async Task RemoveByPatternAsync(string pattern) => await _innerCache.RemoveByPatternAsync(pattern);

    public async Task<bool> ExistsAsync(string key) => await _innerCache.ExistsAsync(key);

    public async Task<TimeSpan?> GetExpirationAsync(string key) => await _innerCache.GetExpirationAsync(key);

    public async Task<bool> AcquireLockAsync(string lockKey, string lockValue, TimeSpan duration) =>
        await _innerCache.AcquireLockAsync(lockKey, lockValue, duration);

    public async Task<bool> ReleaseLockAsync(string lockKey, string lockValue) =>
        await _innerCache.ReleaseLockAsync(lockKey, lockValue);

    public async Task<bool> RenewLockAsync(string lockKey, string lockValue, TimeSpan newDuration) =>
        await _innerCache.RenewLockAsync(lockKey, lockValue, newDuration);

    public async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern) =>
        await _innerCache.GetKeysByPatternAsync(pattern);

    public async Task FlushAsync() => await _innerCache.FlushAsync();

    public async Task<CacheStatistics> GetStatisticsAsync() => await _innerCache.GetStatisticsAsync();

    public ValueTask SetPolicyAsync(CachePolicy policy) => _innerCache.SetPolicyAsync(policy);

    public ValueTask<CachePolicy?> GetPolicyAsync(string key) => _innerCache.GetPolicyAsync(key);

    public async Task<CacheKeyMetadata?> GetKeyMetadataAsync(string key) =>
        await _innerCache.GetKeyMetadataAsync(key);

    #endregion
}
