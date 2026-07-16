// ... (rest of the file remains the same)

## DistributedLockExample

The `DistributedLockExample` class demonstrates the use of distributed locking to prevent cache stampedes and race conditions across multiple instances. It provides methods for processing orders with locks, retrieving orders with stampede protection, refunding orders with timeout locks, and confirming and shipping orders with sequential locks.

### Usage Example

```csharp
using RedisCachePatterns.Examples;
using RedisCachePatterns.Services;
using RedisCachePatterns.Infrastructure.Repositories;

// Assume services are resolved from DI container
ICacheService cacheService = /* obtain cache service */;
IOrderRepository orderRepository = /* obtain repository */;

// Create the distributed lock example handler
var distributedLockExample = new DistributedLockExample(cacheService, orderRepository);

// Process order with lock
var processOrderResult = await distributedLockExample.ProcessOrderWithLockAsync(123);
Console.WriteLine($"Process order result: {processOrderResult.Status}");

// Get order with stampede protection
var order = await distributedLockExample.GetOrderWithStampedeProtectionAsync(123);
Console.WriteLine($"Order: {order?.Id}");

// Refund order with timeout lock
var refundOrderResult = await distributedLockExample.RefundOrderWithTimeoutLockAsync(123);
Console.WriteLine($"Refund order result: {refundOrderResult.Status}");

// Confirm and ship order with locks
var confirmAndShipOrderResult = await distributedLockExample.ConfirmAndShipOrderWithLocksAsync(123);
Console.WriteLine($"Confirm and ship order result: {confirmAndShipOrderResult.Status}");
```

## MonitoringAndMetricsExample

`MonitoringAndMetricsExample` showcases how to use the cache monitoring and metrics infrastructure. It provides helper methods to display cache metrics, perform health checks, retrieve Redis server information, run a live performance monitor, generate a detailed performance report, and automatically identify common bottlenecks.

### Usage Example

```csharp
using System;
using System.Threading.Tasks;
using RedisCachePatterns.Examples;
using RedisCachePatterns.Services;
using RedisCachePatterns.Monitoring;

// Resolve required services from your DI container
ICacheService cacheService = /* obtain ICacheService */;
CacheMetricsCollector metricsCollector = /* obtain CacheMetricsCollector */;
HealthCheckService healthCheck = /* obtain HealthCheckService */;

// Create the example instance
var monitoringExample = new MonitoringAndMetricsExample(
    cacheService,
    metricsCollector,
    healthCheck);

// 1. Show a snapshot of current cache metrics
await monitoringExample.DisplayCacheMetricsAsync();

// 2. Run a health check and react to the result
bool isHealthy = await monitoringExample.CheckCacheHealthAsync();
if (!isHealthy)
{
    Console.WriteLine("⚠ Cache health check failed – investigate immediately.");
}

// 3. Print raw Redis INFO output
await monitoringExample.DisplayRedisInfoAsync();

// 4. Monitor performance for 30 seconds, updating every 5 seconds
await monitoringExample.MonitorCachePerformanceAsync(durationSeconds: 30, intervalSeconds: 5);

// 5. Generate a full performance report and inspect key values
var report = await monitoringExample.GeneratePerformanceReportAsync();
Console.WriteLine($"Report generated at {report.Timestamp}, hit‑rate: {report.HitRate:P2}");

// 6. Run automatic bottleneck detection
await monitoringExample.IdentifyBottlenecksAsync();
```

// ... (rest of the file remains the same)

## CacheAsideExample

The `CacheAsideExample` class demonstrates the Cache‑Aside pattern, showing how to read data from the cache, fall back to the repository on a miss, and populate the cache for future requests. It also includes helper methods for batch retrieval, hit‑rate demonstration, and time‑based refresh logic.

### Usage Example

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RedisCachePatterns.Examples;
using RedisCachePatterns.Services;
using RedisCachePatterns.Infrastructure.Repositories;
using RedisCachePatterns.Domain;

