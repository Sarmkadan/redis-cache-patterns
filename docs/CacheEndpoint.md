# CacheEndpoint

The `CacheEndpoint` class provides an interface for interacting with and managing the state of a Redis-based cache. It allows administrators and services to monitor cache health, perform targeted key invalidations based on pre-defined patterns, clear the cache, and retrieve diagnostic metrics. The class utilizes a standardized `ApiResponse` pattern to encapsulate the results of its operations, ensuring consistent error handling and data retrieval across the application.

## API

*   **`public CacheEndpoint()`**
    *   **Purpose:** Initializes a new instance of the `CacheEndpoint` class.
    *   **Parameters:** None.
    *   **Return Value:** A new `CacheEndpoint` instance.
    *   **Throws:** Does not throw under normal conditions.

*   **`public async Task<ApiResponse<CacheStatistics>> GetStatisticsAsync()`**
    *   **Purpose:** Retrieves current cache usage and performance statistics.
    *   **Parameters:** None.
    *   **Return Value:** An `ApiResponse` containing a `CacheStatistics` object if successful.
    *   **Throws:** Throws `HttpRequestException` if the underlying cache connection fails.

*   **`public async Task<ApiResponse<bool>> InvalidateByPatternAsync()`**
    *   **Purpose:** Invalidates and removes all cache entries that match a pre-configured pattern.
    *   **Parameters:** None.
    *   **Return Value:** An `ApiResponse` containing a `bool` indicating success or failure.
    *   **Throws:** Throws `HttpRequestException` if the cache infrastructure is unreachable.

*   **`public async Task<ApiResponse<bool>> FlushAsync()`**
    *   **Purpose:** Completely empties the cache, removing all stored entries.
    *   **Parameters:** None.
    *   **Return Value:** An `ApiResponse` containing a `bool` indicating success or failure.
    *   **Throws:** Throws `HttpRequestException` if the cache infrastructure is unreachable.

*   **`public async Task<ApiResponse<IEnumerable<string>>> GetKeysByPatternAsync()`**
    *   **Purpose:** Retrieves a list of all keys that currently match a pre-configured pattern.
    *   **Parameters:** None.
    *   **Return Value:** An `ApiResponse` containing an `IEnumerable<string>` of matching keys.
    *   **Throws:** Throws `HttpRequestException` if the cache infrastructure is unreachable.

*   **`public ApiResponse<object> GetMetrics`**
    *   **Purpose:** Gets the current diagnostic metrics for the cache system.
    *   **Parameters:** None (Property).
    *   **Return Value:** An `ApiResponse` containing the metrics object.
    *   **Throws:** Does not throw; returns a failure response if metrics are unavailable.

## Usage

```csharp
// Example 1: Retrieving statistics and performing invalidation
var cacheEndpoint = new CacheEndpoint();

var statsResult = await cacheEndpoint.GetStatisticsAsync();
if (statsResult.Success) {
    Console.WriteLine($"Cache hits: {statsResult.Data.Hits}");
}

var invalidateResult = await cacheEndpoint.InvalidateByPatternAsync();
if (invalidateResult.Success) {
    Console.WriteLine("Cache invalidation successful.");
}
```

```csharp
// Example 2: Listing matching keys and flushing the cache
var cacheEndpoint = new CacheEndpoint();

var keysResult = await cacheEndpoint.GetKeysByPatternAsync();
if (keysResult.Success) {
    foreach (var key in keysResult.Data) {
        Console.WriteLine($"Found key: {key}");
    }
}

var flushResult = await cacheEndpoint.FlushAsync();
if (flushResult.Success) {
    Console.WriteLine("Cache flushed successfully.");
}
```

## Notes

*   **Thread Safety:** The `CacheEndpoint` class is designed to be thread-safe, utilizing underlying thread-safe Redis client libraries.
*   **Destructive Operations:** Both `FlushAsync` and `InvalidateByPatternAsync` are destructive operations. Ensure appropriate permissions and context before invocation, as these actions cannot be reversed.
*   **Pattern Configuration:** The pattern utilized by `InvalidateByPatternAsync` and `GetKeysByPatternAsync` is determined by the configuration supplied during the initialization of the `CacheEndpoint` instance. Ensure the intended pattern is configured before these methods are called.
*   **Infrastructure Dependency:** All asynchronous operations depend on the availability of the Redis cache infrastructure. While the `ApiResponse` wrapper handles expected errors gracefully, critical connectivity failures will result in exceptions.
