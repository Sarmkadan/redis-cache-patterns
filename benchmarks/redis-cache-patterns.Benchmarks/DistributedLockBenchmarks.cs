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

[MemoryDiagnoser]
[SimpleJob]
public class DistributedLockBenchmarks
{
    private ICacheService _cacheService = null!;
    private const string LockKey = "lock:test";
    private const string LockValue = "test-value";

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

    [Benchmark(Description = "Acquire lock")]
    public Task<bool> AcquireLock() => _cacheService.AcquireLockAsync(LockKey, LockValue, TimeSpan.FromSeconds(10));

    [Benchmark(Description = "Release lock")]
    public Task<bool> ReleaseLock() => _cacheService.ReleaseLockAsync(LockKey, LockValue);
}
