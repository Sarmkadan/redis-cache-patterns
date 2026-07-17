
Represents a single entry in the distributed invalidation history log, capturing details about an invalidation event, including the affected cache key, reason for invalidation, and timestamp.

### Usage Example

```csharp
var entry = new InvalidationHistoryEntry
{
    EventId    = Guid.NewGuid().ToString(),
    CacheKey   = "product:123",
    KeyPattern = null,
    Reason     = InvalidationReason.DataUpdate,
    Source     = "MyService",
    OccurredAt = DateTime.UtcNow,
    NodesNotified = 5
};

Console.WriteLine($"Event ID: {entry.EventId}");
Console.WriteLine($"Cache Key: {entry.CacheKey}");
Console.WriteLine($"Reason: {entry.Reason}");
Console.WriteLine($"Source: {entry.Source}");
Console.WriteLine($"Occurred At: {entry.OccurredAt}");
Console.WriteLine($"Nodes Notified: {entry.NodesNotified}");
```

## CompressedCacheService

`CompressedCacheService` provides a caching layer that automatically compresses cached data to minimize memory usage and network overhead for large objects. It is designed for scenarios where storage efficiency is critical and objects can benefit significantly from compression algorithms.

### Usage Example

```csharp
// Using the CompressedCacheService to cache an object
var cacheKey = "app:data:large-object";
var data = new { Id = 1, Name = "Large Dataset", Values = new int[] { 1, 2, 3 } };

// Set data into cache
await compressedCacheService.SetAsync(cacheKey, data);

// Retrieve data from cache
var cachedData = await compressedCacheService.GetAsync<object>(cacheKey);

if (await compressedCacheService.ExistsAsync(cacheKey))
{
    var stats = await compressedCacheService.GetStatisticsAsync();
    Console.WriteLine($"Cache hits: {stats.Hits}");
}
```

## RedisClusterCacheService

The RedisClusterCacheService provides a caching layer designed for high availability Redis Clusters. Automatically routes to master/ replica nodes and provides cache operation fault tolerance. It targets a Redis Cluster deployment and provides standard cache-aside operations (get, set, remove, lock).

### Usage Example

```csharp
var cluster = ConnectionMultiplexer.Connect("localhost:6379").GetCluster();
var config = new ClusterConfiguration
{
    // configure cluster
};

var cache = new RedisClusterCacheService(cluster, config, null);

// Cache-Aside: get or load
var cachedValue = await cache.GetOrLoadAsync("my_key", async () =>
{
    // load data from source
    return await LoadDataFromSourceAsync();
}, expiration: TimeSpan.FromHours(1));

// Write-Through
var writeThroughValue = await cache.WriteAsync("my_key", data, async () =>
{
    // persist data to source
    return await PersistDataToSourceAsync(data);
}, expiration: TimeSpan.FromHours(1));

// Basic operations
await cache.SetAsync("my_key", "value");
var value = await cache.GetAsync<string>("my_key");
await cache.RemoveAsync("my_key");
var exists = await cache.ExistsAsync("my_key");

// Distributed Locks
var acquired = await cache.AcquireLockAsync("my_lock", "lock_value", TimeSpan.FromMinutes(5));
if (acquired)
{
    try
    {
        // do something
    }
    finally
    {
        await cache.ReleaseLockAsync("my_lock", "lock_value");
    }
}
```

## RedisCacheService

The `RedisCacheService` class provides a robust Redis-based caching layer supporting various caching patterns, including cache-aside, write-through, and distributed locks. It offers features like automatic metadata tracking, XFetch early expiration, and cache statistics.

### Usage Example

