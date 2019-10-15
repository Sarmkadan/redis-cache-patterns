#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace RedisCachePatterns.Benchmarks;

/// <summary>
/// Benchmark class for distributed lock operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class DistributedLockBenchmarks
{
    /// <summary>
    /// The cache service instance used for benchmarking.
    /// </summary>
    private ICacheService _cacheService = null!;

    /// <summary>
    /// The key used for acquiring and releasing the lock.
    /// </summary>
    private const string LockKey = "lock:test";

    /// <summary>
    /// The value used for acquiring and releasing the lock.
    /// </summary>
    private const string LockValue = "test-value";

    /// <summary>
    /// Sets up the benchmark by creating a mock cache service.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        // Use a mock cache service as we can't easily benchmark Redis in this environment
        var mockCache = new Mock<ICacheService>();
        mockCache.Setup(c => c.AcquireLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                 .ReturnsAsync(true);
        mockCache.Setup(c => c.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<string>()))
                 .ReturnsAsync(true);
        _cacheService = mockCache.Object;
    }

    /// <summary>
    /// Acquires a lock with the specified key, value, and timeout.
    /// </summary>
    /// <returns>A task representing the result of the lock acquisition.</returns>
    [Benchmark(Description = "Acquire lock")]
    public Task<bool> AcquireLock() => _cacheService.AcquireLockAsync(LockKey, LockValue, TimeSpan.FromSeconds(10));

    /// <summary>
    /// Releases a lock with the specified key and value.
    /// </summary>
    /// <returns>A task representing the result of the lock release.</returns>
    [Benchmark(Description = "Release lock")]
    public Task<bool> ReleaseLock() => _cacheService.ReleaseLockAsync(LockKey, LockValue);
}
