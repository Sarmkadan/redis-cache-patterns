#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Services;
using RedisCachePatterns.Monitoring;
using System;
using System.Threading.Tasks;

namespace RedisCachePatterns.Examples;

/// <summary>
/// Demonstrates cache monitoring, metrics collection, health checks,
/// and observability features for production visibility.
/// </summary>
public class MonitoringAndMetricsExample
{
    private readonly ICacheService _cacheService;
    private readonly CacheMetricsCollector _metricsCollector;
    private readonly HealthCheckService _healthCheck;

    public MonitoringAndMetricsExample(
        ICacheService cacheService,
        CacheMetricsCollector metricsCollector,
        HealthCheckService healthCheck)
    {
        _cacheService = cacheService;
        _metricsCollector = metricsCollector;
        _healthCheck = healthCheck;
    }

    /// <summary>
    /// Collects and displays comprehensive cache metrics.
    /// </summary>
    public async Task DisplayCacheMetricsAsync()
    {
        Console.WriteLine("\n╔════════════════════════════════════════╗");
        Console.WriteLine("║      CACHE METRICS REPORT              ║");
        Console.WriteLine("╚════════════════════════════════════════╝\n");

        try
        {
            // Hit rate metrics
            var hitRate = await _metricsCollector.GetHitRateAsync().ConfigureAwait(false);
            var missRate = await _metricsCollector.GetMissRateAsync().ConfigureAwait(false);
            Console.WriteLine($"Hit Rate:  {hitRate:P2}");
            Console.WriteLine($"Miss Rate: {missRate:P2}");

            // Performance metrics
            var avgTime = await _metricsCollector.GetAverageResponseTimeAsync().ConfigureAwait(false);
            var maxTime = await _metricsCollector.GetMaxResponseTimeAsync().ConfigureAwait(false);
            Console.WriteLine($"\nAvg Response Time: {avgTime:F2}ms");
            Console.WriteLine($"Max Response Time: {maxTime:F2}ms");

            // Capacity metrics
            var totalKeys = await _metricsCollector.GetTotalKeysAsync().ConfigureAwait(false);
            var memory = await _metricsCollector.GetEstimatedMemoryAsync().ConfigureAwait(false);
            Console.WriteLine($"\nTotal Keys: {totalKeys:N0}");
            Console.WriteLine($"Memory Usage: {memory:F2} MB");

            // Operations
            var getOps = await _metricsCollector.GetGetOperationsAsync().ConfigureAwait(false);
            var setOps = await _metricsCollector.GetSetOperationsAsync().ConfigureAwait(false);
            Console.WriteLine($"\nGET Operations: {getOps:N0}");
            Console.WriteLine($"SET Operations: {setOps:N0}");

            // Errors
            var errorCount = await _metricsCollector.GetErrorCountAsync().ConfigureAwait(false);
            Console.WriteLine($"Errors: {errorCount:N0}");

            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to collect metrics: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs a health check on the cache system.
    /// </summary>
    public async Task<bool> CheckCacheHealthAsync()
    {
        Console.WriteLine("\n╔════════════════════════════════════════╗");
        Console.WriteLine("║      CACHE HEALTH CHECK                ║");
        Console.WriteLine("╚════════════════════════════════════════╝\n");

        try
        {
            var isHealthy = await _healthCheck.IsCacheHealthyAsync().ConfigureAwait(false);
            var responseTime = await _healthCheck.MeasureResponseTimeAsync().ConfigureAwait(false);
            var memory = await _healthCheck.GetMemoryUsageAsync().ConfigureAwait(false);
            var connected = await _healthCheck.IsConnectedAsync().ConfigureAwait(false);
            var evictionPolicy = await _healthCheck.GetEvictionPolicyAsync().ConfigureAwait(false);

            Console.WriteLine($"Status:           {(isHealthy ? "✓ HEALTHY" : "✗ UNHEALTHY")}");
            Console.WriteLine($"Connected:        {(connected ? "✓ Yes" : "✗ No")}");
            Console.WriteLine($"Response Time:    {responseTime}ms");
            Console.WriteLine($"Memory Used:      {memory}MB");
            Console.WriteLine($"Eviction Policy:  {evictionPolicy}");

            if (!isHealthy)
            {
                Console.WriteLine("\n⚠ Cache is not healthy. Check Redis connection and server logs.");
            }

            return isHealthy;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Health check failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets detailed Redis server information.
    /// </summary>
    public async Task DisplayRedisInfoAsync()
    {
        Console.WriteLine("\n╔════════════════════════════════════════╗");
        Console.WriteLine("║      REDIS SERVER INFO                 ║");
        Console.WriteLine("╚════════════════════════════════════════╝\n");

        try
        {
            var info = await _cacheService.GetInfoAsync().ConfigureAwait(false);
            Console.WriteLine(info);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to get Redis info: {ex.Message}");
        }
    }

    /// <summary>
    /// Monitors cache performance over time with periodic snapshots.
    /// </summary>
    public async Task MonitorCachePerformanceAsync(int durationSeconds, int intervalSeconds)
    {
        Console.WriteLine($"\n📊 Monitoring cache for {durationSeconds}s (updates every {intervalSeconds}s)\n");

        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(durationSeconds);
        var snapshotCount = 0;

        while (DateTime.UtcNow < endTime)
        {
            snapshotCount++;
            Console.WriteLine($"\n--- Snapshot #{snapshotCount} at {DateTime.UtcNow:HH:mm:ss} ---");

            try
            {
                var hitRate = await _metricsCollector.GetHitRateAsync().ConfigureAwait(false);
                var avgTime = await _metricsCollector.GetAverageResponseTimeAsync().ConfigureAwait(false);
                var totalKeys = await _metricsCollector.GetTotalKeysAsync().ConfigureAwait(false);

                Console.WriteLine($"Hit Rate: {hitRate:P2} | Avg Time: {avgTime:F2}ms | Keys: {totalKeys:N0}");

                var elapsed = DateTime.UtcNow - startTime;
                var remaining = endTime - DateTime.UtcNow;
                Console.WriteLine($"Elapsed: {elapsed.TotalSeconds:F0}s | Remaining: {remaining.TotalSeconds:F0}s");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Error collecting metrics: {ex.Message}");
            }

            await Task.Delay(intervalSeconds * 1000).ConfigureAwait(false);
        }

        Console.WriteLine($"\n✓ Monitoring complete ({snapshotCount} snapshots captured)");
    }

    /// <summary>
    /// Generates a performance report with key statistics.
    /// </summary>
    public async Task<PerformanceReport> GeneratePerformanceReportAsync()
    {
        Console.WriteLine("Generating performance report...\n");

        var report = new PerformanceReport
        {
            Timestamp = DateTime.UtcNow,
            HitRate = await _metricsCollector.GetHitRateAsync(),
            MissRate = await _metricsCollector.GetMissRateAsync(),
            AverageResponseTimeMs = await _metricsCollector.GetAverageResponseTimeAsync(),
            MaxResponseTimeMs = await _metricsCollector.GetMaxResponseTimeAsync(),
            TotalKeys = await _metricsCollector.GetTotalKeysAsync(),
            MemoryUsageMb = await _metricsCollector.GetEstimatedMemoryAsync(),
            GetOperations = await _metricsCollector.GetGetOperationsAsync(),
            SetOperations = await _metricsCollector.GetSetOperationsAsync(),
            ErrorCount = await _metricsCollector.GetErrorCountAsync()
        };

        DisplayPerformanceReport(report);
        return report;
    }

    /// <summary>
    /// Displays formatted performance report.
    /// </summary>
    private void DisplayPerformanceReport(PerformanceReport report)
    {
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║     PERFORMANCE REPORT                 ║");
        Console.WriteLine("╚════════════════════════════════════════╝\n");

        Console.WriteLine($"Report Time:   {report.Timestamp:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Hit Rate:      {report.HitRate:P2}");
        Console.WriteLine($"Miss Rate:     {report.MissRate:P2}");
        Console.WriteLine($"Avg Response:  {report.AverageResponseTimeMs:F2}ms");
        Console.WriteLine($"Max Response:  {report.MaxResponseTimeMs:F2}ms");
        Console.WriteLine($"Total Keys:    {report.TotalKeys:N0}");
        Console.WriteLine($"Memory:        {report.MemoryUsageMb:F2}MB");
        Console.WriteLine($"GET Ops:       {report.GetOperations:N0}");
        Console.WriteLine($"SET Ops:       {report.SetOperations:N0}");
        Console.WriteLine($"Errors:        {report.ErrorCount:N0}");

        // Assessment
        Console.WriteLine();
        if (report.HitRate > 0.8)
            Console.WriteLine("✓ Excellent hit rate (>80%)");
        else if (report.HitRate > 0.6)
            Console.WriteLine("△ Good hit rate (60-80%)");
        else
            Console.WriteLine("⚠ Low hit rate (<60%) - consider cache warming");

        if (report.AverageResponseTimeMs < 5)
            Console.WriteLine("✓ Excellent response time (<5ms)");
        else if (report.AverageResponseTimeMs < 20)
            Console.WriteLine("△ Good response time (5-20ms)");
        else
            Console.WriteLine("⚠ Slow response time (>20ms) - check Redis and network");
    }

    /// <summary>
    /// Identifies and reports performance bottlenecks.
    /// </summary>
    public async Task IdentifyBottlenecksAsync()
    {
        Console.WriteLine("\n🔍 Identifying performance bottlenecks...\n");

        var avgTime = await _metricsCollector.GetAverageResponseTimeAsync().ConfigureAwait(false);
        var hitRate = await _metricsCollector.GetHitRateAsync().ConfigureAwait(false);
        var memory = await _metricsCollector.GetEstimatedMemoryAsync().ConfigureAwait(false);

        var issues = new List<string>();

        if (avgTime > 50)
            issues.Add("⚠ High response time (>50ms) - network/load issue");

        if (hitRate < 0.5)
            issues.Add("⚠ Low hit rate (<50%) - cache not warming effectively");

        if (memory > 1000)
            issues.Add("⚠ High memory usage (>1GB) - consider eviction policy");

        if (issues.Count == 0)
        {
            Console.WriteLine("✓ No bottlenecks detected");
        }
        else
        {
            foreach (var issue in issues)
            {
                Console.WriteLine(issue);
            }
        }
    }
}

/// <summary>
/// Performance report data structure.
/// </summary>
public class PerformanceReport
{
    public DateTime Timestamp { get; set; }
    public double HitRate { get; set; }
    public double MissRate { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public double MaxResponseTimeMs { get; set; }
    public long TotalKeys { get; set; }
    public double MemoryUsageMb { get; set; }
    public long GetOperations { get; set; }
    public long SetOperations { get; set; }
    public long ErrorCount { get; set; }
}
