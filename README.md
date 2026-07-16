# redis-cache-patterns

Production-ready Redis caching patterns for .NET: cache-aside, write-through,
distributed locks, tag/pattern invalidation, cross-instance invalidation
(pub/sub + streams), compression, negative caching, circuit breaking, warming
and metrics - built on StackExchange.Redis, net10.0.

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the full picture: module
layout, the `ICacheService` contract and its implementations
(`RedisCacheService`, `RedisClusterCacheService`, `CompressedCacheService`),
the two-tier invalidation pipeline, key design decisions with their
trade-offs, extension points and known limitations. Quick start for DI:

```csharp
services.AddRedisCachePatterns("localhost:6379"); // full demo stack
// or
services.AddRedisCache();                          // just the cache layer
```

## WriteThoughIntegrationTests

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



## InventoryServiceTests

The `InventoryServiceTests` class provides comprehensive unit tests for the `InventoryService` class, which manages inventory operations with Redis caching and distributed locking. It verifies that inventory items are correctly retrieved from cache or repository, that distributed locks prevent race conditions during inventory reservation, that cache invalidation works as expected, and that low stock detection functions properly. The tests also validate proper error handling for insufficient inventory scenarios and reservation cleanup.

### Usage Example

```csharp
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Services;

public class InventoryServiceExample
{
    private readonly Mock<IInventoryRepository> _mockRepo = new();
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly Mock<ILogger<InventoryService>> _mockLogger = new();
    private readonly InventoryService _inventoryService;

    public InventoryServiceExample()
    {
        _inventoryService = new InventoryService(
            _mockRepo.Object,
            _mockCache.Object,
            _mockLogger.Object
        );
    }

    public async Task ManageInventoryExample()
    {
        // Setup test data
        var inventoryItem = new InventoryItem
        {
            Id = 1,
            ProductId = 100,
            Warehouse = "WH-US-East",
            QuantityOnHand = 500,
            QuantityReserved = 0,
            QuantityAvailable = 500,
            ReorderPoint = 50,
            MaxStock = 1000,
            LastUpdated = DateTime.UtcNow
        };

        // Mock cache to return inventory item
        _mockCache
            .Setup(c => c.GetOrLoadAsync<InventoryItem>(
                "inventory:1",
                It.IsAny<Func<Task<InventoryItem>>>(),
                It.IsAny<TimeSpan?>()
            ))
            .ReturnsAsync(inventoryItem);

        // Get inventory by ID - uses cache
        var result = await _inventoryService.GetInventoryByIdAsync(1);
        Console.WriteLine($"Inventory found: {result?.ProductId}");

        // Get inventory by product and warehouse
        var warehouseInventory = await _inventoryService.GetByProductAndWarehouseAsync(100, "WH-US-East");
        Console.WriteLine($"Warehouse inventory: {warehouseInventory?.QuantityAvailable}");

        // Reserve inventory with distributed lock
        var reservationSuccess = await _inventoryService.ReserveInventoryAsync(
            100,  // productId
            "WH-US-East",  // warehouse
            10,  // quantity to reserve
            "instance-1"  // instance ID for lock
        );
        Console.WriteLine($"Reservation successful: {reservationSuccess}");

        // Get all inventory for a product (multiple warehouses)
        var productInventory = await _inventoryService.GetInventoryByProductAsync(100);
        Console.WriteLine($"Product has inventory in {productInventory?.Count()} warehouses");

        // Get low stock items
        var lowStockItems = await _inventoryService.GetLowStockItemsAsync();
        Console.WriteLine($"Found {lowStockItems?.Count()} items below reorder point");

        // Release reservation
        var releaseSuccess = await _inventoryService.ReleaseReservationAsync(1, 5);
        Console.WriteLine($"Reservation released: {releaseSuccess}");
    }
}
```

This example demonstrates how to instantiate the test class and exercise its test methods, which validate that the `InventoryService` correctly integrates with both the caching layer and the repository layer while maintaining proper distributed locking and cache invalidation semantics.

## OrderServiceTests

The `OrderServiceTests` class provides comprehensive unit tests for the `OrderService` class, validating Redis caching behavior for order operations. It verifies that cache operations are correctly scoped, that repository calls are bypassed when cached data is available, and that cache invalidation works as expected when orders are created, confirmed, cancelled, or when user orders are retrieved. The tests also ensure proper distributed locking behavior for order confirmation and proper error handling for not-found scenarios.

### Usage Example

