#nullable enable

using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace RedisCachePatterns.Monitoring.Tests;

/// <summary>
/// Unit tests for CacheStatisticsAggregator to verify basic functionality, edge cases,
/// and correctness of statistics aggregation.
/// </summary>
[Collection("Sequential")]
public sealed class CacheStatisticsAggregatorTests : IDisposable
{
    private readonly CacheStatisticsAggregator _aggregator;
    private bool _disposed;

    public CacheStatisticsAggregatorTests()
    {
        _aggregator = CacheStatisticsAggregator.Instance;
        _aggregator.Reset(); // Start with clean state for each test
    }

    /// <summary>
    /// Verifies that GetStatistics returns valid results when no operations have been recorded.
    /// Ensures hit ratio calculation handles zero operations correctly (no division by zero).
    /// </summary>
    [Fact]
    public void GetStatistics_ZeroOperations_ReturnsValidSnapshot()
    {
        // Act
        var stats = _aggregator.GetStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.Hits.Should().Be(0);
        stats.Misses.Should().Be(0);
        stats.Errors.Should().Be(0);
        stats.TotalOperations.Should().Be(0);
        stats.TotalKeys.Should().Be(0);
        stats.MemoryUsedBytes.Should().Be(0);
        stats.HitRate.Should().Be(0);
        stats.CapturedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Verifies that IncrementHits correctly increments the hit counter.
    /// </summary>
    [Fact]
    public void IncrementHits_IncrementsCounter()
    {
        // Arrange
        _aggregator.Reset();

        // Act
        _aggregator.IncrementHits();
        _aggregator.IncrementHits();
        _aggregator.IncrementHits();

        // Assert
        var stats = _aggregator.GetStatistics();
        stats.Hits.Should().Be(3);
        stats.TotalOperations.Should().Be(3);
        stats.HitRate.Should().Be(100.0);
    }

    /// <summary>
    /// Verifies that IncrementMisses correctly increments the miss counter.
    /// </summary>
    [Fact]
    public void IncrementMisses_IncrementsCounter()
    {
        // Arrange
        _aggregator.Reset();

        // Act
        _aggregator.IncrementMisses();
        _aggregator.IncrementMisses();

        // Assert
        var stats = _aggregator.GetStatistics();
        stats.Misses.Should().Be(2);
        stats.TotalOperations.Should().Be(2);
        stats.HitRate.Should().Be(0.0);
    }

    /// <summary>
    /// Verifies that IncrementErrors correctly increments the error counter.
    /// </summary>
    [Fact]
    public void IncrementErrors_IncrementsCounter()
    {
        // Arrange
        _aggregator.Reset();

        // Act
        _aggregator.IncrementErrors();
        _aggregator.IncrementErrors();
        _aggregator.IncrementErrors();
        _aggregator.IncrementErrors();

        // Assert
        var stats = _aggregator.GetStatistics();
        stats.Errors.Should().Be(4);
        stats.TotalOperations.Should().Be(4);
        stats.HitRate.Should().Be(0.0);
    }

    /// <summary>
    /// Verifies that mixed operations correctly calculate hit rate.
    /// </summary>
    [Fact]
    public void MixedOperations_CalculatesCorrectHitRate()
    {
        // Arrange
        _aggregator.Reset();

        // Act: 100 hits, 50 misses, 10 errors
        for (int i = 0; i < 100; i++)
        {
            _aggregator.IncrementHits();
        }
        for (int i = 0; i < 50; i++)
        {
            _aggregator.IncrementMisses();
        }
        for (int i = 0; i < 10; i++)
        {
            _aggregator.IncrementErrors();
        }

        // Assert
        var stats = _aggregator.GetStatistics();
        stats.Hits.Should().Be(100);
        stats.Misses.Should().Be(50);
        stats.Errors.Should().Be(10);
        stats.TotalOperations.Should().Be(160);

        // Hit rate should be 100 / 160 = 62.5%
        stats.HitRate.Should().BeApproximately(62.5, 0.001);
    }

    /// <summary>
    /// Verifies that RecordOperationDuration records the duration without throwing exceptions.
    /// </summary>
    [Fact]
    public void RecordOperationDuration_RecordsDuration()
    {
        // Arrange
        _aggregator.Reset();

        // Act
        _aggregator.RecordOperationDuration(10.5);
        _aggregator.RecordOperationDuration(25.0);
        _aggregator.RecordOperationDuration(5.25);

        // Assert - no exception thrown, method is callable
        var stats = _aggregator.GetStatistics();
        stats.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that Reset sets all counters to zero.
    /// </summary>
    [Fact]
    public void Reset_SetsAllCountersToZero()
    {
        // Arrange - populate counters
        for (int i = 0; i < 100; i++)
        {
            _aggregator.IncrementHits();
            _aggregator.IncrementMisses();
            _aggregator.IncrementErrors();
        }

        var beforeReset = _aggregator.GetStatistics();
        beforeReset.Hits.Should().Be(100);
        beforeReset.Misses.Should().Be(100);
        beforeReset.Errors.Should().Be(100);
        beforeReset.TotalOperations.Should().Be(300);

        // Act - reset
        _aggregator.Reset();

        // Assert
        var afterReset = _aggregator.GetStatistics();
        afterReset.Hits.Should().Be(0);
        afterReset.Misses.Should().Be(0);
        afterReset.Errors.Should().Be(0);
        afterReset.TotalOperations.Should().Be(0);
        afterReset.HitRate.Should().Be(0);

        // Verify LastReset was updated
        _aggregator.LastReset.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Verifies that multiple Reset calls work correctly.
    /// </summary>
    [Fact]
    public void Reset_MultipleTimes_WorksCorrectly()
    {
        // Arrange & Act - multiple resets with operations in between
        _aggregator.IncrementHits();
        _aggregator.Reset();

        var stats1 = _aggregator.GetStatistics();
        stats1.Hits.Should().Be(0);

        _aggregator.IncrementHits();
        _aggregator.IncrementHits();
        _aggregator.Reset();

        var stats2 = _aggregator.GetStatistics();
        stats2.Hits.Should().Be(0);

        _aggregator.IncrementMisses();
        var stats3 = _aggregator.GetStatistics();
        stats3.Misses.Should().Be(1);
    }

    /// <summary>
    /// Verifies that LastReset returns a valid timestamp.
    /// </summary>
    [Fact]
    public void LastReset_ReturnsValidTimestamp()
    {
        // Act
        var lastReset = _aggregator.LastReset;

        // Assert
        lastReset.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that GetStatistics returns independent snapshots (not affected by subsequent operations).
    /// </summary>
    [Fact]
    public void GetStatistics_ReturnsIndependentSnapshot()
    {
        // Arrange
        _aggregator.IncrementHits();
        _aggregator.IncrementMisses();

        // Act - get snapshot
        var snapshot1 = _aggregator.GetStatistics();

        // Perform more operations
        _aggregator.IncrementHits();
        _aggregator.IncrementErrors();

        // Get another snapshot
        var snapshot2 = _aggregator.GetStatistics();

        // Assert - snapshot1 should not reflect later operations
        snapshot1.Hits.Should().Be(1);
        snapshot1.Misses.Should().Be(1);
        snapshot1.Errors.Should().Be(0);
        snapshot1.TotalOperations.Should().Be(2);

        // snapshot2 should reflect all operations
        snapshot2.Hits.Should().Be(2);
        snapshot2.Misses.Should().Be(1);
        snapshot2.Errors.Should().Be(1);
        snapshot2.TotalOperations.Should().Be(4);
    }

    /// <summary>
    /// Verifies that the singleton instance is accessible.
    /// </summary>
    [Fact]
    public void Instance_ReturnsSingleton()
    {
        // Act
        var instance1 = CacheStatisticsAggregator.Instance;
        var instance2 = CacheStatisticsAggregator.Instance;

        // Assert - both references should point to the same object
        instance1.Should().BeSameAs(instance2);
        instance1.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the Meter property returns a valid Meter instance.
    /// </summary>
    [Fact]
    public void Meter_ReturnsValidMeter()
    {
        // Act
        var meter = _aggregator.Meter;

        // Assert
        meter.Should().NotBeNull();
        meter.Name.Should().Be("RedisCachePatterns.CacheStatistics");
    }

    /// <summary>
    /// Verifies that counters can handle large numbers without issues.
    /// </summary>
    [Fact]
    public void LargeNumberOperations_HandledCorrectly()
    {
        // Arrange & Act - perform many operations
        for (int i = 0; i < 10000; i++)
        {
            _aggregator.IncrementHits();
        }

        // Assert
        var stats = _aggregator.GetStatistics();
        stats.Hits.Should().Be(10000);
        stats.TotalOperations.Should().Be(10000);
        stats.HitRate.Should().Be(100.0);
    }

    /// <summary>
    /// Verifies that GetStatistics can be called multiple times without side effects.
    /// </summary>
    [Fact]
    public void GetStatistics_MultipleCalls_NoSideEffects()
    {
        // Arrange
        _aggregator.IncrementHits();
        _aggregator.IncrementMisses();

        // Act - call multiple times
        var stats1 = _aggregator.GetStatistics();
        var stats2 = _aggregator.GetStatistics();
        var stats3 = _aggregator.GetStatistics();

        // Assert - all snapshots should have same counter values (timestamp may differ slightly)
        stats1.Hits.Should().Be(1);
        stats1.Misses.Should().Be(1);
        stats2.Hits.Should().Be(1);
        stats2.Misses.Should().Be(1);
        stats3.Hits.Should().Be(1);
        stats3.Misses.Should().Be(1);

        // Verify counter values are consistent
        stats1.TotalOperations.Should().Be(2);
        stats2.TotalOperations.Should().Be(2);
        stats3.TotalOperations.Should().Be(2);
    }

    /// <summary>
    /// Verifies that operations can be interleaved with snapshots without corruption.
    /// </summary>
    [Fact]
    public async Task InterleavedOperationsAndSnapshots_NoDataCorruption()
    {
        // Arrange
        var tasks = new Task[10];

        // Act - interleave operations and snapshots
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() =>
            {
                if (index % 2 == 0)
                {
                    // Even indices: perform operations
                    for (int j = 0; j < 100; j++)
                    {
                        _aggregator.IncrementHits();
                    }
                }
                else
                {
                    // Odd indices: get snapshots
                    for (int j = 0; j < 10; j++)
                    {
                        var stats = _aggregator.GetStatistics();
                        stats.Should().NotBeNull();
                    }
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert - final count should be reasonable
        var finalStats = _aggregator.GetStatistics();
        finalStats.Hits.Should().BeGreaterOrEqualTo(500); // At least 500 hits from even tasks
        finalStats.TotalOperations.Should().BeGreaterOrEqualTo(500);
    }

    /// <summary>
    /// Verifies that the aggregator can be used after Reset without issues.
    /// </summary>
    [Fact]
    public void OperationsAfterReset_WorkCorrectly()
    {
        // Arrange - populate and reset
        for (int i = 0; i < 50; i++)
        {
            _aggregator.IncrementHits();
        }
        _aggregator.Reset();

        // Act - perform new operations after reset
        for (int i = 0; i < 75; i++)
        {
            _aggregator.IncrementMisses();
        }

        // Assert
        var stats = _aggregator.GetStatistics();
        stats.Hits.Should().Be(0); // Reset should have cleared hits
        stats.Misses.Should().Be(75);
        stats.TotalOperations.Should().Be(75);
        stats.HitRate.Should().Be(0.0);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        // Reset to clean state for other tests
        _aggregator.Reset();
    }
}
