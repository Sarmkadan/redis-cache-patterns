# CacheEndpointExtensions

CacheEndpointExtensions provides a set of static utility methods for managing Redis cache endpoints, including pattern-based invalidation, enhanced statistics retrieval, paginated key enumeration, and cache flushing with statistical feedback. These methods are designed to simplify common cache management operations while providing structured responses through the ApiResponse wrapper.

## API

### Methods

#### `InvalidateByPatternWithPrefixAsync`
Invalidates all cache entries matching a specified pattern with a given prefix.

**Parameters**
- `IConnectionMultiplexer` cacheEndpoint: The Redis connection multiplexer instance.
- `string pattern`: The pattern to match keys for invalidation.
- `string prefix`: The prefix to apply to the pattern.

**Returns**
- `Task<ApiResponse<bool>>`: Indicates whether the invalidation operation succeeded.

**Throws**
- `ArgumentNullException`: If cacheEndpoint, pattern, or prefix is null.
- `RedisException`: If the Redis server is unavailable or the operation fails.

---

#### `GetEnhancedStatisticsAsync`
Retrieves detailed cache statistics including hit/miss ratios, memory usage, and key counts.

**Parameters**
- `IConnectionMultiplexer` cacheEndpoint: The Redis connection multiplexer instance.

**Returns**
- `Task<ApiResponse<EnhancedCacheStatistics>>`: Contains metrics such as Hits, Misses, HitRate, MemoryUsedBytes, and captured timestamp.

**Throws**
- `ArgumentNullException`: If cacheEndpoint is null.
- `RedisException`: If the Redis server is unreachable or statistics retrieval fails.

---

#### `GetKeysByPatternPagedAsync`
Retrieves a paginated list of cache keys matching a specified pattern.

**Parameters**
- `IConnectionMultiplexer` cacheEndpoint: The Redis connection multiplexer instance.
- `string pattern`: The pattern to match keys.
- `int page`: The page number to retrieve (1-based index).
- `int pageSize`: The number of keys per page.

**Returns**
- `Task<ApiResponse<PaginatedResult<string>>>`: Contains Items (list of keys), TotalCount, Page, PageSize, and TotalPages.

**Throws**
- `ArgumentNullException`: If cacheEndpoint or pattern is null.
- `ArgumentOutOfRangeException`: If page or pageSize is less than 1.
- `RedisException`: If the Redis server is unavailable or the scan operation fails.

---

#### `FlushWithStatisticsAsync`
Flushes the entire cache and returns statistics about the operation.

**Parameters**
- `IConnectionMultiplexer` cacheEndpoint: The Redis connection multiplexer instance.

**Returns**
- `Task<ApiResponse<FlushOperationResult>>`: Contains KeysBefore, KeysAfter, and KeysFlushed counts.

**Throws**
- `ArgumentNullException`: If cacheEndpoint is null.
- `RedisException`: If the Redis server is unreachable or the flush operation fails.

---

### Properties

#### `Items`
A read-only list of keys returned by a paginated query.

**Type**
- `IReadOnlyList<string>`

---

#### `TotalCount`
Total number of keys matching the query pattern.

**Type**
- `int`

---

#### `Page`
Current page number in the paginated result.

**Type**
- `int`

---

#### `PageSize`
Number of keys per page in the paginated result.

**Type**
- `int`

---

#### `TotalPages`
Total number of pages available for the query.

**Type**
- `int`

---

#### `TotalKeys`
Total number of keys in the cache (used in EnhancedCacheStatistics).

**Type**
- `int`

---

#### `Hits`
Number of cache hits recorded.

**Type**
- `int`

---

#### `Misses`
Number of cache misses recorded.

**Type**
- `int`

---

#### `HitRate`
Ratio of cache hits to total accesses (Hits / (Hits + Misses)).

**Type**
- `double`

---

#### `MemoryUsedBytes`
Memory used by the cache in bytes.

**Type**
- `long`

---

#### `MemoryUsedMB`
Memory used by the cache in megabytes (MemoryUsedBytes / 1024 / 1024).

**Type**
- `double`

---

#### `CapturedAt`
Timestamp when the statistics were captured.

**Type**
- `DateTime`

---

#### `Success`
Indicates whether the operation completed successfully.

**Type**
- `bool`

---

#### `KeysBefore`
Number of keys in the cache before a flush operation.

**Type**
- `int`

---

#### `KeysAfter`
Number of keys in the cache after a flush operation.

**Type**
- `int`

---

#### `KeysFlushed`
Number of keys removed during the flush operation.

**Type**
- `int`

---

## Usage

### Example 1: Invalidate Keys by Pattern
```csharp
var multiplexer = ConnectionMultiplexer.Connect("localhost");
var response = await CacheEndpointExtensions.InvalidateByPatternWithPrefixAsync(
    multiplexer, 
    "user:*", 
    "session:"
);

if (response.Success)
{
    Console.WriteLine("Invalidation completed successfully.");
}
else
{
    Console.WriteLine($"Invalidation failed: {response.ErrorMessage}");
}
```

### Example 2: Retrieve Enhanced Statistics
```csharp
var multiplexer = ConnectionMultiplexer.Connect("localhost");
var statsResponse = await CacheEndpointExtensions.GetEnhancedStatisticsAsync(multiplexer);

if (statsResponse.Success)
{
    var stats = statsResponse.Data;
    Console.WriteLine($"Hit Rate: {stats.HitRate:P2}");
    Console.WriteLine($"Memory Used: {stats.MemoryUsedMB} MB");
}
else
{
    Console.WriteLine($"Failed to retrieve stats: {statsResponse.ErrorMessage}");
}
```

---

## Notes

- All methods are static and thread-safe provided the underlying `IConnectionMultiplexer` instance is thread-safe (as per StackExchange.Redis guarantees).
- `GetKeysByPatternPagedAsync` uses Redis's SCAN command internally, which is non-blocking but may return inconsistent results during concurrent writes.
- `FlushWithStatisticsAsync` performs a full cache flush and should be used cautiously in production environments.
- `HitRate` may be NaN if both Hits and Misses are zero; consumers should handle this case explicitly.
- `MemoryUsedMB` is a computed value and may lose precision for very large byte values due to floating-point representation.
- `InvalidateByPatternWithPrefixAsync` may not invalidate keys in clustered Redis environments if the pattern spans multiple hash slots.