// Resolve required services from your DI container
ICacheService cacheService = /* obtain ICacheService */;
IProductRepository productRepository = /* obtain IProductRepository */;

// Create the example instance
var cacheAside = new CacheAsideExample(cacheService, productRepository);

// 1. Get a single product using cache‑aside
Product? product = await cacheAside.GetProductWithCacheAsideAsync(42);
Console.WriteLine($"Product: {product?.Name ?? "not found"}");

// 2. Demonstrate cache hits over multiple calls
await cacheAside.DemonstrateCacheHitsAsync(42, requestCount: 5);

// 3. Retrieve several products efficiently
int[] ids = { 1, 2, 3, 4 };
List<Product> products = await cacheAside.GetProductsByCacheAsideAsync(ids);
Console.WriteLine($"Fetched {products.Count} products");

// 4. Get a product with a custom refresh lifetime
TimeSpan lifetime = TimeSpan.FromMinutes(30);
Product? refreshed = await cacheAside.GetProductWithRefreshAsync(42, lifetime);
Console.WriteLine($"Refreshed product: {refreshed?.Name}");
```

## ErrorHandlingAndResilienceExample

The `ErrorHandlingAndResilienceExample` class demonstrates production‑grade error handling and resilience patterns for cache implementations. It includes graceful degradation when cache services fail, exponential backoff retries for transient failures, circuit breaker pattern to prevent cascading failures, bulkhead isolation to limit resource usage, timeout handling, and cache consistency validation between Redis and the database.

### Usage Example

```csharp
using System;
using System.Threading.Tasks;
using RedisCachePatterns.Examples;
using RedisCachePatterns.Services;
using RedisCachePatterns.Infrastructure.Repositories;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Results;

// Resolve required services from your DI container
ICacheService cacheService = /* obtain ICacheService */;
IProductRepository productRepository = /* obtain IProductRepository */;
ILogger<ErrorHandlingAndResilienceExample> logger = /* obtain ILogger */;

// Create the example instance
var resilienceExample = new ErrorHandlingAndResilienceExample(cacheService, productRepository, logger);

// 1. Get product with graceful degradation (cache failure falls back to database)
Product? product1 = await resilienceExample.GetProductWithGracefulDegradationAsync(42);
Console.WriteLine($"Product via graceful degradation: {product1?.Name ?? "not found"}");

// 2. Get product with retry logic (handles transient failures)
Product? product2 = await resilienceExample.GetProductWithRetryAsync(42);
Console.WriteLine($"Product via retry: {product2?.Name ?? "not found"}");

// 3. Use circuit breaker to prevent cascading failures
var breaker = new ErrorHandlingAndResilienceExample.CacheCircuitBreaker();
Product? product3 = await resilienceExample.GetProductWithCircuitBreakerAsync(42, breaker);
Console.WriteLine($"Product via circuit breaker: {product3?.Name ?? "not found"}");

// 4. Use bulkhead isolation to limit concurrent cache operations
var bulkhead = new ErrorHandlingAndResilienceExample.BulkheadIsolation(maxConcurrent: 5);
Product? product4 = await bulkhead.ExecuteAsync(() => resilienceExample.GetProductWithRetryAsync(42));
Console.WriteLine($"Product via bulkhead: {product4?.Name ?? "not found"}");

// 5. Update product with comprehensive error handling
var updateResult = await resilienceExample.UpdateProductWithErrorHandlingAsync(new Product { Id = 42, Name = "Updated Product", Price = 99.99m, Stock = 100 });
Console.WriteLine($"Update result: {updateResult.IsSuccess}");

// 6. Get product with timeout (prevents hanging)
Product? product5 = await resilienceExample.GetProductWithTimeoutAsync(42, timeoutMs: 3000);
Console.WriteLine($"Product via timeout: {product5?.Name ?? "not found"}");

// 7. Validate cache consistency between Redis and database
var consistencyResult = await resilienceExample.ValidateCacheConsistencyAsync(42);
Console.WriteLine($"Cache consistency: {consistencyResult.IsSuccess}");
```
