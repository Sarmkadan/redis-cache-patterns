#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Exceptions;

namespace RedisCachePatterns.Services;

/// <summary>
/// Circuit-breaker decorator over <see cref="ICacheService"/> implementing fail-open semantics for reads.
///
/// <para><b>Decorator Pattern:</b></para>
/// <para>This class implements the decorator pattern, wrapping any <see cref="ICacheService"/> implementation
///to add circuit breaker functionality. Users can compose decorators in any order:
/// <code>new CircuitBreakerCacheService(new CompressedCacheService(new RedisCacheService(...)))</code></para>
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
public sealed class CircuitBreakerCacheService : ICacheService
{
    private readonly ICacheService _inner;
    private readonly ILogger<CircuitBreakerCacheService>? _logger;
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

    public CircuitBreakerCacheService(
        ICacheService inner,
        int failureThreshold = 5,
        TimeSpan? breakDuration = null,
        ILogger<CircuitBreakerCacheService>? logger = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        FailureThreshold = failureThreshold > 0
            ? failureThreshold
            : throw new ArgumentOutOfRangeException(nameof(failureThreshold), "FailureThreshold must be positive");
        _breakDuration = breakDuration ?? TimeSpan.FromSeconds(30);
        _logger = logger;
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
            _logger?.LogDebug("Circuit OPEN - bypassing cache for GetOrLoadAsync key: {Key}", key);
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
            _logger?.LogDebug("Circuit OPEN - bypassing cache for GetAsync key: {Key}", key);
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
            _logger?.LogDebug("Circuit OPEN - bypassing cache for SetAsync key: {Key}", key);
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
            _logger?.LogDebug("Circuit OPEN - bypassing cache for RemoveAsync key: {Key}", key);
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
    /// Retrieves a cached value by key and refreshes its TTL on successful read (sliding expiration).
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key to look up.</param>
    /// <param name="slidingExpiration">The TTL to apply on every successful read.</param>
    /// <returns>The deserialized value if found; otherwise <c>default</c>.</returns>
    public async Task<T?> GetWithSlidingExpirationAsync<T>(string key, TimeSpan slidingExpiration)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            _logger?.LogDebug("Circuit OPEN - bypassing cache for GetWithSlidingExpirationAsync key: {Key}", key);
            return default;
        }

        try
        {
            var result = await _inner.GetWithSlidingExpirationAsync<T>(key, slidingExpiration).ConfigureAwait(false);
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
    /// Cache-aside with sliding expiration: on a cache hit the entry's TTL is reset to
    /// <paramref name="slidingExpiration"/> so that actively accessed entries remain warm.
    /// </summary>
    public async Task<T?> GetOrLoadWithSlidingExpirationAsync<T>(
        string key,
        Func<Task<T>> loadFn,
        TimeSpan slidingExpiration)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (loadFn == null) throw new ArgumentNullException(nameof(loadFn));

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            _logger?.LogDebug("Circuit OPEN - bypassing cache for GetOrLoadWithSlidingExpirationAsync key: {Key}", key);
            return await loadFn().ConfigureAwait(false);
        }

        try
        {
            var result = await _inner.GetOrLoadWithSlidingExpirationAsync(key, loadFn, slidingExpiration).ConfigureAwait(false);
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
    /// Cache stampede prevention via probabilistic early expiration (XFetch algorithm).
    /// </summary>
    public async Task<T?> GetOrLoadWithEarlyExpirationAsync<T>(
        string key,
        Func<Task<T>> loadFn,
        TimeSpan expiration,
        double beta = 1.0)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (loadFn == null) throw new ArgumentNullException(nameof(loadFn));

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            _logger?.LogDebug("Circuit OPEN - bypassing cache for GetOrLoadWithEarlyExpirationAsync key: {Key}", key);
            return await loadFn().ConfigureAwait(false);
        }