```csharp
using Moq;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Services;
using Xunit;

public class OrderServiceTestsExample
{
    private readonly OrderService _orderService;
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly Mock<IOrderRepository> _mockRepo = new();
    private readonly Mock<ILogger<OrderService>> _mockLogger = new();

    public OrderServiceTestsExample()
    {
        _orderService = new OrderService(
            _mockRepo.Object,
            _mockCache.Object,
            _mockLogger.Object);
    }

    public async Task ExampleUsage()
    {
        // Setup test data
        var order = new Order
        {
            Id = 1,
            UserId = 100,
            OrderNumber = "ORD-12345678",
            Status = OrderStatus.Pending,
            TotalAmount = 99.99m,
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        // Test GetOrderByIdAsync - should use cache when available
        _mockCache.Setup(c => c.GetOrLoadAsync<Order>(
            "order:1",
            It.IsAny<Func<Task<Order>>>(),
            It.IsAny<TimeSpan?>()
        ))
            .ReturnsAsync(order);

        var result = await _orderService.GetOrderByIdAsync(1);
        Assert.Equal(1, result.Id);
        _mockRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);

        // Test GetOrderByNumberAsync - should retrieve order by order number
        _mockCache.Setup(c => c.GetOrLoadAsync<Order>(
            "order:number:ORD-12345678",
            It.IsAny<Func<Task<Order>>>(),
            It.IsAny<TimeSpan?>()
        ))
            .ReturnsAsync(order);

        var orderByNumber = await _orderService.GetOrderByNumberAsync("ORD-12345678");
        Assert.Equal("ORD-12345678", orderByNumber.OrderNumber);

        // Test GetUserOrdersAsync - should return user orders from cache
        var userOrders = new List<Order>
        {
            new Order { Id = 1, UserId = 100, OrderNumber = "ORD-001", Status = OrderStatus.Pending },
            new Order { Id = 2, UserId = 100, OrderNumber = "ORD-002", Status = OrderStatus.Confirmed }
        };

        _mockCache.Setup(c => c.GetOrLoadAsync<IEnumerable<Order>>(
            "orders:user:100",
            It.IsAny<Func<Task<IEnumerable<Order>>>>(),
            It.IsAny<TimeSpan?>()
        ))
            .ReturnsAsync(userOrders);

        var userOrdersResult = await _orderService.GetUserOrdersAsync(100);
        Assert.Equal(2, userOrdersResult.Count());
    }
}
```

This example demonstrates how to instantiate the test class and exercise its test methods, which validate that the `OrderService` correctly integrates with Redis caching for order operations including cache-aside pattern usage, distributed locking for order confirmation, and proper cache invalidation strategies.

## CacheAnalyticsDashboardTests

The `CacheAnalyticsDashboardTests` class provides comprehensive unit tests for the `CacheAnalyticsDashboard` class, which tracks and analyzes Redis cache hit/miss statistics. These tests validate that cache access patterns are correctly recorded, that snapshot data is properly aggregated, and that the dashboard correctly identifies hot, cold, and low-hit-rate keys for performance monitoring and optimization.

### Usage Example

```csharp
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Monitoring;

public class CacheAnalyticsExample
{
    private readonly Mock<ILogger<CacheAnalyticsDashboard>> _loggerMock = new();
    private readonly CacheAnalyticsDashboard _dashboard;

    public CacheAnalyticsExample()
    {
        _dashboard = new CacheAnalyticsDashboard(
            _loggerMock.Object,
            topN: 10,
            lowHitRateThreshold: 0.3,
            coldKeyAge: TimeSpan.FromMinutes(5)
        );
    }

    public void TrackCacheAccess()
    {
        // Record cache hits and misses
        _dashboard.RecordHit("user:123");
        _dashboard.RecordHit("user:123");
        _dashboard.RecordMiss("product:456");
        _dashboard.RecordHit("user:123");
        _dashboard.RecordMiss("product:456");
        _dashboard.RecordMiss("product:456");
        
        // Get statistics for a specific key
        var stats = _dashboard.GetKeyStats("user:123");
        Console.WriteLine($"Hits: {stats?.Hits}, Misses: {stats?.Misses}, HitRate: {stats?.HitRate:P}");
    }

    public void GeneratePerformanceReport()
    {
        // Simulate various cache access patterns
        for (int i = 0; i < 100; i++) _dashboard.RecordHit("hot:key");
        for (int i = 0; i < 10; i++) _dashboard.RecordMiss("cold:key");
        for (int i = 0; i < 50; i++) _dashboard.RecordHit("warm:key");
        
        // Get snapshot for performance analysis
        var snapshot = _dashboard.GetSnapshot();
        
        Console.WriteLine($"Total Hits: {snapshot.TotalHits}");
        Console.WriteLine($"Total Misses: {snapshot.TotalMisses}");
        Console.WriteLine($"Overall Hit Rate: {snapshot.OverallHitRate:P}");
        Console.WriteLine($"Unique Keys Tracked: {snapshot.UniqueKeysTracked}");
        
        // Get hot keys (most frequently accessed)
        Console.WriteLine("\nTop 10 Hot Keys:");
        foreach (var key in snapshot.HotKeys.Take(10))
        {
            Console.WriteLine($"  {key.Key}: {key.TotalAccesses} accesses ({key.HitRate:P} hit rate)");
        }
        
        // Get low hit rate keys (performance optimization candidates)
        Console.WriteLine("\nKeys with Low Hit Rate (< 30%):");
        foreach (var key in snapshot.LowHitRateKeys)
        {
            Console.WriteLine($"  {key.Key}: {key.HitRate:P} hit rate ({key.TotalAccesses} total accesses)");
        }
        
        // Get cold keys (not recently accessed)
        Console.WriteLine("\nCold Keys (not accessed in last 5 minutes):");
        foreach (var key in snapshot.ColdKeys)
        {
            Console.WriteLine($"  {key.Key}");
        }
        
        // Generate and display formatted report
        var report = _dashboard.RenderReport();
        Console.WriteLine("\n" + report);
    }

    public void ResetStatistics()
    {
        // Reset counters for a new monitoring period
        _dashboard.Reset();
        
        var snapshot = _dashboard.GetSnapshot();
        Console.WriteLine($"After reset - Total Hits: {snapshot.TotalHits}, Total Misses: {snapshot.TotalMisses}");
    }
}
```

