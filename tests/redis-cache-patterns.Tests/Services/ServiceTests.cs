// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Events;
using RedisCachePatterns.Exceptions;
using RedisCachePatterns.Infrastructure.Repositories;
using RedisCachePatterns.Services;
using Xunit;

namespace RedisCachePatterns.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _mockRepo = new();
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly Mock<ILogger<ProductService>> _mockLogger = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(_mockRepo.Object, _mockCache.Object, _mockLogger.Object);
    }

    private static Product MakeProduct(int id = 1, string sku = "SKU-001", string category = "Electronics") => new()
    {
        Id = id,
        Name = "Test Product",
        Sku = sku,
        Price = 29.99m,
        StockQuantity = 50,
        ReorderLevel = 10,
        Category = category,
        IsActive = true
    };

    [Fact]
    public async Task GetProductByIdAsync_WhenCacheReturnsProduct_DoesNotCallRepository()
    {
        var product = MakeProduct(id: 1);
        _mockCache
            .Setup(c => c.GetOrLoadAsync<Product>(
                "product:1",
                It.IsAny<Func<Task<Product>>>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(product);

        var result = await _sut.GetProductByIdAsync(1);

        result.Should().BeEquivalentTo(product);
        _mockRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetProductByIdAsync_UsesCorrectlyScopedCacheKey()
    {
        _mockCache
            .Setup(c => c.GetOrLoadAsync<Product>(
                "product:99",
                It.IsAny<Func<Task<Product>>>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((Product?)null);

        await _sut.GetProductByIdAsync(99);

        _mockCache.Verify(c => c.GetOrLoadAsync<Product>(
            "product:99",
            It.IsAny<Func<Task<Product>>>(),
            It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_WhenSkuAlreadyExists_ThrowsValidationException()
    {
        var existing = MakeProduct(sku: "DUPE-SKU");
        _mockCache
            .Setup(c => c.GetOrLoadAsync<Product>(
                "product:sku:DUPE-SKU",
                It.IsAny<Func<Task<Product>>>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(existing);

        Func<Task> act = () => _sut.CreateProductAsync(MakeProduct(id: 0, sku: "DUPE-SKU"));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*SKU already exists*");
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task CreateProductAsync_WhenSkuIsNew_PersistsProductAndCachesIt()
    {
        var input = MakeProduct(id: 0, sku: "FRESH-SKU");
        var persisted = MakeProduct(id: 7, sku: "FRESH-SKU");

        _mockCache
            .Setup(c => c.GetOrLoadAsync<Product>(
                "product:sku:FRESH-SKU",
                It.IsAny<Func<Task<Product>>>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((Product?)null);
        _mockRepo.Setup(r => r.AddAsync(input)).ReturnsAsync(persisted);
        _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<Product>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockCache.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _sut.CreateProductAsync(input);

        result.Id.Should().Be(7);
        _mockCache.Verify(c => c.SetAsync("product:7", persisted, It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProductAsync_WhenProductDoesNotExist_ReturnsFalseWithoutDeletion()
    {
        _mockCache
            .Setup(c => c.GetOrLoadAsync<Product>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<Product>>>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.DeleteProductAsync(999);

        result.Should().BeFalse();
        _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductPriceAsync_WhenProductDoesNotExist_ThrowsNotFoundException()
    {
        _mockCache
            .Setup(c => c.GetOrLoadAsync<Product>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<Product>>>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((Product?)null);

        Func<Task> act = () => _sut.UpdateProductPriceAsync(777, 50m);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateProductStockAsync_WhenResultingStockIsLow_InvalidatesLowStockCacheEntry()
    {
        var product = MakeProduct();
        product.StockQuantity = 15;
        product.ReorderLevel = 10;

        _mockCache
            .Setup(c => c.GetOrLoadAsync<Product>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<Product>>>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(product);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>())).ReturnsAsync(product);
        _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<Product>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockCache.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Reduce by 12: 15 - 12 = 3, which is below reorderLevel=10
        await _sut.UpdateProductStockAsync(1, -12);

        _mockCache.Verify(c => c.RemoveAsync("products:lowstock"), Times.AtLeastOnce);
    }
}

public class CacheInvalidationServiceTests
{
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly Mock<IEventPublisher> _mockPublisher = new();
    private readonly Mock<ILogger<CacheInvalidationService>> _mockLogger = new();
    private readonly CacheInvalidationService _sut;

    public CacheInvalidationServiceTests()
    {
        _sut = new CacheInvalidationService(
            _mockCache.Object,
            _mockPublisher.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void RegisterKeyWithTags_SingleTag_KeyIsRetrievableByTag()
    {
        _sut.RegisterKeyWithTags("product:1", "catalog");
        _sut.GetKeysByTag("catalog").Should().Contain("product:1");
    }

    [Fact]
    public void RegisterKeyWithTags_MultipleTags_KeyAppearsUnderEachTag()
    {
        _sut.RegisterKeyWithTags("order:42", "orders", "user-session");
        _sut.GetKeysByTag("orders").Should().Contain("order:42");
        _sut.GetKeysByTag("user-session").Should().Contain("order:42");
    }

    [Fact]
    public void RegisterKeyWithTags_SameKeyRegisteredTwiceUnderSameTag_StoredOnce()
    {
        _sut.RegisterKeyWithTags("product:5", "catalog");
        _sut.RegisterKeyWithTags("product:5", "catalog");
        _sut.GetKeysByTag("catalog").Should().ContainSingle(k => k == "product:5");
    }

    [Fact]
    public async Task InvalidateByTagAsync_WhenTagHasKeys_RemovesEachKeyAndClearsTagIndex()
    {
        _sut.RegisterKeyWithTags("product:1", "catalog");
        _sut.RegisterKeyWithTags("product:2", "catalog");
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockPublisher.Setup(p => p.PublishAsync(It.IsAny<CacheInvalidatedEvent>())).Returns(Task.CompletedTask);

        await _sut.InvalidateByTagAsync("catalog");

        _mockCache.Verify(c => c.RemoveAsync("product:1"), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("product:2"), Times.Once);
        _sut.GetKeysByTag("catalog").Should().BeEmpty();
    }

    [Fact]
    public async Task InvalidateByTagAsync_WhenTagNotRegistered_DoesNotInvokeRemove()
    {
        await _sut.InvalidateByTagAsync("nonexistent-tag");
        _mockCache.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvalidateByTagAsync_WhenTagHasKeys_PublishesCacheInvalidatedEvent()
    {
        _sut.RegisterKeyWithTags("user:10", "users");
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockPublisher.Setup(p => p.PublishAsync(It.IsAny<CacheInvalidatedEvent>())).Returns(Task.CompletedTask);

        await _sut.InvalidateByTagAsync("users");

        _mockPublisher.Verify(p => p.PublishAsync(It.Is<CacheInvalidatedEvent>(
            e => e.KeysAffected == 1)), Times.Once);
    }
}
