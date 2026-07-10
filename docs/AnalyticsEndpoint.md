# AnalyticsEndpoint

The `AnalyticsEndpoint` class provides a structured interface for interacting with the analytics data store within the `redis-cache-patterns` system. This class exposes asynchronous methods that allow applications to retrieve high-level dashboard snapshots, detailed reports, and aggregate key access statistics, as well as provide functionality to perform a reset of the stored analytics data.

## API

### AnalyticsEndpoint()
Initializes a new instance of the `AnalyticsEndpoint` class.

### Task<ApiResponse<AnalyticsDashboardResponse>> GetSnapshotAsync()
Asynchronously retrieves an `AnalyticsDashboardResponse` object containing a high-level overview of cache performance and current usage metrics.
*   **Returns:** A `Task` resolving to an `ApiResponse` wrapping the dashboard data.
*   **Throws:** `HttpRequestException` or similar communication exceptions if the underlying API service is unreachable or returns an error.

### Task<ApiResponse<string>> GetReportAsync()
Asynchronously generates and returns a formatted report as a string.
*   **Returns:** A `Task` resolving to an `ApiResponse` containing the generated report string.
*   **Throws:** `HttpRequestException` if the request fails or if the report generation service encounters an error.

### Task<ApiResponse<KeyAccessStats>> GetKeyStatsAsync()
Asynchronously fetches aggregate `KeyAccessStats` representing access patterns for all tracked keys.
*   **Returns:** A `Task` resolving to an `ApiResponse` containing the aggregate key access statistics.
*   **Throws:** `HttpRequestException` if the statistics service is unreachable.

### Task<ApiResponse<bool>> ResetAsync()
Asynchronously resets the analytics data store to its initial state.
*   **Returns:** A `Task` resolving to an `ApiResponse<bool>` where `true` indicates a successful reset, and `false` indicates failure.
*   **Throws:** `HttpRequestException` if the reset operation cannot be completed due to service unavailability.

## Usage

### Retrieving a Dashboard Snapshot
```csharp
var analytics = new AnalyticsEndpoint();
var response = await analytics.GetSnapshotAsync();

if (response.Success)
{
    Console.WriteLine($"Cache Hit Rate: {response.Data.HitRate}%");
}
else
{
    Console.WriteLine($"Error retrieving snapshot: {response.ErrorMessage}");
}
```

### Resetting Analytics Data
```csharp
var analytics = new AnalyticsEndpoint();
var result = await analytics.ResetAsync();

if (result.Success && result.Data)
{
    Console.WriteLine("Analytics data successfully reset.");
}
else
{
    Console.WriteLine("Failed to reset analytics data.");
}
```

## Notes

*   **Thread Safety:** The `AnalyticsEndpoint` instance is designed to be thread-safe, allowing shared usage across asynchronous operations in a multi-threaded application.
*   **Exception Handling:** All methods return an `ApiResponse<T>` wrapper, which should be inspected via the `Success` property before accessing the `Data` property to ensure the operation completed as expected.
*   **Network Dependency:** As these methods perform network-bound operations to interact with the underlying cache-patterns service, they may throw network-related exceptions (e.g., timeouts, connection failures) if the service is unreachable.
