#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using RedisCachePatterns.Utilities;

namespace RedisCachePatterns.Services;

/// <summary>
/// Cache-aside helpers for <see cref="ICacheService"/> that protect cache misses against stampedes.
/// </summary>
/// <remarks>
/// Protection is opt-in per call through <see cref="StampedeProtectionOptions"/> and has two layers:
/// <list type="bullet">
/// <item><description>
/// In-process single-flight: concurrent misses for the same key on the same cache instance share one
/// factory invocation and its result.
/// </description></item>
/// <item><description>
/// Cross-node lock: only the holder of <c>lock:{key}</c> runs the factory. Other callers poll the cache
/// for the value the holder publishes, and load it themselves if none appears within
/// <see cref="StampedeProtectionOptions.LockWaitTimeout"/>, so a stuck holder cannot block reads.
/// </description></item>
/// </list>
/// </remarks>
public static class CacheAsideExtensions
{
    // Per-cache-instance table of in-flight loads, keyed by cache key. Scoped to the instance so that
    // two caches (for example, two Redis deployments) never share a load for the same key string.
    private static readonly ConditionalWeakTable<ICacheService, ConcurrentDictionary<string, Task<object?>>> InFlightByCache = new();

    /// <summary>
    /// Returns the cached value for <paramref name="key"/>, or runs <paramref name="factory"/> on a miss,
    /// stores its non-null result and returns it, with stampede protection as configured by
    /// <paramref name="stampedeProtection"/>.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="cache">The cache to read from and write to.</param>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Loads the value on a miss. Receives a token that is cancelled when
    /// <paramref name="cancellationToken"/> is cancelled or, when a distributed lock is used, when the lock is lost.</param>
    /// <param name="expiration">Optional expiration for the stored value.</param>
    /// <param name="stampedeProtection">Stampede protection settings. Defaults to
    /// <see cref="StampedeProtectionOptions.Default"/> (in-process single-flight).</param>
    /// <param name="cancellationToken">Token that cancels the call.</param>
    /// <returns>The cached or freshly loaded value; <c>null</c> when the factory returns <c>null</c>
    /// (nothing is stored in that case).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cache"/> or <paramref name="factory"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="stampedeProtection"/> has a non-positive
    /// <see cref="StampedeProtectionOptions.LockDuration"/> or <see cref="StampedeProtectionOptions.PollInterval"/>,
    /// or a negative <see cref="StampedeProtectionOptions.LockWaitTimeout"/>.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    public static async Task<T?> GetOrSetAsync<T>(
        this ICacheService cache,
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        StampedeProtectionOptions? stampedeProtection = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(factory);

        var options = stampedeProtection ?? StampedeProtectionOptions.Default;
        options.Validate();

        var cached = await cache.GetAsync<T>(key);
        if (cached is not null)
        {
            return cached;
        }

        if (!options.CoalesceInProcess)
        {
            return await LoadAsync(cache, key, factory, expiration, options, cancellationToken);
        }

        var inFlight = InFlightByCache.GetValue(cache, static _ => new ConcurrentDictionary<string, Task<object?>>(StringComparer.Ordinal));
        return await CoalesceAsync<T>(
            inFlight,
            key,
            token => LoadAsync(cache, key, factory, expiration, options, token),
            cancellationToken);
    }

    /// <summary>
    /// Runs <paramref name="load"/> once per key among concurrent callers. Followers await the leader's
    /// result instead of loading again.
    /// </summary>
    private static async Task<T?> CoalesceAsync<T>(
        ConcurrentDictionary<string, Task<object?>> inFlight,
        string key,
        Func<CancellationToken, Task<T?>> load,
        CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var shared = inFlight.GetOrAdd(key, completion.Task);
        if (!ReferenceEquals(shared, completion.Task))
        {
            try
            {
                return (T?)await shared.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The leader was cancelled by its own caller; that cancellation must not leak into this call.
                return await load(cancellationToken);
            }
        }

        try
        {
            var value = await load(cancellationToken);
            completion.SetResult(value);
            return value;
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                completion.SetCanceled();
            }
            else
            {
                completion.SetException(ex);
            }

            throw;
        }
        finally
        {
            inFlight.TryRemove(new KeyValuePair<string, Task<object?>>(key, completion.Task));
        }
    }

    /// <summary>
    /// Loads the value, either directly or under the distributed lock when it is enabled.
    /// </summary>
    private static async Task<T?> LoadAsync<T>(
        ICacheService cache,
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration,
        StampedeProtectionOptions options,
        CancellationToken cancellationToken)
    {
        if (!options.DistributedLock)
        {
            return await LoadAndStoreAsync(cache, key, factory, expiration, cancellationToken);
        }

        await using var lockHelper = new DistributedLockHelper(
            cache,
            CacheKeyHelper.BuildLockKey(key),
            duration: options.LockDuration);
        var deadline = DateTimeOffset.UtcNow + options.LockWaitTimeout;

        while (true)
        {
            if (await lockHelper.AcquireAsync())
            {
                using var lockScope = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lockHelper.LockLostToken);
                try
                {
                    return await LoadAndStoreAsync(cache, key, factory, expiration, lockScope.Token);
                }
                finally
                {
                    await lockHelper.ReleaseAsync();
                }
            }

            // Another node holds the lock. Its result, if any, appears in the cache; otherwise retry the lock.
            if (DateTimeOffset.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(options.PollInterval, cancellationToken);
            var published = await cache.GetAsync<T>(key);
            if (published is not null)
            {
                return published;
            }
        }

        // The holder did not publish a value within LockWaitTimeout (slow or crashed). Load directly so
        // readers are never blocked indefinitely.
        return await LoadAndStoreAsync(cache, key, factory, expiration, cancellationToken);
    }

    /// <summary>
    /// Re-checks the cache (another node may have populated it while this caller waited), then runs the
    /// factory and stores a non-null result.
    /// </summary>
    private static async Task<T?> LoadAndStoreAsync<T>(
        ICacheService cache,
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration,
        CancellationToken cancellationToken)
    {
        var existing = await cache.GetAsync<T>(key);
        if (existing is not null)
        {
            return existing;
        }

        var loaded = await factory(cancellationToken);
        if (loaded is not null)
        {
            await cache.SetAsync(key, loaded, expiration);
        }

        return loaded;
    }
}