This example demonstrates how to use the `CacheAnalyticsDashboard` to monitor Redis cache performance, identify optimization opportunities, and generate performance reports for cache-aside patterns and other caching strategies.

```csharp
using Moq;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Services;
using Xunit;

public class OrderServiceTestsExample
{
    private readonly OrderService _orderService;
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly Mock<IOrderRepository> _mockRepo = new();
    private readonly Mock<ILogger<OrderService>> _mockLogger = new();

    public OrderServiceTestsExample()
    {
        _orderService = new OrderService(
            _mockRepo.Object,
            _mockCache.Object,
            _mockLogger.Object);
    }

    public async Task ExampleUsage()
    {
        // Setup test data
        var order = new Order
        {
            Id = 1,
            UserId = 100,
            OrderNumber = "ORD-12345678",
            Status = OrderStatus.Pending,
            TotalAmount = 99.99m,
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        // Test GetOrderByIdAsync - should use cache when available
        _mockCache.Setup(c => c.GetOrLoadAsync<Order>(
            "order:1",
            It.IsAny<Func<Task<Order>>>(),
            It.IsAny<TimeSpan?>()
        ))
            .ReturnsAsync(order);

        var result = await _orderService.GetOrderByIdAsync(1);
        Assert.Equal(1, result.Id);
        _mockRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);

        // Test GetOrderByNumberAsync - should retrieve order by order number
        _mockCache.Setup(c => c.GetOrLoadAsync<Order>(
            "order:number:ORD-12345678",
            It.IsAny<Func<Task<Order>>>(),
            It.IsAny<TimeSpan?>()
        ))
            .ReturnsAsync(order);

        var orderByNumber = await _orderService.GetOrderByNumberAsync("ORD-12345678");
        Assert.Equal("ORD-12345678", orderByNumber.OrderNumber);

        // Test GetUserOrdersAsync - should return user orders from cache
        var userOrders = new List<Order>
        {
            new Order { Id = 1, UserId = 100, OrderNumber = "ORD-001", Status = OrderStatus.Pending },
            new Order { Id = 2, UserId = 100, OrderNumber = "ORD-002", Status = OrderStatus.Confirmed }
        };

        _mockCache.Setup(c => c.GetOrLoadAsync<IEnumerable<Order>>(
            "orders:user:100",
            It.IsAny<Func<Task<IEnumerable<Order>>>>(),
            It.IsAny<TimeSpan?>()
        ))
            .ReturnsAsync(userOrders);

        var userOrdersResult = await _orderService.GetUserOrdersAsync(100);
        Assert.Equal(2, userOrdersResult.Count());

        // Test CreateOrderAsync - should generate order number and cache result
        var newOrder = new Order { UserId = 50, TotalAmount = 150.00m };
        var createdOrder = new Order
        {
            Id = 10,
            UserId = 50,
            OrderNumber = "ORD-87654321",
            Status = OrderStatus.Pending,
            TotalAmount = 150.00m,
            CreatedAt = DateTime.UtcNow
        };

        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .ReturnsAsync(createdOrder);
        _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<Order>(), It.IsAny<TimeSpan?>>()))
            .Returns(Task.CompletedTask);
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var created = await _orderService.CreateOrderAsync(newOrder);
        Assert.Equal(10, created.Id);
        Assert.StartsWith("ORD-", created.OrderNumber);
        _mockCache.Verify(c => c.SetAsync("order:10", createdOrder, It.IsAny<TimeSpan?>>()), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("orders:user:50"), Times.Once);

        // Test ConfirmOrderAsync - should use distributed lock for safe confirmation
        var pendingOrder = new Order { Id = 20, UserId = 200, Status = OrderStatus.Pending };
        _mockCache.Setup(c => c.GetOrLoadAsync<Order>(
            "order:20",
            It.IsAny<Func<Task<Order>>>(),
            It.IsAny<TimeSpan?>()
        ))
            .ReturnsAsync(pendingOrder);
        _mockCache.Setup(c => c.AcquireLockAsync("order:lock:20", "instance-1", It.IsAny<TimeSpan>>()))
            .ReturnsAsync(true);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Order>>()))
            .ReturnsAsync(pendingOrder);
        _mockCache.Setup(c => c.ReleaseLockAsync("order:lock:20", "instance-1"))
            .ReturnsAsync(true);

        var confirmed = await _orderService.ConfirmOrderAsync(20, "instance-1");
        Assert.True(confirmed);

        // Test CancelOrderAsync - should cancel order and invalidate user cache
        var cancelOrder = new Order { Id = 30, UserId = 300, Status = OrderStatus.Pending };
        _mockCache.Setup(c => c.GetOrLoadAsync<Order>(
            "order:30",
            It.IsAny<Func<Task<Order>>>(),
            It.IsAny<TimeSpan?>()
        ))
            .ReturnsAsync(cancelOrder);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Order>>()))
            .ReturnsAsync(cancelOrder);
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<Order>(), It.IsAny<TimeSpan?>>()))
            .Returns(Task.CompletedTask);

        var cancelled = await _orderService.CancelOrderAsync(30);
        Assert.True(cancelled);
        _mockCache.Verify(c => c.RemoveAsync("orders:user:300"), Times.Once);
    }
}
```