```csharp
var connection = new RedisConnection("localhost:6379");
var cache = new RedisCacheService(connection, logger);

// Cache-Aside: get or load
var cachedValue = await cache.GetOrLoadAsync("my_key", async () =>
{
    // load data from source
    return await LoadDataFromSourceAsync();
}, expiration: TimeSpan.FromHours(1));

// Sliding expiration
var cachedValueWithSlidingExpiration = await cache.GetOrLoadWithSlidingExpirationAsync("my_key", async () =>
{
    // load data from source
    return await LoadDataFromSourceAsync();
}, TimeSpan.FromHours(1));

// Basic get
var value = await cache.GetAsync<string>("my_key");

// Set
await cache.SetAsync("my_key", "value");

// Write-Through
var writeThroughValue = await cache.WriteAsync("my_key", "data", async () =>
{
    // persist data to source
    return "persistedData";
}, expiration: TimeSpan.FromHours(1));

// Remove
await cache.RemoveAsync("my_key");

// Remove by pattern
await cache.RemoveByPatternAsync("pattern:*");

// Exists
var exists = await cache.ExistsAsync("my_key");

// Get expiration
var expiration = await cache.GetExpirationAsync("my_key");

// Distributed Locks
var acquired = await cache.AcquireLockAsync("my_lock", "lock_value", TimeSpan.FromMinutes(5));
if (acquired)
{
    try
    {
        // do something
    }
    finally
    {
        await cache.ReleaseLockAsync("my_lock", "lock_value");
    }
}

// Renew lock
var renewed = await cache.RenewLockAsync("my_lock", "lock_value", TimeSpan.FromMinutes(5));

// Get keys by pattern
var keys = await cache.GetKeysByPatternAsync("pattern:*");

// Flush
await cache.FlushAsync();

// Get statistics
var stats = await cache.GetStatisticsAsync();

// Set policy
await cache.SetPolicyAsync(new CachePolicy { Key = "my_key", DefaultExpiration = TimeSpan.FromHours(1) });

// Get policy
var policy = await cache.GetPolicyAsync("my_key");

// Early expiration
var earlyExpirationValue = await cache.GetOrLoadWithEarlyExpirationAsync("my_key", async () =>
{
    // load data from source
    return await LoadDataFromSourceAsync();
}, TimeSpan.FromHours(1));

// Get metadata
var metadata = await cache.GetKeyMetadataAsync("my_key");
```

## CacheCircuitBreakerService

`CacheCircuitBreakerService` decorates an `ICacheService` with a circuit‑breaker pattern. It watches for `CacheException` failures; when the number of consecutive failures reaches `FailureThreshold` the circuit opens, temporarily bypassing cache reads and suppressing writes until the configured break duration elapses.

### Usage Example

```csharp
// Assume an existing ICacheService implementation (e.g., RedisCacheService)
var innerCache = new RedisCacheService(new RedisConnection("localhost:6379"), logger);

// Wrap it with a circuit breaker: open after 3 failures, stay open for 10 seconds
var cache = new CacheCircuitBreakerService(innerCache, failureThreshold: 3, breakDuration: TimeSpan.FromSeconds(10));

// Cache‑aside: get or load (will fall back to the loader when the circuit is open)
var value = await cache.GetOrLoadAsync("product:42", async () =>
{
    // Load the product from a database or other source
    return await LoadProductAsync(42);
}, expiration: TimeSpan.FromMinutes(30));

// Direct read (returns default(T) when the circuit is open)
var cached = await cache.GetAsync<string>("product:42");

// Write (no‑op when the circuit is open)
await cache.SetAsync("product:42", value);

// Remove (no‑op when the circuit is open)
await cache.RemoveAsync("product:42");

// Inspect circuit state
Console.WriteLine($"State: {cache.State}");
Console.WriteLine($"Consecutive failures: {cache.ConsecutiveFailures}");
Console.WriteLine($"Opened at (UTC): {cache.OpenedAtUtc?.ToString("o") ?? "N/A"}");

// Manually record a successful call (resets the failure count)
cache.RecordSuccess();

// Manually record a failure (increments the failure count)
cache.RecordFailure();

// Reset the circuit to the closed state
cache.Reset();
```

## ICacheService

The ICacheService interface defines a caching service contract implementing cache-aside, write-through, and distributed lock patterns. Implementations handle Redis connection failures gracefully.

### Usage Example

```csharp
var cache = new MyCacheServiceImplementation(); // Assuming a concrete implementation

// Get cache statistics
var stats = await cache.GetStatisticsAsync();
Console.WriteLine($"Total keys: {stats.TotalKeys}");
Console.WriteLine($"Memory used: {stats.MemoryUsedBytes} bytes");
Console.WriteLine($"Hits: {stats.Hits}");
Console.WriteLine($"Misses: {stats.Misses}");
Console.WriteLine($"Captured at: {stats.CapturedAt}");
```

## CacheWarmingService

The `CacheWarmingService` is responsible for pre-loading cache keys before they are accessed, reducing the load on backend systems and improving response times for critical operations. It supports multiple warming strategies that can be added dynamically and executed asynchronously. The service tracks execution metrics including start/end times, duration, success/failure counts, and any errors encountered during warming.

### Usage Example

