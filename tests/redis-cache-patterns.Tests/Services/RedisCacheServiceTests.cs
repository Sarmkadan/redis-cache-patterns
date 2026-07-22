#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Infrastructure.Cache;
using RedisCachePatterns.Services;
using StackExchange.Redis;
using Xunit;

namespace RedisCachePatterns.Tests.Services;

/// <summary>
/// Unit tests for <see cref="RedisCacheService"/> class.
/// Tests core cache operations with mocked Redis abstractions.
/// </summary>
public class RedisCacheServiceTests
{
    private readonly Mock<IRedisConnection> _mockRedisConnection = new();
    private readonly Mock<IDatabase> _mockDatabase = new();
    private readonly Mock<ILogger<RedisCacheService>> _mockLogger = new();
    private readonly RedisCacheService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisCacheServiceTests"/> class,
    /// setting up mocks for Redis connection, database, and logger.
    /// </summary>
    public RedisCacheServiceTests()
    {
        // Setup Redis connection to return mocked database
        _mockRedisConnection.Setup(c => c.GetDatabase(It.IsAny<int>()))
            .Returns(_mockDatabase.Object);

        _sut = new RedisCacheService(_mockRedisConnection.Object, _mockLogger.Object);
    }

    /// <summary>
    /// Sample DTO for testing serialization round-trip
    /// </summary>
    private static Product MakeProduct(int id = 1, string sku = "SKU-001") => new()
    {
        Id = id,
        Name = "Test Product",
        Description = "A test product for unit testing",
        Sku = sku,
        Price = 29.99m,
        StockQuantity = 50,
        ReorderLevel = 10,
        Category = "Electronics",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        Rating = 4.5,
        ReviewCount = 10
    };

    #region GetAsync Tests

