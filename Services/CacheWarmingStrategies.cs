#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Services;

// ─── Strategy contracts ──────────────────────────────────────────────────────

/// <summary>
/// Defines the warm-up priority assigned to a cache key or group of keys.
/// Higher-priority entries are loaded before lower-priority ones when the
/// <see cref="PriorityWarmingStrategy"/> executes.
/// </summary>
public enum WarmingPriority
{
    /// <summary>Load last — background or speculative pre-population.</summary>
    Low = 0,

    /// <summary>Default priority for most cache entries.</summary>
    Normal = 1,

    /// <summary>Load before <see cref="Normal"/> entries, e.g. critical reference data.</summary>
    High = 2,

    /// <summary>Load first — application cannot start without these entries.</summary>
    Critical = 3
}

/// <summary>
/// Describes a single item to be warm-loaded: its cache key, the factory that
/// produces its value, an optional TTL override, and a warming priority.
/// </summary>
public sealed class WarmingEntry
{
    /// <summary>The Redis key under which the value will be stored.</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>Factory that produces the value to be cached.</summary>
    public Func<Task<object?>> ValueFactory { get; init; } = () => Task.FromResult<object?>(null);

    /// <summary>Optional TTL. When <c>null</c> the cache service's default TTL is used.</summary>
    public TimeSpan? Expiration { get; init; }

    /// <summary>Warming priority, used to order execution in <see cref="PriorityWarmingStrategy"/>.</summary>
    public WarmingPriority Priority { get; init; } = WarmingPriority.Normal;
}

// ─── Concrete strategies ─────────────────────────────────────────────────────

/// <summary>
/// Warms a fixed, developer-supplied list of entries in declaration order.
/// Each entry provides its own value factory so any data source can be used.
/// </summary>
public sealed class DelegateWarmingStrategy : CacheWarmingStrategy
{
    private readonly IReadOnlyList<WarmingEntry> _entries;
    private readonly ILogger<DelegateWarmingStrategy> _logger;

