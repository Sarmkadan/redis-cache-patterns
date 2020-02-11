// existing content ...

## Repository

The `Repository` class provides a basic implementation of the generic repository pattern. It allows you to perform CRUD (Create, Read, Update, Delete) operations on a collection of objects.

### Usage Example
```csharp
var repository = new Repository<MyEntity>();

// Get all entities
var entities = await repository.GetAllAsync();
Console.WriteLine($"Entities: {entities.Count()}");

// Get an entity by ID
var entity = await repository.GetByIdAsync(1);
Console.WriteLine($"Entity: {entity?.Name}");

// Add a new entity
var newEntity = new MyEntity { Name = "New Entity" };
await repository.AddAsync(newEntity);
Console.WriteLine($"Added entity: {newEntity.Id}");

// Update an existing entity
entity.Name = "Updated Entity";
await repository.UpdateAsync(entity);
Console.WriteLine($"Updated entity: {entity.Name}");

// Delete an entity
await repository.DeleteAsync(1);
Console.WriteLine($"Deleted entity: 1");

// Count all entities
var count = await repository.CountAsync();
Console.WriteLine($"Count: {count}");

// Check if an entity exists
var exists = await repository.ExistsAsync(1);
Console.WriteLine($"Exists: {exists}");

## AnalyticsEndpoint

The `AnalyticsEndpoint` class provides a REST API surface for querying cache analytics collected by the `CacheAnalyticsDashboard`. It exposes endpoints for retrieving analytics snapshots, rendered reports, key-specific statistics, and resetting counters.

### Usage Example
```csharp
// Create endpoint with injected dependencies
var endpoint = new AnalyticsEndpoint(
    dashboard: cacheAnalyticsDashboard,
    logger: logger,
    performanceMonitor: performanceMonitor
);

// Get full analytics snapshot with optional report
var snapshotResponse = await endpoint.GetSnapshotAsync(includeReport: true);
if (snapshotResponse.IsSuccess)
{
    var dashboard = snapshotResponse.Data;
    Console.WriteLine($"Hit rate: {dashboard.HitRate:P0}");
    Console.WriteLine($"Total keys: {dashboard.TotalKeys}");
    Console.WriteLine($"Top 5 hot keys: {string.Join(", ", dashboard.TopHotKeys.Take(5))}");
}

// Get rendered text report for console inspection
var reportResponse = await endpoint.GetReportAsync();
if (reportResponse.IsSuccess)
{
    Console.WriteLine(reportResponse.Data);
}

// Get statistics for a specific cache key
var keyStatsResponse = await endpoint.GetKeyStatsAsync("user:123:profile");
if (keyStatsResponse.IsSuccess)
{
    var stats = keyStatsResponse.Data;
    Console.WriteLine($"Accesses: {stats.TotalAccesses}, Hits: {stats.Hits}, Misses: {stats.Misses}");
}

// Reset all analytics counters (e.g., after cache flush)
var resetResponse = await endpoint.ResetAsync();
if (resetResponse.IsSuccess)
{
    Console.WriteLine("Analytics counters reset successfully");
}
```
