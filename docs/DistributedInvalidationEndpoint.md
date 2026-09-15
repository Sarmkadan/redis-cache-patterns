# DistributedInvalidationEndpoint

The `DistributedInvalidationEndpoint` class broadcasts exact-key and glob-pattern cache invalidations through an `IDistributedInvalidationBroadcaster`. It also exposes the broadcaster's local history. Successful operations and broadcaster failures are returned in the standard `ApiResponse<T>` format provided by `ApiEndpointBase`.

## Request and response models

### `InvalidateKeyRequest`

Describes an exact-key invalidation.

*   **`CacheKey`** (`string`): Exact Redis key to invalidate. The endpoint requires a non-empty, non-whitespace value.
*   **`Reason`** (`InvalidationReason`): Reason for the invalidation. Defaults to `InvalidationReason.DataUpdate`.
*   **`Source`** (`string`): Name of the requesting service for audit tracing. Defaults to an empty string.

### `InvalidatePatternRequest`

Describes a pattern-based invalidation.

*   **`KeyPattern`** (`string`): Glob-style pattern, such as `product:*`, to apply on each receiving node. The endpoint requires a non-empty, non-whitespace value.
*   **`Reason`** (`InvalidationReason`): Reason for the invalidation. Defaults to `InvalidationReason.DataUpdate`.
*   **`Source`** (`string`): Name of the requesting service for audit tracing. Defaults to an empty string.

### `InvalidationBroadcastResult`

Summarizes a completed broadcast.

*   **`Success`** (`bool`): `true` when the broadcast operation completed without an exception.
*   **`NodesNotified`** (`long`): Node count copied from the newest history entry, or `0` when no entry is available. The broadcaster may use `-1` when the pub/sub count is unavailable.
*   **`EventId`** (`string`): Identifier copied from the newest history entry, or an empty string when no entry is available.
*   **`BroadcastAt`** (`DateTime`): UTC time at which the endpoint creates the result.

## API

### `DistributedInvalidationEndpoint(IDistributedInvalidationBroadcaster broadcaster, ILogger<DistributedInvalidationEndpoint> logger, PerformanceMonitor performanceMonitor)`

Initializes the endpoint with its broadcaster and shared endpoint infrastructure.

*   **`broadcaster`:** Publishes invalidation events and maintains local history. Passing `null` throws `ArgumentNullException`.
*   **`logger`:** Logger used by `ApiEndpointBase` for operation results and errors.
*   **`performanceMonitor`:** Monitor used to measure endpoint operations.

### `Task<ApiResponse<InvalidationBroadcastResult>> InvalidateKeyAsync(InvalidateKeyRequest request)`

Broadcasts an invalidation for one exact cache key.

*   **`request`:** Contains the key, reason, and source forwarded to `BroadcastAsync`.
*   **Return Value:** On success, an `ApiResponse<InvalidationBroadcastResult>` with status code 200 and details taken from the newest local history entry.
*   **Operation Name:** `InvalidateKey(<cache key>)`, used for logging and performance measurement.
*   **Validation:** A null request or a blank `CacheKey` causes `ArgumentException` before the operation is wrapped by `ApiEndpointBase`; callers must handle that exception directly.
*   **Broadcast Errors:** Exceptions raised during broadcasting are converted to failure responses: argument errors use status code 400, invalid-operation errors use 409, and unexpected errors use 500.

### `Task<ApiResponse<InvalidationBroadcastResult>> InvalidatePatternAsync(InvalidatePatternRequest request)`

Broadcasts an invalidation for all keys matching a glob pattern.

*   **`request`:** Contains the pattern, reason, and source forwarded to `BroadcastPatternAsync`.
*   **Return Value:** On success, an `ApiResponse<InvalidationBroadcastResult>` with status code 200 and details taken from the newest local history entry.
*   **Operation Name:** `InvalidatePattern(<key pattern>)`, used for logging and performance measurement.
*   **Validation:** A null request or a blank `KeyPattern` causes `ArgumentException` before the operation is wrapped by `ApiEndpointBase`; callers must handle that exception directly.
*   **Broadcast Errors:** Exceptions raised during broadcasting use the same failure-response mapping as `InvalidateKeyAsync`.

### `Task<ApiResponse<IReadOnlyList<InvalidationHistoryEntry>>> GetHistoryAsync()`

Returns the invalidation history recorded on the current node.

*   **Parameters:** None.
*   **Return Value:** An `ApiResponse` whose `Data` contains a snapshot of history entries ordered newest first.
*   **Operation Name:** `GetInvalidationHistory`, used for logging and performance measurement.
*   **Errors:** Exceptions raised while retrieving history are converted to failure responses by `ApiEndpointBase`.

## Usage

```csharp
using RedisCachePatterns.API;
using RedisCachePatterns.Domain;

var endpoint = serviceProvider.GetRequiredService<DistributedInvalidationEndpoint>();

var response = await endpoint.InvalidateKeyAsync(new InvalidateKeyRequest
{
    CacheKey = "product:42",
    Reason = InvalidationReason.DataUpdate,
    Source = "CatalogService"
});

if (response.IsSuccess && response.Data is not null)
{
    Console.WriteLine($"Event {response.Data.EventId} notified {response.Data.NodesNotified} nodes.");
}
else
{
    Console.WriteLine($"Invalidation failed: {response.Error}");
}
```

```csharp
var patternResponse = await endpoint.InvalidatePatternAsync(new InvalidatePatternRequest
{
    KeyPattern = "product:*",
    Reason = InvalidationReason.ManualPurge,
    Source = "AdminPortal"
});

var historyResponse = await endpoint.GetHistoryAsync();
if (historyResponse.IsSuccess && historyResponse.Data is not null)
{
    foreach (var entry in historyResponse.Data)
    {
        Console.WriteLine($"{entry.OccurredAt:u} {entry.EventId}");
    }
}
```

## Notes

*   **Dependency injection:** `AddDistributedInvalidation()` registers the broadcaster, but the current registration method does not register `DistributedInvalidationEndpoint`; register or construct the endpoint separately.
*   **Local history:** `GetHistoryAsync()` returns history stored by this broadcaster instance. Its configured maximum size and lifecycle determine which events are present.
*   **Delivery semantics:** The endpoint reports that the broadcast call completed; it does not independently verify that every receiving node removed its matching cache entries.
*   **Cancellation:** The endpoint methods do not accept a `CancellationToken`, even though the broadcaster interface supports one.
*   **Response handling:** Check `IsSuccess` before reading `Data`. Failure responses expose their message through `Error` and their HTTP-style code through `StatusCode`.
