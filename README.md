// existing content ...

## ApiEndpointBase

`ApiEndpointBase` is a base class for API endpoints that provides built-in validation, logging, and error handling. It ensures consistent behavior across all API operations and provides a standard API response format.

### Usage Example
```csharp
var endpoint = new MyApiEndpoint(logger, performanceMonitor);
var result = await endpoint.ExecuteAsync(() => MyOperation(), "MyOperation");
if (result.IsSuccess)
{
    Console.WriteLine($"Operation succeeded: {result.Data}");
}
else
{
    Console.WriteLine($"Error: {result.Error}, Status code: {result.StatusCode}");
}
```

## CacheEndpoint

`CacheEndpoint` is an API endpoint for cache management operations. It provides methods for retrieving cache statistics, invalidating cache keys by pattern, flushing the cache, retrieving keys by pattern, and retrieving cache metrics.

### Usage Example
```csharp
var cacheEndpoint = new CacheEndpoint(cacheService, invalidationService, logger, performanceMonitor);
var statistics = await cacheEndpoint.GetStatisticsAsync();
Console.WriteLine($"Cache statistics: {statistics}");

var invalidated = await cacheEndpoint.InvalidateByPatternAsync("pattern");
Console.WriteLine($"Invalidate by pattern result: {invalidated}");

var flushed = await cacheEndpoint.FlushAsync();
Console.WriteLine($"Flush result: {flushed}");

var keys = await cacheEndpoint.GetKeysByPatternAsync("pattern");
Console.WriteLine($"Keys by pattern: {string.Join(", ", keys)}");

var metrics = cacheEndpoint.GetMetrics();
Console.WriteLine($"Cache metrics: {metrics}");
```
