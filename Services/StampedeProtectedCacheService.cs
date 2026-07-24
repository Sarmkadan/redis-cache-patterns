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
    private readonly ConcurrentDictionary<string, LockEntry> _keyLocks = new();

    public StampedeProtectedCacheService(ICacheService innerCache, ILogger<StampedeProtectedCacheService> logger)
    {
        _innerCache = innerCache;
        _logger = logger;
    }

    #region Helper – per‑key lock handling

    /// <summary>
    /// Per-key semaphore with a reference count. The count tracks how many callers hold a
    /// reference to the entry (waiting or executing). The entry is only removed from the
    /// dictionary and disposed once the count drops to zero, which prevents the race where
    /// one caller disposes a semaphore that another caller has already fetched but not yet
    /// awaited (previously observable as ObjectDisposedException or two concurrent loads
    /// for the same key).
    /// </summary>
    private sealed class LockEntry
    {
        public readonly SemaphoreSlim Semaphore = new(1, 1);
        public int RefCount;
    }

    private LockEntry AcquireEntry(string key)
    {
        while (true)
        {
            var entry = _keyLocks.GetOrAdd(key, _ => new LockEntry());
            lock (entry)
            {
                // A ref count of -1 marks an entry that lost the race and is being retired;
                // loop and fetch/create a fresh one.
                if (entry.RefCount < 0) continue;
                entry.RefCount++;
                return entry;
            }
        }
    }

    private void ReleaseEntry(string key, LockEntry entry)
    {
        entry.Semaphore.Release();
        lock (entry)
        {
            entry.RefCount--;
            if (entry.RefCount == 0)
            {
                entry.RefCount = -1; // retire: no new callers may join this entry
                _keyLocks.TryRemove(new KeyValuePair<string, LockEntry>(key, entry));
                entry.Semaphore.Dispose();
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

        var entry = AcquireEntry(key);
        await entry.Semaphore.WaitAsync();
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
            ReleaseEntry(key, entry);
        }
    }

    public async Task<T?> GetOrLoadWithSlidingExpirationAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan slidingExpiration)
    {
        var cached = await _innerCache.GetAsync<T>(key);
        if (cached != null) return cached;

        var entry = AcquireEntry(key);
        await entry.Semaphore.WaitAsync();
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
            ReleaseEntry(key, entry);
        }
    }

    public async Task<T?> GetOrLoadWithEarlyExpirationAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan expiration, double beta = 1.0)
    {
        var entry = AcquireEntry(key);
        await entry.Semaphore.WaitAsync();
        try
        {
            // Delegate to the inner cache's implementation which already handles early expiration.
            return await _innerCache.GetOrLoadWithEarlyExpirationAsync(key, loadFn, expiration, beta);
        }
        finally
        {
            ReleaseEntry(key, entry);
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

public async Task<CacheKeyMetadata?> GetKeyMetadataAsync(string key) => await _innerCache.GetKeyMetadataAsync(key);

public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys) =>
    await _innerCache.GetManyAsync<T>(keys);

#endregion
}