This example demonstrates how to instantiate the test class and exercise its test methods, which validate that the `OrderService` correctly integrates with Redis caching for order operations including cache-aside pattern usage, distributed locking for order confirmation, and proper cache invalidation strategies.


## ValidationHelperTests

The `ValidationHelperTests` class provides comprehensive unit tests for the `ValidationHelper` utility class, which offers methods for validating various domain values such as usernames, email addresses, passwords, product names, prices, and quantities. These validation methods ensure data integrity by enforcing business rules and throwing descriptive validation exceptions when constraints are violated. The test suite covers edge cases including null values, empty strings, boundary conditions, and invalid formats.

### Usage Example

```csharp
using RedisCachePatterns.Utilities;
using RedisCachePatterns.Exceptions;

public class UserRegistrationService
{
    public void RegisterUser(string username, string email, string password)
    {
        try
        {
            // Validate user input before processing
            ValidationHelper.ValidateUsername(username);
            ValidationHelper.ValidateEmail(email);
            ValidationHelper.ValidatePassword(password);
            
            // If we get here, all validations passed
            Console.WriteLine("User input is valid. Processing registration...");
            
            // Proceed with registration logic
        }
        catch (ValidationException ex)
        {
            Console.WriteLine($"Validation failed: {ex.Message}");
            // Handle validation error appropriately
        }
    }
    
    public void CreateProduct(string productName, decimal price, int quantity)
    {
        try
        {
            // Validate product data
            ValidationHelper.ValidateProductName(productName);
            ValidationHelper.ValidatePrice(price);
            ValidationHelper.ValidateQuantity(quantity);
            
            // All validations passed
            Console.WriteLine("Product data is valid. Creating product...");
        }
        catch (ValidationException ex)
        {
            Console.WriteLine($"Product validation failed: {ex.Message}");
        }
    }
    
    public void ProcessOrder(decimal totalAmount)
    {
        try
        {
            // Validate order total
            ValidationHelper.ValidatePrice(totalAmount);
            
            if (totalAmount <= 0)
            {
                throw new ValidationException("Order total must be greater than zero");
            }
            
            Console.WriteLine("Order validation successful");
        }
        catch (ValidationException ex)
        {
            Console.WriteLine($"Order validation failed: {ex.Message}");
        }
    }
}

// Example usage
var registrationService = new UserRegistrationService();

// Valid inputs - no exceptions
registrationService.RegisterUser("john_doe", "john@example.com", "SecurePass123");
registrationService.CreateProduct("Premium Widget", 99.99m, 100);
registrationService.ProcessOrder(150.50m);

// Invalid inputs - will throw ValidationException
try
{
    registrationService.RegisterUser("", "invalid-email", "short");
}
catch (ValidationException ex)
{
    Console.WriteLine(ex.Message); // "*empty*" or "*Invalid email*"
}
```

## CoreFunctionalityTests

The `CoreFunctionalityTests` class provides essential utility methods for Redis cache key management, validation, normalization, and parsing. It includes functionality for building properly formatted cache keys, validating key formats, normalizing keys to consistent formats, and parsing complex cache key structures. These utilities are fundamental for maintaining consistent cache key patterns across the application and ensuring proper cache operations.

### Usage Example

```csharp
using RedisCachePatterns.Utilities;

public class CacheKeyManagementExample
{
    private readonly CoreFunctionalityTests _core = new CoreFunctionalityTests();

    public void BuildAndValidateKeys()
    {
        // Build properly formatted cache keys with multiple parameters
        var userKey = _core.BuildKey("user", 123, "profile");
        Console.WriteLine($"User key: {userKey}"); // Output: user:123:profile

        // Build key with null parameters (ignored)
        var partialKey = _core.BuildKey("product", null, 456, null);
        Console.WriteLine($"Partial key: {partialKey}"); // Output: product:456

        // Build pattern with wildcard
        var userPattern = _core.BuildPattern("user", "*", "settings");
        Console.WriteLine($"User pattern: {userPattern}"); // Output: user:*:settings

        // Validate cache key format
        var isValid = _core.IsValidKey(userKey);
        Console.WriteLine($"Is valid key: {isValid}"); // Output: True

        var invalidKey = "invalid key";
        var isInvalid = _core.IsValidKey(invalidKey);
        Console.WriteLine($"Is invalid key: {isInvalid}"); // Output: False

        // Normalize key to lowercase and trim whitespace
        var normalized = _core.NormalizeKey("  User:123:Profile  ");
        Console.WriteLine($"Normalized key: {normalized}"); // Output: user:123:profile

        // Parse complex cache key into its components
        var parts = _core.ParseKey("user:123:profile:settings:dark");
        Console.WriteLine($"Parsed key parts: {string.Join(", ", parts)}");
        // Output: user, 123, profile, settings, dark
    }

    public void KeyManagementPatterns()
    {
        // Create consistent key patterns for different entity types
        var productKey = _core.BuildKey("product", 789);
        var orderKey = _core.BuildKey("order", 456, "details");
        var cacheKey = _core.BuildKey("cache", "config", "redis");

        Console.WriteLine($"Product key: {productKey}");
        Console.WriteLine($"Order key: {orderKey}");
        Console.WriteLine($"Cache key: {cacheKey}");

        // Validate all keys before using them
        if (_core.IsValidKey(productKey) && _core.IsValidKey(orderKey))
        {
            Console.WriteLine("All keys are valid and can be used with Redis operations");
        }

        // Normalize keys before storing to ensure consistency
        var inconsistentKey = "  PRODUCT:123  ";
        var normalizedKey = _core.NormalizeKey(inconsistentKey);
        Console.WriteLine($"Consistent key format: {normalizedKey}");
    }
}
```

