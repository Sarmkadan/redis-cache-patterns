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
/// Circuit-breaker decorator over ICacheService. Counts consecutive cache failures
/// (CacheException and its subclasses); at FailureThreshold the circuit opens for BreakDuration,
/// during which reads go straight to the loader and writes are silently skipped.
/// After BreakDuration one probe call is allowed (HalfOpen); success closes the circuit,
/// failure re-opens it.
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

    /// <summary>Cache-aside through the breaker. If Open, calls loadFn directly.</summary>
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
            return await _inner.GetOrLoadAsync(key, loadFn, expiration).ConfigureAwait(false);
        }
        catch (CacheException)
        {
            RecordFailure();
            throw;
        }
    }

    /// <summary>Get through the breaker; returns default(T) when Open.</summary>
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
            return await _inner.GetAsync<T>(key).ConfigureAwait(false);
        }
        catch (CacheException)
        {
            RecordFailure();
            throw;
        }
    }

    /// <summary>Set through the breaker; no-op when Open.</summary>
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
        }
        catch (CacheException)
        {
            RecordFailure();
            throw;
        }
    }

    /// <summary>Remove through the breaker; no-op when Open.</summary>
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
        }
        catch (CacheException)
        {
            RecordFailure();
            throw;
        }
    }

    /// <summary>Record a successful cache call: resets failures, closes circuit if HalfOpen.</summary>
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

    /// <summary>Record a cache failure: increments counter, opens circuit at threshold or if HalfOpen.</summary>
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

    /// <summary>Manually reset the breaker to Closed with zero failures.</summary>
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