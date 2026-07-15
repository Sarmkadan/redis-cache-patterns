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

## CacheException

`CacheException` is the base exception class for all cache-related errors in the Redis Cache Patterns library. It provides standardized error handling with an optional error code and timestamp tracking for all cache exceptions.

### Usage Example
```csharp
// Basic exception usage
try
{
    var value = await cacheService.GetAsync<string>("non_existent_key");
}
catch (CacheException ex)
{
    Console.WriteLine($"Cache error occurred at {ex.OccurredAt:O}: {ex.Message}");
    if (ex.ErrorCode != null)
    {
        Console.WriteLine($"Error code: {ex.ErrorCode}");
    }
}

// Exception with error code
var connectionEx = new CacheConnectionException("Failed to connect to Redis server", "REDIS_CONN_001");
Console.WriteLine($"Connection failed: {connectionEx.ErrorCode} - {connectionEx.Message}");

// Exception with inner exception
try
{
    await cacheService.SetAsync("key", data, TimeSpan.FromMinutes(5));
}
catch (Exception ex)
{
    throw new CacheTimeoutException("Cache operation timed out", TimeSpan.FromSeconds(30), ex);
}
```

## BusinessException

`BusinessException` is a base exception class for business-related errors in the Redis Cache Patterns library. It provides standardized error handling with an optional error code and a dictionary of error messages for validation scenarios.

### Usage Example

```csharp
// Basic exception usage
try
{
    var inventoryService = new InventoryService();
    inventoryService.CheckInventory("product123", 10);
}
catch (BusinessException ex)
{
    Console.WriteLine($"Business error occurred: {ex.Message}");
    if (ex.ErrorCode != null)
    {
        Console.WriteLine($"Error code: {ex.ErrorCode}");
    }
    
    // Access validation errors
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"{error.Key}: {string.Join(", ", error.Value)}");
    }
}

// Exception with error code
var insufficientInventory = new InsufficientInventoryException("Not enough stock available", "INV_001", 5, 3);
Console.WriteLine($"Insufficient inventory: {insufficientInventory.ErrorCode} - Requested: {insufficientInventory.Requested}, Available: {insufficientInventory.Available}");

// Exception with validation errors
var validationErrors = new Dictionary<string, List<string>>
{
    { "Email", new List<string> { "Email is required", "Email format is invalid" } },
    { "Quantity", new List<string> { "Quantity must be positive" } }
};
var validationEx = new ValidationException("Validation failed", validationErrors);
Console.WriteLine($"Validation errors: {validationEx.Errors.Count} fields with errors");
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
