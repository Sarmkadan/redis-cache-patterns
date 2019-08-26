# API Reference

Complete reference for the Redis Cache Patterns public API.

## ICacheService Interface

The main interface for all cache operations.

### Retrieval Methods

#### GetAsync<T>

Retrieves a value from cache by key.

```csharp
Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
```

**Parameters**:
- `key` (string) - Cache key
- `ct` (CancellationToken) - Cancellation token

**Returns**: Cached value or null if not found

**Throws**: `CacheException` if operation fails

**Example**:
```csharp
var product = await cacheService.GetAsync<Product>("product:123");
if (product != null)
    Console.WriteLine(product.Name);
```

#### GetAsync<T> (Multiple Keys)

Retrieves multiple values from cache.

```csharp
Task<Dictionary<string, T?>> GetAsync<T>(IEnumerable<string> keys, 
    CancellationToken ct = default);
```

**Parameters**:
- `keys` (IEnumerable<string>) - Cache keys to retrieve
- `ct` (CancellationToken) - Cancellation token

**Returns**: Dictionary mapping keys to values

**Example**:
```csharp
var keys = new[] { "product:1", "product:2", "product:3" };
var products = await cacheService.GetAsync<Product>(keys);

foreach (var kvp in products)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value?.Name}");
}
```

#### ExistsAsync

Checks if a key exists in cache.

```csharp
Task<bool> ExistsAsync(string key, CancellationToken ct = default);
```

**Example**:
```csharp
if (await cacheService.ExistsAsync("product:123"))
    Console.WriteLine("Product is cached");
```

### Storage Methods

#### SetAsync<T>

Stores a value in cache with expiration.

```csharp
Task SetAsync<T>(string key, T value, TimeSpan expiration, 
    CancellationToken ct = default);
```

**Parameters**:
- `key` (string) - Cache key
- `value` (T) - Value to cache
- `expiration` (TimeSpan) - Time to live
- `ct` (CancellationToken) - Cancellation token

**Throws**: `CacheException` if serialization fails

**Example**:
```csharp
var product = new Product { Id = 1, Name = "Widget" };
await cacheService.SetAsync(
    "product:1", 
    product, 
    TimeSpan.FromHours(2));
```

#### SetIfNotExistsAsync<T>

Stores a value only if the key doesn't exist (atomic operation).

```csharp
Task<bool> SetIfNotExistsAsync<T>(string key, T value, TimeSpan expiration,
    CancellationToken ct = default);
```

**Parameters**: Same as SetAsync

**Returns**: true if set, false if key already exists

**Example**:
```csharp
var wasSet = await cacheService.SetIfNotExistsAsync(
    "product:1", 
    newProduct, 
    TimeSpan.FromHours(2));

if (!wasSet)
    Console.WriteLine("Key already exists");
```

#### SetAsync (Batch)

Stores multiple values atomically.

```csharp
Task SetAsync<T>(Dictionary<string, T> items, TimeSpan expiration,
    CancellationToken ct = default);
```

**Example**:
```csharp
var items = new Dictionary<string, Product>
{
    { "product:1", product1 },
    { "product:2", product2 }
};

await cacheService.SetAsync(items, TimeSpan.FromHours(2));
```

### Removal Methods

#### RemoveAsync

Removes a single key from cache.

```csharp
Task RemoveAsync(string key, CancellationToken ct = default);
```

**Example**:
```csharp
await cacheService.RemoveAsync("product:123");
```

#### RemoveAsync (Multiple Keys)

Removes multiple keys from cache.

```csharp
Task RemoveAsync(IEnumerable<string> keys, CancellationToken ct = default);
```

**Example**:
```csharp
var keys = new[] { "product:1", "product:2", "product:3" };
await cacheService.RemoveAsync(keys);
```

#### InvalidateAsync

Removes keys matching a pattern.

```csharp
Task InvalidateAsync(string keyPattern, CancellationToken ct = default);
```

**Parameters**:
- `keyPattern` (string) - Redis glob pattern (supports * and ?)

**Patterns**:
- `product:*` - All products
- `product:category:5:*` - Products in category 5
- `user:123:*` - All data for user 123
- `*` - All keys (use with caution!)

**Example**:
```csharp
// Invalidate all products
await cacheService.InvalidateAsync("product:*");

// Invalidate category-specific products
await cacheService.InvalidateAsync("product:category:5:*");
```

### Utility Methods

#### GetKeysByPatternAsync

Finds all keys matching a pattern.

```csharp
Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern,
    CancellationToken ct = default);
```

