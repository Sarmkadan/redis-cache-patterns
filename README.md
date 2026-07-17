// ... (rest of the file remains the same)

## OrderServiceExtensions

The `OrderServiceExtensions` class provides a set of extension methods for the `OrderService` class, offering additional convenience and batch operations for managing orders. These methods enable you to retrieve orders by ID, status, or date range, as well as count orders by status or user.

Here is an example of how to use the `OrderServiceExtensions` methods:

```csharp
using RedisCachePatterns.Services;

// Assume an existing OrderService instance (implementation details omitted)
OrderService orderService = /* obtain order service instance */;

// Try to get an order by its ID
var order = await orderService.TryGetOrderByIdAsync(123);

// Get orders by status with pagination
var orders = await orderService.GetOrdersByStatusPagedAsync(OrderStatus.Pending, 1, 10);

// Count orders by status
var count = await orderService.CountOrdersByStatusAsync(OrderStatus.Pending);

// Get orders from the last 7 days
var recentOrders = await orderService.GetOrdersFromLastDaysAsync(7);

// Count orders for a specific user
var userOrderCount = await orderService.CountUserOrdersAsync(123);

// Try to get an order by its order number
var orderNumber = await orderService.TryGetOrderByNumberAsync("ORD-12345");

// Get orders by status with formatted status
var formattedOrders = await orderService.GetOrdersByStatusWithFormattedStatusAsync(OrderStatus.Pending);

// Get orders in a specific date range
var dateRangeOrders = await orderService.GetOrdersInDateRangeFormattedAsync(DateTime.Now.AddDays(-7), DateTime.Now);
```

## CacheMonitor

The `CacheMonitor` class is responsible for tracking cache performance and health. It provides methods to retrieve cache statistics, print statistics, track cache entries, and calculate average hit rates.

// ... (rest of the file remains the same)

## KeyAccessStatsExtensions

The `KeyAccessStatsExtensions` class provides extension methods for the `KeyAccessStats` type, offering enhanced analytics and decision-making capabilities for cache key access patterns. These methods enable you to analyze cache efficiency, determine key hotness/coldness, calculate key age, and make informed eviction decisions based on real access data.

Here is an example of how to use the `KeyAccessStatsExtensions` methods:

```csharp
using RedisCachePatterns.Monitoring;

// Assume we have a KeyAccessStats instance from monitoring
KeyAccessStats stats = new KeyAccessStats(
    key: "user:123:profile",
    hits: 1567,
    misses: 234,
    firstSeenAt: DateTime.UtcNow.AddDays(-7),
    lastAccessedAt: DateTime.UtcNow.AddHours(-2)
);

// Get basic statistics
long hits = stats.GetHits();
long misses = stats.GetMisses();
TimeSpan age = stats.GetAge();

// Determine if key is hot or cold
bool isHot = stats.IsHotKey(); // true if >= 100 accesses
bool isCold = stats.IsColdKey(TimeSpan.FromHours(1)); // true if not accessed in last hour

// Check cache efficiency
bool poorEfficiency = stats.HasPoorEfficiency(); // true if hit rate < 50%

// Get human-readable summaries
string machineString = stats.ToMachineString(); // Compact format for logging
string summary = stats.ToSummaryString(); // Detailed human-readable format

// Make eviction decisions
bool shouldEvict = stats.ShouldEvict(); // true if key meets eviction criteria
```

## DistributedLockHelperExtensions

The `DistributedLockHelperExtensions` class provides extension methods for the `DistributedLockHelper` type, offering convenient and robust patterns for working with distributed locks in Redis. These methods simplify common scenarios like acquiring locks with timeouts, executing operations with retry logic, and managing batch operations under a single lock.

Here is an example of how to use the `DistributedLockHelperExtensions` methods:

```csharp
using RedisCachePatterns.Utilities;

// Assume we have a DistributedLockHelper instance
var lockHelper = new DistributedLockHelper("resource:123:processing");

// Acquire a lock with timeout (wait up to 5 seconds for the lock)
bool acquired = await lockHelper.AcquireAsync(TimeSpan.FromSeconds(5));
if (acquired)
{
    try
    {
        // Execute an action while holding the lock
        await lockHelper.ExecuteWithRetryAsync(async () =>
        {
            // Perform critical section work here
            Console.WriteLine("Processing resource under lock...");
            await Task.Delay(100);
        });
    }
    finally
    {
        // Release the lock when done
        lockHelper.Release();
    }
}

// Execute a function with retry logic (up to 5 retries with 200ms delay)
var result = await lockHelper.ExecuteWithRetryAsync(async () =>
{
    // Perform work and return a result
    return "processed-value";
}, maxRetries: 5, retryDelay: TimeSpan.FromMilliseconds(200));

if (result != null)
{
    Console.WriteLine($"Result: {result}");
}

// Execute multiple actions in a batch under the same lock
bool batchSuccess = await lockHelper.ExecuteBatchAsync(new List<Func<Task>>
{
    async () => await ProcessOrderAsync(123),
    async () => await UpdateInventoryAsync(123),
    async () => await SendNotificationAsync(123)
});

// Check if lock is currently held
if (lockHelper.IsHeld())
{
    string? lockValueHex = lockHelper.GetLockValueHex();
    Console.WriteLine($"Lock is held with value: {lockValueHex}");
}
```

## DiagnosticsProviderExtensions

The `DiagnosticsProviderExtensions` class provides extension methods for collecting and formatting diagnostic information from Redis cache and application health monitoring. These methods enable you to retrieve cache statistics summaries, application and system information, filter warning messages, and collect comprehensive diagnostics for troubleshooting and monitoring purposes.

Here is an example of how to use the `DiagnosticsProviderExtensions` methods:

```csharp
using RedisCachePatterns.Monitoring;

// Assume we have a diagnostics provider instance
var diagnosticsProvider = new DiagnosticsProvider();

// Check if there are any warnings
bool hasWarnings = await diagnosticsProvider.HasWarningsAsync();

// Get filtered warning messages
var warnings = await diagnosticsProvider.FilterWarningsAsync();

// Get a summary of cache statistics
string cacheStats = await diagnosticsProvider.GetCacheStatsSummaryAsync();

// Get application information
string? appInfo = await diagnosticsProvider.GetApplicationInfoAsync();

// Get system information
string? systemInfo = await diagnosticsProvider.GetSystemInfoAsync();

// Get all diagnostics as a dictionary
var allDiagnostics = await diagnosticsProvider.GetAllDiagnosticsAsync();

// Example: Log diagnostics to console
if (hasWarnings)
{
    Console.WriteLine("Cache warnings detected:");
    var warnings = await diagnosticsProvider.FilterWarningsAsync();
    foreach (var warning in warnings)
    {
        Console.WriteLine($"  - {warning}");
    }
}

Console.WriteLine($"Cache Stats: {await diagnosticsProvider.GetCacheStatsSummaryAsync()}");
Console.WriteLine($"App Info: {await diagnosticsProvider.GetApplicationInfoAsync()}");
Console.WriteLine($"System Info: {await diagnosticsProvider.GetSystemInfoAsync()}");
```