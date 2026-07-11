# CacheInvalidationExampleExtensions

The `CacheInvalidationExampleExtensions` class provides a set of static extension methods designed to demonstrate common patterns for managing cache invalidation and data consistency in a Redis-backed caching environment. These utilities abstract complex operations such as bulk invalidation, versioned updates, and scheduled maintenance tasks, facilitating the implementation of cache-aside or write-through strategies.

## API

### InvalidateCategoriesBulkAsync
Invalidates multiple product categories within the cache simultaneously to ensure data consistency across related datasets.
- **Parameters:**
    - `IEnumerable<string> categories`: The collection of category identifiers to be invalidated.
- **Return Value:** A `Task<OperationResult>` indicating the success or failure of the bulk invalidation operation.
- **Exceptions:** Throws `ArgumentNullException` if the categories collection is null. Throws `RedisException` if a connection issue occurs during the invalidation process.

### UpdateProductWithVersioningAsync
Updates a product record in the underlying data store and synchronizes the cache using a versioning key to prevent race conditions during concurrent updates.
- **Parameters:**
    - `Product product`: The product object containing the updated data and its current version.
- **Return Value:** A `Task<OperationResult>` indicating the result of the update and cache invalidation.
- **Exceptions:** Throws `ConcurrencyException` if the versioning check fails. Throws `RedisException` if the cache update fails.

### InvalidateCategoryOnlyAsync
Invalidates a single category key within the cache.
- **Parameters:**
    - `string category`: The identifier of the category to be removed from the cache.
- **Return Value:** A `Task<OperationResult>` indicating whether the category was successfully invalidated.
- **Exceptions:** Throws `ArgumentException` if the category identifier is null or empty. Throws `RedisException` if communication with the Redis server fails.

### PerformScheduledCleanupAsync
Executes maintenance logic to expire or remove stale cache entries based on predefined criteria or system-wide TTL policies.
- **Parameters:** None.
- **Return Value:** A `Task<OperationResult>` providing a report on the cleanup operation, including the number of entries removed.
- **Exceptions:** Throws `RedisException` if the cleanup script or command fails to execute against the cache.

## Usage

```csharp
// Example 1: Bulk and specific category invalidation
var categoriesToClear = new List<string> { "electronics", "home-appliances" };
var result = await cacheProvider.InvalidateCategoriesBulkAsync(categoriesToClear);

if (result.Success)
{
    await cacheProvider.InvalidateCategoryOnlyAsync("kitchen-gadgets");
}
```

```csharp
// Example 2: Updating a product with versioning
var updatedProduct = new Product { Id = 101, Name = "New Laptop", Version = 2 };
var result = await cacheProvider.UpdateProductWithVersioningAsync(updatedProduct);

if (!result.Success)
{
    // Handle update failure (e.g., retry or log)
}
```

## Notes

- **Thread Safety:** These methods rely on the underlying Redis client implementation. While the extension methods themselves are static and stateless, they depend on thread-safe configuration of the Redis connection instance.
- **Atomicity:** Bulk operations such as `InvalidateCategoriesBulkAsync` are not inherently atomic. If a partial failure occurs during execution, some keys may remain in the cache. Implement appropriate retry logic where strict consistency is required.
- **Error Handling:** The `OperationResult` should be evaluated in all cases to verify that the underlying cache operation completed as expected. Exceptions should be caught and handled to account for network volatility when interacting with remote Redis servers.
