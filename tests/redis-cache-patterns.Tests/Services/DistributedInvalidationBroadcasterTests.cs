#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Configuration;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Infrastructure.Cache;
using RedisCachePatterns.Services;
using StackExchange.Redis;
using Xunit;

namespace RedisCachePatterns.Tests.Services;

/// <summary>
/// Unit tests for <see cref="DistributedInvalidationBroadcaster"/> class.
/// Tests the pub/sub message broadcasting and receiving functionality.
/// </summary>
public class DistributedInvalidationBroadcasterTests
{
    private readonly Mock<IRedisConnection> _mockRedisConnection = new();
    private readonly Mock<ICacheService> _mockCacheService = new();
    private readonly Mock<ILogger<DistributedInvalidationBroadcaster>> _mockLogger = new();
    private readonly Mock<IRedisStreamInvalidationService> _mockStreamService = new();
    private readonly DistributedInvalidationOptions _options = new();
    private readonly DistributedInvalidationBroadcaster _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedInvalidationBroadcasterTests"/> class.
    /// </summary>
    public DistributedInvalidationBroadcasterTests()
    {
        _options.PubSubChannel = "test:invalidation:broadcast";
        _sut = new DistributedInvalidationBroadcaster(
            _mockRedisConnection.Object,
            _mockCacheService.Object,
            _mockLogger.Object,
            _options,
            _mockStreamService.Object);
    }

