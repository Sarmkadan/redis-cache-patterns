# BatchOperationsExampleExtensions

The `BatchOperationsExampleExtensions` class provides a suite of static extension methods designed to optimize interaction with Redis caches through batch processing. By consolidating multiple operations into single request cycles, these methods minimize network round-trips and reduce latency when managing large sets of cached data, such as product catalogs or inventory records.

## API

### GetExistingProductsBatchAsync
Retrieves a collection of `Product` entities from the cache for a given list of product identifiers.
- **Parameters:** `IEnumerable<string> productIds` (The set of keys to fetch).
- **Returns:** `Task<List<Product>>` containing the successfully retrieved items.
- **Throws:** `ArgumentNullException` if `productIds` is null; `CacheException` on Redis connectivity failures.

### ConditionalBatchUpdateAsync
Performs updates on multiple cached items, ensuring that each update satisfies predefined conditions before execution.
- **Parameters:** `IEnumerable<ProductUpdate> updates` (Collection of update operations).
- **Returns:** `Task<OperationResult>` indicating the success or failure of the batch operation.
- **Throws:** `ArgumentNullException` if `updates` is null; `CacheException` for underlying data access errors.

### InvalidateCachePatternAsync
Removes all cache entries that match a specified key pattern, facilitating bulk invalidation of related cached data.
- **Parameters:** `string pattern` (The glob-style pattern for keys to remove).
- **Returns:** `Task<OperationResult>` representing the status of the invalidation.
- **Throws:** `ArgumentException` if `pattern` is null or empty.

### WarmFilteredCacheAsync
Populates the cache with a set of items based on a filtering criterion, effectively pre-warming the cache for anticipated access patterns.
- **Parameters:** `IFilterCriteria criteria` (The criteria defining which items to cache).
- **Returns:** `Task<OperationResult>` indicating the success of the warming process.
- **Throws:** `CacheException` if the data source is inaccessible.

## Usage

### Batch Retrieval and Conditional Update
```csharp
var ids = new List<string> { "prod-001", "prod-002" };
var existingProducts = await BatchOperationsExampleExtensions.GetExistingProductsBatchAsync(ids);

var updates = existingProducts.Select(p => new ProductUpdate(p.Id, p.Version + 1));
var result = await BatchOperationsExampleExtensions.ConditionalBatchUpdateAsync(updates);
```

### Pattern Invalidation and Cache Warming
```csharp
// Clear all product-related cache entries
await BatchOperationsExampleExtensions.InvalidateCachePatternAsync("product:*");

// Pre-warm the cache with high-demand items
var criteria = new HighDemandFilter();
await BatchOperationsExampleExtensions.WarmFilteredCacheAsync(criteria);
```

## Notes

- **Thread Safety:** These methods are designed for concurrent use. However, when performing `ConditionalBatchUpdateAsync` on shared resources, ensure appropriate locking mechanisms are implemented outside the batch operation if atomicity across multiple batches is required.
- **Performance Considerations:** While batch operations reduce network overhead, excessively large batches can lead to blocking or timeouts in Redis. It is recommended to batch operations into reasonably sized chunks (e.g., 50-100 items per batch) to maintain optimal performance.
- **Error Handling:** When a subset of operations within a batch fails, `OperationResult` will reflect a partial failure state; ensure that caller logic is capable of handling partial successes versus complete failures.
