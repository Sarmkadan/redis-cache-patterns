#nullable enable
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Infrastructure.Cache;
using RedisCachePatterns.Services;
using StackExchange.Redis;
using Xunit;

namespace RedisCachePatterns.Tests.Services;

public class DistributedInvalidationBroadcasterTests
{
    private readonly Mock<IRedisConnection> _mockRedis = new();
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly Mock<ILogger<DistributedInvalidationBroadcaster>> _mockLogger = new();
    private readonly Mock<IConnectionMultiplexer> _mockMultiplexer = new();
    private readonly Mock<ISubscriber> _mockSubscriber = new();

    public DistributedInvalidationBroadcasterTests()
    {
        _mockRedis.Setup(r => r.GetConnection()).Returns(_mockMultiplexer.Object);
        _mockMultiplexer.Setup(m => m.GetSubscriber(null)).Returns(_mockSubscriber.Object);
    }

    private DistributedInvalidationBroadcaster CreateBroadcaster(
        IRedisStreamInvalidationService? streamService = null,
        DistributedInvalidationOptions? options = null)
    {
        return new DistributedInvalidationBroadcaster(
            _mockRedis.Object,
            _mockCache.Object,
            _mockLogger.Object,
            options ?? new DistributedInvalidationOptions { UseStreamFallback = false },
            streamService);
    }

    // ─── BroadcastAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task BroadcastAsync_PublishesToPubSubChannel()
    {
        _mockSubscriber
            .Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(2L);

        var broadcaster = CreateBroadcaster();

        await broadcaster.BroadcastAsync("product:1", InvalidationReason.DataUpdate, "test-service");

        _mockSubscriber.Verify(
            s => s.PublishAsync(
                It.Is<RedisChannel>(c => c == RedisChannel.Literal("cache:invalidation:broadcast")),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastAsync_RecordsHistoryEntry()
    {
        _mockSubscriber
            .Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(3L);

        var broadcaster = CreateBroadcaster();

        await broadcaster.BroadcastAsync("order:42", InvalidationReason.DataDelete, "order-svc");

        var history = broadcaster.GetHistory();
        history.Should().HaveCount(1);
        history[0].CacheKey.Should().Be("order:42");
        history[0].Reason.Should().Be(InvalidationReason.DataDelete);
        history[0].Source.Should().Be("order-svc");
        history[0].NodesNotified.Should().Be(3);
    }

    [Fact]
    public async Task BroadcastAsync_WithEmptyKey_ThrowsArgumentException()
    {
        var broadcaster = CreateBroadcaster();

        Func<Task> act = () => broadcaster.BroadcastAsync(string.Empty);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ─── BroadcastPatternAsync ────────────────────────────────────────────────

    [Fact]
    public async Task BroadcastPatternAsync_RecordsPatternInHistory()
    {
        _mockSubscriber
            .Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1L);

        var broadcaster = CreateBroadcaster();

        await broadcaster.BroadcastPatternAsync("user:*", InvalidationReason.ManualPurge, "admin");

        var history = broadcaster.GetHistory();
        history.Should().HaveCount(1);
        history[0].KeyPattern.Should().Be("user:*");
        history[0].CacheKey.Should().BeNull();
    }

    [Fact]
    public async Task BroadcastPatternAsync_WithEmptyPattern_ThrowsArgumentException()
    {
        var broadcaster = CreateBroadcaster();

        Func<Task> act = () => broadcaster.BroadcastPatternAsync("   ");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ─── History bounding ─────────────────────────────────────────────────────

    [Fact]
    public async Task BroadcastAsync_WhenHistoryExceedsMax_OldestEntriesDropped()
    {
        _mockSubscriber
            .Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1L);

        var broadcaster = CreateBroadcaster(options: new DistributedInvalidationOptions
        {
            MaxHistorySize = 3,
            UseStreamFallback = false
        });

        for (var i = 0; i < 5; i++)
            await broadcaster.BroadcastAsync($"key:{i}");

        var history = broadcaster.GetHistory();
        history.Count.Should().BeLessOrEqualTo(3);
    }

    // ─── Stream fallback ─────────────────────────────────────────────────────

    [Fact]
    public async Task BroadcastAsync_WhenStreamFallbackEnabled_AlsoPublishesToStream()
    {
        _mockSubscriber
            .Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(2L);

        var mockStream = new Mock<IRedisStreamInvalidationService>();
        mockStream
            .Setup(s => s.PublishAsync(It.IsAny<CacheInvalidationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var broadcaster = CreateBroadcaster(
            streamService: mockStream.Object,
            options: new DistributedInvalidationOptions { UseStreamFallback = true });

        await broadcaster.BroadcastAsync("inventory:100");

        mockStream.Verify(
            s => s.PublishAsync(
                It.Is<CacheInvalidationEvent>(e => e.CacheKey == "inventory:100"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
