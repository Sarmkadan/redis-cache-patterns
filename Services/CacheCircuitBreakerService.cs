#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading;
using System.Threading.Tasks;
using RedisCachePatterns.Exceptions;

namespace RedisCachePatterns.Services;

/// <summary>
/// <b>Legacy wrapper for backward compatibility.</b>
///
/// <para>This class is maintained for backward compatibility. The new decorator pattern implementation
/// is <see cref="CircuitBreakerCacheService"/> which implements <see cref="ICacheService"/> directly.</para>
///
/// <para>For new code, use the decorator pattern:
/// <code>new CircuitBreakerCacheService(new CompressedCacheService(new RedisCacheService(...)))</code></para>
///
/// <para>Or use the DI extension method:
/// <code>services.AddCircuitBreakerCache(failureThreshold, breakDuration)</code></para>
/// </summary>
[Obsolete("Use CircuitBreakerCacheService decorator instead. This class is maintained for backward compatibility.")]
public sealed class CacheCircuitBreakerService
{
    private readonly CircuitBreakerCacheService _inner;

    // Flag used to ensure only a single thread performs the probe when the circuit is half‑open.
    // 0 = no probe in progress, 1 = probe in progress.
    private int _probeInProgress = 0;

    public int FailureThreshold => _inner.FailureThreshold;
    public TimeSpan BreakDuration => _inner.BreakDuration;
    public CacheCircuitState State => _inner.State;
    public int ConsecutiveFailures => _inner.ConsecutiveFailures;
    public DateTime? OpenedAtUtc => _inner.OpenedAtUtc;

    public CacheCircuitBreakerService(
        ICacheService inner,
        int failureThreshold = 5,
        TimeSpan? breakDuration = null)
    {
        _inner = new CircuitBreakerCacheService(inner, failureThreshold, breakDuration);
    }

    /// <summary>
    /// Retrieves a cached value or loads it using <paramref name="loadFn"/> when missing.
    /// In the half‑open state only a single thread is allowed to execute <paramref name="loadFn"/>
    /// (the "probe"). Other concurrent callers will wait for the probe to finish and then
    /// attempt to read the value from the cache.
    /// </summary>
    public async Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan? expiration = null)
    {
        // If the circuit is not half‑open, simply delegate to the inner service.
        if (_inner.State != CacheCircuitState.HalfOpen)
        {
            return await _inner.GetOrLoadAsync(key, loadFn, expiration).ConfigureAwait(false);
        }

        // Circuit is half‑open – allow exactly one probe.
        if (Interlocked.CompareExchange(ref _probeInProgress, 1, 0) == 0)
        {
            try
            {
                // This thread is the probe.
                return await _inner.GetOrLoadAsync(key, loadFn, expiration).ConfigureAwait(false);
            }
            finally
            {
                // Reset the flag so other threads can proceed after the probe.
                Interlocked.Exchange(ref _probeInProgress, 0);
            }
        }
        else
        {
            // Another thread is already probing. Wait until the probe finishes,
            // then attempt to read the value from the cache (it may have been populated).
            while (Volatile.Read(ref _probeInProgress) == 1)
            {
                await Task.Yield();
            }

            // After the probe completes, simply try to get the value (no load function).
            return await _inner.GetAsync<T>(key).ConfigureAwait(false);
        }
    }

    public Task<T?> GetAsync<T>(string key)
        => _inner.GetAsync<T>(key);

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        => _inner.SetAsync(key, value, expiration);

    public Task RemoveAsync(string key)
        => _inner.RemoveAsync(key);

    public void RecordSuccess()
        => _inner.RecordSuccess();

    public void RecordFailure()
        => _inner.RecordFailure();

    public void Reset()
        => _inner.Reset();
}

/// <summary>State of the circuit protecting the cache backend.</summary>
public enum CacheCircuitState
{
    Closed = 0,
    Open = 1,
    HalfOpen = 2
}
