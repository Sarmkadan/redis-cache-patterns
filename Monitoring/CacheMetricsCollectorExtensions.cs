#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Collections.Concurrent;
using System.Globalization;

namespace RedisCachePatterns.Monitoring;

/// <summary>
/// Extension methods for <see cref="CacheMetricsCollector"/> that provide additional functionality
/// for monitoring, reporting, and analysis of cache performance metrics
/// </summary>
public static class CacheMetricsCollectorExtensions
{
    /// <summary>
    /// Records a cache operation with the specified type and latency
    /// </summary>
    /// <param name="collector">The metrics collector instance</param>
    /// <param name="operationType">Type of operation (e.g., "Get", "Set", "Delete")</param>
    /// <param name="key">The cache key involved in the operation</param>
    /// <param name="elapsedMs">Elapsed time in milliseconds</param>
    /// <param name="success">Whether the operation succeeded</param>
    /// <exception cref="ArgumentNullException">Thrown when collector or key is null</exception>
    public static void RecordOperation(this CacheMetricsCollector collector, string operationType, string key, long elapsedMs, bool success)
    {
        ArgumentNullException.ThrowIfNull(collector);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentException.ThrowIfNullOrEmpty(operationType);

        if (success)
        {
            if (operationType.Equals("Get", StringComparison.OrdinalIgnoreCase))
            {
                collector.RecordHit(key, elapsedMs);
            }
            else
            {
                // For write operations, we don't track hits/misses
            }
        }
        else
        {
            collector.RecordError(operationType);
        }
    }

    /// <summary>
    /// Gets a formatted string representation of the current metrics
    /// including human-readable hit rate and uptime
    /// </summary>
    /// <param name="collector">The metrics collector instance</param>
    /// <returns>Formatted metrics string</returns>
    /// <exception cref="ArgumentNullException">Thrown when collector is null</exception>
    public static string GetFormattedMetrics(this CacheMetricsCollector collector)
    {
        ArgumentNullException.ThrowIfNull(collector);

        var metrics = collector.GetMetrics();
        return $"""
Cache Metrics Report
===================
Total Hits:        {metrics.TotalHits:N0}
Total Misses:      {metrics.TotalMisses:N0}
Hit Rate:          {metrics.HitRate:F2}%
Average Hit Latency: {metrics.AverageHitLatencyMs:N0}ms
Average Miss Latency: {metrics.AverageMissLatencyMs:N0}ms
Evictions:         {metrics.Evictions:N0}
Errors:            {metrics.Errors:N0}
Uptime:            {metrics.UptimeSeconds:N2} seconds

Status: {(metrics.HitRate >= 95 ? "✅ Healthy" : metrics.HitRate >= 80 ? "⚠️ Warning" : "❌ Critical")}
""";
    }

    /// <summary>
    /// Gets the current cache hit rate as a percentage (0-100)
    /// </summary>
    /// <param name="collector">The metrics collector instance</param>
    /// <returns>Hit rate percentage</returns>
    /// <exception cref="ArgumentNullException">Thrown when collector is null</exception>
    public static double GetHitRatePercentage(this CacheMetricsCollector collector)
    {
        ArgumentNullException.ThrowIfNull(collector);
        return collector.GetMetrics().HitRate;
    }

    /// <summary>
    /// Checks if the cache is currently experiencing a high error rate
    /// </summary>
    /// <param name="collector">The metrics collector instance</param>
    /// <param name="threshold">Error rate threshold as a percentage (0-100)</param>
    /// <returns>True if error rate exceeds threshold, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when collector is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is outside valid range</exception>
    public static bool HasHighErrorRate(this CacheMetricsCollector collector, double threshold = 5.0)
    {
        ArgumentNullException.ThrowIfNull(collector);
        ArgumentOutOfRangeException.ThrowIfLessThan(threshold, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(threshold, 100);

        var metrics = collector.GetMetrics();
        var totalOperations = metrics.TotalHits + metrics.TotalMisses;

        if (totalOperations == 0)
        {
            return false;
        }

        var errorRate = (double)metrics.Errors / totalOperations * 100;
        return errorRate > threshold;
    }
}