**Returns**: Enumerable of matching keys

**Example**:
```csharp
var keys = await cacheService.GetKeysByPatternAsync("product:*");
foreach (var key in keys)
    Console.WriteLine(key);
```

#### GetExpireSecondsAsync

Gets the remaining TTL for a key.

```csharp
Task<long> GetExpireSecondsAsync(string key, 
    CancellationToken ct = default);
```

**Returns**: Seconds remaining, -1 if no expiration, -2 if key doesn't exist

**Example**:
```csharp
var ttl = await cacheService.GetExpireSecondsAsync("product:123");
Console.WriteLine($"TTL: {ttl} seconds");
```

#### GetInfoAsync

Gets Redis server information.

```csharp
Task<string> GetInfoAsync(CancellationToken ct = default);
```

**Returns**: Raw Redis INFO output

**Example**:
```csharp
var info = await cacheService.GetInfoAsync();
Console.WriteLine(info);
```

#### IncrementAsync

Atomically increments a numeric value.

```csharp
Task<long> IncrementAsync(string key, long value = 1,
    CancellationToken ct = default);
```

**Example**:
```csharp
// Increment hit counter
var count = await cacheService.IncrementAsync("hits:product:123", 1);
Console.WriteLine($"Hit count: {count}");
```

### Locking Methods

#### AcquireLockAsync

Acquires a distributed lock.

```csharp
Task<bool> AcquireLockAsync(string key, TimeSpan duration,
    CancellationToken ct = default);
```

**Parameters**:
- `key` (string) - Lock key
- `duration` (TimeSpan) - Lock expiration

**Returns**: true if acquired, false if already locked

**Example**:
```csharp
if (!await cacheService.AcquireLockAsync("order-123", TimeSpan.FromSeconds(30)))
{
    Console.WriteLine("Lock already held by another process");
    return;
}

try
{
    // Critical section
    await ProcessOrderAsync(123);
}
finally
{
    await cacheService.ReleaseLockAsync("order-123");
}
```

#### ReleaseLockAsync

Releases a distributed lock.

```csharp
Task ReleaseLockAsync(string key, CancellationToken ct = default);
```

**Example**:
```csharp
await cacheService.ReleaseLockAsync("order-123");
```

#### ExtendLockAsync

Extends the duration of an existing lock.

```csharp
Task<bool> ExtendLockAsync(string key, TimeSpan newDuration,
    CancellationToken ct = default);
```

**Returns**: true if extended, false if lock not held

**Example**:
```csharp
if (await cacheService.ExtendLockAsync("order-123", TimeSpan.FromSeconds(60)))
    Console.WriteLine("Lock extended");
```

## CacheKeyBuilder Utility

Provides consistent key generation.

### Methods

#### BuildProductKey

```csharp
static string BuildProductKey(int productId);
// Output: "product:{productId}"
```

#### BuildUserKey

```csharp
static string BuildUserKey(int userId);
// Output: "user:{userId}"
```

#### BuildOrderKey

```csharp
static string BuildOrderKey(int orderId);
// Output: "order:{orderId}"
```

#### BuildCategoryKey

```csharp
static string BuildCategoryKey(int categoryId);
// Output: "category:{categoryId}"
```

#### BuildInventoryKey

```csharp
static string BuildInventoryKey(int itemId);
// Output: "inventory:{itemId}"
```

## Services

### UserService

Manages user operations with caching.

```csharp
public class UserService
{
    public Task<User?> GetUserByIdAsync(int userId);
    public Task<User> CreateUserAsync(User user);
    public Task<User> UpdateUserAsync(User user);
    public Task DeleteUserAsync(int userId);
    public Task<IEnumerable<User>> GetUsersAsync(int page = 1);
}
```

### ProductService

Manages product catalog with caching.

```csharp
public class ProductService
{
    public Task<Product?> GetProductByIdAsync(int productId);
    public Task<Product> CreateProductAsync(Product product);
    public Task<Product> UpdateProductAsync(Product product);
    public Task DeleteProductAsync(int productId);
    public Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
    public Task<IEnumerable<Product>> SearchProductsAsync(string query);
}
```

### OrderService

Manages orders with distributed locks.

```csharp
public class OrderService
{
    public Task<Order?> GetOrderByIdAsync(int orderId);
    public Task<Order> CreateOrderAsync(Order order);
    public Task<Order> ConfirmOrderAsync(int orderId);
    public Task<Order> ShipOrderAsync(int orderId);
    public Task<Order> RefundOrderAsync(int orderId);
}
```