```csharp
// Setup dependencies
var cacheService = new RedisCacheService(redisConnection, logger);
var warmingService = new CacheWarmingService(cacheService);

// Add warming strategies
warmingService.AddStrategy(new PredefinedKeyStrategy("product:123", "Product 123"));
warmingService.AddStrategy(new PredefinedKeyStrategy("product:456", "Product 456"));
warmingService.AddStrategy(new PredefinedKeyStrategy("category:electronics", "Electronics Category"));

// Execute warming asynchronously
var result = await warmingService.WarmAsync();

// Output results
Console.WriteLine($"Warming completed: {result}");
Console.WriteLine($"Total items warmed: {warmingService.TotalItemsWarmed}");
Console.WriteLine($"Successful strategies: {warmingService.SuccessfulStrategies}");
Console.WriteLine($"Failed strategies: {warmingService.FailedStrategies}");
Console.WriteLine($"Duration: {warmingService.DurationMs}ms");
Console.WriteLine($"Started at: {warmingService.StartedAt}");
Console.WriteLine($"Completed at: {warmingService.CompletedAt}");

// Inspect errors
if (warmingService.Errors.Any())
{
    Console.WriteLine("Errors encountered:");
    foreach (var error in warmingService.Errors)
    {
        Console.WriteLine($"- {error}");
    }
}
```

## WarmingEntry

`WarmingEntry` represents a cache warming entry that defines what to warm and how to warm it. It encapsulates a cache key, a value factory function to generate the cached value, expiration policy, and priority settings. This type is used by cache warming strategies to coordinate pre-loading of critical cache keys before they are accessed.

### Usage Example

```csharp
// Create a warming entry for a product detail page
var productEntry = new WarmingEntry
{
    Key = "product:12345:details",
    ValueFactory = async () =>
    {
        // Load product data from database or API
        var product = await productRepository.GetProductByIdAsync(12345);
        return product;
    },
    Expiration = TimeSpan.FromHours(2),
    Priority = WarmingPriority.High
};

// Create a warming entry for a category listing with sliding expiration
var categoryEntry = new WarmingEntry
{
    Key = "category:electronics:products",
    ValueFactory = async () =>
    {
        // Load electronics products from database
        var products = await productRepository.GetProductsByCategoryAsync("Electronics");
        return products;
    },
    Expiration = TimeSpan.FromMinutes(30),
    Priority = WarmingPriority.Normal
};

// Create a warming entry with no explicit expiration (will use cache defaults)
var featuredEntry = new WarmingEntry
{
    Key = "home:featured-products",
    ValueFactory = async () =>
    {
        // Load featured products
        var featured = await productRepository.GetFeaturedProductsAsync();
        return featured;
    },
    Priority = WarmingPriority.Low
};

// Use with CacheWarmingScheduler
var scheduler = new CacheWarmingScheduler();
scheduler.Start();

// Add warming entries to scheduler
scheduler.Add(productEntry);
scheduler.Add(categoryEntry);
scheduler.Add(featuredEntry);

// Execute warming
var result = await scheduler.ExecuteAsync();

// Output results
Console.WriteLine($"Warming completed: {result}");
Console.WriteLine($"Total entries warmed: {scheduler.TotalItemsWarmed}");

// Stop the scheduler when done
scheduler.Stop();
scheduler.Dispose();
```

## CacheInvalidationService

The `CacheInvalidationService` manages cache invalidation strategies and patterns, supporting tag-based invalidation, pattern matching, and smart dependency tracking. It maintains an in‑memory index of cache keys mapped to tags, enabling efficient group invalidation and dependency management across distributed cache systems.

### Usage Example

```csharp
// Setup dependencies
var cacheService = new RedisCacheService(redisConnection, logger);
var eventPublisher = new EventPublisher();
var invalidationService = new CacheInvalidationService(cacheService, eventPublisher, logger);

// Register cache keys with tags for group invalidation
invalidationService.RegisterKeyWithTags("product:123", "products", "electronics");
invalidationService.RegisterKeyWithTags("product:456", "products", "electronics");
invalidationService.RegisterKeyWithTags("category:electronics", "categories");

// Invalidate all products in the electronics category
await invalidationService.InvalidateByTagAsync("electronics");

// Get all keys associated with a tag
var electronicsKeys = invalidationService.GetKeysByTag("electronics");
Console.WriteLine($"Keys tagged as electronics: {string.Join(", ", electronicsKeys)}");

// Invalidate by pattern (e.g., all product keys)
await invalidationService.InvalidateByPatternAsync("product:*");

// Invalidate a specific key with its dependencies
await invalidationService.InvalidateWithDependenciesAsync("product:123");
```

## BatchProcessingService

The `BatchProcessingService<T>` provides efficient batch processing capabilities for handling bulk operations. It groups items into batches and processes them asynchronously using a configurable batch size and flush interval. This service is ideal for scenarios requiring high-throughput processing of queued items while minimizing resource usage.

### Usage Example

