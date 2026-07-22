#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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

    public static CacheStatisticsAggregator Instance => _instance.Value;

    // Backing fields for Interlocked operations
    private long _totalHits;
    private long _totalMisses;
    private long _totalErrors;
    private long _totalOperations;
    private DateTime _lastReset = DateTime.UtcNow;

    // Private constructor to enforce singleton pattern
    private CacheStatisticsAggregator()
    {
        // Initialize with current time
    }

    /// <summary>
    /// Increment the cache hit counter using Interlocked operations.
    /// </summary>
    public void IncrementHits()
    {
        Interlocked.Increment(ref _totalHits);
        Interlocked.Increment(ref _totalOperations);
    }

    /// <summary>
    /// Increment the cache miss counter using Interlocked operations.
    /// </summary>
    public void IncrementMisses()
    {
        Interlocked.Increment(ref _totalMisses);
        Interlocked.Increment(ref _totalOperations);
    }

    /// <summary>
    /// Increment the error counter using Interlocked operations.
    /// </summary>
    public void IncrementErrors()
    {
        Interlocked.Increment(ref _totalErrors);
        Interlocked.Increment(ref _totalOperations);
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

    public void Dispose()
    {
        // Nothing to dispose in this simple singleton
    }
}
