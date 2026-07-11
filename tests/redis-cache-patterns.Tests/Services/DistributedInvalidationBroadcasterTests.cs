#nullable enable
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Configuration;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Infrastructure.Cache;
using RedisCachePatterns.Services;
using StackExchange.Redis;
using Xunit;

/// <summary>
/// Tests for the DistributedInvalidationBroadcaster class.
/// </summary>
namespace RedisCachePatterns.Tests.Services;

public class DistributedInvalidationBroadcasterTests
{
    private readonly Mock<IRedisConnection> _mockRedis = new();
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly Mock<ILogger<DistributedInvalidationBroadcaster>> _mockLogger = new();
    private readonly Mock<IConnectionMultiplexer> _mockMultiplexer = new();
    private readonly Mock<ISubscriber> _mockSubscriber = new();

    /// <summary>
    /// Initializes a new instance of the DistributedInvalidationBroadcasterTests class.
    /// </summary>
    public DistributedInvalidationBroadcasterTests()
    {
        _mockRedis.Setup(r => r.GetConnection()).Returns(_mockMultiplexer.Object);
        _mockMultiplexer.Setup(m => m.GetSubscriber(null)).Returns(_mockSubscriber.Object);
    }

    /// <summary>
    /// Creates a new instance of the DistributedInvalidationBroadcaster class.
    /// </summary>
    /// <param name="streamService">The IRedisStreamInvalidationService instance to use.</param>
    /// <param name="options">The DistributedInvalidationOptions instance to use.</param>
    /// <returns>A new instance of the DistributedInvalidationBroadcaster class.</returns>
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

    /// <summary>
    /// Tests that the BroadcastAsync method publishes to the pub/sub channel.
    /// </summary>
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

    /// <summary>
    /// Tests that the BroadcastAsync method records a history entry.
    /// </summary>
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

    /// <summary>
    /// Tests that the BroadcastAsync method throws an ArgumentException when the key is empty.
    /// </summary>
    [Fact]
    public async Task BroadcastAsync_WithEmptyKey_ThrowsArgumentException()
    {
        var broadcaster = CreateBroadcaster();

        Func<Task> act = () => broadcaster.BroadcastAsync(string.Empty);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ─── BroadcastPatternAsync ────────────────────────────────────────────────

    /// <summary>
    /// Tests that the BroadcastPatternAsync method records a pattern in the history.
    /// </summary>
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

    /// <summary>
    /// Tests that the BroadcastPatternAsync method throws an ArgumentException when the pattern is empty.
    /// </summary>
    [Fact]
    public async Task BroadcastPatternAsync_WithEmptyPattern_ThrowsArgumentException()
    {
        var broadcaster = CreateBroadcaster();

        Func<Task> act = () => broadcaster.BroadcastPatternAsync("   ");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ─── History bounding ─────────────────────────────────────────────────────

    /// <summary>
    /// Tests that the BroadcastAsync method drops the oldest entries when the history exceeds the maximum size.
    /// </summary>
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

    /// <summary>
    /// Tests that the BroadcastAsync method publishes to the stream when the stream fallback is enabled.
    /// </summary>
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
