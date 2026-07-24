#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Diagnostics.Metrics;
using System.Diagnostics;
using RedisCachePatterns.Monitoring;

namespace RedisCachePatterns.Examples;

/// <summary>
/// Demonstrates the System.Diagnostics.Metrics export functionality of CacheStatisticsAggregator.
/// Shows how metrics are automatically recorded and can be consumed by OpenTelemetry collectors.
/// </summary>
public class MetricsExportExample
{
    /// <summary>
    /// Demonstrates the metrics collection and export functionality.
    /// </summary>
    public void DemonstrateMetricsExport()
    {
        Console.WriteLine("\n╔════════════════════════════════════════╗");
        Console.WriteLine("║ CacheStatisticsAggregator Metrics Export Demo ║");
        Console.WriteLine("╚════════════════════════════════════════╝\n");

        // Get the singleton instance
        var statsAggregator = CacheStatisticsAggregator.Instance;

        Console.WriteLine($"Meter Name: {statsAggregator.Meter.Name}");
        Console.WriteLine($"Meter Version: {statsAggregator.Meter.Version}");
        Console.WriteLine($"Number of Instruments: {statsAggregator.Meter.Instruments.Count()}");

        Console.WriteLine("\n--- Available Metrics ---");
        foreach (var instrument in statsAggregator.Meter.Instruments)
        {
            Console.WriteLine($"  • {instrument.Name} ({instrument.InstrumentType})");
            Console.WriteLine($"    Description: {instrument.Description}");
            Console.WriteLine($"    Unit: {instrument.Unit}");
        }

        // Simulate cache operations
        Console.WriteLine("\n--- Simulating Cache Operations ---");

        // Cache hit
        statsAggregator.IncrementHits();
        statsAggregator.RecordOperationDuration(2.5);
        Console.WriteLine("✓ Recorded cache hit");

        // Another cache hit
        statsAggregator.IncrementHits();
        statsAggregator.RecordOperationDuration(1.8);
        Console.WriteLine("✓ Recorded cache hit");

        // Cache miss
        statsAggregator.IncrementMisses();
        statsAggregator.RecordOperationDuration(15.2);
        Console.WriteLine("✓ Recorded cache miss");

        // Cache miss
        statsAggregator.IncrementMisses();
        statsAggregator.RecordOperationDuration(22.1);
        Console.WriteLine("✓ Recorded cache miss");

        // Error
        statsAggregator.IncrementErrors();
        statsAggregator.RecordOperationDuration(50.7);
        Console.WriteLine("✓ Recorded error");

        // Get statistics snapshot
        Console.WriteLine("\n--- Current Statistics Snapshot ---");
        var stats = statsAggregator.GetStatistics();
        Console.WriteLine($"Hits: {stats.Hits}");
        Console.WriteLine($"Misses: {stats.Misses}");
        Console.WriteLine($"Errors: {stats.Errors}");
        Console.WriteLine($"Total Operations: {stats.TotalOperations}");
        Console.WriteLine($"Hit Rate: {stats.HitRate:F2}%");
        Console.WriteLine($"Captured At: {stats.CapturedAt:yyyy-MM-dd HH:mm:ss}");

        // Demonstrate observable gauge
        Console.WriteLine("\n--- Observable Gauge (Hit Ratio) ---");
        Console.WriteLine("The hit_ratio gauge is automatically updated and can be queried by metrics systems.");
        Console.WriteLine("Current calculated hit ratio: " + CalculateHitRatio(stats.Hits, stats.TotalOperations));

        Console.WriteLine("\n--- Metric Export Configuration ---");
        Console.WriteLine("To export these metrics to monitoring systems:");
        Console.WriteLine("1. Configure OpenTelemetry exporter in your application:");
        Console.WriteLine("   services.AddOpenTelemetry()");
        Console.WriteLine("       .WithMetrics(builder => builder.AddMeter(CacheStatisticsAggregator.Instance.Meter.Name))");
        Console.WriteLine("       .AddOtlpExporter()");
        Console.WriteLine("");
        Console.WriteLine("2. Or use Prometheus exporter:");
        Console.WriteLine("   services.AddOpenTelemetry()");
        Console.WriteLine("       .WithMetrics(builder => builder.AddMeter(CacheStatisticsAggregator.Instance.Meter.Name))");
        Console.WriteLine("       .AddPrometheusExporter()");
        Console.WriteLine("");
        Console.WriteLine("3. Metrics will be available at /metrics endpoint if using ASP.NET Core");

        Console.WriteLine("\n--- Metric Names for Querying ---");
        Console.WriteLine("These metrics can be queried using:");
        Console.WriteLine("- cache_hits_total");
        Console.WriteLine("- cache_misses_total");
        Console.WriteLine("- cache_errors_total");
        Console.WriteLine("- cache_operation_duration_bucket");
        Console.WriteLine("- cache_hit_ratio");
    }


    private double CalculateHitRatio(long hits, long totalOperations)
    {
        return totalOperations > 0 ? (double)hits / totalOperations : 0;
    }
}