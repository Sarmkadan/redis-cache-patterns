// existing content ...

## InvalidateKeyRequest

The `InvalidateKeyRequest` class represents a request to invalidate a specific cache key across a distributed cache. It allows specifying the cache key, invalidation reason, and the source of the request.

### Usage Example
```csharp
var request = new InvalidateKeyRequest
{
    CacheKey = "user:123:profile",
    Reason = InvalidationReason.DataUpdate,
    Source = "UserService"
};

// Use the request with DistributedInvalidationEndpoint
var endpoint = new DistributedInvalidationEndpoint(
    broadcaster: distributedInvalidationBroadcaster,
    logger: logger,
    performanceMonitor: performanceMonitor
);

var result = await endpoint.InvalidateKeyAsync(request);
if (result.IsSuccess)
{
    Console.WriteLine($"Invalidated key {request.CacheKey} with reason {request.Reason} from {request.Source}");
    var broadcastResult = result.Data;
    Console.WriteLine($"Success: {broadcastResult.Success}, Nodes notified: {broadcastResult.NodesNotified}, Event ID: {broadcastResult.EventId}");
}
