// existing content ...

// WriteThoughIntegrationTests

The `WriteThoughIntegrationTests` class provides comprehensive integration tests for the Write-Through caching pattern, demonstrating how data is written to both the data source and cache synchronously. These tests validate that the cache is updated only when the data source update is successful, ensuring data consistency. The tests cover various scenarios including successful updates, handling failures in the data source, and concurrent access.

### Usage Example

```csharp
using RedisCachePatterns.Services;
using RedisCachePatterns.Domain;

public class ProductService
{
    private readonly ICacheService _cache;
    private readonly IProductRepository _productRepository;

    public ProductService(ICacheService cache, IProductRepository productRepository)
    {
        _cache = cache;
        _productRepository = productRepository;
    }

    public async Task<Product> UpdateProductAsync(Product product)
    {
        // Write through pattern - update data source and cache
        var updatedProduct = await _cache.WriteThroughAsync(
            $"product:{product.Id}",
            product,
            async () => await _productRepository.UpdateProductAsync(product)
        );
        
        return updatedProduct;
    }
}

// Usage
var productService = new ProductService(mockCache, new MockProductRepository());
var product = new Product { Id = 1, Name = "Updated Product" };
var updatedProduct = await productService.UpdateProductAsync(product);
```

## CacheAsideIntegrationTests

The `CacheAsideIntegrationTests` class demonstrates end-to-end integration scenarios for the cache-aside pattern, validating concurrent access, distributed locking, compression, validation, idempotency, retry policies, and circuit breaker patterns. These tests use a mock cache service to simulate Redis behavior without requiring an actual Redis connection, making them fast and deterministic while still exercising the full workflow patterns that applications would encounter in production.

### Usage Example

```csharp
using RedisCachePatterns.Services;
using RedisCachePatterns.Domain;

public class ProductService
{
    private readonly ICacheService _cache;
    private readonly IProductRepository _productRepository;
    
    public ProductService(ICacheService cache, IProductRepository productRepository)
    {
        _cache = cache;
        _productRepository = productRepository;
    }
    
    public async Task<Product?> GetProductAsync(int productId)
    {
        // Try to get from cache first (cache-aside pattern)
        var cachedProduct = await _cache.GetOrLoadAsync<Product>(
            $"product:{productId}",
            async () => await _productRepository.GetProductAsync(productId),
            TimeSpan.FromMinutes(30)
        );
        
        return cachedProduct;
    }
    
    public async Task<Product> UpdateProductAsync(Product product)
    {
        // Update product in database
        var updatedProduct = await _productRepository.UpdateProductAsync(product);
        
        // Write back to cache
        await _cache.SetAsync($"product:{product.Id}", updatedProduct);
        
        return updatedProduct;
    }
    
    public async Task<Product> GetOrCreateProductAsync(int productId, Func<Task<Product>> createFn)
    {
        // Use WriteAsync for atomic create-or-update operations
        return await _cache.WriteAsync(
            $"product:{productId}",
            await createFn(),
            async () => await _productRepository.GetProductAsync(productId)
        );
    }
}

// Example usage in a test scenario
var productService = new ProductService(mockCache, new MockProductRepository());

// First call - loads from source
var product = await productService.GetProductAsync(1);

// Second call - returns from cache
var cachedProduct = await productService.GetProductAsync(1);

// Concurrent access is handled safely
var tasks = Enumerable.Range(1, 10)
    .Select(i => productService.GetProductAsync(i))
    .ToList();
var results = await Task.WhenAll(tasks);
``` 
// ... rest of file content ...

## ProductTests

The `ProductTests` class contains a suite of unit tests that verify the core behavior of the `Product` domain model. It checks stock thresholds, price updates, discount calculations, rating validation, and availability logic, ensuring that the `Product` class behaves correctly under a variety of conditions.

```csharp
using RedisCachePatterns.Domain;
using RedisCachePatterns.Tests.Domain;

// Instantiate the test class (it has a parameter‑less constructor)
var productTests = new ProductTests();

// Execute a few representative test methods directly
productTests.IsLowStock_WhenStockEqualsReorderLevel_ReturnsTrue();
productTests.UpdatePrice_WithValidValue_UpdatesPriceAndSetsTimestamp();
productTests.SetRating_WithNegativeValue_ThrowsArgumentException();
```

These examples demonstrate how the public test methods can be invoked programmatically, which can be useful for custom test runners or exploratory debugging.

## ProductServiceTests

The `ProductServiceTests` class provides unit tests for the `ProductService` class, validating the interaction between caching and data access layers. It verifies that cache operations are correctly scoped, that repository calls are bypassed when cached data is available, and that cache invalidation works as expected when products are created, updated, or deleted. The tests also ensure proper error handling for validation and not-found scenarios.

