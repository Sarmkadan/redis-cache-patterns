# CacheStatisticsEndpoint

The `CacheStatisticsEndpoint` class exposes the current aggregated cache statistics and provides an operation for resetting the counters. It wraps each operation in an `ApiResponse<T>` through `ApiEndpointBase`, which adds performance measurement, operational logging, and consistent error handling.

## API

### `CacheStatisticsEndpoint(CacheStatisticsAggregator statsAggregator, ILogger<CacheStatisticsEndpoint> logger, PerformanceMonitor performanceMonitor)`

Initializes a new endpoint with the statistics aggregator and shared endpoint infrastructure.

*   **`statsAggregator`:** The aggregator queried and reset by the endpoint. Passing `null` throws `ArgumentNullException`.
*   **`logger`:** Logger used by `ApiEndpointBase` for operation results and errors.
*   **`performanceMonitor`:** Monitor used to measure endpoint operations.

### `Task<ApiResponse<CacheStatistics>> GetStatisticsAsync()`

Returns a point-in-time snapshot of the aggregated counters.

*   **Parameters:** None.
*   **Return Value:** A completed task containing an `ApiResponse<CacheStatistics>`. On success, `Data` contains hits, misses, errors, total operations, the calculated hit rate, and the UTC capture time. `TotalKeys` and `MemoryUsedBytes` are currently returned as zero by the aggregator.
*   **Operation Name:** `GetCacheStatistics`, used for logging and performance measurement.
*   **Errors:** Exceptions are converted by `ApiEndpointBase` into failure responses. Argument errors use status code 400, invalid-operation errors use 409, and unexpected errors use 500.

### `Task<ApiResponse<bool>> ResetAsync()`

Resets the aggregator's hit, miss, error, and total-operation counters to zero. This is useful at boundaries such as a cache flush or deployment.

*   **Parameters:** None.
*   **Return Value:** A completed task containing a successful `ApiResponse<bool>` with `Data` set to `true` when the reset completes.
*   **Operation Name:** `ResetCacheStatistics`, used for logging and performance measurement.
*   **Errors:** Exceptions are converted by `ApiEndpointBase` into failure responses using the same status-code mapping as `GetStatisticsAsync()`.

## Usage

```csharp
using RedisCachePatterns.API;

// Resolve the endpoint after registering it with AddCacheStatisticsEndpoint().
var endpoint = serviceProvider.GetRequiredService<CacheStatisticsEndpoint>();

var response = await endpoint.GetStatisticsAsync();
if (response.IsSuccess && response.Data is not null)
{
    Console.WriteLine($"Hits: {response.Data.Hits}");
    Console.WriteLine($"Misses: {response.Data.Misses}");
    Console.WriteLine($"Errors: {response.Data.Errors}");
    Console.WriteLine($"Hit rate: {response.Data.HitRate:F2}%");
}
else
{
    Console.WriteLine($"Unable to retrieve statistics: {response.Error}");
}
```

```csharp
var resetResponse = await endpoint.ResetAsync();
if (resetResponse.IsSuccess && resetResponse.Data)
{
    Console.WriteLine("Cache statistics reset successfully.");
}
```

## Notes

*   **Dependency injection:** Register the endpoint and aggregator with `AddCacheStatisticsEndpoint()`, or construct the endpoint with the three required dependencies.
*   **In-memory data:** The endpoint reads from `CacheStatisticsAggregator`; it does not query Redis or make a network request.
*   **Response handling:** Check `IsSuccess` before reading `Data`. Failed operations place the error message in `Error` and the mapped status code in `StatusCode`.
*   **Reset scope:** `ResetAsync()` resets only the in-memory aggregate counters. It does not flush cache entries or reset the metrics instruments' externally exported cumulative state.
*   **Concurrency:** Counter snapshots and resets rely on the aggregator's atomic operations and can be invoked concurrently.
