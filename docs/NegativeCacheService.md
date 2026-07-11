# NegativeCacheService

The `NegativeCacheService` is a specialized component designed to implement the negative caching pattern, preventing repeated attempts to retrieve or compute data that is known to be absent. By storing explicit markers for missing keys with a configurable time-to-live (TTL), this service reduces load on underlying data sources and improves overall system latency when dealing with frequent cache misses.

## API

### `NegativeTtl`
```csharp
public TimeSpan NegativeTtl { get; set; }
```
Gets or sets the duration for which a negative cache entry remains valid. This value determines how long the service will treat a specific key as "missing" before allowing a subsequent lookup attempt to proceed to the underlying data source.

### `NegativeHits`
```csharp
public long NegativeHits { get; }
```
Gets the cumulative count of successful negative cache lookups. This metric increments each time a request is served by an existing negative marker rather than triggering a data source fetch.

### `NegativeCacheService`
```csharp
public NegativeCacheService(...)
```
Initializes a new instance of the `NegativeCacheService` class. Constructor parameters typically include the underlying Redis connection wrapper and configuration options required to manage the negative cache state.

### `GetOrLoadWithNegativeCachingAsync<T>`
```csharp
public async Task<T?> GetOrLoadWithNegativeCachingAsync<T>(
    string key,
    Func<Task<T?>> loader,
    CancellationToken cancellationToken = default
)
```
Attempts to retrieve a value associated with the specified key. If the key exists in the positive cache, the value is returned. If a negative marker exists, `default(T)` is returned immediately. If neither exists, the provided `loader` function is invoked; if the loader returns null, a negative marker is stored.
*   **Parameters**:
    *   `key`: The unique identifier for the cached item.
    *   `loader`: An asynchronous function to fetch the data from the primary source if not found in any cache layer.
    *   `cancellationToken`: A token to cancel the operation.
*   **Returns**: The cached value, the newly loaded value, or `default(T)` if the item is negatively cached or the loader returns null.
*   **Throws**: Throws exceptions propagated by the `loader` delegate or underlying Redis communication errors.

### `IsNegativelyCachedAsync`
```csharp
public async Task<bool> IsNegativelyCachedAsync(string key, CancellationToken cancellationToken = default)
```
Checks whether a specific key currently holds a negative cache marker.
*   **Parameters**:
    *   `key`: The unique identifier to check.
    *   `cancellationToken`: A token to cancel the operation.
*   **Returns**: `true` if the key is marked as negative; otherwise, `false`.
*   **Throws**: Throws on underlying Redis communication failures.

### `MarkNegativeAsync`
```csharp
public Task MarkNegativeAsync(string key, CancellationToken cancellationToken = default)
```
Explicitly sets a negative cache marker for the specified key using the current `NegativeTtl` value. This is useful when external logic determines that a key should be treated as missing without invoking the standard loading flow.
*   **Parameters**:
    *   `key`: The unique identifier to mark.
    *   `cancellationToken`: A token to cancel the operation.
*   **Returns**: A task representing the completion of the write operation.
*   **Throws**: Throws on underlying Redis communication failures.

### `ClearNegativeAsync`
```csharp
public async Task<bool> ClearNegativeAsync(string key, CancellationToken cancellationToken = default)
```
Removes a negative cache marker for the specified key, allowing future requests to attempt a fresh load from the data source.
*   **Parameters**:
    *   `key`: The unique identifier to clear.
    *   `cancellationToken`: A token to cancel the operation.
*   **Returns**: `true` if a negative marker was found and removed; `false` if the key did not have a negative marker.
*   **Throws**: Throws on underlying Redis communication failures.

## Usage

### Example 1: Standard Negative Caching Flow
This example demonstrates retrieving a user profile. If the user does not exist in the database, the result is cached negatively to prevent repeated database hits for the same missing ID.

```csharp
public class UserService
{
    private readonly NegativeCacheService _cache;
    private readonly IRepository _repository;

    public UserService(NegativeCacheService cache, IRepository repository)
    {
        _cache = cache;
        _repository = repository;
    }

    public async Task<User?> GetUserAsync(string userId)
    {
        return await _cache.GetOrLoadWithNegativeCachingAsync(
            key: $"user:{userId}",
            loader: async () => await _repository.FindUserByIdAsync(userId),
            cancellationToken: CancellationToken.None
        );
    }
}
```

### Example 2: Manual Invalidation and Re-check
In scenarios where data might be created externally after a negative cache entry was established, the negative marker can be explicitly cleared to force a re-evaluation on the next request.

```csharp
public async Task RefreshUserStatusAsync(string userId)
{
    // Assume logic here creates the user in the DB if it was previously missing
    
    // Clear the negative cache so the next read attempts a DB lookup
    var wasCleared = await _cache.ClearNegativeAsync($"user:{userId}");
    
    if (wasCleared)
    {
        // Optionally log that a stale negative entry was purged
        Console.WriteLine($"Negative cache cleared for user {userId}");
    }
}
```

## Notes

*   **Thread Safety**: The service is designed to be thread-safe for concurrent operations. The `NegativeHits` counter utilizes atomic operations to ensure accuracy under high concurrency.
*   **TTL Expiration**: Once the `NegativeTtl` expires, the underlying Redis key is automatically removed by the server. Subsequent calls to `IsNegativelyCachedAsync` will return `false`, and `GetOrLoadWithNegativeCachingAsync` will re-invoke the loader.
*   **Type Consistency**: When using `GetOrLoadWithNegativeCachingAsync<T>`, ensure the `loader` function returns `null` (not a thrown exception) to indicate a missing resource. Throwing an exception in the loader will propagate to the caller and will not result in a negative cache entry being created.
*   **Race Conditions**: In highly concurrent environments where multiple threads simultaneously miss the cache for the same key, the underlying implementation typically employs locking or atomic Redis commands (such as SETNX) to ensure the `loader` is not executed redundantly more than necessary, though exact behavior depends on the specific Redis client configuration injected.
