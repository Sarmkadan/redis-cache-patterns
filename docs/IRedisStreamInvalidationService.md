# IRedisStreamInvalidationService

The `IRedisStreamInvalidationService` interface defines the contract for publishing cache invalidation events to a Redis Stream within the `redis-cache-patterns` architecture. It facilitates decoupled communication between cache writers and subscribers by serializing invalidation commands into stream entries, supporting both specific key removals and pattern-based flushes to ensure data consistency across distributed nodes.

## API

### `RedisStreamCacheInvalidationService`
This is the concrete implementation of the `IRedisStreamInvalidationService` interface. It handles the underlying Redis connection management and stream entry serialization required to dispatch invalidation messages. Instantiating this class provides the operational logic for the methods defined in the interface.

### `Task PublishAsync(string key)`
Publishes a specific cache key invalidation event to the configured Redis stream.
*   **Parameters**:
    *   `key`: The exact cache key string to be invalidated.
*   **Return Value**: A `Task` representing the asynchronous operation. The task completes when the entry has been successfully appended to the stream.
*   **Exceptions**: Throws a connection-related exception if the Redis server is unreachable or if the stream write operation fails due to network issues or permissions.

### `Task PublishAsync(IEnumerable<string> keys)`
Publishes a batch of specific cache key invalidation events to the configured Redis stream in a single operation.
*   **Parameters**:
    *   `keys`: A collection of cache key strings to be invalidated.
*   **Return Value**: A `Task` representing the asynchronous operation. The task completes when all keys have been processed and appended to the stream.
*   **Exceptions**: Throws if the Redis connection is lost during the batch operation or if the input collection is null. Individual key formatting errors may result in a partial failure depending on the underlying client implementation.

### `Task PublishPatternAsync(string pattern)`
Publishes a pattern-based invalidation event, instructing subscribers to invalidate all keys matching the provided glob-style pattern.
*   **Parameters**:
    *   `pattern`: The glob-style pattern (e.g., `user:*`, `session:123:*`) matching the keys to be invalidated.
*   **Return Value**: A `Task` representing the asynchronous operation. The task completes when the pattern message is written to the stream.
*   **Exceptions**: Throws if the Redis server is unavailable or if the pattern string is invalid or empty.

## Usage

### Example 1: Invalidating a Single Entity
When updating a specific user record, the corresponding cache entry must be invalidated to prevent stale data reads.

```csharp
public class UserService
{
    private readonly IRedisStreamInvalidationService _invalidationService;

    public UserService(IRedisStreamInvalidationService invalidationService)
    {
        _invalidationService = invalidationService;
    }

    public async Task UpdateUserAsync(string userId, UserData data)
    {
        // Perform database update logic here...
        
        // Publish invalidation for the specific user key
        await _invalidationService.PublishAsync($"user:{userId}");
    }
}
```

### Example 2: Bulk Invalidation by Pattern
When a global configuration change affects all product listings, a pattern-based invalidation ensures all related cache entries are cleared without enumerating every key individually.

```csharp
public class ProductCatalogService
{
    private readonly IRedisStreamInvalidationService _invalidationService;

    public ProductCatalogService(IRedisStreamInvalidationService invalidationService)
    {
        _invalidationService = invalidationService;
    }

    public async Task RefreshGlobalPricingAsync()
    {
        // Perform pricing update logic in the database...

        // Publish pattern to invalidate all product cache entries
        await _invalidationService.PublishPatternAsync("product:*:price");
    }
}
```

## Notes

*   **Thread Safety**: The `IRedisStreamInvalidationService` implementations are designed to be thread-safe. Multiple threads may concurrently invoke `PublishAsync` or `PublishPatternAsync` without external locking mechanisms.
*   **Fire-and-Forget Semantics**: These methods operate on a fire-and-forget basis regarding the delivery confirmation from subscribers. The returned `Task` only guarantees that the message has been successfully written to the Redis Stream, not that any subscriber has processed it.
*   **Pattern Matching**: The `PublishPatternAsync` method relies on the subscriber's ability to interpret glob patterns. The service itself does not verify if keys matching the pattern currently exist in the cache; it strictly broadcasts the intent to invalidate.
*   **Batching Limits**: When using `PublishAsync(IEnumerable<string>)`, extremely large collections may exceed Redis command size limits or timeout thresholds. It is recommended to batch large key sets into chunks of reasonable size (e.g., 100–500 keys) before invoking the method.
*   **Connection Resilience**: Transient network failures will result in exceptions thrown by the task. Callers should implement retry policies appropriate for their consistency requirements, as lost invalidation messages can lead to temporary data staleness.
