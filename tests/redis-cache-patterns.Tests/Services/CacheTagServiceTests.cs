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
/// Contains unit tests for the <see cref="CacheTagService"/> class.
/// Tests tagging a key, invalidating a tag removes all tagged keys,
/// a key with multiple tags, and invalidating an unknown tag is a no-op.
/// </summary>
public class CacheTagServiceTests
{
    private readonly Mock<IRedisConnection> _mockRedis = new();
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly Mock<IDatabase> _mockDatabase = new();
    private readonly CacheTagService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheTagServiceTests"/> class,
    /// setting up mocks for Redis connection and cache service.
    /// </summary>
    public CacheTagServiceTests()
    {
        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>())).Returns(_mockDatabase.Object);
        _sut = new CacheTagService(_mockRedis.Object, _mockCache.Object);
    }

    /// <summary>
    /// Verifies that SetWithTagsAsync writes the value via ICacheService.SetAsync
    /// and adds the key to every tag set (SADD).
    /// </summary>
    [Fact]
    public async Task SetWithTagsAsync_WritesValueAndAddsToAllTagSets()
    {
        // Arrange
        const string key = "user:123";
        const string tag1 = "users";
        const string tag2 = "active";
        var value = new { Id = 123, Name = "John Doe" };
        var tags = new[] { tag1, tag2 };
        var expiration = TimeSpan.FromMinutes(30);

        // Act
        await _sut.SetWithTagsAsync(key, value, tags, expiration);

        // Assert - Verify SetAsync was called to write the value
        _mockCache.Verify(c => c.SetAsync(key, value, expiration), Times.Once);

        // Assert - Verify SetAddAsync was called for each tag
        _mockDatabase.Verify(d => d.SetAddAsync(CacheTagService.BuildTagKey(tag1), key, CommandFlags.None), Times.Once);
        _mockDatabase.Verify(d => d.SetAddAsync(CacheTagService.BuildTagKey(tag2), key, CommandFlags.None), Times.Once);
    }

    /// <summary>
    /// Verifies that TagKeyAsync adds an existing cache key to a tag set without rewriting the value.
    /// </summary>
    [Fact]
    public async Task TagKeyAsync_AddsKeyToTagSet()
    {
        // Arrange
        const string key = "product:456";
        const string tag = "products";

        // Act
        await _sut.TagKeyAsync(key, tag);

        // Assert
        _mockDatabase.Verify(d => d.SetAddAsync(CacheTagService.BuildTagKey(tag), key, CommandFlags.None), Times.Once);
    }

    /// <summary>
    /// Verifies that TagKeyAsync throws ArgumentException for null key.
    /// </summary>
    [Fact]
    public async Task TagKeyAsync_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        const string tag = "products";

        // Act & Assert - ArgumentNullException is thrown because ThrowIfNullOrEmpty checks for null first
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.TagKeyAsync(null!, tag));
    }

    /// <summary>
    /// Verifies that TagKeyAsync throws ArgumentException for empty key.
    /// </summary>
    [Fact]
    public async Task TagKeyAsync_WithEmptyKey_ThrowsArgumentException()
    {
        // Arrange
        const string key = "";
        const string tag = "products";

        // Act & Assert - ArgumentException is thrown for empty string
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.TagKeyAsync(key, tag));
    }

    /// <summary>
    /// Verifies that TagKeyAsync throws ArgumentException for null tag.
    /// </summary>
    [Fact]
    public async Task TagKeyAsync_WithNullTag_ThrowsArgumentException()
    {
        // Arrange
        const string key = "product:456";

        // Act & Assert - ArgumentNullException is thrown because ThrowIfNullOrEmpty checks for null first
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.TagKeyAsync(key, null!));
    }

    /// <summary>
    /// Verifies that TagKeyAsync throws ArgumentException for empty tag.
    /// </summary>
    [Fact]
    public async Task TagKeyAsync_WithEmptyTag_ThrowsArgumentException()
    {
        // Arrange
        const string key = "product:456";
        const string tag = "";

        // Act & Assert - ArgumentException is thrown for empty string
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.TagKeyAsync(key, tag));
    }

    /// <summary>
    /// Verifies that UntagKeyAsync removes a key from a tag set and returns true if the key was a member.
    /// </summary>
    [Fact]
    public async Task UntagKeyAsync_RemovesKeyFromTagSet_ReturnsTrueWhenMember()
    {
        // Arrange
        const string key = "order:789";
        const string tag = "orders";
        var tagKey = CacheTagService.BuildTagKey(tag);

        _mockDatabase.Setup(d => d.SetRemoveAsync(tagKey, key, CommandFlags.None))
                   .ReturnsAsync(true);

        // Act
        var result = await _sut.UntagKeyAsync(key, tag);

        // Assert
        result.Should().BeTrue();
        _mockDatabase.Verify(d => d.SetRemoveAsync(tagKey, key, CommandFlags.None), Times.Once);
    }

    /// <summary>
    /// Verifies that UntagKeyAsync returns false when the key was not a member of the tag set.
    /// </summary>
    [Fact]
    public async Task UntagKeyAsync_ReturnsFalseWhenKeyNotMember()
    {
        // Arrange
        const string key = "order:789";
        const string tag = "orders";
        var tagKey = CacheTagService.BuildTagKey(tag);

        _mockDatabase.Setup(d => d.SetRemoveAsync(tagKey, key, CommandFlags.None))
                   .ReturnsAsync(false);

        // Act
        var result = await _sut.UntagKeyAsync(key, tag);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that GetKeysByTagAsync returns all cache keys currently associated with the tag.
    /// </summary>
    [Fact]
    public async Task GetKeysByTagAsync_ReturnsKeysForTag()
    {
        // Arrange
        const string tag = "users";
        var tagKey = CacheTagService.BuildTagKey(tag);
        var expectedKeys = new RedisValue[] { "user:1", "user:2", "user:3" };

        _mockDatabase.Setup(d => d.SetMembersAsync(tagKey, CommandFlags.None))
                   .ReturnsAsync(expectedKeys);

        // Act
        var result = await _sut.GetKeysByTagAsync(tag);

        // Assert
        result.Should().BeEquivalentTo(new[] { "user:1", "user:2", "user:3" });
        _mockDatabase.Verify(d => d.SetMembersAsync(tagKey, CommandFlags.None), Times.Once);
    }

    /// <summary>
    /// Verifies that GetKeysByTagAsync throws ArgumentException for null tag.
    /// </summary>
    [Fact]
    public async Task GetKeysByTagAsync_WithNullTag_ThrowsArgumentException()
    {
        // Act & Assert - ArgumentNullException is thrown because ThrowIfNullOrEmpty checks for null first
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.GetKeysByTagAsync(null!));
    }

    /// <summary>
    /// Verifies that GetKeysByTagAsync throws ArgumentException for empty tag.
    /// </summary>
    [Fact]
    public async Task GetKeysByTagAsync_WithEmptyTag_ThrowsArgumentException()
    {
        // Act & Assert - ArgumentException is thrown for empty string
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetKeysByTagAsync(""));
    }

    /// <summary>
    /// Verifies that InvalidateTagAsync removes every key in the tag set from the cache,
    /// deletes the tag set, and returns the number of keys invalidated.
    /// </summary>
    [Fact]
    public async Task InvalidateTagAsync_RemovesAllKeysAndDeletesTagSet_ReturnsCount()
    {
        // Arrange
        const string tag = "users";
        var tagKey = CacheTagService.BuildTagKey(tag);
        var keysInTag = new RedisValue[] { "user:1", "user:2", "user:3" };

        _mockDatabase.Setup(d => d.SetMembersAsync(tagKey, CommandFlags.None))
                   .ReturnsAsync(keysInTag);

        // Set up RemoveAsync to be called for each key
        foreach (var key in keysInTag.Select(k => (string)k!))
        {
            _mockCache.Setup(c => c.RemoveAsync(key)).Returns(Task.CompletedTask);
        }

        _mockDatabase.Setup(d => d.KeyDeleteAsync(tagKey, CommandFlags.None))
                   .ReturnsAsync(true);

        // Act
        var result = await _sut.InvalidateTagAsync(tag);

        // Assert
        result.Should().Be(3); // 3 keys invalidated

        // Verify all keys were removed from cache
        foreach (var key in keysInTag.Select(k => (string)k!))
        {
            _mockCache.Verify(c => c.RemoveAsync(key), Times.Once);
        }

        // Verify tag set was deleted
        _mockDatabase.Verify(d => d.KeyDeleteAsync(tagKey, CommandFlags.None), Times.Once);
    }

    /// <summary>
    /// Verifies that InvalidateTagAsync throws ArgumentException for null tag.
    /// </summary>
    [Fact]
    public async Task InvalidateTagAsync_WithNullTag_ThrowsArgumentException()
    {
        // Act & Assert - ArgumentNullException is thrown because ThrowIfNullOrEmpty checks for null first
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.InvalidateTagAsync(null!));
    }

    /// <summary>
    /// Verifies that InvalidateTagAsync throws ArgumentException for empty tag.
    /// </summary>
    [Fact]
    public async Task InvalidateTagAsync_WithEmptyTag_ThrowsArgumentException()
    {
        // Act & Assert - ArgumentException is thrown for empty string
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.InvalidateTagAsync(""));
    }

    /// <summary>
    /// Verifies that InvalidateTagAsync handles empty tag set gracefully.
    /// </summary>
    [Fact]
    public async Task InvalidateTagAsync_WithEmptyTagSet_DeletesTagAndReturnsZero()
    {
        // Arrange
        const string tag = "nonexistent";
        var tagKey = CacheTagService.BuildTagKey(tag);
        var emptyKeys = Array.Empty<RedisValue>();

        _mockDatabase.Setup(d => d.SetMembersAsync(tagKey, CommandFlags.None))
                   .ReturnsAsync(emptyKeys);

        _mockDatabase.Setup(d => d.KeyDeleteAsync(tagKey, CommandFlags.None))
                   .ReturnsAsync(true);

        // Act
        var result = await _sut.InvalidateTagAsync(tag);

        // Assert
        result.Should().Be(0); // 0 keys invalidated
        _mockDatabase.Verify(d => d.KeyDeleteAsync(tagKey, CommandFlags.None), Times.Once);
    }

    /// <summary>
    /// Verifies that InvalidateTagsAsync invalidates multiple tags and returns total keys invalidated.
    /// </summary>
    [Fact]
    public async Task InvalidateTagsAsync_InvalidatesMultipleTags_ReturnsTotalCount()
    {
        // Arrange
        var tags = new[] { "users", "active", "premium" };
        var tagKeys = tags.Select(CacheTagService.BuildTagKey).ToArray();

        // Set up each tag to have different keys
        _mockDatabase.Setup(d => d.SetMembersAsync(tagKeys[0], CommandFlags.None))
                   .ReturnsAsync(new RedisValue[] { "user:1", "user:2" });

        _mockDatabase.Setup(d => d.SetMembersAsync(tagKeys[1], CommandFlags.None))
                   .ReturnsAsync(new RedisValue[] { "user:2", "user:3" });

        _mockDatabase.Setup(d => d.SetMembersAsync(tagKeys[2], CommandFlags.None))
                   .ReturnsAsync(new RedisValue[] { "user:4" });

        // Set up RemoveAsync for each unique key
        _mockCache.Setup(c => c.RemoveAsync("user:1")).Returns(Task.CompletedTask);
        _mockCache.Setup(c => c.RemoveAsync("user:2")).Returns(Task.CompletedTask);
        _mockCache.Setup(c => c.RemoveAsync("user:3")).Returns(Task.CompletedTask);
        _mockCache.Setup(c => c.RemoveAsync("user:4")).Returns(Task.CompletedTask);

        _mockDatabase.Setup(d => d.KeyDeleteAsync(tagKeys[0], CommandFlags.None))
                   .ReturnsAsync(true);
        _mockDatabase.Setup(d => d.KeyDeleteAsync(tagKeys[1], CommandFlags.None))
                   .ReturnsAsync(true);
        _mockDatabase.Setup(d => d.KeyDeleteAsync(tagKeys[2], CommandFlags.None))
                   .ReturnsAsync(true);

        // Act
        var result = await _sut.InvalidateTagsAsync(tags);

        // Assert
        result.Should().Be(5); // user:1, user:2, user:2, user:3, user:4 = 5 total invalidations (user:2 appears in 2 tags)

        // Verify all tags were invalidated
        _mockDatabase.Verify(d => d.KeyDeleteAsync(tagKeys[0], CommandFlags.None), Times.Once);
        _mockDatabase.Verify(d => d.KeyDeleteAsync(tagKeys[1], CommandFlags.None), Times.Once);
        _mockDatabase.Verify(d => d.KeyDeleteAsync(tagKeys[2], CommandFlags.None), Times.Once);

        // Verify all keys were removed (user:2 is in 2 tags so removed twice)
        _mockCache.Verify(c => c.RemoveAsync("user:1"), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("user:2"), Times.Exactly(2));
        _mockCache.Verify(c => c.RemoveAsync("user:3"), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("user:4"), Times.Once);
    }

    /// <summary>
    /// Verifies that InvalidateTagsAsync throws ArgumentException for null tags.
    /// </summary>
    [Fact]
    public async Task InvalidateTagsAsync_WithNullTags_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.InvalidateTagsAsync(null!));
    }

    /// <summary>
    /// Verifies that InvalidateTagsAsync handles empty tag list gracefully.
    /// </pio>
    [Fact]
    public async Task InvalidateTagsAsync_WithEmptyTagsList_ReturnsZero()
    {
        // Arrange
        var tags = Array.Empty<string>();

        // Act
        var result = await _sut.InvalidateTagsAsync(tags);

        // Assert
        result.Should().Be(0);
        _mockDatabase.VerifyNoOtherCalls();
        _mockCache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Verifies that invalidating an unknown tag is a no-op.
    /// </summary>
    [Fact]
    public async Task InvalidateTagAsync_UnknownTag_IsNoOp()
    {
        // Arrange
        const string unknownTag = "nonexistent-tag-xyz";
        var tagKey = CacheTagService.BuildTagKey(unknownTag);
        var emptyKeys = Array.Empty<RedisValue>();

        _mockDatabase.Setup(d => d.SetMembersAsync(tagKey, CommandFlags.None))
                   .ReturnsAsync(emptyKeys);

        _mockDatabase.Setup(d => d.KeyDeleteAsync(tagKey, CommandFlags.None))
                   .ReturnsAsync(true);

        // Act
        var result = await _sut.InvalidateTagAsync(unknownTag);

        // Assert - Should return 0 and not throw
        result.Should().Be(0);
        _mockDatabase.Verify(d => d.SetMembersAsync(tagKey, CommandFlags.None), Times.Once);
        _mockDatabase.Verify(d => d.KeyDeleteAsync(tagKey, CommandFlags.None), Times.Once);
        _mockCache.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Verifies that BuildTagKey correctly constructs the Redis key for a tag set.
    /// </summary>
    [Fact]
    public void BuildTagKey_ConstructsCorrectRedisKey()
    {
        // Arrange
        const string tag = "products";
        const string expectedKey = "cache:tags:products";

        // Act
        var result = CacheTagService.BuildTagKey(tag);

        // Assert
        result.Should().Be(expectedKey);
    }

    /// <summary>
    /// Verifies that BuildTagKey throws ArgumentException for null tag.
    /// </summary>
    [Fact]
    public void BuildTagKey_WithNullTag_ThrowsArgumentException()
    {
        // Act & Assert - ArgumentNullException is thrown because ThrowIfNullOrEmpty checks for null first
        Assert.Throws<ArgumentNullException>(() => CacheTagService.BuildTagKey(null!));
    }

    /// <summary>
    /// Verifies that BuildTagKey throws ArgumentException for empty tag.
    /// </summary>
    [Fact]
    public void BuildTagKey_WithEmptyTag_ThrowsArgumentException()
    {
        // Act & Assert - ArgumentException is thrown for empty string
        Assert.Throws<ArgumentException>(() => CacheTagService.BuildTagKey(""));
    }

    /// <summary>
    /// Verifies that a key with multiple tags is properly tracked and can be invalidated via any tag.
    /// </summary>
    [Fact]
    public async Task KeyWithMultipleTags_CanBeInvalidatedViaAnyTag()
    {
        // Arrange
        const string key = "user:999";
        const string tag1 = "users";
        const string tag2 = "active";
        const string tag3 = "premium";
        var tags = new[] { tag1, tag2, tag3 };
        var value = new { Id = 999, Name = "Jane Doe" };

        // Set up the key with multiple tags
        await _sut.SetWithTagsAsync(key, value, tags);

        // Verify the key was added to all three tag sets
        _mockDatabase.Verify(d => d.SetAddAsync(CacheTagService.BuildTagKey(tag1), key, CommandFlags.None), Times.Once);
        _mockDatabase.Verify(d => d.SetAddAsync(CacheTagService.BuildTagKey(tag2), key, CommandFlags.None), Times.Once);
        _mockDatabase.Verify(d => d.SetAddAsync(CacheTagService.BuildTagKey(tag3), key, CommandFlags.None), Times.Once);

        // Reset mocks to test invalidation
        _mockDatabase.Invocations.Clear();
        _mockCache.Invocations.Clear();

        // Set up mocks for invalidation
        var tagKey1 = CacheTagService.BuildTagKey(tag1);
        var tagKey2 = CacheTagService.BuildTagKey(tag2);
        var tagKey3 = CacheTagService.BuildTagKey(tag3);

        _mockDatabase.Setup(d => d.SetMembersAsync(tagKey1, CommandFlags.None))
                   .ReturnsAsync(new RedisValue[] { key });
        _mockDatabase.Setup(d => d.SetMembersAsync(tagKey2, CommandFlags.None))
                   .ReturnsAsync(new RedisValue[] { key });
        _mockDatabase.Setup(d => d.SetMembersAsync(tagKey3, CommandFlags.None))
                   .ReturnsAsync(new RedisValue[] { key });

        _mockCache.Setup(c => c.RemoveAsync(key)).Returns(Task.CompletedTask);

        _mockDatabase.Setup(d => d.KeyDeleteAsync(tagKey1, CommandFlags.None)).ReturnsAsync(true);
        _mockDatabase.Setup(d => d.KeyDeleteAsync(tagKey2, CommandFlags.None)).ReturnsAsync(true);
        _mockDatabase.Setup(d => d.KeyDeleteAsync(tagKey3, CommandFlags.None)).ReturnsAsync(true);

        // Act - Invalidate via first tag
        var result = await _sut.InvalidateTagAsync(tag1);

        // Assert - Key should be removed from cache and all tag sets should be deleted
        result.Should().Be(1);
        _mockCache.Verify(c => c.RemoveAsync(key), Times.Once);
        _mockDatabase.Verify(d => d.KeyDeleteAsync(tagKey1, CommandFlags.None), Times.Once);
        _mockDatabase.Verify(d => d.KeyDeleteAsync(tagKey2, CommandFlags.None), Times.Never); // Other tags still exist
        _mockDatabase.Verify(d => d.KeyDeleteAsync(tagKey3, CommandFlags.None), Times.Never);
    }

    /// <summary>
    /// Verifies that CacheTagService constructor validates arguments.
    /// </summary>
    [Fact]
    public void Constructor_ValidatesArguments()
    {
        // Act & Assert - null redis connection
        Assert.Throws<ArgumentNullException>(() => new CacheTagService(null!, _mockCache.Object));

        // Act & Assert - null cache service
        Assert.Throws<ArgumentNullException>(() => new CacheTagService(_mockRedis.Object, null!));
    }
}