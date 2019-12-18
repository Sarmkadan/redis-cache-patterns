# CacheInvalidationExample

The `CacheInvalidationExample` class serves as a practical reference implementation for managing cache consistency within the `redis-cache-patterns` project. It encapsulates various strategies for invalidating cached data, ranging from targeted key removal to cascading updates and time-to-live (TTL) based expiration. This component is designed to demonstrate how to handle cache coherence during product updates, category changes, and routine maintenance tasks like stale entry cleanup, ensuring that downstream consumers always retrieve accurate data from the Redis store.

## API

### Constructor

**`public CacheInvalidationExample()`**

Initializes a new instance of the `CacheInvalidationExample` class. This constructor typically configures the necessary Redis connection clients and internal logging mechanisms required for subsequent invalidation operations.

### Methods

**`public async Task<OperationResult> InvalidateCategoryProductsAsync(string categoryId)`**

Invalidates all cache entries associated with a specific product category.
*   **Parameters**: `categoryId` (string) – The unique identifier of the category whose products need to be flushed from the cache.
*   **Returns**: A `Task<OperationResult>` indicating the success or failure of the bulk invalidation operation.
*   **Throws**: Throws an exception if the Redis connection is unavailable or if the `categoryId` is null or empty.

**`public async Task<OperationResult> InvalidateSpecificProductAsync(string productId)`**

Removes a single, specific product entry from the cache.
*   **Parameters**: `productId` (string) – The unique identifier of the product to invalidate.
*   **Returns**: A `Task<OperationResult>` detailing the outcome of the deletion.
*   **Throws**: Throws an exception if the underlying Redis client fails to execute the delete command.

**`public async Task<OperationResult> InvalidateAllCacheAsync()`**

Performs a global flush, removing all keys managed by this cache pattern instance.
*   **Parameters**: None.
*   **Returns**: A `Task<OperationResult>` confirming the completion of the global flush.
*   **Throws**: Throws a critical exception if the flush operation times out or encounters permission issues on the Redis server. Use with caution in production environments.

**`public async Task<OperationResult> UpdateProductWithCascadingInvalidationAsync(Product product)`**

Updates a product record and automatically invalidates related cache entries, including the specific product key and any aggregate category lists it belongs to.
*   **Parameters**: `product` (Product) – The updated product entity containing current state and category associations.
*   **Returns**: A `Task<OperationResult>` indicating whether both the update trigger and subsequent invalidations succeeded.
*   **Throws**: Throws if the cascading logic encounters a race condition or if any step in the invalidation chain fails.

**`public async Task<OperationResult> UpdateProductWithTTLInvalidationAsync(Product product, TimeSpan ttl)`**

Updates a product entry in the cache while explicitly resetting or defining its Time-To-Live (TTL), effectively invalidating the previous expiration schedule.
*   **Parameters**: `product` (Product) – The product data to update; `ttl` (TimeSpan) – The new duration before the entry expires.
*   **Returns**: A `Task<OperationResult>` reflecting the success of the set-and-expire operation.
*   **Throws**: Throws if the provided `ttl` is negative or if the Redis SETEX command fails.

**`public async Task<OperationResult> UpdateProductWithConditionalInvalidationAsync(Product product, Func<Product, bool> condition)`**

Invalidates the cached version of a product only if a specific condition predicate is met against the incoming product data.
*   **Parameters**: `product` (Product) – The candidate product data; `condition` (Func<Product, bool>) – The predicate to evaluate.
*   **Returns**: A `Task<OperationResult>` indicating if invalidation occurred or was skipped based on the condition.
*   **Throws**: Throws if the `condition` delegate is null or throws an exception during evaluation.

**`public async Task<OperationResult> InvalidateProductsAsync(IEnumerable<string> productIds)`**

Batch invalidates multiple product entries in a single operation to reduce network round-trips.
*   **Parameters**: `productIds` (IEnumerable<string>) – A collection of product identifiers to remove.
*   **Returns**: A `Task<OperationResult>` summarizing the batch operation status.
*   **Throws**: Throws if the collection is null or if the batch pipeline execution fails.

**`public async Task<OperationResult> InvalidateStaleEntriesAsync(DateTime threshold)`**

Scans for and removes cache entries that are identified as stale based on a provided timestamp threshold (typically used for entries lacking explicit TTLs or requiring logic-based expiration).
*   **Parameters**: `threshold` (DateTime) – The cutoff time; entries older than this are considered stale.
*   **Returns**: A `Task<OperationResult>` with counts of scanned and removed entries.
*   **Throws**: Throws if the scan operation exceeds configured timeouts or encounters serialization errors during metadata inspection.

## Usage

### Example 1: Handling a Product Update with Cascading Invalidation
This example demonstrates updating a product and ensuring that both the individual product cache and the parent category list are invalidated to prevent data inconsistency.

```csharp
public async Task HandleProductUpdateAsync(string productId, Product updatedProduct)
{
    var invalidator = new CacheInvalidationExample();
    
    // Perform the update logic and cascade invalidation to related keys
    var result = await invalidator.UpdateProductWithCascadingInvalidationAsync(updatedProduct);

    if (!result.IsSuccess)
    {
        // Log failure and potentially retry or alert
        Console.WriteLine($"Cache invalidation failed: {result.ErrorMessage}");
    }
}
```

### Example 2: Batch Invalidation for a Category Migration
When moving multiple products between categories, it is efficient to batch invalidate the specific product keys before recalculating their new category aggregations.

```csharp
public async Task MigrateProductsAsync(List<string> productIdsToMigrate)
{
    var invalidator = new CacheInvalidationExample();

    // Bulk invalidate the specific product keys
    var result = await invalidator.InvalidateProductsAsync(productIdsToMigrate);

    if (result.IsSuccess)
    {
        // Proceed with database migration logic knowing cache is clean for these IDs
        await DatabaseService.UpdateProductCategoriesAsync(productIdsToMigrate);
    }
}
```

## Notes

*   **Thread Safety**: The methods within `CacheInvalidationExample` are designed to be thread-safe for concurrent invocation. However, callers should be aware that race conditions may occur between a read operation and an invalidation call (e.g., `InvalidateSpecificProductAsync`), which is an inherent characteristic of distributed caching systems.
*   **Atomicity**: Operations like `UpdateProductWithCascadingInvalidationAsync` attempt to group commands logically, but they do not guarantee database-level transactional atomicity across the cache and the primary data store unless wrapped in an external transaction scope.
*   **Performance Considerations**: `InvalidateAllCacheAsync` is a heavy operation that blocks other key operations on some Redis configurations depending on the key count; it should be reserved for maintenance windows or catastrophic consistency failures.
*   **Null Handling**: Most methods will throw an `ArgumentNullException` or similar validation exception if passed null identifiers or empty collections, preventing accidental no-op executions that might mask underlying logic errors.
*   **Stale Entry Scanning**: `InvalidateStaleEntriesAsync` relies on key scanning patterns which can be performance-intensive on large datasets. It is recommended to run this method during low-traffic periods or with restrictive thresholds.
