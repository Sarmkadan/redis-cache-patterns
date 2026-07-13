// ... existing content ...

## InvalidateKeyRequestExtensions

The `InvalidateKeyRequestExtensions` class provides a set of extension methods for building and customizing `InvalidateKeyRequest` objects. These extensions simplify the process of creating requests to invalidate cache keys based on specific criteria.

### Usage Examples

```csharp
// Invalidate a cache key for a specific product
var productId = 123;
var request = InvalidateKeyRequestExtensions.ForProduct(productId);

// Invalidate a cache key for a specific user
var userId = 456;
request = InvalidateKeyRequestExtensions.ForUser(userId);

// Invalidate a cache key for a specific session
request = InvalidateKeyRequestExtensions.ForSession("session-id");

// Customize the invalidation reason
request = request.WithReason(InvalidationReason.DataUpdate);

// Check if the request is for manual purge
if (request.IsManualPurge) {
    Console.WriteLine("Manual purge requested");
}

// Check if the request is for data update
if (request.IsDataUpdate) {
    Console.WriteLine("Data update requested");
}
```

// ... existing content ...
