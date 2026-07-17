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

## CacheInvalidationExample

The `CacheInvalidationExample` class demonstrates various cache invalidation strategies including pattern-based invalidation, selective key removal, cascading invalidation, time-based expiration, and conditional invalidation. It provides methods to invalidate entire categories of products, specific individual products, or the entire cache when needed, ensuring data consistency across distributed systems.

### Usage Example

```csharp
using System;
using System.Threading.Tasks;
using RedisCachePatterns.Examples;
using RedisCachePatterns.Services;
using RedisCachePatterns.Infrastructure.Repositories;
using RedisCachePatterns.Domain;

// Resolve required services from your DI container
ICacheService cacheService = /* obtain ICacheService */;
IProductRepository productRepository = /* obtain IProductRepository */;
ICategoryRepository categoryRepository = /* obtain ICategoryRepository */;

// Create the example instance
var cacheInvalidation = new CacheInvalidationExample(cacheService, productRepository, categoryRepository);

// 1. Invalidate all products in a specific category using pattern matching
var categoryInvalidationResult = await cacheInvalidation.InvalidateCategoryProductsAsync(5);
Console.WriteLine($"Category invalidation result: {categoryInvalidationResult.Status}");

// 2. Selectively invalidate a specific product
var specificInvalidationResult = await cacheInvalidation.InvalidateSpecificProductAsync(42);
Console.WriteLine($"Specific product invalidation result: {specificInvalidationResult.Status}");

// 3. Update product with cascading invalidation (invalidates all related caches)
var product = new Product { Id = 100, Name = "New Product", Price = 29.99m, CategoryId = 5, Stock = 50 };
var cascadingResult = await cacheInvalidation.UpdateProductWithCascadingInvalidationAsync(product);
Console.WriteLine($"Cascading invalidation result: {cascadingResult.Status}");

// 4. Update product with TTL-based invalidation (cache auto-expires after 1 minute)
var ttlResult = await cacheInvalidation.UpdateProductWithTTLInvalidationAsync(product);
Console.WriteLine($"TTL invalidation result: {ttlResult.Status}");

// 5. Update product with conditional invalidation (only invalidates if price changes > 10%)
var conditionalResult = await cacheInvalidation.UpdateProductWithConditionalInvalidationAsync(product, new[] { "10" });
Console.WriteLine($"Conditional invalidation result: {conditionalResult.Status}");

// 6. Batch invalidate multiple products
var batchResult = await cacheInvalidation.InvalidateProductsAsync(new[] { 1, 2, 3, 4, 5 });
Console.WriteLine($"Batch invalidation result: {batchResult.Status}");

// 7. Invalidate stale entries older than 30 minutes
var staleResult = await cacheInvalidation.InvalidateStaleEntriesAsync(TimeSpan.FromMinutes(30));
Console.WriteLine($"Stale entries invalidation result: {staleResult.Status}");
```

## AuthenticationMiddleware

The `AuthenticationMiddleware` class provides authentication and authorization capabilities for API endpoints. It supports both Bearer token and API key authentication schemes, validates tokens, and enriches the request context with user identity information including claims and authentication details.

### Usage Example

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Middleware;

// Create logger (typically from DI container)
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<AuthenticationMiddleware>();

// Create middleware with optional valid API keys
var middleware = new AuthenticationMiddleware(logger, new[] { "valid-api-key-123" });

// Example 1: Validate a Bearer token
var bearerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
var authContext = middleware.CreateContextFromBearerToken(bearerToken);

Console.WriteLine($"Authenticated user: {authContext.UserId}");
Console.WriteLine($"Scheme: {authContext.AuthScheme}");
Console.WriteLine($"IsAuthenticated: {authContext.IsAuthenticated}");

if (authContext.HasClaim("name"))
{
    Console.WriteLine($"Name claim: {authContext.GetClaim("name")}");
}

// Example 2: Use with InvokeAsync for HTTP-style authentication
Func<AuthContext, Task> next = async (ctx) => 
{
    Console.WriteLine($"Processing request for user: {ctx.UserId}");
    await Task.CompletedTask;
};

// Simulate an Authorization header
await middleware.InvokeAsync("Bearer " + bearerToken, next);

// Example 3: Validate an API key
Func<AuthContext, Task> apiKeyNext = async (ctx) => 
{
    Console.WriteLine($"Processing API request for key: {ctx.UserId}");
    await Task.CompletedTask;
};

await middleware.InvokeAsync("ApiKey valid-api-key-123", apiKeyNext);
```

## ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` class provides centralized error handling for ASP.NET Core applications, intercepting exceptions and converting them to structured error responses with proper logging. It maps domain-specific exceptions to appropriate HTTP status codes and includes error identifiers for tracking and debugging.

### Usage Example

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Middleware;
using RedisCachePatterns.Exceptions;

// Create logger (typically from DI container)
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<ErrorHandlingMiddleware>();

// Create the error handling middleware
var middleware = new ErrorHandlingMiddleware(logger);

// Example 1: Handle a business validation error
try
{
    // Simulate business logic that throws a BusinessException
    throw new BusinessException("Product with ID 42 not found in inventory");
}
catch (Exception ex)
{
    await middleware.HandleExceptionAsync(ex);
}

