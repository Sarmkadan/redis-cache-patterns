#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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

    public Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan? expiration = null)
        => _inner.GetOrLoadAsync(key, loadFn, expiration);

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