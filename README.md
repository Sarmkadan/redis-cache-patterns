
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

