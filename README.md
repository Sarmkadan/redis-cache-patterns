// README.md
## StampedeProtectedCacheService

The `StampedeProtectedCacheService` class provides a set of extension methods for the `ICacheService` interface, offering additional convenience and batch operations for managing cache entries. These methods enable you to retrieve cache entries by key, set cache entries with expiration, remove cache entries, and check if a key exists in the cache.

Here is an example of how to use the `StampedeProtectedCacheService` methods:
```csharp
using RedisCachePatterns.Services;

// Assume an existing ICacheService instance (implementation details omitted)
ICacheService cacheService = /* obtain cache service instance */;

// Try to get a cache entry by its key
var entry = await cacheService.GetAsync<string>("key:123");

// Set a cache entry with expiration
await cacheService.SetAsync("key:123", "value", TimeSpan.FromHours(1));

// Remove a cache entry
await cacheService.RemoveAsync("key:123");
```


## BulkGetRequest

The `BulkGetRequest` class defines a structure for requesting multiple cache entries in a single operation. By providing a list of keys, it allows for efficient batch retrieval of data from the cache.

Example usage:
```csharp
using RedisCachePatterns.Domain;

// Define the keys to retrieve
var request = new BulkGetRequest
{
    Keys = new List<string> { "key:1", "key:2", "key:3" },
    ReturnNullForMissing = true
};

// Use the request with a bulk operation service
// Assuming an existing implementation:
// BulkGetResponse<string> response = await bulkCacheService.GetAsync<string>(request);

// Inspect the results
// foreach (var result in response.Results)
// {
//     Console.WriteLine($"Key: {result.Key}, Found: {result.Found}, Value: {result.Value}");
// }
```


## RedisCacheServiceTests

`RedisCacheServiceTests` provides a comprehensive test suite for the `RedisCacheService`, covering critical cache operations such as retrieval, storage, removal, and existence checks. The suite also validates advanced features, including `GetOrLoadAsync` with deserialization failure handling and `GetWithSlidingExpirationAsync` logic, ensuring reliable performance across diverse cache hit and miss scenarios.

Example usage in a test context:
```csharp
using RedisCachePatterns.Tests.Services;
using System.Threading.Tasks;

// Example of how the tests are structured to validate RedisCacheService:
public class RedisCacheServiceUsageExamples
{
    public async Task DemonstrateTestCapabilities(RedisCacheServiceTests tests)
    {
        // Validating basic storage and retrieval functionality
        await tests.SetAsync_StoresValueInCache();
        await tests.GetAsync_WhenKeyExists_ReturnsDeserializedValue();

        // Validating GetOrLoad logic and cache eviction on failure
        await tests.GetOrLoadAsync_WhenCacheMiss_CallsLoadFnAndCachesResult();

        // Validating sliding expiration behavior
        await tests.GetWithSlidingExpirationAsync_WhenCacheHit_ReturnsValueAndResetsTTL();
    }
}
```


## CacheTagServiceTests

The `CacheTagServiceTests` class validates the behavior of cache‑tag operations, ensuring that keys can be associated with tags, retrieved by tag, and invalidated correctly. It covers scenarios such as adding keys to tags, handling invalid arguments, and bulk invalidation of multiple tags.

Example usage:
```csharp
using RedisCachePatterns.Tests.Services;
using System.Threading.Tasks;

public class CacheTagServiceUsageExamples
{
    private readonly CacheTagServiceTests _tests = new CacheTagServiceTests();

    public async Task RunAllExamples()
    {
        // Write a value and associate it with multiple tags
        await _tests.SetWithTagsAsync_WritesValueAndAddsToAllTagSets();

        // Add a single key to a tag
        await _tests.TagKeyAsync_AddsKeyToTagSet();

        // Remove a key from a tag and check the result
        bool removed = await _tests.UntagKeyAsync_RemovesKeyFromTagSet_ReturnsTrueWhenMember();

        // Retrieve all keys associated with a specific tag
        var keys = await _tests.GetKeysByTagAsync_ReturnsKeysForTag();

        // Invalidate a tag, removing all its keys
        long removedCount = await _tests.InvalidateTagAsync_RemovesAllKeysAndDeletesTagSet_ReturnsCount();

        // Invalidate multiple tags at once
        long totalRemoved = await _tests.InvalidateTagsAsync_InvalidatesMultipleTags_ReturnsTotalCount();
    }
}
```


## CacheStatisticsAggregator

`CacheStatisticsAggregator` aggregates cache operation metrics such as hits, misses, and errors, providing a snapshot of current statistics and supporting reset and disposal semantics. It is typically used to collect and expose runtime cache health data for monitoring or diagnostics.

Example usage:
```csharp
using System;
using RedisCachePatterns.Monitoring;

using var aggregator = new CacheStatisticsAggregator();

// Record some cache activity
aggregator.IncrementHits();
aggregator.IncrementMisses();
aggregator.IncrementErrors();

// Retrieve the aggregated statistics
CacheStatistics stats = aggregator.GetStatistics();
Console.WriteLine($"Hits: {stats.Hits}, Misses: {stats.Misses}, Errors: {stats.Errors}");

// Reset the counters if needed
aggregator.Reset();

// When finished, the aggregator can be disposed (handled by the using statement above)
```