```csharp
// Setup a batch processor for processing user events
var batchProcessor = new BatchProcessingService<UserEvent>(
    async (batch) => 
    {
        // Process the batch of user events
        foreach (var userEvent in batch)
        {
            Console.WriteLine($"Processing event for user {userEvent.UserId}: {userEvent.Action}");
        }
        
        // Simulate database persistence
        await Task.Delay(100);
    },
    logger,
    batchSize: 50,
    flushInterval: TimeSpan.FromSeconds(10)
);

// Start the batch processor with automatic flushing
batchProcessor.Start();

// Enqueue items to be processed in batches
for (int i = 0; i < 150; i++)
{
    batchProcessor.Enqueue(new UserEvent
    {
        UserId = i,
        Action = "UpdateProfile",
        Timestamp = DateTime.UtcNow
    });
}

// Check queue size
Console.WriteLine($"Current queue size: {batchProcessor.GetQueueSize()}");

// Manually flush remaining items
await batchProcessor.FlushAsync();

// Stop the batch processor
batchProcessor.Stop();
```

## InventoryService

The `InventoryService` provides inventory management functionality with distributed locking to prevent race conditions across multiple application instances. It supports inventory lookup by ID, product and warehouse combinations, reservation and release operations, stock receiving and dispatching, and low stock monitoring.

### Usage Example

```csharp
// Setup dependencies
var repository = new InventoryRepository(connectionString);
var cache = new RedisCacheService(redisConnection, logger);
var inventoryService = new InventoryService(repository, cache, logger);

// Get inventory by ID
var inventoryItem = await inventoryService.GetInventoryByIdAsync(1);
if (inventoryItem != null)
{
    Console.WriteLine($"Inventory ID: {inventoryItem.Id}, Product: {inventoryItem.ProductId}, Warehouse: {inventoryItem.Warehouse}");
}

// Get inventory by product and warehouse
var productInventory = await inventoryService.GetByProductAndWarehouseAsync(101, "Warehouse-A");
if (productInventory != null)
{
    Console.WriteLine($"Product {productInventory.ProductId} in {productInventory.Warehouse}: {productInventory.QuantityAvailable} available");
}

// Get all inventory items for a product
var productItems = await inventoryService.GetInventoryByProductAsync(101);
foreach (var item in productItems)
{
    Console.WriteLine($"Warehouse {item.Warehouse}: {item.QuantityAvailable} available");
}

// Reserve inventory with distributed lock
var reservationSuccess = await inventoryService.ReserveInventoryAsync(
    productId: 101,
    warehouse: "Warehouse-A",
    quantity: 5,
    instanceId: Guid.NewGuid().ToString()
);

if (reservationSuccess)
{
    Console.WriteLine("Inventory reserved successfully");
}

// Release reservation
var releaseSuccess = await inventoryService.ReleaseReservationAsync(
    inventoryId: 1,
    quantity: 2
);

// Receive stock
var receiveSuccess = await inventoryService.ReceiveStockAsync(
    productId: 101,
    warehouse: "Warehouse-A",
    quantity: 50,
    instanceId: Guid.NewGuid().ToString()
);

// Dispatch stock
var dispatchSuccess = await inventoryService.DispatchStockAsync(
    productId: 101,
    warehouse: "Warehouse-A",
    quantity: 3,
    instanceId: Guid.NewGuid().ToString()
);

// Get low stock items
var lowStockItems = await inventoryService.GetLowStockItemsAsync();
foreach (var item in lowStockItems)
{
    Console.WriteLine($"Low stock alert: Product {item.ProductId} - only {item.QuantityAvailable} available");
}

// Get total quantity for a product across all warehouses
var totalQuantity = await inventoryService.GetTotalProductQuantityAsync(101);
Console.WriteLine($"Total quantity for product 101: {totalQuantity}");
```

## UserService

The `UserService` handles user operations with an integrated caching strategy. It provides methods for CRUD operations on users, authentication, and querying users by various criteria, all backed by Redis caching to improve performance and reduce database load.

### Usage Example