## BatchProcessingServiceTests

The `BatchProcessingServiceTests` class provides comprehensive unit tests for the `BatchProcessingService<T>` class, which implements batch processing with configurable batch size and optional periodic flushing. These tests verify that items are accumulated until the batch size is reached, that manual flushing works correctly, that periodic batch processing functions as expected, and that error handling maintains system stability. The test suite also validates queue size tracking, timer management, and proper disposal behavior.

### Usage Example

```csharp
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Services;

public class BatchProcessingExample
{
    private readonly Mock<ILogger<BatchProcessingService<int>>> _mockLogger = new();
    private readonly List<List<int>> _processedBatches = new();

    public BatchProcessingExample()
    {
        // Setup batch processing function
        async Task ProcessBatch(List<int> batch)
        {
            _processedBatches.Add(batch);
            await Task.CompletedTask;
        }

        // Create service with batch size of 3 and 100ms flush interval
        var service = new BatchProcessingService<int>(
            ProcessBatch,
            _mockLogger.Object,
            batchSize: 3,
            flushInterval: TimeSpan.FromMilliseconds(100)
        );
    }

    public async Task ManualBatchProcessing()
    {
        // When batch size is reached, processing happens immediately
        _processedBatches.Clear();
        var service = new BatchProcessingService<int>(
            async batch => _processedBatches.Add(batch),
            _mockLogger.Object,
            batchSize: 3
        );

        service.Enqueue(1);
        service.Enqueue(2);
        service.Enqueue(3); // Batch size reached - processes immediately

        await Task.Delay(50);
        
        // Batch is processed
        Console.WriteLine($"Processed {_processedBatches.Count} batch(es)");
        // Output: Processed 1 batch(es)
    }

    public async Task FlushControl()
    {
        var service = new BatchProcessingService<int>(
            async batch => _processedBatches.Add(batch),
            _mockLogger.Object,
            batchSize: 5
        );

        service.Enqueue(1);
        service.Enqueue(2);
        service.Enqueue(3);

        // Queue has 3 items, below batch size of 5
        Console.WriteLine($"Queue size: {service.GetQueueSize()}"); // Output: Queue size: 3

        // Manually flush to process pending items
        await service.FlushAsync();
        
        Console.WriteLine($"Processed {_processedBatches.Count} batch(es)"); // Output: Processed 1 batch(es)
    }

    public async Task PeriodicProcessing()
    {
        var service = new BatchProcessingService<string>(
            async batch => _processedBatches.Add(batch),
            _mockLogger.Object,
            batchSize: 100,
            flushInterval: TimeSpan.FromMilliseconds(150)
        );

        service.Start(); // Start periodic batch processing
        
        service.Enqueue("item1");
        service.Enqueue("item2");

        await Task.Delay(200); // Wait for flush interval
        
        Console.WriteLine($"Processed {_processedBatches.Count} batch(es)"); // Output: Processed 1 batch(es)
        
        service.Stop();
    }

    public void QueueManagement()
    {
        var service = new BatchProcessingService<int>(
            async batch => { await Task.CompletedTask; },
            _mockLogger.Object,
            batchSize: 10
        );

        service.Enqueue(1);
        service.Enqueue(2);
        service.Enqueue(3);

        Console.WriteLine($"Current queue size: {service.GetQueueSize()}"); // Output: Current queue size: 3
    }

    public async Task ErrorHandling()
    {
        var service = new BatchProcessingService<int>(
            async batch => throw new InvalidOperationException("Processing failed"),
            _mockLogger.Object,
            batchSize: 2
        );

        service.Enqueue(1);
        service.Enqueue(2);
        
        await service.FlushAsync(); // Exception is logged but processing continues
        
        Console.WriteLine("Processing continued after error");
    }

    public void DisposeBehavior()
    {
        var service = new BatchProcessingService<int>(
            async batch => await Task.CompletedTask,
            _mockLogger.Object,
            batchSize: 100,
            flushInterval: TimeSpan.FromMilliseconds(100)
        );

        service.Start();
        service.Dispose(); // Stops the flush timer
        
        // Timer is stopped, no automatic processing
        service.Enqueue(1);
        Thread.Sleep(200);
        
        Console.WriteLine("Timer stopped after disposal");
    }
}
```

This example demonstrates how to use the `BatchProcessingService<T>` for various batch processing scenarios including manual batch control, periodic flushing, queue management, error handling, and proper resource cleanup.



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


## CompressedCacheServiceTests

The `CompressedCacheServiceTests` class provides comprehensive unit tests for the `CompressedCacheService` which provides transparent compression/decompression of cached values to optimize Redis storage usage. The tests validate that small values are stored uncompressed, large values are automatically compressed using GZIP, and that all cache operations properly delegate to the underlying cache service while maintaining the compression contract.

### Usage Example

