## InvalidationHistoryEntry

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

## NegativeCacheService

`NegativeCacheService` implements cache-aside with negative caching to protect against cache penetration attacks. When a loader function returns null for a given key, the service stores a sentinel value (`"__NEGATIVE__"`) with a short TTL instead of leaving the cache empty. Subsequent requests for the same key immediately return null without hitting the data source, preventing repeated expensive lookups for non-existent entities.


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
```
