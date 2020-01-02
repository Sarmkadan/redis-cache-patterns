# HealthCheckService
The `HealthCheckService` class is designed to provide a comprehensive health check for the Redis cache, allowing developers to assess the overall status and identify potential issues. It offers a range of properties and methods to evaluate the health of the cache, including connection status, component checks, and issue detection.

## API
* `public HealthCheckService`: The constructor for the `HealthCheckService` class, used to create a new instance.
* `public async Task<HealthStatus> CheckHealthAsync`: Asynchronously checks the health of the Redis cache and returns a `HealthStatus` object indicating the overall health. This method may throw exceptions if there are issues connecting to the Redis instance or if other errors occur during the health check.
* `public async Task<bool> IsReadyAsync`: Asynchronously checks if the Redis cache is ready for use and returns a boolean value indicating its readiness. This method may throw exceptions if there are issues connecting to the Redis instance or if other errors occur during the check.
* `public string Overall`: A property that provides a summary of the overall health of the Redis cache.
* `public bool RedisConnected`: A property that indicates whether the `HealthCheckService` is connected to the Redis instance.
* `public Dictionary<string, string> Components`: A property that contains a dictionary of components and their respective health statuses.
* `public List<string> Issues`: A property that contains a list of issues detected during the health check.
* `public DateTime CheckedAt`: A property that indicates the last time the health check was performed.
* `public override string ToString`: Overrides the `ToString` method to provide a string representation of the `HealthCheckService` instance.

## Usage
The following examples demonstrate how to use the `HealthCheckService` class:
```csharp
// Example 1: Simple health check
var healthCheckService = new HealthCheckService();
var healthStatus = await healthCheckService.CheckHealthAsync();
Console.WriteLine($"Overall health: {healthStatus}");

// Example 2: Checking readiness and handling exceptions
var healthCheckService = new HealthCheckService();
try
{
    var isReady = await healthCheckService.IsReadyAsync();
    if (isReady)
    {
        Console.WriteLine("Redis cache is ready for use.");
    }
    else
    {
        Console.WriteLine("Redis cache is not ready for use.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error checking readiness: {ex.Message}");
}
```

## Notes
When using the `HealthCheckService` class, consider the following edge cases and thread-safety remarks:
* The `CheckHealthAsync` and `IsReadyAsync` methods are asynchronous and may throw exceptions if there are issues connecting to the Redis instance or if other errors occur during the health check.
* The `RedisConnected` property and `IsReadyAsync` method may return different results if the connection to the Redis instance is lost or established after the initial check.
* The `Components` and `Issues` properties are populated during the health check and may not reflect the current state of the Redis cache if the health check has not been performed recently.
* The `CheckedAt` property indicates the last time the health check was performed and can be used to determine if the health check is stale.
* The `HealthCheckService` class is designed to be thread-safe, but it is still important to ensure that instances are properly synchronized if accessed from multiple threads.
