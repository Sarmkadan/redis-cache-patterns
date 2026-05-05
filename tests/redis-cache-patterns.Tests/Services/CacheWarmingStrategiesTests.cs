#nullable enable
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Services;
using Xunit;

namespace RedisCachePatterns.Tests.Services;

public class CacheWarmingStrategiesTests
{
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly Mock<ILogger<DelegateWarmingStrategy>> _delegateLogger = new();
    private readonly Mock<ILogger<PriorityWarmingStrategy>> _priorityLogger = new();
    private readonly Mock<ILogger<ParallelWarmingStrategy>> _parallelLogger = new();
    private readonly Mock<ILogger<PatternRefreshWarmingStrategy>> _patternLogger = new();

    // ─── DelegateWarmingStrategy ─────────────────────────────────────────────

    [Fact]
    public async Task DelegateWarmingStrategy_WhenAllEntriesHaveValues_WarmsAllKeys()
    {
        var entries = new[]
        {
            new WarmingEntry { Key = "key:1", ValueFactory = () => Task.FromResult<object?>("v1") },
            new WarmingEntry { Key = "key:2", ValueFactory = () => Task.FromResult<object?>("v2") },
        };

        var strategy = new DelegateWarmingStrategy("test", entries, _delegateLogger.Object);

        var warmed = await strategy.ExecuteAsync(_mockCache.Object);

        warmed.Should().Be(2);
        _mockCache.Verify(c => c.SetAsync("key:1", "v1", null), Times.Once);
        _mockCache.Verify(c => c.SetAsync("key:2", "v2", null), Times.Once);
    }

    [Fact]
    public async Task DelegateWarmingStrategy_WhenFactoryReturnsNull_SkipsKey()
    {
        var entries = new[]
        {
            new WarmingEntry { Key = "key:1", ValueFactory = () => Task.FromResult<object?>(null) },
            new WarmingEntry { Key = "key:2", ValueFactory = () => Task.FromResult<object?>("v2") },
        };

        var strategy = new DelegateWarmingStrategy("test", entries, _delegateLogger.Object);

        var warmed = await strategy.ExecuteAsync(_mockCache.Object);

        warmed.Should().Be(1);
        _mockCache.Verify(c => c.SetAsync("key:1", It.IsAny<object>(), It.IsAny<TimeSpan?>()), Times.Never);
        _mockCache.Verify(c => c.SetAsync("key:2", "v2", null), Times.Once);
    }

    [Fact]
    public async Task DelegateWarmingStrategy_WhenFactoryThrows_ContinuesAndReturnsPartialCount()
    {
        var entries = new[]
        {
            new WarmingEntry { Key = "key:bad", ValueFactory = () => throw new InvalidOperationException("source unavailable") },
            new WarmingEntry { Key = "key:ok",  ValueFactory = () => Task.FromResult<object?>("value") },
        };

        var strategy = new DelegateWarmingStrategy("test", entries, _delegateLogger.Object);

        var warmed = await strategy.ExecuteAsync(_mockCache.Object);

        warmed.Should().Be(1);
    }

    // ─── PriorityWarmingStrategy ─────────────────────────────────────────────

    [Fact]
    public async Task PriorityWarmingStrategy_ExecutesCriticalBeforeNormalEntries()
    {
        var executionOrder = new List<string>();

        var critical = new WarmingEntry
        {
            Key = "critical:1",
            Priority = WarmingPriority.Critical,
            ValueFactory = () => { executionOrder.Add("critical"); return Task.FromResult<object?>("c"); }
        };
        var normal = new WarmingEntry
        {
            Key = "normal:1",
            Priority = WarmingPriority.Normal,
            ValueFactory = () => { executionOrder.Add("normal"); return Task.FromResult<object?>("n"); }
        };

        var strategy = new PriorityWarmingStrategy("ordered", _priorityLogger.Object)
            .Add(normal)
            .Add(critical);

        await strategy.ExecuteAsync(_mockCache.Object);

        executionOrder.Should().ContainInOrder("critical", "normal");
    }

    [Fact]
    public async Task PriorityWarmingStrategy_WarmsTotalCountAcrossAllPriorities()
    {
        var strategy = new PriorityWarmingStrategy("multi", _priorityLogger.Object)
            .Add(new WarmingEntry { Key = "h:1", Priority = WarmingPriority.High,   ValueFactory = () => Task.FromResult<object?>("hv") })
            .Add(new WarmingEntry { Key = "l:1", Priority = WarmingPriority.Low,    ValueFactory = () => Task.FromResult<object?>("lv") })
            .Add(new WarmingEntry { Key = "n:1", Priority = WarmingPriority.Normal, ValueFactory = () => Task.FromResult<object?>("nv") });

        var count = await strategy.ExecuteAsync(_mockCache.Object);

        count.Should().Be(3);
    }

