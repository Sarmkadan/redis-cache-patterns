
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

