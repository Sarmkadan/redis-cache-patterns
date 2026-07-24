using RedisCachePatterns.Exceptions;

namespace RedisCachePatterns.Services;

/// <summary>State of the circuit protecting the cache backend.</summary>
public enum CacheCircuitState
{
    Closed = 0,
    Open = 1,
    HalfOpen = 2
}

/// <summary>
/// Circuit-breaker decorator over ICacheService implementing fail-open semantics for reads.
///
/// <para><b>Fail-Open Behavior:</b></para>
/// <list type="bullet">
/// <item><description><see cref="GetAsync"/> returns <c>default(T)</c> when circuit is open (fail-open, never throws)</description></item>
/// <item><description><see cref="GetOrLoadAsync"/> bypasses cache and invokes <paramref name="loadFn"/> directly when circuit is open (fail-open)</description></item>
/// <item><description><see cref="SetAsync"/>, <see cref="RemoveAsync"/>, and other write operations are no-ops when circuit is open (fail-open)</description></item>
/// </list>
///
/// <para><b>Circuit States:</b></para>
/// <list type="bullet">
/// <item><description><see cref="CacheCircuitState.Closed"/>: Normal operation, failures tracked</description></item>
/// <item><description><see cref="CacheCircuitState.Open"/>: Circuit open for <see cref="BreakDuration"/>, cache unavailable</description></item>
/// <item><description><see cref="CacheCircuitState.HalfOpen"/>: Single probe call allowed (bounded trial), circuit may close or re-open</description></item>
/// </list>
///
/// <para><b>Failure Handling:</b></para>
/// <list type="bullet">
/// <item><description>Consecutive failures counted while Closed or HalfOpen</description></item>
/// <item><description>At <see cref="FailureThreshold"/>, circuit opens for <see cref="BreakDuration"/></description></item>
/// <item><description>After <see cref="BreakDuration"/>, circuit enters HalfOpen state for one bounded trial call</description></item>
/// <item><description>Success in HalfOpen closes circuit; failure re-opens it</description></item>
/// </list>
/// </summary>
public sealed class CacheCircuitBreakerService
{
    private readonly ICacheService _inner;
    private readonly object _sync = new();
    private readonly TimeSpan _breakDuration;

    public int FailureThreshold { get; }
    public TimeSpan BreakDuration => _breakDuration;

    /// <summary>Current circuit state (evaluates cooldown expiry lazily).</summary>
    public CacheCircuitState State { get; private set; }

    /// <summary>Consecutive failures observed while Closed/HalfOpen.</summary>
    public int ConsecutiveFailures { get; private set; }

    /// <summary>UTC time the circuit last opened, if any.</summary>
    public DateTime? OpenedAtUtc { get; private set; }

    public CacheCircuitBreakerService(ICacheService inner, int failureThreshold = 5, TimeSpan? breakDuration = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        FailureThreshold = failureThreshold > 0 ? failureThreshold : throw new ArgumentOutOfRangeException(nameof(failureThreshold), "FailureThreshold must be positive");
        _breakDuration = breakDuration ?? TimeSpan.FromSeconds(30);
        State = CacheCircuitState.Closed;
    }