### InventoryService

Manages inventory with cache invalidation.

```csharp
public class InventoryService
{
    public Task<InventoryItem?> GetItemAsync(int itemId);
    public Task<int> GetStockAsync(int productId);
    public Task ReserveStockAsync(int productId, int quantity);
    public Task ReleaseStockAsync(int productId, int quantity);
    public Task<IEnumerable<InventoryItem>> GetLowStockItemsAsync(int threshold);
}
```

## Monitoring API

### CacheMetricsCollector

Collects cache metrics.

```csharp
public class CacheMetricsCollector
{
    public Task<double> GetHitRateAsync();
    public Task<double> GetMissRateAsync();
    public Task<double> GetAverageResponseTimeAsync();
    public Task<double> GetMaxResponseTimeAsync();
    public Task<long> GetTotalKeysAsync();
    public Task<double> GetEstimatedMemoryAsync();
    public Task<long> GetGetOperationsAsync();
    public Task<long> GetSetOperationsAsync();
    public Task<long> GetErrorCountAsync();
}
```

### HealthCheckService

Checks cache health.

```csharp
public class HealthCheckService
{
    public Task<bool> IsCacheHealthyAsync();
    public Task<int> MeasureResponseTimeAsync();
    public Task<int> GetMemoryUsageAsync();
    public Task<bool> IsConnectedAsync();
    public Task<string> GetEvictionPolicyAsync();
}
```

## Configuration

### CacheConfigurationOptions

```csharp
public class CacheConfigurationOptions
{
    public string ConnectionString { get; set; }
    public int DefaultDatabase { get; set; }
    public int DefaultExpirationSeconds { get; set; }
    public int ConnectTimeout { get; set; }
    public int SyncTimeout { get; set; }
    public bool EnableCompression { get; set; }
    public int CompressionThreshold { get; set; }
    public bool EnableMetrics { get; set; }
    public int LockTimeoutSeconds { get; set; }
    public string KeyPrefix { get; set; }
    public int MaxKeyLength { get; set; }
}
```

### Configuration Example

```csharp
services.AddRedisCacheServices("localhost:6379", options =>
{
    options.DefaultExpirationSeconds = 3600;
    options.EnableCompression = true;
    options.CompressionThreshold = 1024;
    options.EnableMetrics = true;
    options.KeyPrefix = "myapp:";
});
```

## Exceptions

### CacheException

Base exception for cache operations.

```csharp
public class CacheException : Exception
{
    public string? ErrorCode { get; set; }
    public CacheException(string message, Exception? innerException = null);
}
```

### BusinessException

Exception for business logic errors.

```csharp
public class BusinessException : Exception
{
    public BusinessException(string message);
}
```

## Extension Methods

### StringExtensions

```csharp
// Format string
"Hello {0}".FormatWith("World")  // "Hello World"

// Hash string
"password".ToSha256();            // SHA256 hash

// Truncate string
"longstring".Truncate(5);         // "long..."
```

### CollectionExtensions

```csharp
// Check if empty
var items = new List<int>();
items.IsEmpty();                   // true

// Batch operation
var batches = items.Batch(100);
```

## Result Wrapper

### OperationResult

Wraps operation outcomes.

```csharp
public class OperationResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }

    public static OperationResult Success();
    public static OperationResult Failure(string message);
}

public class OperationResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }

    public static OperationResult<T> Success(T data);
    public static OperationResult<T> Failure(string message);
}
```

**Example**:
```csharp
var result = await userService.UpdateUserAsync(user);
if (!result.IsSuccess)
    Console.WriteLine($"Error: {result.Message}");
else
    Console.WriteLine($"User updated: {result.Data?.Name}");
```

## Rate Limiting

### RateLimitingMiddleware

Limits requests per user/IP.

```csharp
app.UseMiddleware<RateLimitingMiddleware>(
    requestsPerSecond: 10,
    requestsPerMinute: 600);
```

## Caching Headers Middleware

Automatically sets cache headers.

```csharp
app.UseMiddleware<CachingHeaderMiddleware>();
```

Sets:
- `Cache-Control: public, max-age=3600`
- `ETag` for cache validation
- `Last-Modified` header

## Error Handling Middleware

Global exception handler.

```csharp
app.UseMiddleware<ErrorHandlingMiddleware>();
```

Catches and formats exceptions as JSON responses.

## Versioning

Current stable API version: **1.2.0**

See [CHANGELOG.md](../CHANGELOG.md) for breaking changes by version.
