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
/// Uses Interlocked operations for thread-safe counter updates.
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

    // Backing fields for Interlocked operations
    private long _totalHits;
    private long _totalMisses;
    private long _totalErrors;
    private long _totalOperations;
    private DateTime _lastReset = DateTime.UtcNow;

    // Private constructor to enforce singleton pattern
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
    /// Gets the meter instance for this aggregator.
    /// </summary>
    public Meter Meter => _meter;

    /// <summary>
    /// Increment the cache hit counter using Interlocked operations.
    /// </summary>
    public void IncrementHits()
    {
        Interlocked.Increment(ref _totalHits);
        Interlocked.Increment(ref _totalOperations);

        _hitsCounter.Add(1);
    }

    /// <summary>
    /// Increment the cache miss counter using Interlocked operations.
    /// </summary>
    public void IncrementMisses()
    {
        Interlocked.Increment(ref _totalMisses);
        Interlocked.Increment(ref _totalOperations);

        _missesCounter.Add(1);
    }

    /// <summary>
    /// Increment the error counter using Interlocked operations.
    /// </summary>
    public void IncrementErrors()
    {
        Interlocked.Increment(ref _totalErrors);
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
    /// Get the current statistics snapshot.
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        var now = DateTime.UtcNow;
        var operations = Interlocked.Read(ref _totalOperations);

        return new CacheStatistics
        {
            TotalKeys = 0, // Will be aggregated from cache services
            MemoryUsedBytes = 0, // Will be aggregated from cache services
            Hits = Interlocked.Read(ref _totalHits),
            Misses = Interlocked.Read(ref _totalMisses),
            Errors = Interlocked.Read(ref _totalErrors),
            TotalOperations = operations,
            CapturedAt = now
        };
    }

    /// <summary>
    /// Reset all counters to zero.
    /// </summary>
    public void Reset()
    {
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
    private double CalculateHitRatio()
    {
        var hits = Interlocked.Read(ref _totalHits);
        var operations = Interlocked.Read(ref _totalOperations);

        return operations > 0 ? (double)hits / operations : 0;
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    public static CacheStatisticsAggregator Instance => _instance.Value;
}