    /// <summary>
    /// Verifies that BroadcastAsync publishes a message with the correct channel and payload.
    /// </summary>
    [Fact]
    public async Task BroadcastAsync_PublishesExpectedChannelAndMessage()
    {
        // Arrange
        var cacheKey = "product:123";
        var testEventId = Guid.NewGuid().ToString();

        var mockSubscriber = new Mock<ISubscriber>();
        _mockRedisConnection.Setup(c => c.GetConnection())
            .Returns(new Mock<IConnectionMultiplexer>().Object);
        _mockRedisConnection.Setup(c => c.GetConnection().GetSubscriber())
            .Returns(mockSubscriber.Object);

        (RedisChannel channel, RedisValue message) publishedMessage = default;
        mockSubscriber.Setup(s => s.PublishAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisChannel, RedisValue, CommandFlags>((channel, message, flags) =>
            {
                publishedMessage = (channel, message);
            })
            .ReturnsAsync(5L); // 5 nodes notified

        // Act
        await _sut.BroadcastAsync(cacheKey, InvalidationReason.DataUpdate, "test-service");

        // Assert
        mockSubscriber.Verify(s => s.PublishAsync(
            RedisChannel.Literal(_options.PubSubChannel),
            It.IsAny<RedisValue>(),
            It.IsAny<CommandFlags>()),
            Times.Once);

        publishedMessage.Should().NotBe(default);
        publishedMessage.channel.ToString().Should().Be(_options.PubSubChannel);

        var payload = publishedMessage.message.ToString();
        payload.Should().NotBeNullOrWhiteSpace();

        var deserialized = JsonSerializer.Deserialize<CacheInvalidationEvent>(payload);
        deserialized.Should().NotBeNull();
        deserialized!.CacheKey.Should().Be(cacheKey);
        deserialized.Reason.Should().Be(InvalidationReason.DataUpdate);
        deserialized.Source.Should().Be("test-service");
        deserialized.EventId.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Verifies that BroadcastAsync publishes to stream fallback when configured.
    /// </summary>
    [Fact]
    public async Task BroadcastAsync_PublishesToStreamFallback_WhenConfigured()
    {
        // Arrange
        var options = new DistributedInvalidationOptions { UseStreamFallback = true };
        var sut = new DistributedInvalidationBroadcaster(
            _mockRedisConnection.Object,
            _mockCacheService.Object,
            _mockLogger.Object,
            options,
            _mockStreamService.Object);

        var mockSubscriber = new Mock<ISubscriber>();
        _mockRedisConnection.Setup(c => c.GetConnection())
            .Returns(new Mock<IConnectionMultiplexer>().Object);
        _mockRedisConnection.Setup(c => c.GetConnection().GetSubscriber())
            .Returns(mockSubscriber.Object);

        mockSubscriber.Setup(s => s.PublishAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(3L);

        var cacheKey = "user:456";
        var testEvent = new CacheInvalidationEvent
        {
            CacheKey = cacheKey,
            Reason = InvalidationReason.DataDelete,
            Source = "user-service"
        };

        // Act
        await sut.BroadcastAsync(cacheKey, InvalidationReason.DataDelete, "user-service");

        // Assert
        _mockStreamService.Verify(s => s.PublishAsync(
            It.Is<CacheInvalidationEvent>(e => e.CacheKey == cacheKey && e.Reason == InvalidationReason.DataDelete),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that BroadcastAsync does not publish to stream when UseStreamFallback is false.
    /// </summary>
    [Fact]
    public async Task BroadcastAsync_DoesNotPublishToStream_WhenUseStreamFallbackIsFalse()
    {
        // Arrange
        var options = new DistributedInvalidationOptions { UseStreamFallback = false };
        var sut = new DistributedInvalidationBroadcaster(
            _mockRedisConnection.Object,
            _mockCacheService.Object,
            _mockLogger.Object,
            options,
            _mockStreamService.Object);

        var mockSubscriber = new Mock<ISubscriber>();
        _mockRedisConnection.Setup(c => c.GetConnection())
            .Returns(new Mock<IConnectionMultiplexer>().Object);
        _mockRedisConnection.Setup(c => c.GetConnection().GetSubscriber())
            .Returns(mockSubscriber.Object);

        mockSubscriber.Setup(s => s.PublishAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(2L);

        // Act
        await sut.BroadcastAsync("order:789", InvalidationReason.ManualPurge, "admin-service");

        // Assert
        _mockStreamService.Verify(s => s.PublishAsync(
            It.IsAny<CacheInvalidationEvent>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Verifies that BroadcastAsync does not publish to stream when streamService is null.
    /// </summary>
    [Fact]
    public async Task BroadcastAsync_DoesNotPublishToStream_WhenStreamServiceIsNull()
    {
        // Arrange
        var options = new DistributedInvalidationOptions { UseStreamFallback = true };
        var sut = new DistributedInvalidationBroadcaster(
            _mockRedisConnection.Object,
            _mockCacheService.Object,
            _mockLogger.Object,
            options,
            streamService: null);

        var mockSubscriber = new Mock<ISubscriber>();
        _mockRedisConnection.Setup(c => c.GetConnection())
            .Returns(new Mock<IConnectionMultiplexer>().Object);
        _mockRedisConnection.Setup(c => c.GetConnection().GetSubscriber())
            .Returns(mockSubscriber.Object);

        mockSubscriber.Setup(s => s.PublishAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(1L);

        // Act
        await sut.BroadcastAsync("session:abc", InvalidationReason.ConfigurationChange, "config-service");

        // Assert
        _mockStreamService.Verify(s => s.PublishAsync(
            It.IsAny<CacheInvalidationEvent>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Verifies that BroadcastPatternAsync publishes a message with the correct channel and payload.
    /// </summary>
    [Fact]
    public async Task BroadcastPatternAsync_PublishesExpectedChannelAndMessage()
    {
        // Arrange
        var keyPattern = "user:*";

        var mockSubscriber = new Mock<ISubscriber>();
        _mockRedisConnection.Setup(c => c.GetConnection())
            .Returns(new Mock<IConnectionMultiplexer>().Object);
        _mockRedisConnection.Setup(c => c.GetConnection().GetSubscriber())
            .Returns(mockSubscriber.Object);

        (RedisChannel channel, RedisValue message) publishedMessage = default;
        mockSubscriber.Setup(s => s.PublishAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisChannel, RedisValue, CommandFlags>((channel, message, flags) =>
            {
                publishedMessage = (channel, message);
            })
            .ReturnsAsync(3L); // 3 nodes notified

        // Act
        await _sut.BroadcastPatternAsync(keyPattern, InvalidationReason.DataUpdate, "test-service");

        // Assert
        mockSubscriber.Verify(s => s.PublishAsync(
            RedisChannel.Literal(_options.PubSubChannel),
            It.IsAny<RedisValue>(),
            It.IsAny<CommandFlags>()),
            Times.Once);

        publishedMessage.Should().NotBe(default);
        publishedMessage.channel.ToString().Should().Be(_options.PubSubChannel);

        var payload = publishedMessage.message.ToString();
        payload.Should().NotBeNullOrWhiteSpace();

        var deserialized = JsonSerializer.Deserialize<CacheInvalidationEvent>(payload);
        deserialized.Should().NotBeNull();
        deserialized!.KeyPattern.Should().Be(keyPattern);
        deserialized.Reason.Should().Be(InvalidationReason.DataUpdate);
        deserialized.Source.Should().Be("test-service");
        deserialized.EventId.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Verifies that BroadcastAsync records history entry with correct details.
    /// </summary>
    [Fact]
    public async Task BroadcastAsync_RecordsHistoryEntryWithCorrectDetails()
    {
        // Arrange
        var cacheKey = "test:key";
        var eventId = Guid.NewGuid().ToString();

        var mockSubscriber = new Mock<ISubscriber>();
        _mockRedisConnection.Setup(c => c.GetConnection())
            .Returns(new Mock<IConnectionMultiplexer>().Object);
        _mockRedisConnection.Setup(c => c.GetConnection().GetSubscriber())
            .Returns(mockSubscriber.Object);

        mockSubscriber.Setup(s => s.PublishAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(7L);

        // Act
        await _sut.BroadcastAsync(cacheKey, InvalidationReason.DependencyChange, "dependency-service");

        // Assert - verify history contains the entry
        var history = _sut.GetHistory();
        history.Should().NotBeEmpty();
        history.Should().HaveCount(1);

        var entry = history[0];
        entry.EventId.Should().NotBeNullOrWhiteSpace();
        entry.CacheKey.Should().Be(cacheKey);
        entry.KeyPattern.Should().BeNull();
        entry.Reason.Should().Be(InvalidationReason.DependencyChange);
        entry.Source.Should().Be("dependency-service");
        entry.NodesNotified.Should().Be(7L);
        entry.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Verifies that history is capped at MaxHistorySize.
    /// </summary>
    [Fact]
    public async Task BroadcastAsync_HistoryIsCappedAtMaxHistorySize()
    {
        // Arrange
        var options = new DistributedInvalidationOptions { MaxHistorySize = 3 };
        var sut = new DistributedInvalidationBroadcaster(
            _mockRedisConnection.Object,
            _mockCacheService.Object,
            _mockLogger.Object,
            options,
            _mockStreamService.Object);

        var mockSubscriber = new Mock<ISubscriber>();
        _mockRedisConnection.Setup(c => c.GetConnection())
            .Returns(new Mock<IConnectionMultiplexer>().Object);
        _mockRedisConnection.Setup(c => c.GetConnection().GetSubscriber())
            .Returns(mockSubscriber.Object);

        mockSubscriber.Setup(s => s.PublishAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(1L);

        // Act - broadcast 5 times (more than MaxHistorySize)
        await sut.BroadcastAsync("key:1");
        await sut.BroadcastAsync("key:2");
        await sut.BroadcastAsync("key:3");
        await sut.BroadcastAsync("key:4");
        await sut.BroadcastAsync("key:5");

        // Assert - history should only contain last 3 entries
        var history = sut.GetHistory();
        history.Should().HaveCount(3);
        history[0].CacheKey.Should().Be("key:3");
        history[1].CacheKey.Should().Be("key:4");
        history[2].CacheKey.Should().Be("key:5");
    }

    /// <summary>
    /// Verifies that BroadcastAsync throws when cacheKey is null or whitespace.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BroadcastAsync_ThrowsWhenCacheKeyIsInvalid(string? invalidKey)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.BroadcastAsync(invalidKey!));
    }

    /// <summary>
    /// Verifies that BroadcastPatternAsync throws when keyPattern is null or whitespace.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BroadcastPatternAsync_ThrowsWhenKeyPatternIsInvalid(string? invalidPattern)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.BroadcastPatternAsync(invalidPattern!));
    }

    /// <summary>
    /// Verifies that BroadcastAsync handles exceptions during publish and logs them.
    /// </nsummary>
    [Fact]
    public async Task BroadcastAsync_HandlesPublishExceptionAndLogsError()
    {
        // Arrange
        var cacheKey = "error:test";
        var mockSubscriber = new Mock<ISubscriber>();

        _mockRedisConnection.Setup(c => c.GetConnection())
            .Returns(new Mock<IConnectionMultiplexer>().Object);
        _mockRedisConnection.Setup(c => c.GetConnection().GetSubscriber())
            .Returns(mockSubscriber.Object);

        mockSubscriber.Setup(s => s.PublishAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        // Act & Assert
        await Assert.ThrowsAsync<RedisConnectionException>(() => _sut.BroadcastAsync(cacheKey));

        // Verify error was logged
        _mockLogger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to broadcast invalidation event")),
            It.IsAny<RedisException>(),
            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)!),
            Times.Once);
    }

    /// <summary>
    /// Verifies that SubscribeAsync subscribes to the correct channel.
    /// </summary>
    [Fact]
    public async Task SubscribeAsync_SubscribesToCorrectChannel()
    {
        // Arrange
        var mockSubscriber = new Mock<ISubscriber>();
        _mockRedisConnection.Setup(c => c.GetConnection())
            .Returns(new Mock<IConnectionMultiplexer>().Object);
        _mockRedisConnection.Setup(c => c.GetConnection().GetSubscriber())
            .Returns(mockSubscriber.Object);

        // Act
        await _sut.SubscribeAsync();

        // Assert
        mockSubscriber.Verify(s => s.SubscribeAsync(
            RedisChannel.Literal(_options.PubSubChannel),
            It.IsAny<Action<RedisChannel, RedisValue>>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that SubscribeAsync handles connection failures and logs them.
    /// </summary>
    [Fact]
    public async Task SubscribeAsync_HandlesConnectionFailureAndLogsError()
    {
        // Arrange
        _mockRedisConnection.Setup(c => c.GetConnection().GetSubscriber())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        // Act & Assert
        await Assert.ThrowsAsync<RedisConnectionException>(() => _sut.SubscribeAsync());

        // Verify error was logged
        _mockLogger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to subscribe to invalidation channel")),
            It.IsAny<RedisException>(),
            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)!),
            Times.Once);
    }
}
