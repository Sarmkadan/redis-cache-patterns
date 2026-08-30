#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Moq;
using RedisCachePatterns.Infrastructure.Cache;
using RedisCachePatterns.Services;
using StackExchange.Redis;
using Xunit;

namespace RedisCachePatterns.Tests.Services;

/// <summary>
/// Unit tests for <see cref="CacheTagService"/>.
/// </summary>
public class CacheTagServiceTests
{
    private readonly Mock<IRedisConnection> _mockRedis = new();
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly Mock<IDatabase> _mockDatabase = new();
    private readonly CacheTagService _sut;

    /// <summary>
    /// Initializes mocks and the service under test.
    /// </summary>
    public CacheTagServiceTests()
    {
        _mockRedis.Setup(redis => redis.GetDatabase(It.IsAny<int>()))
            .Returns(_mockDatabase.Object);
        _sut = new CacheTagService(_mockRedis.Object, _mockCache.Object);
    }

    /// <summary>
    /// Verifies that the constructor rejects a null Redis connection.
    /// </summary>
    [Fact]
    public void Constructor_WithNullRedis_ThrowsArgumentNullException()
    {
        var act = () => new CacheTagService(null!, _mockCache.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("redis");
    }

    /// <summary>
    /// Verifies that the constructor rejects a null cache service.
    /// </summary>
    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        var act = () => new CacheTagService(_mockRedis.Object, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cache");
    }

    /// <summary>
    /// Verifies that a value is cached and its key is added to every requested tag set.
    /// </summary>
    [Fact]
    public async Task SetWithTagsAsync_WritesValueAndAddsKeyToEveryTagSet()
    {
        const string key = "user:123";
        var value = new { Id = 123, Name = "Ada" };
        var expiration = TimeSpan.FromMinutes(30);

        await _sut.SetWithTagsAsync(key, value, new[] { "users", "active" }, expiration);

        _mockCache.Verify(cache => cache.SetAsync(key, value, expiration), Times.Once);
        _mockDatabase.Verify(database => database.SetAddAsync(
            "cache:tags:users", key, CommandFlags.None), Times.Once);
        _mockDatabase.Verify(database => database.SetAddAsync(
            "cache:tags:active", key, CommandFlags.None), Times.Once);
    }

    /// <summary>
    /// Verifies key argument validation when tagging a key.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task TagKeyAsync_WithNullOrEmptyKey_ThrowsArgumentException(string? key)
    {
        var act = () => _sut.TagKeyAsync(key!, "users");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Verifies tag argument validation when tagging a key.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task TagKeyAsync_WithNullOrEmptyTag_ThrowsArgumentException(string? tag)
    {
        var act = () => _sut.TagKeyAsync("user:123", tag!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Verifies that tagging performs SADD against the expected Redis set.
    /// </summary>
    [Fact]
    public async Task TagKeyAsync_AddsKeyToTagSet()
    {
        await _sut.TagKeyAsync("product:456", "products");

        _mockDatabase.Verify(database => database.SetAddAsync(
            "cache:tags:products", "product:456", CommandFlags.None), Times.Once);
    }

    /// <summary>
    /// Verifies key argument validation when removing a tag from a key.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task UntagKeyAsync_WithNullOrEmptyKey_ThrowsArgumentException(string? key)
    {
        var act = () => _sut.UntagKeyAsync(key!, "orders");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Verifies tag argument validation when removing a tag from a key.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task UntagKeyAsync_WithNullOrEmptyTag_ThrowsArgumentException(string? tag)
    {
        var act = () => _sut.UntagKeyAsync("order:789", tag!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Verifies that untagging performs SREM and returns the Redis result.
    /// </summary>
    [Fact]
    public async Task UntagKeyAsync_RemovesKeyFromTagSetAndReturnsResult()
    {
        _mockDatabase.Setup(database => database.SetRemoveAsync(
                "cache:tags:orders", "order:789", CommandFlags.None))
            .ReturnsAsync(true);

        var result = await _sut.UntagKeyAsync("order:789", "orders");

        result.Should().BeTrue();
        _mockDatabase.Verify(database => database.SetRemoveAsync(
            "cache:tags:orders", "order:789", CommandFlags.None), Times.Once);
    }

    /// <summary>
    /// Verifies that all SMEMBERS results are returned as cache keys.
    /// </summary>
    [Fact]
    public async Task GetKeysByTagAsync_ReturnsMembers()
    {
        _mockDatabase.Setup(database => database.SetMembersAsync(
                "cache:tags:users", CommandFlags.None))
            .ReturnsAsync(new RedisValue[] { "user:1", "user:2", "user:3" });

        var result = await _sut.GetKeysByTagAsync("users");

        result.Should().Equal("user:1", "user:2", "user:3");
        _mockDatabase.Verify(database => database.SetMembersAsync(
            "cache:tags:users", CommandFlags.None), Times.Once);
    }

    /// <summary>
    /// Verifies that invalidation executes the atomic member-key removal script for the tag set.
    /// </summary>
    [Fact]
    public async Task InvalidateTagAsync_RemovesMemberKeysAndReturnsCount()
    {
        _mockDatabase.Setup(database => database.ScriptEvaluateAsync(
                It.IsAny<LuaScript>(),
                It.IsAny<object>(),
                CommandFlags.None))
            .ReturnsAsync(RedisResult.Create(3));

        var result = await _sut.InvalidateTagAsync("users");

        result.Should().Be(3);
        _mockDatabase.Verify(database => database.ScriptEvaluateAsync(
            It.IsAny<LuaScript>(),
            It.Is<object>(parameters =>
                ((RedisKey)parameters.GetType().GetProperty("tagKey")!.GetValue(parameters)!) ==
                (RedisKey)"cache:tags:users"),
            CommandFlags.None), Times.Once);
    }
}
