# CacheServiceExtensions

The `CacheServiceExtensions` class provides a set of static asynchronous extension methods designed to implement common caching patterns on top of a distributed cache service, typically backed by Redis. These methods abstract complex logic such as cache-aside retrieval with fallback fetching, batch operations, distributed locking, and retry mechanisms, enabling developers to integrate robust caching strategies with minimal boilerplate code.

## API

### `GetOrFetchAsync<T>`
Retrieves a value from the cache; if the value is missing or expired, it invokes a provided factory function to fetch the data, stores the result in the cache, and returns it.
*   **Parameters**: Takes the cache instance, a string key, a `Func<Task<T>>` factory delegate for data retrieval, and an optional `TimeSpan` expiration duration.
*   **Return Value**: Returns a `Task<T?>` containing the cached or freshly fetched value. Returns `default(T)` if the factory returns null.
*   **Exceptions**: Throws exceptions propagated from the factory delegate if data fetching fails.

### `SetWithInvalidationAsync<T>`
Stores a value in the cache and optionally triggers invalidation events for dependent keys or patterns.
*   **Parameters**: Accepts the cache instance, a string key, the value of type `T`, an optional `TimeSpan` expiration, and a list of dependent keys to invalidate.
*   **Return Value**: Returns a `Task` that completes when the set operation and any associated invalidation signals are finished.
*   **Exceptions**: May throw if the underlying cache connection fails or if serialization of the value fails.

### `ExecuteWithLockAsync<TResult>`
Executes a critical section of code protected by a distributed lock to prevent concurrent execution across multiple instances.
*   **Parameters**: Requires the cache instance, a unique lock key, a `Func<Task<TResult>>` action to execute, and an optional timeout for acquiring the lock.
*   **Return Value**: Returns a `Task<TResult>` containing the result of the executed action.
*   **Exceptions**: Throws a timeout exception if the lock cannot be acquired within the specified duration; propagates exceptions from the inner action.

### `SetBatchAsync<T>`
Persists multiple key-value pairs to the cache in a single optimized operation.
*   **Parameters**: Accepts the cache instance and an `IEnumerable<KeyValuePair<string, T>>` collection of items to store.
*   **Return Value**: Returns a `Task` that completes when all items have been written.
*   **Exceptions**: Throws if the batch operation fails partially or wholly, depending on the underlying provider's transactional guarantees.

### `GetBatchAsync<T>`
Retrieves multiple values from the cache simultaneously based on a collection of keys.
*   **Parameters**: Takes the cache instance and an `IEnumerable<string>` of keys to retrieve.
*   **Return Value**: Returns a `Task<Dictionary<string, T?>>` mapping requested keys to their values. Keys not found in the cache are present in the dictionary with a value of `default(T)`.
*   **Exceptions**: Throws if the underlying retrieval operation fails due to network or serialization errors.

### `GetWithRetryAsync<T>`
Attempts to retrieve a value from the cache with an exponential backoff retry policy in case of transient failures.
*   **Parameters**: Includes the cache instance, the string key, the maximum number of retry attempts, and the initial delay duration.
*   **Return Value**: Returns a `Task<T?>` with the retrieved value or `default(T)` if not found.
*   **Exceptions**: Re-throws the last encountered exception if all retry attempts are exhausted.

### `WarmCacheAsync<T>`
Pre-populates the cache with a specific set of data to reduce latency for subsequent requests.
*   **Parameters**: Accepts the cache instance, an `IEnumerable<KeyValuePair<string, T>>` of data to preload, and an optional `TimeSpan` expiration.
*   **Return Value**: Returns a `Task` that completes when the warming process is finished.
*   **Exceptions**: Throws if the underlying set operations fail during the warming process.

## Usage

### Example 1: Cache-Aside Pattern with Fallback
This example demonstrates retrieving a user profile. If the data is not in the cache, it fetches it from the database, stores it with a 10-minute expiration, and returns the result.

```csharp
public async Task<UserProfile> GetUserProfileAsync(string userId)
{
    var cacheKey = $"user:profile:{userId}";
    
    return await _cache.GetOrFetchAsync(
        key: cacheKey,
        factory: async () => await _userRepository.GetByIdAsync(userId),
        expiration: TimeSpan.FromMinutes(10)
    );
}
```

### Example 2: Distributed Lock for Report Generation
This example ensures that a heavy report generation process runs only once across all application instances at a given time.

```csharp
public async Task<byte[]> GenerateReportAsync(string reportId)
{
    var lockKey = $"lock:report:{reportId}";

    return await _cache.ExecuteWithLockAsync(
        key: lockKey,
        action: async () => await _reportService.CreateReportBinaryAsync(reportId),
        lockTimeout: TimeSpan.FromSeconds(30)
    );
}
```

## Notes

*   **Thread Safety**: As all methods are static and operate on an injected cache instance passed as an argument, the class itself is thread-safe. However, the thread safety of the underlying `IDistributedCache` implementation passed to these methods must be guaranteed by the consumer.
*   **Null Handling**: Methods returning generic types (e.g., `GetOrFetchAsync`, `GetBatchAsync`) return `default(T)` when a key is missing. For reference types, this is `null`; consumers should handle nullable reference types appropriately.
*   **Serialization**: These extensions assume the underlying cache service handles serialization. Passing non-serializable objects to `Set` or `SetBatch` methods will result in runtime exceptions.
*   **Retry Logic**: `GetWithRetryAsync` is intended strictly for transient network failures. It will not retry logical errors such as missing keys or serialization exceptions, ensuring that permanent failures are surfaced immediately after retries are exhausted.
*   **Lock Timeouts**: When using `ExecuteWithLockAsync`, care must be taken to ensure the `lockTimeout` is longer than the expected execution time of the action to prevent deadlocks where the lock expires while the operation is still running.