```csharp
using RedisCachePatterns.Services;
using RedisCachePatterns.Domain;
using Xunit;

public class ProductServiceTestsExample
{
    private readonly ProductService _productService;
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly Mock<IProductRepository> _mockRepository = new();

    public ProductServiceTestsExample()
    {
        _productService = new ProductService(
            _mockCache.Object,
            _mockRepository.Object);
    }

    public async Task ExampleUsage()
    {
        // Setup test data
        var product = new Product
        {
            Id = 1,
            Sku = "PROD-001",
            Name = "Test Product",
            Price = 99.99m,
            Stock = 100
        };

        // Test GetProductByIdAsync - should use cache when available
        _mockCache.Setup(c => c.GetOrLoadAsync<Product>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<Product>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(product);

        var result = await _productService.GetProductByIdAsync(1);
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetProductAsync(1), Times.Never);

        // Test CreateProductAsync - should validate SKU uniqueness
        _mockRepository.Setup(r => r.ProductExistsBySkuAsync("PROD-001"))
            .ReturnsAsync(false);

        await _productService.CreateProductAsync(product);
        _mockRepository.Verify(r => r.AddProductAsync(product), Times.Once);
        _mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), product), Times.Once);

        // Test UpdateProductPriceAsync - should throw if product doesn't exist
        _mockRepository.Setup(r => r.GetProductAsync(999))
            .ReturnsAsync((Product)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _productService.UpdateProductPriceAsync(999, 129.99m));

        // Test DeleteProductAsync - should return false if product doesn't exist
        _mockRepository.Setup(r => r.GetProductAsync(999))
            .ReturnsAsync((Product)null);

        var deleteResult = await _productService.DeleteProductAsync(999);
        Assert.False(deleteResult);
        _mockRepository.Verify(r => r.DeleteProductAsync(999), Times.Never);
    }
}
```

This example demonstrates how to instantiate the test class and exercise its test methods, which validate that the `ProductService` correctly integrates with both the caching layer and the repository layer while maintaining proper error handling and cache invalidation semantics.


## CacheWarmingStrategiesTests

The `CacheWarmingStrategiesTests` class provides a comprehensive suite of tests for various cache warming strategies that pre-populate the cache with data before it's requested. These strategies help reduce cache misses and improve application performance by ensuring frequently accessed data is available in the cache from the start. The tests cover delegate-based warming, priority-based execution, parallel execution, and pattern-based refreshing approaches.

### Usage Example

```csharp
using RedisCachePatterns.Services;
using RedisCachePatterns.Domain;
using Xunit;

public class CacheWarmingExample
{
    private readonly ICacheService _cache;
    private readonly IProductRepository _productRepository;

    public CacheWarmingExample()
    {
        _cache = new CacheService();
        _productRepository = new ProductRepository();
    }

    public async Task WarmCriticalProductsFirst()
    {
        // Use PriorityWarmingStrategy to warm critical products before normal ones
        var strategy = new PriorityWarmingStrategy();
        
        // Critical products warm first
        await strategy.WarmAsync(
            _cache,
            new[] { 
                new CacheEntry("product:1", CachePriority.Critical),
                new CacheEntry("product:2", CachePriority.Critical),
                new CacheEntry("product:3", CachePriority.Normal)
            },
            async key => await _productRepository.GetProductAsync(int.Parse(key.Split(':')[1]))
        );
    }

    public async Task WarmInParallel()
    {
        // Use ParallelWarmingStrategy to warm multiple cache entries concurrently
        var strategy = new ParallelWarmingStrategy();
        
        var results = await strategy.WarmAsync(
            _cache,
            new[] {
                new CacheEntry("product:101", CachePriority.Normal),
                new CacheEntry("product:102", CachePriority.Normal),
                new CacheEntry("product:103", CachePriority.Normal)
            },
            async key => await _productRepository.GetProductAsync(int.Parse(key.Split(':')[1]))
        );
        
        Assert.Equal(3, results);
    }

    public async Task WarmWithDelegate()
    {
        // Use DelegateWarmingStrategy to warm cache entries using a factory function
        var strategy = new DelegateWarmingStrategy();
        
        await strategy.WarmAsync(
            _cache,
            new[] {
                new CacheEntry("product:201", CachePriority.Normal),
                new CacheEntry("product:202", CachePriority.Normal)
            },
            async key => {
                // Custom factory logic based on cache key
                if (key.Contains("201"))
                    return new Product { Id = 201, Name = "Product 201" };
                return null; // Will be skipped
            }
        );
    }

    public async Task RefreshPatternMatches()
    {
        // Use PatternRefreshWarmingStrategy to refresh all keys matching a pattern
        var strategy = new PatternRefreshWarmingStrategy();
        
        await strategy.WarmAsync(
            _cache,
            new[] { new CacheEntry("product:*", CachePriority.Normal) },
            async pattern => {
                // Scan Redis for all keys matching the pattern
                var keys = await _cache.ScanKeysAsync(pattern);
                return keys.Select(k => new CacheEntry(k, CachePriority.Normal));
            }
        );
    }

    public void ValidateSchedulerBehavior()
    {
        // CacheWarmingScheduler ensures strategies can only be started once
        var scheduler = new CacheWarmingScheduler();
        
        scheduler.Start();
        Assert.Throws<InvalidOperationException>(() => scheduler.Start());
        
        scheduler.Stop();
        scheduler.Stop(); // Should not throw
    }
}
```
