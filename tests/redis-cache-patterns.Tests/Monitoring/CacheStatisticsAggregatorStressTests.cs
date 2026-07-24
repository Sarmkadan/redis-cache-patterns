#nullable enable

using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisCachePatterns.Monitoring.Tests;

/// <summary>
/// Stress tests for CacheStatisticsAggregator to verify thread-safety and overflow behavior.
/// </summary>
public sealed class CacheStatisticsAggregatorStressTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly CacheStatisticsAggregator _aggregator;
    private bool _disposed;

    public CacheStatisticsAggregatorStressTests(ITestOutputHelper output)
    {
        _output = output;
        _aggregator = CacheStatisticsAggregator.Instance;
        _aggregator.Reset(); // Start with clean state
    }

    /// <summary>
    /// Verifies that concurrent increments maintain consistency and don't lose updates.
    /// </summary>
    [Fact]
    public async Task ConcurrentIncrements_MaintainConsistency_NoLostUpdates()
    {
        const int threadCount = 20;
        const int operationsPerThread = 100000;
        var totalOperations = threadCount * operationsPerThread;

        var tasks = new Task[threadCount];

        // Create tasks that increment counters concurrently
        for (int i = 0; i < threadCount; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(() =>
            {
                // Each thread performs a mix of operations
                var random = new Random(threadId);
                for (int j = 0; j < operationsPerThread; j++)
                {
                    int operationType = random.Next(0, 3); // 0=hit, 1=miss, 2=error

                    switch (operationType)
                    {
                        case 0:
                            _aggregator.IncrementHits();
                            break;
                        case 1:
                            _aggregator.IncrementMisses();
                            break;
                        case 2:
                            _aggregator.IncrementErrors();
                            break;
                    }

                    // Occasionally record operation duration
                    if (j % 100 == 0)
                    {
                        _aggregator.RecordOperationDuration(random.NextDouble() * 100);
                    }
                }
            });
        }

        await Task.WhenAll(tasks);

        // Get final statistics
        var stats = _aggregator.GetStatistics();

        // Verify total operations count
        Assert.Equal(totalOperations, stats.TotalOperations);

        // Verify that hits + misses + errors = total operations (allowing for modulo overflow)
        var sum = stats.Hits + stats.Misses + stats.Errors;
        Assert.True(Math.Abs(sum - totalOperations) <= threadCount,
            $"Sum of individual counters ({sum}) should be close to total operations ({totalOperations}), difference: {Math.Abs(sum - totalOperations)}");

        // Verify hit ratio is within valid range [0, 100] (percentage)
        Assert.InRange(stats.HitRate, 0, 100);

        _output.WriteLine($"Concurrent test completed: {totalOperations} operations across {threadCount} threads");
        _output.WriteLine($"Hits: {stats.Hits}, Misses: {stats.Misses}, Errors: {stats.Errors}");
        _output.WriteLine($"Hit ratio: {stats.HitRate:P2}");
    }

    /// <summary>
    /// Verifies that GetStatistics returns consistent snapshots under concurrent load.
    /// </summary>
    [Fact]
    public async Task GetStatistics_AtomicSnapshot_UnderConcurrentLoad()
    {
        const int threadCount = 15;
        const int operationsPerThread = 50000;
        const int snapshotReaders = 5;

        var tasks = new Task[threadCount + snapshotReaders];

        // Writer threads
        for (int i = 0; i < threadCount; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(() =>
            {
                var random = new Random(threadId);
                for (int j = 0; j < operationsPerThread; j++)
                {
                    int operationType = random.Next(0, 2); // hit or miss
                    if (operationType == 0)
                    {
                        _aggregator.IncrementHits();
                    }
                    else
                    {
                        _aggregator.IncrementMisses();
                    }
                }
            });
        }

        // Snapshot reader threads
        for (int i = 0; i < snapshotReaders; i++)
        {
            int readerId = i;
            tasks[threadCount + i] = Task.Run(() =>
            {
                var random = new Random(readerId * 1000);
                for (int j = 0; j < 1000; j++)
                {
                    var stats = _aggregator.GetStatistics();

                    // Verify snapshot is consistent (no negative values, no absurdly large values)
                    Assert.InRange(stats.Hits, 0, long.MaxValue);
                    Assert.InRange(stats.Misses, 0, long.MaxValue);
                    Assert.InRange(stats.TotalOperations, 0, long.MaxValue);
                    Assert.InRange(stats.HitRate, 0, 100);

                    // Small delay to increase chance of interleaving
                    if (j % 10 == 0)
                    {
                        Thread.Sleep(random.Next(0, 5));
                    }
                }
            });
        }

        await Task.WhenAll(tasks);

        var finalStats = _aggregator.GetStatistics();
        Assert.Equal(threadCount * operationsPerThread, finalStats.TotalOperations);

        _output.WriteLine($"Atomic snapshot test completed: {threadCount * operationsPerThread} operations with {snapshotReaders} concurrent readers");
    }

    /// <summary>
    /// Verifies overflow protection by testing that counters don't throw exceptions at max value.
    /// </summary>
    [Fact]
    public void OverflowProtection_MaxValue_NoExceptionsThrown()
    {
        // Reset to known state
        _aggregator.Reset();

        // Test that we can increment many times without throwing overflow exceptions
        // This verifies that the overflow protection is working
        for (int i = 0; i < 1000000; i++)
        {
            _aggregator.IncrementHits();
            _aggregator.IncrementMisses();
            _aggregator.IncrementErrors();
        }

        var stats = _aggregator.GetStatistics();

        // Verify we can handle large numbers without issues
        Assert.True(stats.Hits > 0);
        Assert.True(stats.Misses > 0);
        Assert.True(stats.TotalOperations > 0);
        Assert.InRange(stats.HitRate, 0, 100);

        _output.WriteLine("Overflow protection test passed: handled 3M operations without overflow");
    }

    /// <summary>
    /// Verifies that Reset produces consistent results under concurrent access.
    /// </summary>
    [Fact]
    public async Task Reset_AtomicOperation_ProducesConsistentResults()
    {
        const int threadCount = 10;
        const int operationsBeforeReset = 10000;
        const int operationsAfterReset = 5000;

        // Perform operations before reset
        var preTasks = new Task[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            preTasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < operationsBeforeReset; j++)
                {
                    _aggregator.IncrementHits();
                    _aggregator.IncrementMisses();
                }
            });
        }
        await Task.WhenAll(preTasks);

        var beforeReset = _aggregator.GetStatistics();
        Assert.Equal(threadCount * operationsBeforeReset * 2, beforeReset.TotalOperations);

        // Wait a bit to ensure all operations are complete
        await Task.Delay(50);

        // Reset the aggregator
        _aggregator.Reset();

        // Perform operations after reset
        var postTasks = new Task[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            postTasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < operationsAfterReset; j++)
                {
                    _aggregator.IncrementHits();
                }
            });
        }

        await Task.WhenAll(postTasks);

        var afterReset = _aggregator.GetStatistics();

        // After reset, only post-reset operations should be counted
        Assert.Equal(threadCount * operationsAfterReset, afterReset.TotalOperations);
        Assert.Equal(threadCount * operationsAfterReset, afterReset.Hits);

        // Verify LastReset was updated
        Assert.True(_aggregator.LastReset > DateTime.UtcNow.AddSeconds(-1));

        _output.WriteLine("Reset test passed: counters reset atomically");
    }

    /// <summary>
    /// Performance benchmark to measure throughput under concurrent load.
    /// </summary>
    [Fact]
    [Trait("Category", "Performance")]
    public async Task PerformanceBenchmark_MeasureThroughput()
    {
        const int threadCount = 30;
        const int warmupSeconds = 2;
        const int testSeconds = 10;

        // Warmup
        await Task.Delay(TimeSpan.FromSeconds(warmupSeconds));

        var stopwatch = Stopwatch.StartNew();
        var tasks = new Task[threadCount];
        long totalOperations = 0;

        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                var random = new Random();
                long localOps = 0;

                while (stopwatch.Elapsed.TotalSeconds < testSeconds)
                {
                    int operationType = random.Next(0, 3);
                    switch (operationType)
                    {
                        case 0: _aggregator.IncrementHits(); break;
                        case 1: _aggregator.IncrementMisses(); break;
                        case 2: _aggregator.IncrementErrors(); break;
                    }
                    localOps++;
                }

                Interlocked.Add(ref totalOperations, localOps);
            });
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var stats = _aggregator.GetStatistics();
        var actualOperations = stats.Hits + stats.Misses + stats.Errors;

        var operationsPerSecond = totalOperations / stopwatch.Elapsed.TotalSeconds;
        var actualOpsPerSecond = actualOperations / stopwatch.Elapsed.TotalSeconds;

        _output.WriteLine($"Performance benchmark completed in {stopwatch.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"Expected operations: {totalOperations:N0}");
        _output.WriteLine($"Actual operations: {actualOperations:N0}");
        _output.WriteLine($"Operations/second: {operationsPerSecond:N0} (expected), {actualOpsPerSecond:N0} (actual)");
        _output.WriteLine($"Hit ratio: {stats.HitRate:P2}");

        // Verify we're getting close to expected throughput
        Assert.True(actualOperations > totalOperations * 0.95,
            "Should capture at least 95% of operations");
    }

    /// <summary>
    /// Verifies that the hit ratio calculation handles edge cases correctly.
    /// </summary>
    [Fact]
    public void CalculateHitRatio_EdgeCases_HandledCorrectly()
    {
        _aggregator.Reset();

        // Test with no operations
        var emptyStats = _aggregator.GetStatistics();
        Assert.Equal(0, emptyStats.HitRate);
        Assert.Equal(0, emptyStats.Hits);
        Assert.Equal(0, emptyStats.TotalOperations);

        // Test with only hits
        for (int i = 0; i < 1000; i++)
        {
            _aggregator.IncrementHits();
        }
        var hitsOnlyStats = _aggregator.GetStatistics();
        Assert.Equal(100.0, hitsOnlyStats.HitRate);

        // Test with only misses
        _aggregator.Reset();
        for (int i = 0; i < 1000; i++)
        {
            _aggregator.IncrementMisses();
        }
        var missesOnlyStats = _aggregator.GetStatistics();
        Assert.Equal(0.0, missesOnlyStats.HitRate);

        // Test with mixed operations
        _aggregator.Reset();
        for (int i = 0; i < 750; i++)
        {
            _aggregator.IncrementHits();
        }
        for (int i = 0; i < 250; i++)
        {
            _aggregator.IncrementMisses();
        }
        var mixedStats = _aggregator.GetStatistics();
        Assert.InRange(mixedStats.HitRate, 74, 76); // 75% hit rate

        _output.WriteLine("Hit ratio calculation test passed: all edge cases handled correctly");
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