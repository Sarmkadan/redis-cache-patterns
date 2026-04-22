#nullable enable
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Services;
using Xunit;

namespace RedisCachePatterns.Tests.Services;

public class CompressedCacheServiceTests
{
    private readonly Mock<ICacheService> _mockInnerCache = new();
    private readonly Mock<ILogger<CompressedCacheService>> _mockLogger = new();

    [Fact]
    public async Task GetAsync_WhenCachedValueIsNotCompressed_ReturnsParsedValue()
    {
        var key = "test-key";
        var value = new TestData { Id = 1, Name = "Test" };
        var json = System.Text.Json.JsonSerializer.Serialize(value);

        _mockInnerCache.Setup(c => c.GetAsync<string>(key))
            .ReturnsAsync(json);

        var service = new CompressedCacheService(_mockInnerCache.Object, _mockLogger.Object);
        var result = await service.GetAsync<TestData>(key);

        result.Should().NotBeNull();
        result?.Id.Should().Be(1);
        result?.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetAsync_WhenCachedValueIsCompressed_DecompressesAndReturnsValue()
    {
        var key = "compressed-key";
        var value = new TestData { Id = 2, Name = "Compressed" };
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        var compressed = RedisCachePatterns.Utilities.CompressionUtil.CompressString(json);
        var compressedValue = "GZIP::" + System.Convert.ToBase64String(compressed);

        _mockInnerCache.Setup(c => c.GetAsync<string>(key))
            .ReturnsAsync(compressedValue);

        var service = new CompressedCacheService(_mockInnerCache.Object, _mockLogger.Object);
        var result = await service.GetAsync<TestData>(key);

        result.Should().NotBeNull();
        result?.Id.Should().Be(2);
        result?.Name.Should().Be("Compressed");
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsNull()
    {
        _mockInnerCache.Setup(c => c.GetAsync<string>(It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var service = new CompressedCacheService(_mockInnerCache.Object, _mockLogger.Object);
        var result = await service.GetAsync<TestData>("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_WithSmallValue_DoesNotCompress()
    {
        var key = "small-key";
        var value = new TestData { Id = 3, Name = "Small" };

        _mockInnerCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var service = new CompressedCacheService(_mockInnerCache.Object, _mockLogger.Object, compressionThresholdBytes: 10000);
        await service.SetAsync(key, value);

        _mockInnerCache.Verify(c => c.SetAsync(key, It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_WithLargeValue_CompressesBeforeCaching()
    {
        var key = "large-key";
        var largeValue = new TestData
        {
            Id = 4,
            Name = new string('x', 5000),
            Description = new string('y', 5000)
        };

        _mockInnerCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var service = new CompressedCacheService(_mockInnerCache.Object, _mockLogger.Object, compressionThresholdBytes: 1024);
        await service.SetAsync(key, largeValue);

        _mockInnerCache.Verify(c => c.SetAsync(key, It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetOrLoadAsync_WhenCacheHit_ReturnsValueWithoutLoadingFromSource()
    {
        var key = "cached-key";
        var value = new TestData { Id = 5, Name = "Cached" };
        var json = System.Text.Json.JsonSerializer.Serialize(value);

        _mockInnerCache.Setup(c => c.GetAsync<string>(key))
            .ReturnsAsync(json);

        var loadFnCalled = false;
        var service = new CompressedCacheService(_mockInnerCache.Object, _mockLogger.Object);
        var result = await service.GetOrLoadAsync(key, async () =>
        {
            loadFnCalled = true;
            await Task.CompletedTask;
            return new TestData { Id = 99, Name = "Should not load" };
        });

        result?.Id.Should().Be(5);
        loadFnCalled.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrLoadAsync_OnCacheMiss_LoadsAndCaches()
    {
        var key = "miss-key";

        _mockInnerCache.Setup(c => c.GetAsync<string>(key))
            .ReturnsAsync((string?)null);
        _mockInnerCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var service = new CompressedCacheService(_mockInnerCache.Object, _mockLogger.Object);
        var loadedValue = new TestData { Id = 6, Name = "Loaded" };
        var result = await service.GetOrLoadAsync(key, async () =>
        {
            await Task.CompletedTask;
            return loadedValue;
        });

        result?.Id.Should().Be(6);
        _mockInnerCache.Verify(c => c.SetAsync(key, It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetOrLoadAsync_WhenLoadFnReturnsNull_DoesNotCache()
    {
        var key = "null-key";

        _mockInnerCache.Setup(c => c.GetAsync<string>(key))
            .ReturnsAsync((string?)null);

        var service = new CompressedCacheService(_mockInnerCache.Object, _mockLogger.Object);
        var result = await service.GetOrLoadAsync<TestData>(key, async () =>
        {
            await Task.CompletedTask;
            return null;
        });

        result.Should().BeNull();
        _mockInnerCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Never);
    }

    [Fact]
    public async Task SetAsync_WithExpiration_PassesExpirationToInnerCache()
    {
        var key = "expiring-key";
        var value = new TestData { Id = 7, Name = "Expiring" };
        var expiration = TimeSpan.FromHours(1);

        _mockInnerCache.Setup(c => c.SetAsync(key, It.IsAny<string>(), expiration))
            .Returns(Task.CompletedTask);

        var service = new CompressedCacheService(_mockInnerCache.Object, _mockLogger.Object);
        await service.SetAsync(key, value, expiration);

        _mockInnerCache.Verify(c => c.SetAsync(key, It.IsAny<string>(), expiration), Times.Once);
    }

    [Fact]
    public async Task WriteAsync_DelegatesRequestToInnerCache()
    {
        var key = "write-key";
        var value = new TestData { Id = 8, Name = "Written" };
        var persistedValue = new TestData { Id = 8, Name = "Written", CreatedAt = DateTime.UtcNow };

        _mockInnerCache.Setup(c => c.WriteAsync(
            key, It.IsAny<TestData>(), It.IsAny<Func<Task<TestData>>>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(persistedValue);

        var service = new CompressedCacheService(_mockInnerCache.Object, _mockLogger.Object);
        var result = await service.WriteAsync(key, value, async () =>
        {
            await Task.CompletedTask;
            return persistedValue;
        });

        result.Should().Be(persistedValue);
        _mockInnerCache.Verify(c => c.WriteAsync(
            key, It.IsAny<TestData>(), It.IsAny<Func<Task<TestData>>>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetOrLoadWithSlidingExpirationAsync_DelegatesRequestToInnerCache()
    {
        var key = "sliding-key";
        var value = new TestData { Id = 9, Name = "Sliding" };
        var slidingExpiration = TimeSpan.FromMinutes(10);

        _mockInnerCache.Setup(c => c.GetOrLoadWithSlidingExpirationAsync(
            key, It.IsAny<Func<Task<TestData>>>(), slidingExpiration))
            .ReturnsAsync(value);

        var service = new CompressedCacheService(_mockInnerCache.Object, _mockLogger.Object);
        var result = await service.GetOrLoadWithSlidingExpirationAsync(key, async () =>
        {
            await Task.CompletedTask;
            return value;
        }, slidingExpiration);

        result?.Id.Should().Be(9);
        _mockInnerCache.Verify(c => c.GetOrLoadWithSlidingExpirationAsync(
            key, It.IsAny<Func<Task<TestData>>>(), slidingExpiration), Times.Once);
    }

    [Fact]
    public async Task GetOrLoadWithEarlyExpirationAsync_DelegatesRequestToInnerCache()
    {
        var key = "early-exp-key";
        var value = new TestData { Id = 10, Name = "EarlyExp" };
        var expiration = TimeSpan.FromHours(1);
        var beta = 1.5;

        _mockInnerCache.Setup(c => c.GetOrLoadWithEarlyExpirationAsync(
            key, It.IsAny<Func<Task<TestData>>>(), expiration, beta))
            .ReturnsAsync(value);

        var service = new CompressedCacheService(_mockInnerCache.Object, _mockLogger.Object);
        var result = await service.GetOrLoadWithEarlyExpirationAsync(key, async () =>
        {
            await Task.CompletedTask;
            return value;
        }, expiration, beta);

        result?.Id.Should().Be(10);
        _mockInnerCache.Verify(c => c.GetOrLoadWithEarlyExpirationAsync(
            key, It.IsAny<Func<Task<TestData>>>(), expiration, beta), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_DelegatesRequestToInnerCache()
    {
        var key = "remove-key";

        _mockInnerCache.Setup(c => c.RemoveAsync(key))
            .Returns(Task.CompletedTask);

        var service = new CompressedCacheService(_mockInnerCache.Object, _mockLogger.Object);
        await service.RemoveAsync(key);

        _mockInnerCache.Verify(c => c.RemoveAsync(key), Times.Once);
    }

    [Fact]
    public async Task RemoveByPatternAsync_DelegatesRequestToInnerCache()
    {
        var pattern = "test:*";

        _mockInnerCache.Setup(c => c.RemoveByPatternAsync(pattern))
            .Returns(Task.CompletedTask);

        var service = new CompressedCacheService(_mockInnerCache.Object, _mockLogger.Object);
        await service.RemoveByPatternAsync(pattern);

        _mockInnerCache.Verify(c => c.RemoveByPatternAsync(pattern), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_DelegatesRequestToInnerCache()
    {
        var key = "exists-key";

        _mockInnerCache.Setup(c => c.ExistsAsync(key))
            .ReturnsAsync(true);

        var service = new CompressedCacheService(_mockInnerCache.Object, _mockLogger.Object);
        var result = await service.ExistsAsync(key);

        result.Should().BeTrue();
        _mockInnerCache.Verify(c => c.ExistsAsync(key), Times.Once);
    }

    [Fact]
    public async Task GetExpirationAsync_DelegatesRequestToInnerCache()
    {
        var key = "expiration-key";
        var ttl = TimeSpan.FromHours(2);

        _mockInnerCache.Setup(c => c.GetExpirationAsync(key))
            .ReturnsAsync(ttl);

        var service = new CompressedCacheService(_mockInnerCache.Object, _mockLogger.Object);
        var result = await service.GetExpirationAsync(key);

        result.Should().Be(ttl);
    }

    [Fact]
    public async Task GetKeysByPatternAsync_DelegatesRequestToInnerCache()
    {
        var pattern = "test:*";
        var keys = new[] { "test:1", "test:2", "test:3" };

        _mockInnerCache.Setup(c => c.GetKeysByPatternAsync(pattern))
            .ReturnsAsync(keys.AsEnumerable());

        var service = new CompressedCacheService(_mockInnerCache.Object, _mockLogger.Object);
        var result = await service.GetKeysByPatternAsync(pattern);

        result.Should().Equal(keys);
    }

    [Fact]
    public async Task FlushAsync_DelegatesRequestToInnerCache()
    {
        _mockInnerCache.Setup(c => c.FlushAsync())
            .Returns(Task.CompletedTask);

        var service = new CompressedCacheService(_mockInnerCache.Object, _mockLogger.Object);
        await service.FlushAsync();

        _mockInnerCache.Verify(c => c.FlushAsync(), Times.Once);
    }

    private class TestData
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
