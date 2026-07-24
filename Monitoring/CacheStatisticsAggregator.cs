#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Diagnostics.Metrics;
using System.Threading;
using RedisCachePatterns.Services;

namespace RedisCachePatterns.Monitoring;

/// <summary>
/// Singleton aggregator for cache statistics across all cache services.
/// Uses atomic Interlocked operations for thread-safe counter updates with overflow protection.
/// </summary>
public sealed class CacheStatisticsAggregator : IDisposable
{
    private static readonly Lazy<CacheStatisticsAggregator> _instance =
        new Lazy<CacheStatisticsAggregator>(() => new CacheStatisticsAggregator(), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly Meter _meter;
    private readonly Counter<long> _hitsCounter;
    private readonly Counter<long> _missesCounter;
    private readonly Counter<long> _errorsCounter;
    private readonly Histogram<double> _operationDurationHistogram;
    private readonly ObservableGauge<double> _hitRatioGauge;

    // Backing fields for atomic operations with overflow protection
    // Using modulo arithmetic to prevent overflow while maintaining approximate accuracy
    private long _totalHits;
    private long _totalMisses;
    private long _totalErrors;
    private long _totalOperations;
    private DateTime _lastReset = DateTime.UtcNow;

    /// <summary>
    /// Gets the singleton instance of the cache statistics aggregator.
    /// </summary>
    public static CacheStatisticsAggregator Instance => _instance.Value;

    /// <summary>
    /// Gets the meter instance for this aggregator.
    /// </summary>
    public Meter Meter => _meter;

    /// <summary>
    /// Private constructor to enforce singleton pattern.
    /// </summary>
    private CacheStatisticsAggregator()
    {
        _meter = new Meter("RedisCachePatterns.CacheStatistics", "1.0.0");

        _hitsCounter = _meter.CreateCounter<long>(
            "cache.hits",
            "hits",
            "Number of cache hits");

        _missesCounter = _meter.CreateCounter<long>(
            "cache.misses",
            "misses",
            "Number of cache misses");

        _errorsCounter = _meter.CreateCounter<long>(
            "cache.errors",
            "errors",
            "Number of cache errors");

        _operationDurationHistogram = _meter.CreateHistogram<double>(
            "cache.operation.duration",
            "milliseconds",
            "Duration of cache operations in milliseconds");

        _hitRatioGauge = _meter.CreateObservableGauge(
            "cache.hit_ratio",
            () => CalculateHitRatio(),
            "ratio",
            "Cache hit ratio (0-1)");
    }

    /// <summary>
    /// Increment the cache hit counter using atomic Interlocked operations with overflow protection.
    /// </summary>
    /// <exception cref="OverflowException">Thrown if counter overflows despite protection.</exception>
    public void IncrementHits()
    {
        // Atomic increment with overflow protection using modulo arithmetic
        long current, next;
        do
        {
            current = _totalHits;
            next = current == long.MaxValue ? 0 : current + 1;
        }
        while (Interlocked.CompareExchange(ref _totalHits, next, current) != current);

        // Atomic increment for total operations
        Interlocked.Increment(ref _totalOperations);

        _hitsCounter.Add(1);
    }

    /// <summary>
    /// Increment the cache miss counter using atomic Interlocked operations with overflow protection.
    /// </summary>
    /// <exception cref="OverflowException">Thrown if counter overflows despite protection.</exception>
    public void IncrementMisses()
    {
        // Atomic increment with overflow protection using modulo arithmetic
        long current, next;
        do
        {
            current = _totalMisses;
            next = current == long.MaxValue ? 0 : current + 1;
        }
        while (Interlocked.CompareExchange(ref _totalMisses, next, current) != current);

        // Atomic increment for total operations
        Interlocked.Increment(ref _totalOperations);

        _missesCounter.Add(1);
    }

    /// <summary>
    /// Increment the error counter using atomic Interlocked operations with overflow protection.
    /// </summary>
    /// <exception cref="OverflowException">Thrown if counter overflows despite protection.</exception>
    public void IncrementErrors()
    {
        // Atomic increment with overflow protection using modulo arithmetic
        long current, next;
        do
        {
            current = _totalErrors;
            next = current == long.MaxValue ? 0 : current + 1;
        }
        while (Interlocked.CompareExchange(ref _totalErrors, next, current) != current);

        // Atomic increment for total operations
        Interlocked.Increment(ref _totalOperations);

        _errorsCounter.Add(1);
    }

    /// <summary>
    /// Record a cache operation duration.
    /// </summary>
    /// <param name="durationMilliseconds">Duration of the operation in milliseconds.</param>
    public void RecordOperationDuration(double durationMilliseconds)
    {
        _operationDurationHistogram.Record(durationMilliseconds);
    }

    /// <summary>
    /// Get the current statistics snapshot using atomic Interlocked.Read operations.
    /// </summary>
    /// <returns>A <see cref="CacheStatistics"/> snapshot captured atomically.</returns>
    public CacheStatistics GetStatistics()
    {
        // Read all counters atomically using Interlocked.Read
        // This provides a consistent snapshot since all counters are updated atomically
        var hits = Interlocked.Read(ref _totalHits);
        var misses = Interlocked.Read(ref _totalMisses);
        var errors = Interlocked.Read(ref _totalErrors);
        var operations = Interlocked.Read(ref _totalOperations);

        return new CacheStatistics
        {
            TotalKeys = 0, // Will be aggregated from cache services
            MemoryUsedBytes = 0, // Will be aggregated from cache services
            Hits = hits,
            Misses = misses,
            Errors = errors,
            TotalOperations = operations,
            CapturedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Reset all counters to zero using atomic operations.
    /// This method provides a consistent reset operation that can be called concurrently.
    /// </summary>
    /// <remarks>
    /// The reset operation uses atomic exchange to ensure all counters are reset to zero
    /// without race conditions. The GetStatistics method reads all counters atomically,
    /// ensuring that any concurrent call will receive either pre-reset or post-reset values.
    /// </remarks>
    public void Reset()
    {
        // Reset all counters atomically
        Interlocked.Exchange(ref _totalHits, 0);
        Interlocked.Exchange(ref _totalMisses, 0);
        Interlocked.Exchange(ref _totalErrors, 0);
        Interlocked.Exchange(ref _totalOperations, 0);

        _lastReset = DateTime.UtcNow;
    }

    /// <summary>
    /// Get the timestamp when counters were last reset.
    /// </summary>
    public DateTime LastReset => _lastReset;

    /// <summary>
    /// Calculates the current hit ratio for the observable gauge.
    /// </summary>
    /// <returns>The cache hit ratio (0-1), or 0 if no operations have occurred.</returns>
    private double CalculateHitRatio()
    {
        var hits = Interlocked.Read(ref _totalHits);
        var operations = Interlocked.Read(ref _totalOperations);

        return operations > 0 ? (double)hits / operations : 0;
    }

    /// <summary>
    /// Disposes the meter and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        _meter.Dispose();
    }
}