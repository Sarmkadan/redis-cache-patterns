#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Monitoring;

/// <summary>
/// Collects and aggregates cache performance metrics for monitoring and analysis
/// Tracks hits, misses, latency, and other key performance indicators
/// </summary>
public class CacheMetricsCollector
{
    private readonly ILogger<CacheMetricsCollector> _logger;
    private readonly ConcurrentDictionary<string, MetricData> _metrics = new();
    private DateTime _collectionStartTime = DateTime.UtcNow;

    public CacheMetricsCollector(ILogger<CacheMetricsCollector> logger)
    {
        _logger = logger;
    }

    public void RecordHit(string key, long elapsedMs)
    {
        var metric = _metrics.GetOrAdd("hits", _ => new MetricData());
        metric.Count++;
        metric.TotalMs += elapsedMs;
        metric.LastOccurrence = DateTime.UtcNow;
    }

    public void RecordMiss(string key, long elapsedMs)
    {
        var metric = _metrics.GetOrAdd("misses", _ => new MetricData());
        metric.Count++;
        metric.TotalMs += elapsedMs;
        metric.LastOccurrence = DateTime.UtcNow;
    }

    public void RecordEviction(int count)
    {
        var metric = _metrics.GetOrAdd("evictions", _ => new MetricData());
        metric.Count += count;
        metric.LastOccurrence = DateTime.UtcNow;
    }

    public void RecordError(string operationType)
    {
        var metric = _metrics.GetOrAdd("errors", _ => new MetricData());
        metric.Count++;
        metric.LastOccurrence = DateTime.UtcNow;
        _logger.LogWarning("Cache operation error recorded: {Operation}", operationType);
    }

    public CacheMetrics GetMetrics()
    {
        var hits = _metrics.TryGetValue("hits", out var h) ? h.Count : 0;
        var misses = _metrics.TryGetValue("misses", out var m) ? m.Count : 0;
        var evictions = _metrics.TryGetValue("evictions", out var e) ? e.Count : 0;
        var errors = _metrics.TryGetValue("errors", out var er) ? er.Count : 0;

        var avgHitLatency = hits > 0 ? _metrics["hits"].TotalMs / hits : 0;
        var avgMissLatency = misses > 0 ? _metrics["misses"].TotalMs / misses : 0;

        return new CacheMetrics
        {
            TotalHits = hits,
            TotalMisses = misses,
            HitRate = (hits + misses) > 0 ? (double)hits / (hits + misses) * 100 : 0,
            Evictions = evictions,
            Errors = errors,
            AverageHitLatencyMs = avgHitLatency,
            AverageMissLatencyMs = avgMissLatency,
            UptimeSeconds = (DateTime.UtcNow - _collectionStartTime).TotalSeconds
        };
    }

    public void Reset()
    {
        _metrics.Clear();
        _collectionStartTime = DateTime.UtcNow;
        _logger.LogInformation("Cache metrics reset");
    }

    private class MetricData
    {
        public long Count { get; set; }
        public long TotalMs { get; set; }
        public DateTime LastOccurrence { get; set; }
    }
}

/// <summary>
/// Cache performance metrics snapshot
/// </summary>
public class CacheMetrics
{
    public long TotalHits { get; set; }
    public long TotalMisses { get; set; }
    public double HitRate { get; set; }
    public long Evictions { get; set; }
    public long Errors { get; set; }
    public long AverageHitLatencyMs { get; set; }
    public long AverageMissLatencyMs { get; set; }
    public double UptimeSeconds { get; set; }

    public override string ToString() =>
        $"Hits={TotalHits}, Misses={TotalMisses}, HitRate={HitRate:F2}%, " +
        $"AvgHitLatency={AverageHitLatencyMs}ms, AvgMissLatency={AverageMissLatencyMs}ms";
}