        try
        {
            var result = await _inner.GetOrLoadWithEarlyExpirationAsync(key, loadFn, expiration, beta).ConfigureAwait(false);
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
    /// Write-through pattern: persists the value via <paramref name="persistFn"/> first,
    /// then updates the cache with the persisted result.
    /// </summary>
    public async Task<T> WriteAsync<T>(
        string key,
        T value,
        Func<Task<T>> persistFn,
        TimeSpan? expiration = null)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (persistFn == null) throw new ArgumentNullException(nameof(persistFn));

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            _logger?.LogDebug("Circuit OPEN - bypassing cache for WriteAsync key: {Key}", key);
            return await persistFn().ConfigureAwait(false);
        }

        try
        {
            var result = await _inner.WriteAsync(key, value, persistFn, expiration).ConfigureAwait(false);
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
    /// Checks whether a cache entry exists for the given key without retrieving its value.
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            _logger?.LogDebug("Circuit OPEN - bypassing cache for ExistsAsync key: {Key}", key);
            return false;
        }

        try
        {
            var result = await _inner.ExistsAsync(key).ConfigureAwait(false);
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
    /// Returns the remaining time-to-live for a cache key.
    /// </summary>
    public async Task<TimeSpan?> GetExpirationAsync(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            _logger?.LogDebug("Circuit OPEN - bypassing cache for GetExpirationAsync key: {Key}", key);
            return null;
        }

        try
        {
            var result = await _inner.GetExpirationAsync(key).ConfigureAwait(false);
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
    /// Removes a single cache entry by its exact key.
    /// </summary>
    public async Task RemoveByPatternAsync(string pattern)
    {
        if (pattern == null) throw new ArgumentNullException(nameof(pattern));

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            _logger?.LogDebug("Circuit OPEN - bypassing cache for RemoveByPatternAsync pattern: {Pattern}", pattern);
            return;
        }

        try
        {
            await _inner.RemoveByPatternAsync(pattern).ConfigureAwait(false);
            RecordSuccess();
        }
        catch (CacheException)
        {
            RecordFailure();
            throw;
        }
    }

    /// <summary>
    /// Removes all cache entries whose keys match the given glob-style pattern.
    /// </summary>
    public async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern)
    {
        if (pattern == null) throw new ArgumentNullException(nameof(pattern));

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            _logger?.LogDebug("Circuit OPEN - bypassing cache for GetKeysByPatternAsync pattern: {Pattern}", pattern);
            return Enumerable.Empty<string>();
        }

        try
        {
            var result = await _inner.GetKeysByPatternAsync(pattern).ConfigureAwait(false);
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
    /// Retrieves multiple cached values by their keys in a single batch operation.
    /// </summary>
    public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys)
    {
        if (keys == null) throw new ArgumentNullException(nameof(keys));

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            _logger?.LogDebug("Circuit OPEN - bypassing cache for GetManyAsync");
            return new Dictionary<string, T?>();
        }