```csharp
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Services;
using RedisCachePatterns.Domain;

public class CompressedCacheExample
{
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly Mock<ILogger<CompressedCacheService>> _mockLogger = new();
    private readonly CompressedCacheService _compressedCache;

    public CompressedCacheExample()
    {
        _compressedCache = new CompressedCacheService(
            _mockCache.Object,
            _mockLogger.Object,
            compressionThresholdBytes: 1024
        );
    }

    public async Task BasicOperations()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Price = 99.99m,
            Stock = 100
        };

        // Set a small value - should not compress
        await _compressedCache.SetAsync("product:1", product);
        
        // Set a large value - should compress automatically
        var largeProduct = new Product
        {
            Id = 2,
            Name = "Large Product",
            Description = new string('x', 2000), // Large description
            Price = 199.99m,
            Stock = 50
        };
        await _compressedCache.SetAsync("product:2", largeProduct);

        // Get cached value - handles both compressed and uncompressed transparently
        var cachedProduct = await _compressedCache.GetAsync<Product>("product:1");
        Assert.NotNull(cachedProduct);
        
        var cachedLargeProduct = await _compressedCache.GetAsync<Product>("product:2");
        Assert.NotNull(cachedLargeProduct);
    }

    public async Task CacheAsidePattern()
    {
        var productId = 100;
        var cacheKey = $"product:{productId}";
        
        // Use GetOrLoadAsync for cache-aside pattern
        var product = await _compressedCache.GetOrLoadAsync<Product>(
            cacheKey,
            async () => await LoadProductFromDatabaseAsync(productId),
            TimeSpan.FromMinutes(30)
        );
        
        // Subsequent calls will use cached value
        var cachedProduct = await _compressedCache.GetOrLoadAsync<Product>(
            cacheKey,
            async () => throw new InvalidOperationException("Should not be called"),
            TimeSpan.FromMinutes(30)
        );
    }

    public async Task AdvancedOperations()
    {
        var order = new Order { Id = 1, UserId = 50, TotalAmount = 150.00m };
        
        // WriteAsync with compression
        await _compressedCache.WriteAsync(
            "order:1",
            order,
            async () => await CreateOrderInDatabaseAsync(order)
        );
        
        // Check if key exists
        var exists = await _compressedCache.ExistsAsync("order:1");
        Assert.True(exists);
        
        // Get expiration
        var ttl = await _compressedCache.GetExpirationAsync("order:1");
        
        // Remove single key
        await _compressedCache.RemoveAsync("order:1");
        
        // Remove by pattern
        await _compressedCache.RemoveByPatternAsync("orders:*");
        
        // Check keys by pattern
        var keys = await _compressedCache.GetKeysByPatternAsync("product:*");
        
        // Flush entire cache
        await _compressedCache.FlushAsync();
    }

    private async Task<Product> LoadProductFromDatabaseAsync(int productId)
    {
        await Task.CompletedTask;
        return new Product { Id = productId, Name = "Loaded Product", Price = 49.99m };
    }

    private async Task<Order> CreateOrderInDatabaseAsync(Order order)
    {
        await Task.CompletedTask;
        return new Order { Id = order.Id, UserId = order.UserId, TotalAmount = order.TotalAmount };
    }
}
```

This example demonstrates how to use the `CompressedCacheService` for transparent compression of cached values, including basic CRUD operations, cache-aside patterns, and advanced cache management operations.

## CompressionUtilTests

The `CompressionUtilTests` class provides comprehensive unit tests for the `CompressionUtil` static utility class, which offers methods for compressing and decompressing strings and byte arrays using GZIP compression. These tests validate that compression and decompression operations preserve data integrity across various scenarios including small strings, large repetitive data, empty inputs, Unicode characters, and multiline text. The test suite also verifies compression ratio calculations and decision logic for when compression is worthwhile.

### Usage Example

```csharp
using RedisCachePatterns.Utilities;

public class CompressionExample
{
    public void CompressAndStoreData()
    {
        // Compress a string for storage in Redis
        string originalText = "This is a sample text that might benefit from compression when stored in Redis cache";
        byte[] compressedData = CompressionUtil.CompressString(originalText);
        
        // Store compressed data in Redis
        await redisCache.SetAsync("compressed:text:1", compressedData);
        
        // Retrieve and decompress
        byte[] storedData = await redisCache.GetAsync<byte[]>("compressed:text:1");
        string decompressedText = CompressionUtil.DecompressString(storedData);
        
        Console.WriteLine($"Original size: {originalText.Length} bytes");
        Console.WriteLine($"Compressed size: {compressedData.Length} bytes");
        Console.WriteLine($"Ratio: {CompressionUtil.GetCompressionRatio(originalText.Length, compressedData.Length)}%");
    }
    
    public void CompressBytesForEfficientStorage()
    {
        // Compress byte arrays efficiently
        byte[] data = System.Text.Encoding.UTF8.GetBytes("Large binary data that needs efficient storage");
        byte[] compressedBytes = CompressionUtil.CompressBytes(data);
        
        // Check if compression is worthwhile before applying
        bool shouldCompress = CompressionUtil.IsCompressionWorthwhile(
            data.Length, 
            compressedBytes.Length,
            minSavingsPercent: 15
        );
        
        if (shouldCompress)
        {
            await redisCache.SetAsync("binary:data:1", compressedBytes);
        }
        else
        {
            await redisCache.SetAsync("binary:data:1", data);
        }
    }
    
    public void CompressLargeTextData()
    {
        // For large repetitive text data
        string repetitiveText = string.Concat(Enumerable.Repeat("This is a repeating pattern for compression testing. ", 100));
        byte[] compressed = CompressionUtil.CompressString(repetitiveText);
        
        double ratio = CompressionUtil.GetCompressionRatio(repetitiveText.Length, compressed.Length);
        Console.WriteLine($"Compression achieved: {ratio}% reduction");
        
        string restored = CompressionUtil.DecompressString(compressed);
        Console.WriteLine($"Data integrity: {repetitiveText == restored}");
    }
}
```

