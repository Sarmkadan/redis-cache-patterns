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
/// <remarks>
/// The circuit breaker protects the cache backend by tracking consecutive failures and
/// moving through three states:
/// <list type="bullet">
/// <item><description><see cref="CacheCircuitState.Closed"/> — normal operation; all calls are
/// forwarded to the inner cache service while failures are counted.</description></item>
/// <item><description><see cref="CacheCircuitState.Open"/> — after <see cref="FailureThreshold"/>
/// consecutive failures the circuit trips and calls fail fast without touching the backend.</description></item>
/// <item><description><see cref="CacheCircuitState.HalfOpen"/> — after <see cref="BreakDuration"/>
/// has elapsed, a single probe call is allowed through to test whether the backend has recovered.</description></item>
/// </list>
/// </remarks>
[Obsolete("Use CircuitBreakerCacheService decorator instead. This class is maintained for backward compatibility.")]
public sealed class CacheCircuitBreakerService
{
    private readonly CircuitBreakerCacheService _inner;

    // Flag used to ensure only a single thread performs the probe when the circuit is half‑open.
    // 0 = no probe in progress, 1 = probe in progress.
    private int _probeInProgress = 0;

    /// <summary>
    /// Number of consecutive failures required to trip the circuit from
    /// <see cref="CacheCircuitState.Closed"/> to <see cref="CacheCircuitState.Open"/>.
    /// </summary>
    public int FailureThreshold => _inner.FailureThreshold;

    /// <summary>
    /// How long the circuit remains <see cref="CacheCircuitState.Open"/> before it transitions
    /// to <see cref="CacheCircuitState.HalfOpen"/> and allows a recovery probe through.
    /// </summary>
    public TimeSpan BreakDuration => _inner.BreakDuration;

    /// <summary>
    /// Current state of the circuit: <see cref="CacheCircuitState.Closed"/>,
    /// <see cref="CacheCircuitState.Open"/>, or <see cref="CacheCircuitState.HalfOpen"/>.
    /// </summary>
    public CacheCircuitState State => _inner.State;

    /// <summary>
    /// Number of consecutive failures recorded since the last success or reset.
    /// When this reaches <see cref="FailureThreshold"/>, the circuit opens.
    /// </summary>
    public int ConsecutiveFailures => _inner.ConsecutiveFailures;

    /// <summary>
    /// UTC timestamp of when the circuit last transitioned to <see cref="CacheCircuitState.Open"/>,
    /// or <c>null</c> if the circuit is not currently open.
    /// </summary>
    public DateTime? OpenedAtUtc => _inner.OpenedAtUtc;

    /// <summary>
    /// Creates a new circuit breaker wrapper around the given cache service.
    /// </summary>
    /// <param name="inner">The inner cache service whose calls are protected by the circuit.</param>
    /// <param name="failureThreshold">
    /// Number of consecutive failures before the circuit opens. Defaults to 5.
    /// </param>
    /// <param name="breakDuration">
    /// How long the circuit stays open before allowing a half-open probe.
    /// When <c>null</c>, the default of the underlying decorator is used.
    /// </param>
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
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key to look up.</param>
    /// <param name="loadFn">Factory used to load the value from the source on a cache miss.</param>
    /// <param name="expiration">Optional expiration applied when the loaded value is cached.</param>
    /// <returns>The cached or freshly loaded value, or <c>null</c> if unavailable.</returns>
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

    /// <summary>
    /// Retrieves a cached value by key. Subject to the current circuit state:
    /// when the circuit is open the call fails fast instead of hitting the backend.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key to look up.</param>
    /// <returns>The cached value, or <c>null</c> if not present.</returns>
    public Task<T?> GetAsync<T>(string key)
        => _inner.GetAsync<T>(key);

    /// <summary>
    /// Stores a value in the cache. A successful write counts as a success and can
    /// close a half-open circuit; a failure counts toward <see cref="FailureThreshold"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key to write.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="expiration">Optional expiration for the cached entry.</param>
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        => _inner.SetAsync(key, value, expiration);

    /// <summary>
    /// Removes a cached value by key. Subject to the current circuit state.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    public Task RemoveAsync(string key)
        => _inner.RemoveAsync(key);

    /// <summary>
    /// Records a successful backend call, resetting <see cref="ConsecutiveFailures"/>
    /// to zero and closing the circuit if it was half-open.
    /// </summary>
    public void RecordSuccess()
        => _inner.RecordSuccess();

    /// <summary>
    /// Records a failed backend call, incrementing <see cref="ConsecutiveFailures"/>.
    /// The circuit opens once the count reaches <see cref="FailureThreshold"/>.
    /// </summary>
    public void RecordFailure()
        => _inner.RecordFailure();

    /// <summary>
    /// Resets the circuit to <see cref="CacheCircuitState.Closed"/> and clears the
    /// consecutive failure count.
    /// </summary>
    public void Reset()
        => _inner.Reset();
}

/// <summary>State of the circuit protecting the cache backend.</summary>
public enum CacheCircuitState
{
    /// <summary>
    /// Normal operation. Calls are forwarded to the cache backend and failures are counted.
    /// The circuit trips to <see cref="Open"/> once the configured failure threshold is reached.
    /// </summary>
    Closed = 0,

    /// <summary>
    /// The circuit is tripped. Calls fail fast without touching the cache backend.
    /// After the configured break duration elapses, the circuit moves to <see cref="HalfOpen"/>.
    /// </summary>
    Open = 1,

    /// <summary>
    /// Recovery probe state. A single call is allowed through to test the backend:
    /// on success the circuit returns to <see cref="Closed"/>; on failure it re-opens.
    /// </summary>
    HalfOpen = 2
}