        try
        {
            var result = await _inner.GetManyAsync<T>(keys).ConfigureAwait(false);
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
    /// Retrieves per-key usage metadata from the metadata hash stored alongside the cached entry.
    /// </summary>
    public async Task<CacheKeyMetadata?> GetKeyMetadataAsync(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            _logger?.LogDebug("Circuit OPEN - bypassing cache for GetKeyMetadataAsync key: {Key}", key);
            return null;
        }

        try
        {
            var result = await _inner.GetKeyMetadataAsync(key).ConfigureAwait(false);
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
    /// Acquires a distributed lock using Redis SET NX with automatic expiration.
    /// </summary>
    public async Task<bool> AcquireLockAsync(string lockKey, string lockValue, TimeSpan duration)
    {
        if (lockKey == null) throw new ArgumentNullException(nameof(lockKey));
        if (lockValue == null) throw new ArgumentNullException(nameof(lockValue));

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            _logger?.LogDebug("Circuit OPEN - bypassing cache for AcquireLockAsync key: {LockKey}", lockKey);
            return false;
        }

        try
        {
            var result = await _inner.AcquireLockAsync(lockKey, lockValue, duration).ConfigureAwait(false);
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
    /// Releases a distributed lock atomically.
    /// </summary>
    public async Task<bool> ReleaseLockAsync(string lockKey, string lockValue)
    {
        if (lockKey == null) throw new ArgumentNullException(nameof(lockKey));
        if (lockValue == null) throw new ArgumentNullException(nameof(lockValue));

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            _logger?.LogDebug("Circuit OPEN - bypassing cache for ReleaseLockAsync key: {LockKey}", lockKey);
            return false;
        }

        try
        {
            var result = await _inner.ReleaseLockAsync(lockKey, lockValue).ConfigureAwait(false);
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
    /// Extends the TTL of a distributed lock atomically.
    /// </summary>
    public async Task<bool> RenewLockAsync(string lockKey, string lockValue, TimeSpan newDuration)
    {
        if (lockKey == null) throw new ArgumentNullException(nameof(lockKey));
        if (lockValue == null) throw new ArgumentNullException(nameof(lockValue));

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            _logger?.LogDebug("Circuit OPEN - bypassing cache for RenewLockAsync key: {LockKey}", lockKey);
            return false;
        }

        try
        {
            var result = await _inner.RenewLockAsync(lockKey, lockValue, newDuration).ConfigureAwait(false);
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
    /// Removes all entries from the cache database.
    /// </summary>
    public async Task FlushAsync()
    {
        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            _logger?.LogDebug("Circuit OPEN - bypassing cache for FlushAsync");
            return;
        }

        try
        {
            await _inner.FlushAsync().ConfigureAwait(false);
            RecordSuccess();
        }
        catch (CacheException)
        {
            RecordFailure();
            throw;
        }
    }

    /// <summary>
    /// Retrieves cache statistics including total key count, memory usage, and hit/miss rates.
    /// </summary>
    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            _logger?.LogDebug("Circuit OPEN - bypassing cache for GetStatisticsAsync");
            return new CacheStatistics();
        }

        try
        {
            var result = await _inner.GetStatisticsAsync().ConfigureAwait(false);
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
    /// Registers or updates a cache policy for a specific key pattern.
    /// </summary>
    public ValueTask SetPolicyAsync(Domain.CachePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            _logger?.LogDebug("Circuit OPEN - bypassing cache for SetPolicyAsync policy: {PolicyKey}", policy.Key);
            return ValueTask.CompletedTask;
        }

        try
        {
            var result = _inner.SetPolicyAsync(policy);
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
    /// Retrieves the cache policy configured for a specific key.
    /// </summary>
    public ValueTask<Domain.CachePolicy?> GetPolicyAsync(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var state = EvaluateState();
        if (state == CacheCircuitState.Open)
        {
            _logger?.LogDebug("Circuit OPEN - bypassing cache for GetPolicyAsync key: {Key}", key);
            return ValueTask.FromResult<Domain.CachePolicy?>(null);
        }

        try
        {
            var result = _inner.GetPolicyAsync(key);
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
                    _logger?.LogInformation("Circuit CLOSED after successful probe");
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
                        _logger?.LogWarning(
                            "Circuit OPENED after {FailureCount} consecutive failures (threshold: {Threshold})",
                            ConsecutiveFailures,
                            FailureThreshold);
                    }
                    break;
                case CacheCircuitState.HalfOpen:
                    State = CacheCircuitState.Open;
                    OpenedAtUtc = DateTime.UtcNow;
                    ConsecutiveFailures++;
                    _logger?.LogWarning(
                        "Circuit re-OPENED after probe failure ({FailureCount} failures)",
                        ConsecutiveFailures);
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
            _logger?.LogInformation("Circuit manually reset to CLOSED state");
        }
    }

    private CacheCircuitState EvaluateState()
    {
        lock (_sync)
        {
            if (State == CacheCircuitState.Open)
            {
                var now = DateTime.UtcNow;
                var elapsed = now - OpenedAtUtc.GetValueOrDefault();
                if (elapsed >= _breakDuration)
                {
                    State = CacheCircuitState.HalfOpen;
                    _logger?.LogInformation(
                        "Circuit transitioned to HALF-OPEN after {Elapsed} (break duration: {BreakDuration})",
                        elapsed,
                        _breakDuration);
                }
            }
            return State;
        }
    }
}