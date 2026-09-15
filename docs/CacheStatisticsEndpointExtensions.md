# CacheStatisticsEndpointExtensions

The `CacheStatisticsEndpointExtensions` class provides dependency-injection registration methods for `CacheStatisticsEndpoint`. Its overloads support the default registration, registration with a specific `CacheStatisticsAggregator`, and registration through the cache-services overload.

## API

### `AddCacheStatisticsEndpoint(IServiceCollection services)`

Registers `CacheStatisticsAggregator` and `CacheStatisticsEndpoint` as singleton services.

*   **`services`:** The service collection to configure. Passing `null` throws `ArgumentNullException`.
*   **Return Value:** The same `IServiceCollection` instance, allowing additional registrations to be chained.
*   **Registration Details:** Both services are registered by implementation type and are created when first resolved.

> **Important:** `CacheStatisticsAggregator` currently has a private constructor. Although this overload adds its service descriptor successfully, the default dependency-injection container cannot activate that registration when it is resolved. Use the overload that accepts a `CacheStatisticsAggregator` instance with `CacheStatisticsAggregator.Instance` when the endpoint must be resolved.

### `AddCacheStatisticsEndpoint(IServiceCollection services, CacheStatisticsAggregator statsAggregator)`

Registers `CacheStatisticsEndpoint` as a singleton backed by the supplied statistics aggregator.

*   **`services`:** The service collection to configure. Passing `null` throws `ArgumentNullException`.
*   **`statsAggregator`:** The aggregator instance used by the endpoint. Passing `null` throws `ArgumentNullException`.
*   **Return Value:** The same `IServiceCollection` instance, allowing additional registrations to be chained.
*   **Required Services:** Resolving the endpoint requires `ILogger<CacheStatisticsEndpoint>` and `PerformanceMonitor` to already be available from the service provider.
*   **Registration Details:** The supplied aggregator is captured by the endpoint factory; it is not separately registered as a `CacheStatisticsAggregator` service by this overload.

### `AddCacheStatisticsEndpoint(IServiceCollection services, IEnumerable<ICacheService> cacheServices)`

Registers `CacheStatisticsEndpoint` as a singleton using `CacheStatisticsAggregator.Instance`.

*   **`services`:** The service collection to configure. Passing `null` throws `ArgumentNullException`.
*   **`cacheServices`:** A collection intended to identify cache services whose statistics should be aggregated. In the current implementation, the value is not validated, enumerated, or otherwise used.
*   **Return Value:** The same `IServiceCollection` instance, allowing additional registrations to be chained.
*   **Required Services:** Resolving the endpoint requires `ILogger<CacheStatisticsEndpoint>` and `PerformanceMonitor` to already be available from the service provider.
*   **Registration Details:** This overload does not register the cache services or the shared aggregator in the service collection. It only captures `CacheStatisticsAggregator.Instance` in the endpoint factory.

## Usage

Register the endpoint with the shared aggregator when configuring the application services:

```csharp
using RedisCachePatterns.API;
using RedisCachePatterns.Monitoring;

services.AddCacheStatisticsEndpoint(CacheStatisticsAggregator.Instance);
```

The endpoint can then be resolved and used through the service provider:

```csharp
var endpoint = serviceProvider.GetRequiredService<CacheStatisticsEndpoint>();
var response = await endpoint.GetStatisticsAsync();

if (response.IsSuccess && response.Data is not null)
{
    Console.WriteLine($"Cache hit rate: {response.Data.HitRate:F2}%");
}
```

The cache-services overload is also available, but the collection does not currently affect aggregation:

```csharp
IEnumerable<ICacheService> cacheServices = GetCacheServices();
services.AddCacheStatisticsEndpoint(cacheServices);
```

## Notes

*   **Singleton lifetime:** Each overload registers `CacheStatisticsEndpoint` as a singleton, so its dependencies must be safe to use for the lifetime of the service provider.
*   **Duplicate registrations:** Calling an overload more than once appends registrations. When a single service is resolved, the default container uses the last registration; resolving `IEnumerable<CacheStatisticsEndpoint>` returns all registrations.
*   **Deferred dependency checks:** The factory-based overloads retrieve the logger and performance monitor when the endpoint is first resolved, not when registration occurs.
*   **No Redis connection:** These methods only configure dependency injection. They do not connect to Redis or discover cache services.
*   **Related API:** See `CacheStatisticsEndpoint` for the statistics and reset operations exposed by the registered endpoint.