```csharp
// Setup dependencies
var repository = new UserRepository(connectionString);
var cache = new RedisCacheService(redisConnection, logger);
var userService = new UserService(repository, cache, logger);

// Create a new user
var newUser = new User
{
    Id = 1,
    Username = "john.doe",
    Email = "john.doe@example.com",
    PasswordHash = "hashed_password_123",
    Role = UserRole.User,
    IsActive = true
};
var createdUser = await userService.CreateUserAsync(newUser);
Console.WriteLine($"Created user: {createdUser.Username}");

// Get user by ID (uses cache-aside pattern)
var user = await userService.GetUserByIdAsync(1);
if (user != null)
{
    Console.WriteLine($"User found: {user.Username} ({user.Email})");
}

// Get user by username
var userByUsername = await userService.GetUserByUsernameAsync("john.doe");
if (userByUsername != null)
{
    Console.WriteLine($"User found by username: {userByUsername.Email}");
}

// Update user
user.Email = "john.doe.updated@example.com";
var updatedUser = await userService.UpdateUserAsync(user);
Console.WriteLine($"Updated user email to: {updatedUser.Email}");

// Authenticate user
await userService.AuthenticateAsync("john.doe", "hashed_password_123");
Console.WriteLine("Authentication successful");

// Get all users (uses cache)
var allUsers = await userService.GetAllUsersAsync();
Console.WriteLine($"Total users: {allUsers.Count()}");

// Get active users
var activeUsers = await userService.GetActiveUsersAsync();
Console.WriteLine($"Active users: {activeUsers.Count()}");

// Get users by role
var adminUsers = await userService.GetUsersByRoleAsync(UserRole.Admin);
Console.WriteLine($"Admin users: {adminUsers.Count()}");

// Deactivate user
await userService.DeactivateUserAsync(1);
Console.WriteLine("User deactivated");

// Delete user
await userService.DeleteUserAsync(1);
Console.WriteLine("User deleted");
```

## OrderService

The `OrderService` provides order management functionality with comprehensive caching strategies and distributed locking for concurrent operations. It implements the cache-aside pattern for all read operations and automatically invalidates relevant cache keys on write operations. The service supports order lookup by ID or order number, user-specific order queries, status-based filtering, date range queries, and order lifecycle management (creation, confirmation, shipping, completion, cancellation) with automatic cache invalidation.

### Usage Example

```csharp
// Setup dependencies
var repository = new OrderRepository(connectionString);
var cache = new RedisCacheService(redisConnection, logger);
var orderService = new OrderService(repository, cache, logger);

// Create a new order
var newOrder = new Order
{
  UserId = 42,
  Items = new List<OrderItem>
  {
    new OrderItem { ProductId = 101, Quantity = 2, Price = 19.99m },
    new OrderItem { ProductId = 202, Quantity = 1, Price = 49.99m }
  },
  ShippingAddress = "123 Main St, City"
};
var createdOrder = await orderService.CreateOrderAsync(newOrder);
Console.WriteLine($"Created order: {createdOrder.OrderNumber} (ID: {createdOrder.Id})");

// Get order by ID (uses cache-aside pattern)
var order = await orderService.GetOrderByIdAsync(createdOrder.Id);
if (order != null)
{
  Console.WriteLine($"Order found: {order.OrderNumber} - Status: {order.Status}");
}

// Get order by order number
var orderByNumber = await orderService.GetOrderByNumberAsync(createdOrder.OrderNumber);
if (orderByNumber != null)
{
  Console.WriteLine($"Order found by number: {orderByNumber.Id}");
}

// Get all orders for a user
var userOrders = await orderService.GetUserOrdersAsync(42);
Console.WriteLine($"User 42 has {userOrders.Count()} orders");

// Get orders by status
var pendingOrders = await orderService.GetOrdersByStatusAsync(OrderStatus.Pending);
Console.WriteLine($"Pending orders: {pendingOrders.Count()}");

// Get pending orders (convenience method)
var pending = await orderService.GetPendingOrdersAsync();
Console.WriteLine($"Total pending orders: {pending.Count()}");

// Get orders in date range
var dateRangeOrders = await orderService.GetOrdersInDateRangeAsync(
  DateTime.UtcNow.AddDays(-7),
  DateTime.UtcNow
);
Console.WriteLine($"Orders in last week: {dateRangeOrders.Count()}");

// Confirm order with distributed lock
var confirmSuccess = await orderService.ConfirmOrderAsync(
  createdOrder.Id,
  Guid.NewGuid().ToString()
);
Console.WriteLine($"Order confirmation successful: {confirmSuccess}");

// Ship order
var shipSuccess = await orderService.ShipOrderAsync(
  createdOrder.Id,
  "UPS123456789"
);
Console.WriteLine($"Order shipped successfully: {shipSuccess}");

// Complete order
var completeSuccess = await orderService.CompleteOrderAsync(createdOrder.Id);
Console.WriteLine($"Order completed successfully: {completeSuccess}");

// Cancel order
var cancelSuccess = await orderService.CancelOrderAsync(createdOrder.Id);
Console.WriteLine($"Order cancelled successfully: {cancelSuccess}");
```

## ProductService

The `ProductService` provides product catalog management with comprehensive caching strategies. It implements the cache-aside pattern for all read operations and automatically invalidates relevant cache keys on write operations. The service supports product lookup by ID or SKU, category-based queries, low stock monitoring, and price/stock updates with automatic cache invalidation.


### Usage Example