// Example 2: Use with InvokeAsync for HTTP pipeline integration
Func<Task> next = async () =>
{
    // Simulate your application logic that might throw exceptions
    await Task.CompletedTask;
};

// This will catch and handle any exceptions thrown by the next delegate
await middleware.InvokeAsync(next);

// Example 3: Handle a cache exception with proper status code
try
{
    // Simulate cache operation that throws a CacheException
    throw new CacheException("Redis connection failed");
}
catch (Exception ex)
{
    await middleware.HandleExceptionAsync(ex);
}

// The error response contains:
// - ErrorId: Unique identifier for tracking the error
// - StatusCode: HTTP status code (e.g., 400 for business errors, 500 for server errors)
// - Message: Human-readable error message
// - Details: Additional error details from inner exceptions
// - Timestamp: When the error occurred
```

## CachingHeaderMiddleware

The `CachingHeaderMiddleware` class provides middleware for setting HTTP cache control headers based on response types. It enforces cache policies at the HTTP level, allowing fine-grained control over caching behavior for different API endpoints and paths. The middleware supports public/private caching, max-age directives, stale-while-revalidate policies, and cache invalidation options.

### Usage Example

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Middleware;

// Create logger (typically from DI container)
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<CachingHeaderMiddleware>();

// Create the caching header middleware
var middleware = new CachingHeaderMiddleware(logger);

// Register custom cache policies for different path patterns
middleware.RegisterPolicy("/api/products/*", new CacheControlPolicy
{
    MaxAgeSeconds = 3600,
    SMaxAgeSeconds = 7200,
    IsPublic = true
});

middleware.RegisterPolicy("/api/admin/*", new CacheControlPolicy
{
    NoCache = true,
    NoStore = true,
    MustRevalidate = true
});

middleware.RegisterPolicy("/api/private/*", new CacheControlPolicy
{
    MaxAgeSeconds = 600,
    IsPublic = false,
    MustRevalidate = true
});

// Example 1: Apply cache headers to a request
Func<Task> next = async () =>
{
    Console.WriteLine("Processing request with cache headers...");
    await Task.CompletedTask;
};

// Apply middleware to a path
await middleware.InvokeAsync("/api/products/123", next);

// Example 2: Generate cache control header value for a policy
var policy = new CacheControlPolicy
{
    MaxAgeSeconds = 300,
    IsPublic = true,
    MustRevalidate = true
};

string headerValue = middleware.GenerateHeaderValue(policy);
Console.WriteLine($"Cache-Control: {headerValue}");
// Output: Cache-Control: public, max-age=300, must-revalidate

// Example 3: Use with ASP.NET Core pipeline
// In Startup/Program.cs:
// app.UseMiddleware<CachingHeaderMiddleware>();

// Example 4: Get default policies and customize
var defaultPolicy = middleware.GetPolicyForPath("/api/users/42");
Console.WriteLine($"Default max-age: {defaultPolicy.MaxAgeSeconds}");
```

## BatchOperationsExample

The `BatchOperationsExample` class demonstrates efficient batch operations for working with Redis caches, including bulk retrieval, batch updates, parallel vs sequential processing comparisons, cache warming, and bulk invalidation. It provides methods to handle multiple cache entries simultaneously, reducing network round-trips and improving performance in high-throughput scenarios.

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
var batchOperations = new BatchOperationsExample(cacheService, productRepository);

// 1. Retrieve multiple products in a single batch operation
List<Product> products = await batchOperations.GetProductsBatchAsync(new[] { 1, 2, 3, 4, 5 });
Console.WriteLine($"Retrieved {products.Count} products from cache");

// 2. Set multiple products in the cache with a single operation
var productsToSet = new List<Product>
{
    new Product { Id = 1, Name = "Laptop", Price = 999.99m, Stock = 10 },
    new Product { Id = 2, Name = "Mouse", Price = 29.99m, Stock = 100 },
    new Product { Id = 3, Name = "Keyboard", Price = 79.99m, Stock = 50 }
};
var setResult = await batchOperations.SetProductsBatchAsync(productsToSet);
Console.WriteLine($"Batch set result: {setResult.Status}");

// 3. Invalidate multiple products from cache in a single operation
var invalidateResult = await batchOperations.InvalidateProductsBatchAsync(new[] { 1, 2, 3 });
Console.WriteLine($"Batch invalidate result: {invalidateResult.Status}");

// 4. Warm the cache with products that are frequently accessed
var warmResult = await batchOperations.WarmCacheAsync();
Console.WriteLine($"Cache warming result: {warmResult.Status}");

// 5. Update multiple products in the cache with a single operation
var updateResult = await batchOperations.UpdateProductsBatchAsync(productsToSet);
Console.WriteLine($"Batch update result: {updateResult.Status}");

// 6. Compare sequential vs parallel processing performance
var comparisonResult = await batchOperations.CompareSequentialVsParallelAsync();
Console.WriteLine($"Sequential vs Parallel comparison completed: {comparisonResult.Status}");
```
