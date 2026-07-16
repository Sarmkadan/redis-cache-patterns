## CacheEntry

The `CacheEntry` class represents metadata about a cached entry, providing monitoring, analytics, and management capabilities for cache operations. It tracks access patterns, hit/miss ratios, expiration status, and supports tag-based categorization for efficient cache invalidation and analysis. This type is essential for cache-aside patterns, monitoring dashboards, and implementing cache warming strategies.

### Usage Example

```csharp
using RedisCachePatterns.Domain;

// Create a new cache entry for tracking a product
var productCacheEntry = new CacheEntry
{
    Key = "product:123",
    DataType = "Product",
    SizeInBytes = 1024,
    Status = "active",
    Tags = "electronics,premium"
};

// Record cache access events
productCacheEntry.RecordHit();  // Cache hit occurred
productCacheEntry.RecordHit();  // Another cache hit
productCacheEntry.RecordMiss(); // Cache miss occurred

// Check cache performance metrics
Console.WriteLine($"Hit Rate: {productCacheEntry.HitRate:F1}%");  // Output: Hit Rate: 66.7%
Console.WriteLine($"Access Count: {productCacheEntry.AccessCount}");     // Output: Access Count: 3
Console.WriteLine($"Hit Count: {productCacheEntry.HitCount}");           // Output: Hit Count: 2
Console.WriteLine($"Miss Count: {productCacheEntry.MissCount}");         // Output: Miss Count: 1

// Set expiration and check status
productCacheEntry.SetExpiration(DateTime.UtcNow.AddHours(24));
Console.WriteLine($"Time to expiry: {productCacheEntry.TimeToExpiry?.TotalHours:F1} hours");
Console.WriteLine($"Is expired: {productCacheEntry.IsExpired}");  // Output: Is expired: False

// Add and check tags for cache invalidation
productCacheEntry.AddTag("seasonal");
Console.WriteLine($"Has 'electronics' tag: {productCacheEntry.HasTag("electronics")}");  // Output: True
Console.WriteLine($"Has 'seasonal' tag: {productCacheEntry.HasTag("seasonal")}");      // Output: True

// Invalidate cache entry
productCacheEntry.Invalidate();
Console.WriteLine($"Status after invalidation: {productCacheEntry.Status}");  // Output: Status after invalidation: invalidated
Console.WriteLine(productCacheEntry.ToString());
// Output: "Cache [product:123] - Size: 1024B, Hit Rate: 66.7%, Status: invalidated"
```

## CacheKeyMetadata

The `CacheKeyMetadata` class tracks per-key cache usage statistics, enabling identification of hot keys, cold keys, and access patterns at the individual entry level. It complements aggregate cache statistics by providing detailed metrics for each cached value.

### Usage Example

```csharp
using RedisCachePatterns.Domain;
using System;

// Create metadata for a product cache key
var productMetadata = new CacheKeyMetadata
{
    Key = "product:12345",
    HitCount = 42,
    LastAccessed = DateTime.UtcNow.AddMinutes(-5),
    CreatedAt = DateTime.UtcNow.AddHours(-24),
    SizeBytes = 2048
};

// Record a cache hit
productMetadata.HitCount++;
productMetadata.LastAccessed = DateTime.UtcNow;

// Calculate cache effectiveness
var hitRate = (double)productMetadata.HitCount / 
    (productMetadata.HitCount + productMetadata.MissCount);
Console.WriteLine($"Cache hit rate: {hitRate:P1}");

// Analyze cache entry size
Console.WriteLine($"Entry size: {productMetadata.SizeBytes} bytes");
Console.WriteLine($"Size in KB: {productMetadata.SizeBytes / 1024.0:F2}");

// Check cache freshness
if (productMetadata.CreatedAt.HasValue)
{
    var age = DateTime.UtcNow - productMetadata.CreatedAt.Value;
    Console.WriteLine($"Cache age: {age.TotalHours:F1} hours");
}
```

## CachePolicy

The `CachePolicy` class defines caching behavior and expiration policies for cached data. It controls how cache entries are managed, including expiration times, cache patterns, compression settings, and size limits. This type is essential for implementing consistent caching strategies across an application.

### Usage Example

```csharp
using RedisCachePatterns.Domain;
using System;

// Create a cache policy for product data with 2-hour expiration
var productPolicy = new CachePolicy(
    key: "products:*",
    expiration: TimeSpan.FromHours(2),
    pattern: CachePattern.CacheAside
)
{
    Description = "Cache product catalog with 2-hour TTL",
    MaxSize = 5 * 1024 * 1024, // 5MB
    UseCompression = true
};

// Update expiration dynamically based on system load
if (SystemLoad.IsHigh())
{
    productPolicy.UpdateExpiration(TimeSpan.FromMinutes(30));
}

// Change cache pattern for user preferences
productPolicy.SetPattern(CachePattern.WriteThrough);

// Enable compression for large cache entries
productPolicy.EnableCompression();

// Disable the policy when maintenance is needed
productPolicy.Disable();

// Re-enable after maintenance
productPolicy.Enable();

// Display policy details
Console.WriteLine(productPolicy.ToString());
// Output: Policy [products:*] - Pattern: CacheAside, TTL: 7200s

// Check policy status
Console.WriteLine($"Is active: {productPolicy.IsActive}");
Console.WriteLine($"Created at: {productPolicy.CreatedAt}");
Console.WriteLine($"Updated at: {productPolicy.UpdatedAt}");
```

## InventoryRebalanceWorker

`InventoryRebalanceWorker` is a background service that periodically checks product stock levels, logs low‑stock warnings, and publishes `InventoryLowStockEvent` events for further handling. It can be started, stopped, and disposed of via its public API.

### Usage Example

```csharp
using System;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.BackgroundWorkers;
using RedisCachePatterns.Services;
using RedisCachePatterns.Events;

// Assume the required services are resolved from DI or created manually
ILogger<InventoryRebalanceWorker> logger = /* obtain logger */;
InventoryService inventoryService = /* obtain service */;
ProductService productService = /* obtain service */;
IEventPublisher eventPublisher = /* obtain publisher */;

// Create the worker (default interval is 30 minutes)
var rebalanceWorker = new InventoryRebalanceWorker(
    inventoryService,
    productService,
    eventPublisher,
    logger);

// Start the periodic checks
rebalanceWorker.Start();

// ... application runs ...

// When shutting down, stop the worker and clean up resources
rebalanceWorker.Stop();
rebalanceWorker.Dispose();
```