```csharp
// Setup dependencies
var repository = new ProductRepository(connectionString);
var cache = new RedisCacheService(redisConnection, logger);
var productService = new ProductService(repository, cache, logger);

// Create a new product
var newProduct = new Product
{
  Id = 1,
  Name = "Premium Wireless Headphones",
  Sku = "AUD-WH-001",
  Category = "Electronics",
  Price = 199.99m,
  StockQuantity = 50
};
var createdProduct = await productService.CreateProductAsync(newProduct);
Console.WriteLine($"Created product: {createdProduct.Name} (SKU: {createdProduct.Sku})");

// Get product by ID (uses cache-aside pattern)
var product = await productService.GetProductByIdAsync(1);
if (product != null)
{
  Console.WriteLine($"Product found: {product.Name} - ${product.Price}");
}

// Get product by SKU
var productBySku = await productService.GetProductBySkuAsync("AUD-WH-001");
if (productBySku != null)
{
  Console.WriteLine($"Product found by SKU: {productBySku.Name}");
}

// Get products by category
var electronicsProducts = await productService.GetProductsByCategoryAsync("Electronics");
foreach (var p in electronicsProducts)
{
  Console.WriteLine($"{p.Name} - ${p.Price} ({p.StockQuantity} in stock)");
}

// Search products
var searchResults = await productService.SearchProductsAsync("headphones");
foreach (var p in searchResults)
{
  Console.WriteLine($"Search result: {p.Name}");
}

// Get low stock products
var lowStockProducts = await productService.GetLowStockProductsAsync();
foreach (var p in lowStockProducts)
{
  Console.WriteLine($"Low stock alert: {p.Name} - only {p.StockQuantity} left");
}

// Update product price
await productService.UpdateProductPriceAsync(1, 179.99m);
Console.WriteLine("Product price updated");

// Update product stock
await productService.UpdateProductStockAsync(1, 45);
Console.WriteLine("Product stock updated");

// Update product
product.Price = 169.99m;
var updatedProduct = await productService.UpdateProductAsync(product);
Console.WriteLine($"Product updated: {updatedProduct.Name}");

// Delete product
await productService.DeleteProductAsync(1);
Console.WriteLine("Product deleted");
```

## NegativeCacheService

`NegativeCacheService` implements cache-aside with negative caching to protect against cache penetration attacks. When a loader function returns null for a given key, the service stores a sentinel value (`"__NEGATIVE__"`) with a short TTL instead of leaving the cache empty. Subsequent requests for the same key immediately return null without hitting the data source, preventing repeated expensive lookups for non‑existent entities.


### Usage Example

```csharp
// Setup dependencies
var cache = new RedisCacheService(redisConnection, logger);
var negativeCache = new NegativeCacheService(cache, TimeSpan.FromSeconds(30));

// Cache-aside with negative caching - returns null for non-existent entities without repeated source hits
var product = await negativeCache.GetOrLoadWithNegativeCachingAsync<Product>(
    "product:99999",  // Non-existent product ID
    async () => await productRepository.GetProductByIdAsync(99999)
);

Console.WriteLine(product); // null

// Check if the key is negatively cached
var isNegative = await negativeCache.IsNegativelyCachedAsync("product:99999");
Console.WriteLine($"Is negatively cached: {isNegative}"); // true

Console.WriteLine($"Negative hits: {negativeCache.NegativeHits}"); // 1

// Explicitly mark a key as known-missing
await negativeCache.MarkNegativeAsync("user:12345");

// Clear a negative entry to retry the loader
var wasCleared = await negativeCache.ClearNegativeAsync("product:99999");
Console.WriteLine($"Was cleared: {wasCleared}"); // true

// Now the next call will attempt to load from source again
```

## ModuleRegistration

`ModuleRegistration` provides lifecycle management for background workers and services in the Redis Cache Patterns library. It serves as a centralized coordinator for starting, stopping, and managing long-running operations across the application. The class maintains a collection of active workers and provides methods for controlled worker lifecycle management, including explicit worker activation and graceful shutdown.


### Usage Example

```csharp
// Setup dependency injection container
var services = new ServiceCollection();

// Register background workers
services.AddSingleton<CacheCleanupWorker>();
services.AddSingleton<InventoryRebalanceWorker>();
services.AddSingleton<CacheWarmerWorker>();

// Register ModuleRegistration to manage worker lifecycle
services.AddSingleton<ModuleRegistration>();

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Create ModuleRegistration instance
var moduleRegistration = new ModuleRegistration(serviceProvider);

// Start all registered background workers
moduleRegistration.StartBackgroundWorkers();

// Start a specific worker explicitly
moduleRegistration.StartWorker<CacheCleanupWorker>();

// Later, gracefully stop all workers when application shuts down
moduleRegistration.StopBackgroundWorkers();

// ModuleRegistration implements IDisposable for resource cleanup
moduleRegistration.Dispose();
```