    /// <summary>
    /// Cache-aside pattern with circuit breaker protection.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key. Must not be null.</param>
    /// <param name="loadFn">Factory delegate invoked on cache miss to load the value from the backing store.</param>
    /// <param name="expiration">Optional TTL for the cache entry.</param>
    /// <returns>The cached or freshly loaded value, or <c>default(T)</c> if <paramref name="loadFn"/> returns null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="loadFn"/> is null.</exception>
    /// <remarks>
    /// <para><b>Fail-Open Semantics:</b></para>
    /// <list type="bullet">
    /// <item><description>When circuit is <see cref="CacheCircuitState.Open"/>, bypasses cache and invokes <paramref name="loadFn"/> directly</description></item>
    /// <item><description>Never throws due to circuit breaker state; only propagates <see cref="CacheException"/> from <paramref name="loadFn"/></description></item>
    /// </list>
    /// </remarks>
    public async Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan? expiration = null)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (loadFn == null) throw new ArgumentNullException(nameof(loadFn));

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            return await loadFn().ConfigureAwait(false);
        }

        try
        {
            var result = await _inner.GetOrLoadAsync(key, loadFn, expiration).ConfigureAwait(false);
            RecordSuccess();
            return result;
        }
        catch (CacheException)
        {
            RecordFailure();
            throw;
        }
    }

    /// <summary>
    /// Get through the breaker; returns default(T) when Open.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key. Must not be null.</param>
    /// <returns>The deserialized value if found; otherwise <c>default(T)</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
    /// <remarks>
    /// <para><b>Fail-Open Semantics:</b></para>
    /// <list type="bullet">
    /// <item><description>When circuit is <see cref="CacheCircuitState.Open"/>, returns <c>default(T)</c> without throwing</description></item>
    /// <item><description>Never throws due to circuit breaker state; only propagates <see cref="CacheException"/> from underlying cache</description></item>
    /// </list>
    /// </remarks>
    public async Task<T?> GetAsync<T>(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            return default;
        }

        try
        {
            var result = await _inner.GetAsync<T>(key).ConfigureAwait(false);
            RecordSuccess();
            return result;
        }
        catch (CacheException)
        {
            RecordFailure();
            throw;
        }
    }

    /// <summary>
    /// Set through the breaker; no-op when Open.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key. Must not be null.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="expiration">Optional TTL for the cache entry.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
    /// <remarks>
    /// <para><b>Fail-Open Semantics:</b></para>
    /// <list type="bullet">
    /// <item><description>When circuit is <see cref="CacheCircuitState.Open"/>, silently skips the write operation</description></item>
    /// <item><description>Never throws due to circuit breaker state; only propagates <see cref="CacheException"/> from underlying cache</description></item>
    /// </list>
    /// </remarks>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            return;
        }

        try
        {
            await _inner.SetAsync(key, value, expiration).ConfigureAwait(false);
            RecordSuccess();
        }
        catch (CacheException)
        {
            RecordFailure();
            throw;
        }
    }

    /// <summary>
    /// Remove through the breaker; no-op when Open.
    /// </summary>
    /// <param name="key">The cache key. Must not be null.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
    /// <remarks>
    /// <para><b>Fail-Open Semantics:</b></para>
    /// <list type="bullet">
    /// <item><description>When circuit is <see cref="CacheCircuitState.Open"/>, silently skips the remove operation</description></item>
    /// <item><description>Never throws due to circuit breaker state; only propagates <see cref="CacheException"/> from underlying cache</description></item>
    /// </list>
    /// </remarks>
    public async Task RemoveAsync(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            return;
        }

        try
        {
            await _inner.RemoveAsync(key).ConfigureAwait(false);
            RecordSuccess();
        }
        catch (CacheException)
        {
            RecordFailure();
            throw;
        }
    }

    /// <summary>
    /// Record a successful cache call: resets failures, closes circuit if HalfOpen.
    /// </summary>
    /// <remarks>
    /// <para><b>State Transitions:</b></para>
    /// <list type="bullet">
    /// <item><description><see cref="CacheCircuitState.HalfOpen"/> → <see cref="CacheCircuitState.Closed"/> on success</description></item>
    /// <item><description><see cref="CacheCircuitState.Closed"/> → resets failure counter</description></item>
    /// </list>
    /// </remarks>
    public void RecordSuccess()
    {
        lock (_sync)
        {
            switch (State)
            {
                case CacheCircuitState.HalfOpen:
                    State = CacheCircuitState.Closed;
                    ConsecutiveFailures = 0;
                    OpenedAtUtc = null;
                    break;
                case CacheCircuitState.Closed:
                    ConsecutiveFailures = 0;
                    break;
            }
        }
    }

    /// <summary>
    /// Record a cache failure: increments counter, opens circuit at threshold or if HalfOpen.
    /// </summary>
    /// <remarks>
    /// <para><b>State Transitions:</b></para>
    /// <list type="bullet">
    /// <item><description><see cref="CacheCircuitState.Closed"/> + threshold reached → <see cref="CacheCircuitState.Open"/></description></item>
    /// <item><description><see cref="CacheCircuitState.HalfOpen"/> → <see cref="CacheCircuitState.Open"/> (immediate re-open)</description></item>
    /// </list>
    /// </remarks>
    public void RecordFailure()
    {
        lock (_sync)
        {
            switch (State)
            {
                case CacheCircuitState.Closed:
                    ConsecutiveFailures++;
                    if (ConsecutiveFailures >= FailureThreshold)
                    {
                        State = CacheCircuitState.Open;
                        OpenedAtUtc = DateTime.UtcNow;
                    }
                    break;
                case CacheCircuitState.HalfOpen:
                    State = CacheCircuitState.Open;
                    OpenedAtUtc = DateTime.UtcNow;
                    break;
            }
        }
    }

    /// <summary>
    /// Manually reset the breaker to Closed with zero failures.
    /// </summary>
    /// <remarks>
    /// <para><b>Use Case:</b></para>
    /// <list type="bullet">
    /// <item><description>Recover from known transient failures without waiting for <see cref="BreakDuration"/></description></item>
    /// <item><description>Reset after external system recovery</description></item>
    /// </list>
    /// </remarks>
    public void Reset()
    {
        lock (_sync)
        {
            State = CacheCircuitState.Closed;
            ConsecutiveFailures = 0;
            OpenedAtUtc = null;
        }
    }

    private CacheCircuitState EvaluateState()
    {
        lock (_sync)
        {
            if (State == CacheCircuitState.Open)
            {
                var now = DateTime.UtcNow;
                var elapsed = now - OpenedAtUtc;
                if (elapsed >= _breakDuration)
                {
                    State = CacheCircuitState.HalfOpen;
                }
            }
            return State;
        }
    }
}