    // ─── ParallelWarmingStrategy ─────────────────────────────────────────────

    [Fact]
    public async Task ParallelWarmingStrategy_WarmsAllEntriesConcurrently()
    {
        var entries = Enumerable.Range(1, 10)
            .Select(i => new WarmingEntry
            {
                Key = $"parallel:{i}",
                ValueFactory = () => Task.FromResult<object?>("val")
            })
            .ToList();

        var strategy = new ParallelWarmingStrategy("parallel", entries, _parallelLogger.Object, maxDegreeOfParallelism: 4);

        var count = await strategy.ExecuteAsync(_mockCache.Object);

        count.Should().Be(10);
    }

    [Fact]
    public async Task ParallelWarmingStrategy_WhenSomeEntriesFail_ReturnsSuccessfulCount()
    {
        var entries = new[]
        {
            new WarmingEntry { Key = "ok:1",  ValueFactory = () => Task.FromResult<object?>("v") },
            new WarmingEntry { Key = "bad:1", ValueFactory = () => throw new Exception("oops") },
            new WarmingEntry { Key = "ok:2",  ValueFactory = () => Task.FromResult<object?>("v") },
        };

        var strategy = new ParallelWarmingStrategy("parallel-partial", entries, _parallelLogger.Object);

        var count = await strategy.ExecuteAsync(_mockCache.Object);

        count.Should().Be(2);
    }

    // ─── PatternRefreshWarmingStrategy ───────────────────────────────────────

    [Fact]
    public async Task PatternRefreshWarmingStrategy_RefreshesEachMatchingKey()
    {
        _mockCache
            .Setup(c => c.GetKeysByPatternAsync("product:*"))
            .ReturnsAsync(new[] { "product:1", "product:2" });

        var strategy = new PatternRefreshWarmingStrategy(
            "product-refresh",
            "product:*",
            key => Task.FromResult<object?>(new { Id = key }),
            TimeSpan.FromMinutes(30),
            _patternLogger.Object);

        var count = await strategy.ExecuteAsync(_mockCache.Object);

        count.Should().Be(2);
        _mockCache.Verify(c => c.SetAsync("product:1", It.IsAny<object>(), TimeSpan.FromMinutes(30)), Times.Once);
        _mockCache.Verify(c => c.SetAsync("product:2", It.IsAny<object>(), TimeSpan.FromMinutes(30)), Times.Once);
    }

    [Fact]
    public async Task PatternRefreshWarmingStrategy_WhenPatternScanFails_ReturnsZero()
    {
        _mockCache
            .Setup(c => c.GetKeysByPatternAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Redis unavailable"));

        var strategy = new PatternRefreshWarmingStrategy(
            "fail-refresh",
            "some:*",
            key => Task.FromResult<object?>("v"),
            null,
            _patternLogger.Object);

        var count = await strategy.ExecuteAsync(_mockCache.Object);

        count.Should().Be(0);
    }

    // ─── CacheWarmingScheduler ───────────────────────────────────────────────

    [Fact]
    public void CacheWarmingScheduler_StartTwice_ThrowsInvalidOperationException()
    {
        var warmingSvc = new CacheWarmingService(
            _mockCache.Object,
            Mock.Of<ILogger<CacheWarmingService>>());

        var scheduler = new CacheWarmingScheduler(
            warmingSvc,
            Mock.Of<ILogger<CacheWarmingScheduler>>(),
            interval: TimeSpan.FromHours(1));

        scheduler.Start();

        var act = () => scheduler.Start();
        act.Should().Throw<InvalidOperationException>();

        scheduler.Stop();
        scheduler.Dispose();
    }

    [Fact]
    public void CacheWarmingScheduler_StopBeforeStart_DoesNotThrow()
    {
        var warmingSvc = new CacheWarmingService(
            _mockCache.Object,
            Mock.Of<ILogger<CacheWarmingService>>());

        var scheduler = new CacheWarmingScheduler(
            warmingSvc,
            Mock.Of<ILogger<CacheWarmingScheduler>>(),
            interval: TimeSpan.FromHours(1));

        var act = scheduler.Stop;
        act.Should().NotThrow();
    }
}