## CacheConfiguration

`CacheConfiguration` represents the configuration settings for Redis cache connections and behavior. It encapsulates connection parameters, timeouts, compression settings, and eviction policies that control how the cache service operates. This configuration can be loaded from environment variables or constructed programmatically for flexible deployment scenarios.

### Usage Example

```csharp
// Create a cache configuration programmatically
var config = new CacheConfiguration
{
    ConnectionString = "localhost:6379",
    DatabaseId = 1,
    ConnectTimeoutMs = 10000,
    SyncTimeoutMs = 10000,
    EnableCompression = true,
    MaxCacheSizeBytes = 200 * 1024 * 1024, // 200MB
    EvictionPolicy = "volatile-ttl"
};

Console.WriteLine(config);

// Or load from environment variables
var envConfig = CacheConfiguration.FromEnvironment();
Console.WriteLine($"Loaded config: {envConfig}");
```

## RedisCachePatternsOptions

`RedisCachePatternsOptions` provides configuration settings for the Redis Cache Patterns library. It controls connection parameters, timeouts, compression settings, cache size limits, and eviction policies that determine how the cache services operate. This options class is used when registering the library with dependency injection and can be configured programmatically or loaded from configuration files.

### Usage Example

```csharp
// Configure RedisCachePatternsOptions programmatically
var options = new RedisCachePatternsOptions
{
    ConnectionString = "localhost:6379,password=secret",
    DatabaseId = 1,
    ConnectTimeoutMs = 5000,
    SyncTimeoutMs = 5000,
    EnableCompression = true,
    MaxCacheSizeBytes = 100 * 1024 * 1024, // 100MB
    EvictionPolicy = "allkeys-lru",
    DistributedInvalidation = new DistributedInvalidationOptions
    {
        Enabled = true,
        ChannelName = "redis-cache-patterns:invalidation",
        PublishOnInvalidation = true
    }
};

// Use with ServiceCollection extension
services.AddRedisCachePatterns(options);

// Or configure via connection string
services.AddRedisCachePatterns("localhost:6379");
```

## CacheConfigurationBuilder

`CacheConfigurationBuilder` provides a fluent interface for configuring cache services with policies, compression, warming, and monitoring features. It enables declarative configuration of caching behavior through a chainable builder pattern, making it easy to customize expiration times, add cache policies, and enable optional features.

### Usage Example

```csharp
// Configure cache with default expiration, policies, and features
var cacheConfig = new CacheConfigurationBuilder()
    .WithDefaultExpiration(TimeSpan.FromHours(2))
    .AddPolicy("product:*", TimeSpan.FromMinutes(30))
    .AddPolicy("user:*", TimeSpan.FromMinutes(15))
    .EnableCompression(thresholdBytes: 2048)
    .EnableWarming()
    .EnableMonitoring()
    .Build();

Console.WriteLine(cacheConfig);

// Use with cache service
var cacheService = new RedisCacheService(redisConnection, logger);
var options = cacheConfig.Build();

// Apply configuration
cacheService.DefaultExpiration = options.DefaultExpiration;
foreach (var policy in options.Policies)
{
    await cacheService.SetPolicyAsync(policy);
}

if (options.CompressionEnabled)
{
    cacheService.EnableCompression(options.CompressionThresholdBytes);
}
```

## CacheMetricsCollector

`CacheMetricsCollector` collects and aggregates cache performance metrics for monitoring and analysis. It tracks cache hits, misses, evictions, errors, and latency metrics to provide insights into cache effectiveness and system health. The collector maintains running totals that can be reset and provides a comprehensive snapshot of cache performance through the `GetMetrics()` method.

### Usage Example

```csharp
// Setup dependencies
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<CacheMetricsCollector>();
var metricsCollector = new CacheMetricsCollector(logger);

// Record cache operations
metricsCollector.RecordHit("product:123", 15);  // 15ms latency
metricsCollector.RecordHit("user:456", 8);    // 8ms latency
metricsCollector.RecordMiss("product:999", 200); // 200ms latency (cache miss)
metricsCollector.RecordEviction(2);              // 2 keys evicted
metricsCollector.RecordError("SetAsync");       // Error occurred

// Get current metrics snapshot
var metrics = metricsCollector.GetMetrics();
Console.WriteLine($"Cache Performance Metrics:");
Console.WriteLine($"  Total Hits: {metrics.TotalHits}");
Console.WriteLine($"  Total Misses: {metrics.TotalMisses}");
Console.WriteLine($"  Hit Rate: {metrics.HitRate:F2}%");
Console.WriteLine($"  Average Hit Latency: {metrics.AverageHitLatencyMs}ms");
Console.WriteLine($"  Average Miss Latency: {metrics.AverageMissLatencyMs}ms");
Console.WriteLine($"  Evictions: {metrics.Evictions}");
Console.WriteLine($"  Errors: {metrics.Errors}");
Console.WriteLine($"  Uptime: {metrics.UptimeSeconds:F0} seconds");

// Reset metrics for a new monitoring period
metricsCollector.Reset();
```