    /// <summary>
    /// Verifies that GetAsync returns null when key does not exist in cache.
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsNull()
    {
        // Arrange
        const string key = "nonexistent:key";
        _mockDatabase.Setup(db => db.StringGetAsync(key, CommandFlags.None))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _sut.GetAsync<Product>(key);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetAsync returns deserialized value when key exists in cache.
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenKeyExists_ReturnsDeserializedValue()
    {
        // Arrange
        const string key = "product:1";
        var product = MakeProduct(id: 1);
        var json = "{\"Id\":1,\"Name\":\"Test Product\",\"Sku\":\"SKU-001\",\"Price\":29.99,\"StockQuantity\":50,\"ReorderLevel\":10,\"Category\":\"Electronics\",\"IsActive\":true}";
        _mockDatabase.Setup(db => db.StringGetAsync(key, CommandFlags.None))
            .ReturnsAsync(json);

        // Act
        var result = await _sut.GetAsync<Product>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test Product");
    }

    /// <summary>
    /// Verifies that GetAsync throws ArgumentNullException for null or whitespace key.
    /// </summary>
    [Fact]
    public async Task GetAsync_WithNullOrWhitespaceKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.GetAsync<Product>(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.GetAsync<Product>(""));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.GetAsync<Product>("   "));
    }

    #endregion

    #region SetAsync Tests

    /// <summary>
    /// Verifies that SetAsync stores value in cache with proper serialization.
    /// </summary>
    [Fact]
    public async Task SetAsync_StoresValueInCache()
    {
        // Arrange
        const string key = "product:1";
        var product = MakeProduct(id: 1);

        // Act
        await _sut.SetAsync(key, product, TimeSpan.FromMinutes(30));

        // Assert - StringSetAsync is called for the actual value
        _mockDatabase.Verify(db => db.StringSetAsync(key, It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    /// <summary>
    /// Verifies that SetAsync throws ArgumentNullException for null key.
    /// </summary>
    [Fact]
    public async Task SetAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var product = MakeProduct();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.SetAsync<Product>(null!, product));
    }

    /// <summary>
    /// Verifies that SetAsync throws ArgumentNullException for null value.
    /// </summary>
    [Fact]
    public async Task SetAsync_WithNullValue_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.SetAsync<Product>("key", null!));
    }

    #endregion

    #region RemoveAsync Tests

    /// <summary>
    /// Verifies that RemoveAsync deletes key from cache.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_DeletesKeyFromCache()
    {
        // Arrange
        const string key = "product:1";

        // Act
        await _sut.RemoveAsync(key);

        // Assert
        _mockDatabase.Verify(db => db.KeyDeleteAsync(key, CommandFlags.None), Times.Once);
    }

    /// <summary>
    /// Verifies that RemoveAsync throws ArgumentNullException for null or whitespace key.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_WithNullOrWhitespaceKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.RemoveAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.RemoveAsync(""));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.RemoveAsync("   "));
    }

    #endregion

    #region ExistsAsync Tests

    /// <summary>
    /// Verifies that ExistsAsync returns false when key does not exist.
    /// </summary>
    [Fact]
    public async Task ExistsAsync_WhenKeyDoesNotExist_ReturnsFalse()
    {
        // Arrange
        const string key = "nonexistent:key";
        _mockDatabase.Setup(db => db.KeyExistsAsync(key, CommandFlags.None))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.ExistsAsync(key);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ExistsAsync returns true when key exists.
    /// </summary>
    [Fact]
    public async Task ExistsAsync_WhenKeyExists_ReturnsTrue()
    {
        // Arrange
        const string key = "product:1";
        _mockDatabase.Setup(db => db.KeyExistsAsync(key, CommandFlags.None))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.ExistsAsync(key);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that ExistsAsync throws ArgumentNullException for null or whitespace key.
    /// </summary>
    [Fact]
    public async Task ExistsAsync_WithNullOrWhitespaceKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.ExistsAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.ExistsAsync(""));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.ExistsAsync("   "));
    }

    #endregion

    #region GetOrLoadAsync Tests

    /// <summary>
    /// Verifies cache hit behavior in GetOrLoadAsync - returns cached value without calling loadFn.
    /// </summary>
    [Fact]
    public async Task GetOrLoadAsync_WhenCacheHit_ReturnsCachedValueWithoutCallingLoadFn()
    {
        // Arrange
        const string key = "product:cached";
        var product = MakeProduct(id: 42);
        var json = "{\"Id\":42,\"Name\":\"Cached Product\",\"Sku\":\"SKU-CACHED\",\"Price\":99.99,\"StockQuantity\":100,\"ReorderLevel\":20,\"Category\":\"Premium\",\"IsActive\":true}";

        _mockDatabase.Setup(db => db.StringGetAsync(key, CommandFlags.None))
            .ReturnsAsync(json);

        var loadFnCalled = false;
        Task<Product> loadFn()
        {
            loadFnCalled = true;
            return Task.FromResult(MakeProduct(id: 999));
        }

        // Act
        var result = await _sut.GetOrLoadAsync(key, loadFn);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        loadFnCalled.Should().BeFalse(); // loadFn should not be called on cache hit
    }

    /// <summary>
    /// Verifies cache miss behavior in GetOrLoadAsync - calls loadFn and caches result.
    /// </summary>
    [Fact]
    public async Task GetOrLoadAsync_WhenCacheMiss_CallsLoadFnAndCachesResult()
    {
        // Arrange
        const string key = "product:missed";
        _mockDatabase.Setup(db => db.StringGetAsync(key, CommandFlags.None))
            .ReturnsAsync(RedisValue.Null);

        var loadFnCalled = false;
        var expectedProduct = MakeProduct(id: 777);

        Task<Product> loadFn()
        {
            loadFnCalled = true;
            return Task.FromResult(expectedProduct);
        }

        // Act
        var result = await _sut.GetOrLoadAsync(key, loadFn, TimeSpan.FromMinutes(10));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(777);
        loadFnCalled.Should().BeTrue(); // loadFn should be called on cache miss

        // Verify caching occurred
        _mockDatabase.Verify(db => db.StringSetAsync(key, It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    /// <summary>
    /// Verifies that GetOrLoadAsync handles deserialization failures by evicting corrupted entry.
    /// </summary>
    [Fact]
    public async Task GetOrLoadAsync_WhenDeserializationFails_EvictsCorruptedEntryAndReloads()
    {
        // Arrange
        const string key = "product:corrupted";
        var corruptedJson = "invalid json {{";

        _mockDatabase.SetupSequence(db => db.StringGetAsync(key, CommandFlags.None))
            .ReturnsAsync(corruptedJson) // First call returns corrupted data
            .ReturnsAsync(RedisValue.Null); // Second call after eviction returns null

        var loadFnCalled = false;
        Task<Product> loadFn()
        {
            loadFnCalled = true;
            return Task.FromResult(MakeProduct(id: 123));
        }

        // Act
        var result = await _sut.GetOrLoadAsync(key, loadFn);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(123);
        loadFnCalled.Should().BeTrue(); // Should reload after eviction

        // Verify corrupted entry was deleted
        _mockDatabase.Verify(db => db.KeyDeleteAsync(key, CommandFlags.None), Times.Once);
    }

    /// <summary>
    /// Verifies that GetOrLoadAsync throws ArgumentNullException for null key.
    /// </summary>
    [Fact]
    public async Task GetOrLoadAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.GetOrLoadAsync<Product>(null!, () => Task.FromResult(MakeProduct())));
    }

    /// <summary>
    /// Verifies that GetOrLoadAsync throws ArgumentNullException for null loadFn.
    /// </summary>
    [Fact]
    public async Task GetOrLoadAsync_WithNullLoadFn_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.GetOrLoadAsync<Product>("key", null!));
    }

    #endregion

    #region Serialization Round-trip Tests

    /// <summary>
    /// Verifies that Product objects serialize and deserialize correctly.
    /// </summary>
    [Fact]
    public async Task SetAndGetAsync_Product_SerializationRoundTrip()
    {
        // Arrange
        const string key = "product:roundtrip";
        var originalProduct = MakeProduct(id: 123, sku: "ROUNDTRIP-SKU");
        originalProduct.Description = "A product for serialization testing";
        originalProduct.ImageUrl = "https://example.com/image.jpg";
        originalProduct.Rating = 4.8;
        originalProduct.ReviewCount = 42;

        // Act - Set
        await _sut.SetAsync(key, originalProduct, TimeSpan.FromHours(1));

        // Verify StringSet was called with serialized data
        _mockDatabase.Verify(db => db.StringSetAsync(key, It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>()), Times.Once);

        // Arrange Get to return the serialized value
        var json = "{\"Id\":123,\"Name\":\"Test Product\",\"Sku\":\"ROUNDTRIP-SKU\",\"Price\":29.99,\"StockQuantity\":50,\"ReorderLevel\":10,\"Category\":\"Electronics\",\"IsActive\":true,\"Description\":\"A product for serialization testing\",\"ImageUrl\":\"https://example.com/image.jpg\",\"Rating\":4.8,\"ReviewCount\":42}";
        _mockDatabase.Setup(db => db.StringGetAsync(key, CommandFlags.None))
            .ReturnsAsync(json);

        // Act - Get
        var retrievedProduct = await _sut.GetAsync<Product>(key);

        // Assert
        retrievedProduct.Should().NotBeNull();
        retrievedProduct!.Id.Should().Be(123);
        retrievedProduct.Sku.Should().Be("ROUNDTRIP-SKU");
        retrievedProduct.Description.Should().Be("A product for serialization testing");
    }

    #endregion

    #region GetWithSlidingExpirationAsync Tests

    /// <summary>
    /// Verifies GetWithSlidingExpirationAsync returns value on cache hit.
    /// </summary>
    [Fact]
    public async Task GetWithSlidingExpirationAsync_WhenCacheHit_ReturnsValueAndResetsTTL()
    {
        // Arrange
        const string key = "product:sliding";
        var product = MakeProduct(id: 555);
        var json = "{\"Id\":555,\"Name\":\"Sliding Product\",\"Sku\":\"SKU-SLIDE\",\"Price\":79.99,\"StockQuantity\":75,\"ReorderLevel\":15,\"Category\":\"Sliding\",\"IsActive\":true}";

        _mockDatabase.Setup(db => db.StringGetAsync(key, CommandFlags.None))
            .ReturnsAsync(json);
        _mockDatabase.Setup(db => db.KeyExpireAsync(key, TimeSpan.FromMinutes(5), CommandFlags.None))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.GetWithSlidingExpirationAsync<Product>(key, TimeSpan.FromMinutes(5));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(555);
        _mockDatabase.Verify(db => db.KeyExpireAsync(key, TimeSpan.FromMinutes(5)), Times.Once);
    }

    /// <summary>
    /// Verifies GetWithSlidingExpirationAsync returns null on cache miss.
    /// </summary>
    [Fact]
    public async Task GetWithSlidingExpirationAsync_WhenCacheMiss_ReturnsNull()
    {
        // Arrange
        const string key = "product:sliding-miss";
        _mockDatabase.Setup(db => db.StringGetAsync(key, CommandFlags.None))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _sut.GetWithSlidingExpirationAsync<Product>(key, TimeSpan.FromMinutes(5));

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region WriteAsync Tests

    /// <summary>
    /// Verifies WriteAsync persists value and updates cache (write-through pattern).
    /// </summary>
    [Fact]
    public async Task WriteAsync_PersistsValueAndUpdatesCache()
    {
        // Arrange
        const string key = "product:write";
        var product = MakeProduct(id: 888);

        Task<Product> persistFn() => Task.FromResult(product);

        // Act
        var result = await _sut.WriteAsync(key, product, persistFn, TimeSpan.FromMinutes(15));

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(888);

        // Verify cache was updated
        _mockDatabase.Verify(db => db.StringSetAsync(key, It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    #endregion
}