### Usage Example

```csharp
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Configuration;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Services;
using StackExchange.Redis;

public class DistributedInvalidationExample
{
    private readonly Mock<IRedisConnection> _mockRedis = new();
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly Mock<ILogger<DistributedInvalidationBroadcaster>> _mockLogger = new();
    private readonly Mock<IConnectionMultiplexer> _mockMultiplexer = new();
    private readonly Mock<ISubscriber> _mockSubscriber = new();
    
    public DistributedInvalidationExample()
    {
        _mockRedis.Setup(r => r.GetConnection()).Returns(_mockMultiplexer.Object);
        _mockMultiplexer.Setup(m => m.GetSubscriber(null)).Returns(_mockSubscriber.Object);
    }
    
    public async Task BasicInvalidation()
    {
        // Create broadcaster with default options
        var broadcaster = new DistributedInvalidationBroadcaster(
            _mockRedis.Object,
            _mockCache.Object,
            _mockLogger.Object,
            new DistributedInvalidationOptions { UseStreamFallback = false }
        );
        
        // Broadcast cache invalidation for a specific key
        await broadcaster.BroadcastAsync(
            "product:123",
            InvalidationReason.DataUpdate,
            "product-service"
        );
        
        // Get broadcast history
        var history = broadcaster.GetHistory();
        Console.WriteLine($"Broadcasted {history.Count} invalidation(s)");
    }
    
    public async Task PatternInvalidation()
    {
        // Create broadcaster
        var broadcaster = new DistributedInvalidationBroadcaster(
            _mockRedis.Object,
            _mockCache.Object,
            _mockLogger.Object,
            new DistributedInvalidationOptions { UseStreamFallback = false }
        );
        
        // Broadcast pattern-based invalidation
        await broadcaster.BroadcastPatternAsync(
            "user:*",
            InvalidationReason.ManualPurge,
            "admin-console"
        );
        
        var history = broadcaster.GetHistory();
        Console.WriteLine($"Pattern invalidation recorded: {history[0].KeyPattern}");
    }
    
    public async Task StreamFallbackInvalidation()
    {
        // Create mock stream service for fallback
        var mockStream = new Mock<IRedisStreamInvalidationService>();
        mockStream
            .Setup(s => s.PublishAsync(It.IsAny<CacheInvalidationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        // Create broadcaster with stream fallback enabled
        var broadcaster = new DistributedInvalidationBroadcaster(
            _mockRedis.Object,
            _mockCache.Object,
            _mockLogger.Object,
            new DistributedInvalidationOptions { UseStreamFallback = true },
            mockStream.Object
        );
        
        // Broadcast with stream fallback
        await broadcaster.BroadcastAsync("inventory:456");
        
        // Verify stream was used
        mockStream.Verify(
            s => s.PublishAsync(
                It.Is<CacheInvalidationEvent>(e => e.CacheKey == "inventory:456"),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }
    
    public void HistoryManagement()
    {
        // Create broadcaster with limited history size
        var broadcaster = new DistributedInvalidationBroadcaster(
            _mockRedis.Object,
            _mockCache.Object,
            _mockLogger.Object,
            new DistributedInvalidationOptions { MaxHistorySize = 5 }
        );
        
        // Add multiple invalidations
        for (int i = 0; i < 10; i++)
        {
            broadcaster.BroadcastAsync($"key:{i}").Wait();
        }
        
        // History should be bounded
        var history = broadcaster.GetHistory();
        Console.WriteLine($"History contains {history.Count} entries (max 5)");
    }
    
    public void Validation()
    {
        var broadcaster = new DistributedInvalidationBroadcaster(
            _mockRedis.Object,
            _mockCache.Object,
            _mockLogger.Object,
            new DistributedInvalidationOptions { UseStreamFallback = false }
        );
        
        // Test empty key validation
        try
        {
            broadcaster.BroadcastAsync(string.Empty).Wait();
        }
        catch (AggregateException ex) when (ex.InnerException is ArgumentException)
        {
            Console.WriteLine("Empty key correctly rejected");
        }
        
        // Test empty pattern validation
        try
        {
            broadcaster.BroadcastPatternAsync("   ").Wait();
        }
        catch (AggregateException ex) when (ex.InnerException is ArgumentException)
        {
            Console.WriteLine("Empty pattern correctly rejected");
        }
    }
}
```


## RetryHelperTests

The `RetryHelperTests` class provides comprehensive unit tests for the `RetryHelper` utility class, which implements retry mechanisms with configurable policies and circuit breaker functionality. These tests validate that retry operations correctly handle transient failures, respect maximum retry limits, apply exponential backoff, log warnings appropriately, and maintain proper circuit breaker state transitions. The test suite covers various scenarios including successful operations, failure handling, delay configurations, and circuit breaker behaviors.

### Usage Example

