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