    /// <param name="name">Human-readable strategy name used in log output.</param>
    /// <param name="entries">Entries to warm, evaluated in list order.</param>
    /// <param name="logger">Logger for per-entry diagnostics.</param>
    public DelegateWarmingStrategy(
        string name,
        IEnumerable<WarmingEntry> entries,
        ILogger<DelegateWarmingStrategy> logger)
    {
        Name = name;
        _entries = entries.ToList();
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(ICacheService cacheService)
    {
        var warmed = 0;
        foreach (var entry in _entries)
        {
            try
            {
                var value = await entry.ValueFactory();
                if (value is not null)
                {
                    // Dispatch through the value's runtime type rather than the
                    // static `object` type of WarmingEntry.ValueFactory, so the
                    // cache service serializes/stores it the same way callers
                    // would if they had called SetAsync<T> directly.
                    await cacheService.SetAsync(entry.Key, (dynamic)value, entry.Expiration);
                    warmed++;
                    _logger.LogDebug("Warmed key: {Key}", entry.Key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to warm key: {Key}", entry.Key);
            }
        }
        return warmed;
    }
}

/// <summary>
/// Warms entries in descending <see cref="WarmingPriority"/> order so that critical
/// data is available as early as possible during application start-up.
/// </summary>
public sealed class PriorityWarmingStrategy : CacheWarmingStrategy
{
    private readonly ConcurrentDictionary<WarmingPriority, List<WarmingEntry>> _buckets = new();
    private readonly ILogger<PriorityWarmingStrategy> _logger;

    /// <param name="name">Human-readable name used in log output.</param>
    /// <param name="logger">Logger for per-bucket diagnostics.</param>
    public PriorityWarmingStrategy(string name, ILogger<PriorityWarmingStrategy> logger)
    {
        Name = name;
        _logger = logger;
    }

    /// <summary>
    /// Registers an entry for warming at the given priority level.
    /// Thread-safe: <see cref="ConcurrentDictionary{TKey, TValue}"/> guards bucket access.
    /// </summary>
    public PriorityWarmingStrategy Add(WarmingEntry entry)
    {
        _buckets.GetOrAdd(entry.Priority, _ => new List<WarmingEntry>()).Add(entry);
        return this;
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(ICacheService cacheService)
    {
        var warmed = 0;

        foreach (var priority in Enum.GetValues<WarmingPriority>().OrderByDescending(p => p))
        {
            if (!_buckets.TryGetValue(priority, out var entries) || entries.Count == 0)
                continue;

            _logger.LogInformation("Warming {Count} {Priority}-priority entries", entries.Count, priority);

            foreach (var entry in entries)
            {
                try
                {
                    var value = await entry.ValueFactory();
                    if (value is not null)
                    {
                        await cacheService.SetAsync(entry.Key, value, entry.Expiration);
                        warmed++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warm {Priority} key: {Key}", priority, entry.Key);
                }
            }
        }

        return warmed;
    }
}

/// <summary>
/// Warms cache entries in parallel batches, bounded by a configurable degree of
/// parallelism. Suited for scenarios where individual load operations are I/O-bound
/// and many keys must be warmed quickly (e.g., after a cold deploy).
/// </summary>
public sealed class ParallelWarmingStrategy : CacheWarmingStrategy
{
    private readonly IReadOnlyList<WarmingEntry> _entries;
    private readonly int _maxDegreeOfParallelism;
    private readonly ILogger<ParallelWarmingStrategy> _logger;

    /// <param name="name">Human-readable strategy name.</param>
    /// <param name="entries">Entries to warm concurrently.</param>
    /// <param name="maxDegreeOfParallelism">
    /// Maximum number of concurrent warm-up tasks. Defaults to <c>4</c>.
    /// Increase cautiously to avoid overwhelming upstream data sources.
    /// </param>
    /// <param name="logger">Logger for diagnostics.</param>
    public ParallelWarmingStrategy(
        string name,
        IEnumerable<WarmingEntry> entries,
        ILogger<ParallelWarmingStrategy> logger,
        int maxDegreeOfParallelism = 4)
    {
        Name = name;
        _entries = entries.ToList();
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(ICacheService cacheService)
    {
        var semaphore = new SemaphoreSlim(_maxDegreeOfParallelism, _maxDegreeOfParallelism);
        var warmedCount = 0;

        var tasks = _entries.Select(async entry =>
        {
            await semaphore.WaitAsync();
            try
            {
                var value = await entry.ValueFactory();
                if (value is not null)
                {
                    await cacheService.SetAsync(entry.Key, value, entry.Expiration);
                    Interlocked.Increment(ref warmedCount);
                    _logger.LogDebug("Warmed key in parallel: {Key}", entry.Key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to warm key in parallel: {Key}", entry.Key);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return warmedCount;
    }
}

/// <summary>
/// Warms cache entries that match a glob pattern by re-fetching their values through
/// the provided reload function. Useful for refreshing a key namespace that has expired
/// or been evicted (e.g., reload all <c>product:*</c> entries after a deployment).
/// </summary>
public sealed class PatternRefreshWarmingStrategy : CacheWarmingStrategy
{
    private readonly string _keyPattern;
    private readonly Func<string, Task<object?>> _reloadFn;
    private readonly TimeSpan? _expiration;
    private readonly ILogger<PatternRefreshWarmingStrategy> _logger;

    /// <param name="name">Human-readable strategy name.</param>
    /// <param name="keyPattern">
    /// Glob-style pattern (e.g. <c>product:*</c>) whose matching keys will be refreshed.
    /// </param>
    /// <param name="reloadFn">
    /// Given a cache key, returns the fresh value to store. Return <c>null</c> to skip
    /// a key without counting it as a failure.
    /// </param>
    /// <param name="expiration">TTL applied to refreshed entries.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public PatternRefreshWarmingStrategy(
        string name,
        string keyPattern,
        Func<string, Task<object?>> reloadFn,
        TimeSpan? expiration,
        ILogger<PatternRefreshWarmingStrategy> logger)
    {
        Name = name;
        _keyPattern = keyPattern;
        _reloadFn = reloadFn;
        _expiration = expiration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(ICacheService cacheService)
    {
        IEnumerable<string> keys;
        try
        {
            keys = await cacheService.GetKeysByPatternAsync(_keyPattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate keys for pattern: {Pattern}", _keyPattern);
            return 0;
        }

        var warmed = 0;
        foreach (var key in keys)
        {
            try
            {
                var value = await _reloadFn(key);
                if (value is not null)
                {
                    await cacheService.SetAsync(key, value, _expiration);
                    warmed++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh key: {Key}", key);
            }
        }

        _logger.LogInformation("Pattern refresh warmed {Count} key(s) matching '{Pattern}'", warmed, _keyPattern);
        return warmed;
    }
}

// ─── Warming scheduler ───────────────────────────────────────────────────────

/// <summary>
/// Schedules a <see cref="CacheWarmingService"/> to run its registered strategies
/// at a configurable interval. Designed to be started once at application boot
/// and stopped on graceful shutdown. This class is thread-safe and disposable.
/// </summary>
public sealed class CacheWarmingScheduler : IDisposable
{
    private readonly CacheWarmingService _warmingService;
    private readonly ILogger<CacheWarmingScheduler> _logger;
    private readonly TimeSpan _interval;
    private Timer? _timer;
    private int _isRunning; // 0 = stopped, 1 = running — updated with Interlocked

    /// <param name="warmingService">Service that will execute the warming strategies.</param>
    /// <param name="logger">Logger for operational diagnostics.</param>
    /// <param name="interval">
    /// How often the warming cycle should run. Defaults to 6 hours.
    /// </param>
    public CacheWarmingScheduler(
        CacheWarmingService warmingService,
        ILogger<CacheWarmingScheduler> logger,
        TimeSpan? interval = null)
    {
        _warmingService = warmingService ?? throw new ArgumentNullException(nameof(warmingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _interval = interval ?? TimeSpan.FromHours(6);
    }

    /// <summary>
    /// Starts the scheduler, triggering an immediate first warming cycle followed
    /// by subsequent cycles at the configured interval.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the scheduler is already running.</exception>
    public void Start()
    {
        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
            throw new InvalidOperationException("Cache warming scheduler is already running.");

        _timer = new Timer(OnTimerElapsed, null, TimeSpan.Zero, _interval);
        _logger.LogInformation("Cache warming scheduler started. Interval: {Interval}", _interval);
    }

    /// <summary>Stops the scheduler and releases the underlying timer.</summary>
    public void Stop()
    {
        if (Interlocked.CompareExchange(ref _isRunning, 0, 1) != 1)
            return;

        _timer?.Dispose();
        _timer = null;
        _logger.LogInformation("Cache warming scheduler stopped.");
    }

    private async void OnTimerElapsed(object? _)
    {
        try
        {
            _logger.LogInformation("Scheduled cache warming triggered.");
            var result = await _warmingService.WarmAsync();
            _logger.LogInformation("Scheduled warming complete: {Result}", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled cache warming.");
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
        _timer?.Dispose();
    }
}
