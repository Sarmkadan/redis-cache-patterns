// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Performance monitoring utility tracking operation duration and metrics
/// Provides detailed timing information for cache and service operations
/// </summary>
public class PerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly Dictionary<string, OperationMetrics> _metrics = new();

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger;
    }

    public IDisposable MeasureOperation(string operationName)
    {
        return new OperationTimer(operationName, this);
    }

    public void RecordOperation(string name, long elapsedMs)
    {
        if (!_metrics.ContainsKey(name))
        {
            _metrics[name] = new OperationMetrics { OperationName = name };
        }

        var metrics = _metrics[name];
        metrics.Count++;
        metrics.TotalMs += elapsedMs;
        metrics.MinMs = Math.Min(metrics.MinMs, elapsedMs);
        metrics.MaxMs = Math.Max(metrics.MaxMs, elapsedMs);

        _logger.LogDebug(
            "Operation recorded: {Operation} | Duration: {DurationMs}ms | Total: {TotalCount} calls",
            name, elapsedMs, metrics.Count);
    }

    public OperationMetrics? GetMetrics(string operationName)
    {
        return _metrics.TryGetValue(operationName, out var metrics) ? metrics : null;
    }

    public IEnumerable<OperationMetrics> GetAllMetrics() => _metrics.Values.ToList();

    public void ResetMetrics() => _metrics.Clear();

    public void ResetOperation(string operationName) => _metrics.Remove(operationName);

    private class OperationTimer : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly string _operationName;
        private readonly PerformanceMonitor _monitor;

        public OperationTimer(string operationName, PerformanceMonitor monitor)
        {
            _operationName = operationName;
            _monitor = monitor;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _monitor.RecordOperation(_operationName, _stopwatch.ElapsedMilliseconds);
        }
    }
}

/// <summary>
/// Metrics for a single operation type
/// </summary>
public class OperationMetrics
{
    public string OperationName { get; set; } = string.Empty;
    public int Count { get; set; }
    public long TotalMs { get; set; }
    public long MinMs { get; set; } = long.MaxValue;
    public long MaxMs { get; set; }

    public double AverageMs => Count > 0 ? (double)TotalMs / Count : 0;

    public override string ToString() =>
        $"{OperationName}: {Count} calls, Avg={AverageMs:F2}ms, Min={MinMs}ms, Max={MaxMs}ms";
}