## ServiceRegistration

`ServiceRegistration` provides a set of extension methods that simplify the registration of the Redis cache patterns library into an `IServiceCollection`. It offers overloads for configuring the cache via a connection string, an options object, or an `IConfiguration` section, and also includes helpers for adding background workers and distributed invalidation support.

### Usage Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using RedisCachePatterns.Configuration;
using RedisCachePatterns.Infrastructure.Cache;

// Create a service collection
var services = new ServiceCollection();

// 1. Register cache patterns using a connection string
services.AddRedisCachePatterns("localhost:6379", configBuilder =>
{
    // optional cache configuration, e.g. enable compression
    // configBuilder.EnableCompression();
});

// 2. Register cache patterns using an options instance
var options = new RedisCachePatternsOptions
{
    ConnectionString = "localhost:6379"
};
services.AddRedisCachePatterns(options);

// 3. Register cache patterns using IConfiguration
var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new[]
    {
        new KeyValuePair<string, string>("RedisCachePatterns:ConnectionString", "localhost:6379")
    })
    .Build();

services.AddRedisCachePatterns(configuration);

// Register background workers
services.AddBackgroundWorkers();

// Register distributed invalidation broadcaster
services.AddDistributedInvalidation(new DistributedInvalidationOptions
{
    // configure options as needed
});
```

## KeyAccessStats

`KeyAccessStats` represents per-key cache access statistics tracked by the `CacheAnalyticsDashboard`. It captures detailed metrics about individual cache key usage patterns, including hit/miss counts, timestamps, and calculated hit rates. This data is used to identify hot keys, cold keys, and keys with poor cache effectiveness for optimization purposes.

### Usage Example

```csharp
// Create a cache analytics dashboard
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<CacheAnalyticsDashboard>();
var dashboard = new CacheAnalyticsDashboard(logger);

// Simulate cache operations
for (int i = 0; i < 100; i++)
{
    dashboard.RecordHit("product:123");
    dashboard.RecordMiss("product:456");
}

// Record a few misses for product:456 to create a low hit rate scenario
for (int i = 0; i < 10; i++)
{
    dashboard.RecordMiss("product:456");
}

// Get statistics for a specific key
var stats = dashboard.GetKeyStats("product:123");
if (stats != null)
{
    Console.WriteLine($"Key: {stats.Key}");
    Console.WriteLine($"Hits: {stats.Hits}");
    Console.WriteLine($"Misses: {stats.Misses}");
    Console.WriteLine($"Hit Rate: {stats.HitRate:P}");
    Console.WriteLine($"Total Accesses: {stats.TotalAccesses}");
    Console.WriteLine($"First Seen: {stats.FirstSeenAt:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"Last Accessed: {stats.LastAccessedAt:yyyy-MM-dd HH:mm:ss}");
}

// Get an analytics snapshot
var snapshot = dashboard.GetSnapshot();
Console.WriteLine($"Overall Hit Rate: {snapshot.OverallHitRate:P}");
Console.WriteLine($"Total Keys Tracked: {snapshot.UniqueKeysTracked}");
```

## ServiceRegistration

## DiagnosticsProvider

`DiagnosticsProvider` offers a convenient way to generate detailed diagnostics reports about the application and cache health. It gathers runtime information, system metrics, and cache statistics, and can render the report as plain objects or as an HTML document.

### Usage Example

```csharp
// Assume an ICacheService implementation is already registered
var cacheService = new RedisCacheService(new RedisConnection("localhost:6379"), logger);
var diagnostics = new DiagnosticsProvider(cacheService, logger);

// Generate a strongly‑typed report
DiagnosticReport report = await diagnostics.GenerateReportAsync();
Console.WriteLine($"Report generated at: {report.GeneratedAt}");
Console.WriteLine($"Uptime: {report.ApplicationInfo["Uptime"]}");
Console.WriteLine($"Cache hits: {report.CacheInfo["HitRate"]}");

// Generate an HTML version of the same report
string html = await diagnostics.GenerateHtmlReportAsync();
await File.WriteAllTextAsync("diagnostics.html", html);
Console.WriteLine("HTML diagnostics report written to diagnostics.html");
```
