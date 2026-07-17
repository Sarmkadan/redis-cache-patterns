# ... (rest of the file remains the same)

## CacheKeyHelper

The `CacheKeyHelper` provides consistent key naming conventions for Redis cache operations. It includes methods for building entity keys, collection keys, wildcard patterns, and temporary keys, with built-in validation and normalization utilities.

Here is an example of how to use the `CacheKeyHelper`:
```csharp
// Build entity key
string productKey = CacheKeyHelper.BuildEntityKey<Product>(123);
Console.WriteLine(productKey); // "product:entity:123"

// Build collection key
string productsKey = CacheKeyHelper.BuildCollectionKey<Product>();
Console.WriteLine(productsKey); // "product:collection"

// Build collection with filter
string filteredProductsKey = CacheKeyHelper.BuildCollectionKey<Product>("active=true");
Console.WriteLine(filteredProductsKey); // "product:collection:active=true"

// Build custom key
string customKey = CacheKeyHelper.BuildKey("order", 456, "details");
Console.WriteLine(customKey); // "order:456:details"

// Build pattern for matching
string pattern = CacheKeyHelper.BuildEntityPattern<Product>();
Console.WriteLine(pattern); // "product:entity:*"

// Validate and normalize keys
bool isValid = CacheKeyHelper.IsValidKey(productKey);
string normalized = CacheKeyHelper.NormalizeKey("  PRODUCT:ENTITY:123  ");
Console.WriteLine(normalized); // "product:entity:123"

// Parse key components
string[] parts = CacheKeyHelper.ParseKey(productKey);
Console.WriteLine(string.Join(" | ", parts)); // "product | entity | 123"

// Distributed locks
string lockKey = CacheKeyHelper.BuildLockKey("order:123:lock");
Console.WriteLine(lockKey); // "lock:order:123:lock"

// Temporary data
string tempKey = CacheKeyHelper.BuildTemporaryKey("session");
Console.WriteLine(tempKey); // "temp:session:<guid>"
```

## HealthCheckService

The `HealthCheckService` is responsible for monitoring the health of the application and its cache system. It provides diagnostics for all critical components, including the Redis connection and memory usage. The service can be used to check the overall health of the system and to determine if it is ready to handle requests.

Here is an example of how to use the `HealthCheckService`:
```csharp
var healthCheckService = new HealthCheckService(redisConnection, logger);
var healthStatus = await healthCheckService.CheckHealthAsync();
Console.WriteLine($"Overall Health: {healthStatus.Overall}");
Console.WriteLine($"Redis Connected: {healthStatus.RedisConnected}");
Console.WriteLine($"Components: {string.Join(", ", healthStatus.Components)}");
Console.WriteLine($"Issues: {string.Join(", ", healthStatus.Issues)}");
Console.WriteLine($"Checked At: {healthStatus.CheckedAt}");
var isReady = await healthCheckService.IsReadyAsync();
Console.WriteLine($"Is Ready: {isReady}");
```

## IdempotencyHelper

The `IdempotencyHelper` ensures operations are executed only once in distributed systems by preventing duplicate processing of requests with the same idempotency key. It tracks processed operations, stores results for retrieval, and automatically cleans up expired records.

Here is an example of how to use the `IdempotencyHelper`:

```csharp
using RedisCachePatterns.Utilities;

// Create idempotency helper with default 24-hour retention
var idempotencyHelper = new IdempotencyHelper();

// Check if operation was already processed
string idempotencyKey = "payment:user-123:order-456";
bool isProcessed = idempotencyHelper.IsProcessed(idempotencyKey);
Console.WriteLine($"Is processed: {isProcessed}");

// Execute operation idempotently - will only run the operation once
var result = await idempotencyHelper.ExecuteIdempotentlyAsync(
    idempotencyKey,
    async () => 
    {
        Console.WriteLine("Executing payment operation...");
        await Task.Delay(100); // Simulate payment processing
        return "Payment completed successfully";
    }
);
Console.WriteLine($"Result: {result}");

// Get cached result for already processed operation
string cachedResult = idempotencyHelper.GetResult<string>(idempotencyKey) ?? "No cached result";
Console.WriteLine($"Cached result: {cachedResult}");

// Manually mark operation as processed and store result
idempotencyHelper.MarkAsProcessed(idempotencyKey, "Manual result");

// Clean up expired records
int expiredCount = idempotencyHelper.CleanupExpiredRecords();
Console.WriteLine($"Cleaned up {expiredCount} expired records");
```

## CacheKeyBuilder

`CacheKeyBuilder` offers a collection of static helpers for constructing Redis cache keys in a consistent, colon‑delimited format. It includes a generic `BuildKey` method for arbitrary parts and specialized methods for common entities such as users, products, orders, inventory, and distributed locks.

```csharp
using RedisCachePatterns.Utilities;

// Build a generic key from arbitrary parts
string genericKey = CacheKeyBuilder.BuildKey("session", Guid.NewGuid(), "data");
Console.WriteLine(genericKey); // e.g. "session:3f2504e0-4f89-11d3-9a0c-0305e82c3301:data"

// Entity‑specific keys
string userKey = CacheKeyBuilder.User(42);
string userByUsername = CacheKeyBuilder.UserByUsername("jdoe");
string userByEmail = CacheKeyBuilder.UserByEmail("john@example.com");
string usersByRole = CacheKeyBuilder.UsersByRole("admin");

string productKey = CacheKeyBuilder.Product(1001);
string productBySku = CacheKeyBuilder.ProductBySku("SKU-12345");
string productsByCategory = CacheKeyBuilder.ProductsByCategory("electronics");
string productSearch = CacheKeyBuilder.ProductSearch("laptop");

string orderKey = CacheKeyBuilder.Order(555);
string orderByNumber = CacheKeyBuilder.OrderByNumber("ORD-2023-001");
string ordersByUser = CacheKeyBuilder.OrdersByUser(42);
string ordersByStatus = CacheKeyBuilder.OrdersByStatus("shipped");

string inventoryKey = CacheKeyBuilder.Inventory(77);
string inventoryByProductAndWarehouse = CacheKeyBuilder.InventoryByProductAndWarehouse(1001, "WH-01");
string inventoryByProduct = CacheKeyBuilder.InventoryByProduct(1001);

string lockKey = CacheKeyBuilder.DistributedLock("order:555:process");

// Pattern for scanning all product keys
string productPattern = CacheKeyBuilder.GeneratePattern("product");
Console.WriteLine(productPattern); // "product:*"
```