```csharp
using RedisCachePatterns.Utilities;
using Microsoft.Extensions.Logging;
using Moq;

public class ApiClientWithRetry
{
    private readonly Mock<ILogger> _mockLogger = new();
    
    public async Task<int> FetchDataWithRetryAsync(int maxRetries = 3, int initialDelayMs = 100)
    {
        // Retry operation with exponential backoff
        var result = await RetryHelper.ExecuteWithRetryAsync<int>(
            async () =>
            {
                // Your transient operation that might fail
                return await FetchDataFromExternalServiceAsync();
            },
            maxRetries: maxRetries,
            initialDelayMs: initialDelayMs,
            logger: _mockLogger.Object
        );
        
        return result;
    }
    
    public async Task<bool> ProcessOrderWithCircuitBreakerAsync(string circuitName, int failureThreshold = 5)
    {
        // Use circuit breaker to prevent cascading failures
        var success = await RetryHelper.CircuitBreaker.ExecuteAsync(
            circuitName,
            async () =>
            {
                // Your operation that might fail
                await ProcessPaymentAsync();
                return true;
            },
            failureThreshold: failureThreshold,
            resetTimeoutSeconds: 30
        );
        
        return success;
    }
    
    private async Task<int> FetchDataFromExternalServiceAsync()
    {
        // Simulate external service call
        await Task.CompletedTask;
        return 42; // Your actual data
    }
    
    private async Task ProcessPaymentAsync()
    {
        // Simulate payment processing
        await Task.CompletedTask;
    }
}

// Example usage
var apiClient = new ApiClientWithRetry();

// First attempt - succeeds or retries on failure
var data = await apiClient.FetchDataWithRetryAsync(maxRetries: 3, initialDelayMs: 100);

// Process with circuit breaker protection
var paymentSuccess = await apiClient.ProcessOrderWithCircuitBreakerAsync(
    "payment-circuit",
    failureThreshold: 5
);

// Reset circuit breaker when service recovers
RetryHelper.CircuitBreaker.Reset("payment-circuit");
```

## IdempotencyHelperTests

The `IdempotencyHelperTests` class provides comprehensive unit tests for the `IdempotencyHelper` utility class, which implements idempotency patterns for safely retrying operations without duplicate side effects. It tracks processed operations using unique keys and stores results to prevent reprocessing the same request multiple times. The test suite covers scenarios including basic idempotency checks, result storage and retrieval, expiration handling, type safety, and concurrent key tracking.

### Usage Example

```csharp
using RedisCachePatterns.Utilities;

public class PaymentProcessingService
{
    private readonly IdempotencyHelper _idempotencyHelper = new IdempotencyHelper();

    public async Task<PaymentResult> ProcessPaymentAsync(
        string paymentRequestId, 
        decimal amount, 
        string userId)
    {
        // Check if this payment request has already been processed
        if (_idempotencyHelper.IsProcessed(paymentRequestId))
        {
            // Return the previously stored result to avoid duplicate processing
            var cachedResult = _idempotencyHelper.GetResult<PaymentResult>(paymentRequestId);
            return cachedResult ?? throw new InvalidOperationException("Payment result not found");
        }

        try
        {
            // Process the payment (this might be called multiple times due to retries)
            var paymentResult = await ProcessPaymentInDatabaseAsync(paymentRequestId, amount, userId);

            // Mark as processed and store the result for future retries
            _idempotencyHelper.MarkAsProcessed(paymentRequestId, paymentResult);

            return paymentResult;
        }
        catch (Exception ex)
        {
            // Optionally mark as processed with the error result
            _idempotencyHelper.MarkAsProcessed(paymentRequestId, ex);
            throw;
        }
    }

    public async Task<ApiResponse> HandleApiRequestAsync(string requestId, Func<Task<ApiResponse>> handler)
    {
        // Check for idempotency using the request ID from headers
        if (_idempotencyHelper.IsProcessed(requestId))
        {
            var cachedResponse = _idempotencyHelper.GetResult<ApiResponse>(requestId);
            return cachedResponse ?? new ApiResponse(404, "Not found");
        }

        // Process the request
        var response = await handler();

        // Store the response for idempotent retries
        _idempotencyHelper.MarkAsProcessed(requestId, response);
        
        return response;
    }

    public bool IsRequestProcessed(string requestId)
    {
        // Simple check to see if a request has been processed
        return _idempotencyHelper.IsProcessed(requestId);
    }
}

// Example usage
var paymentService = new PaymentProcessingService();

// First call - processes the payment
var result1 = await paymentService.ProcessPaymentAsync(
    "payment-req-abc123", 
    100.50m, 
    "user-456"
);

// Second call with same request ID - returns cached result
var result2 = await paymentService.ProcessPaymentAsync(
    "payment-req-abc123", 
    100.50m, 
    "user-456"
);

// Results are identical due to idempotency
Console.WriteLine(result1 == result2); // True

// Custom retention period example
var shortTermHelper = new IdempotencyHelper(TimeSpan.FromHours(1));
shortTermHelper.MarkAsProcessed("temp-key", "temporary-data");

// After 1 hour, the key will expire and be considered unprocessed
```

### Key Features
- **Idempotency Tracking**: Track processed operations using unique keys
- **Result Storage**: Store and retrieve results for duplicate requests  
- **Expiration Support**: Configurable retention periods for temporary tracking
- **Type Safety**: Strongly typed result storage and retrieval
- **Concurrent Key Support**: Track multiple independent operations simultaneously
