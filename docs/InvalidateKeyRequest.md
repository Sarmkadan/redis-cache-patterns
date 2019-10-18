# InvalidateKeyRequest

The `InvalidateKeyRequest` type facilitates the orchestration and execution of cache invalidation operations within distributed Redis environments. It encapsulates the necessary parameters for identifying cache entries—either individually by key or collectively by pattern—and provides asynchronous methods to broadcast invalidation commands, track notification status across nodes, and retrieve historical invalidation data.

## API

### Properties

*   **`CacheKey`** (`string`): The specific cache key targeted for invalidation. Used primarily for single-key operations.
*   **`Reason`** (`InvalidationReason`): Specifies the `InvalidationReason` enum or type describing the context of the invalidation (e.g., user update, system maintenance).
*   **`Source`** (`string`): Identifies the origin service or module that initiated the invalidation request for auditing and tracing.
*   **`KeyPattern`** (`string`): A glob-style pattern used to target multiple keys simultaneously for bulk invalidation.
*   **`Success`** (`bool`): Indicates whether the most recent invalidation operation was acknowledged as successful by the distributed system.
*   **`NodesNotified`** (`long`): The count of distributed nodes that acknowledged the invalidation broadcast.
*   **`EventId`** (`string`): A unique identifier for the invalidation event, used for tracking and correlation across logs and history.
*   **`BroadcastAt`** (`DateTime`): The timestamp indicating when the invalidation broadcast was issued.
*   **`DistributedInvalidationEndpoint`** (`DistributedInvalidationEndpoint`): Configuration object defining the endpoint behavior and connection parameters for the distributed invalidation service.

### Methods

*   **`InvalidateKeyAsync()`** (`Task<ApiResponse<InvalidationBroadcastResult>>`): Asynchronously broadcasts an invalidation command for the specified `CacheKey`. Throws exceptions if the communication with the underlying cache infrastructure fails.
*   **`InvalidatePatternAsync()`** (`Task<ApiResponse<InvalidationBroadcastResult>>`): Asynchronously broadcasts an invalidation command for all keys matching the specified `KeyPattern`. Throws exceptions if the communication with the underlying cache infrastructure fails.
*   **`GetHistoryAsync()`** (`Task<ApiResponse<IReadOnlyList<InvalidationHistoryEntry>>>`): Asynchronously retrieves the history of previous invalidation events associated with the request criteria.

## Usage

### Example 1: Direct Key Invalidation
```csharp
var request = new InvalidateKeyRequest 
{
    CacheKey = "user:profile:12345",
    Reason = InvalidationReason.UserUpdated,
    Source = "AccountService"
};

var response = await request.InvalidateKeyAsync();

if (response.IsSuccess)
{
    Console.WriteLine($"Invalidation broadcast to {request.NodesNotified} nodes.");
}
```

### Example 2: Pattern Invalidation
```csharp
var request = new InvalidateKeyRequest 
{
    KeyPattern = "tenant:*:config",
    Source = "ConfigurationManager"
};

var response = await request.InvalidatePatternAsync();

if (response.Value.Success)
{
    // Handle successful bulk invalidation
}
```

## Notes

*   **Asynchronous Execution**: All methods are asynchronous and should be awaited. Failure to await these methods may result in unhandled exceptions or incomplete operations.
*   **Thread Safety**: The `InvalidateKeyRequest` instance itself is not inherently thread-safe for concurrent property modification. It is designed to be instantiated and configured before invoking its asynchronous methods.
*   **Distributed Failures**: In a distributed system, a broadcast may not reach every node. The `NodesNotified` property and the `InvalidationBroadcastResult` should be inspected to confirm the extent of the operation's success.
*   **Exception Handling**: Users should implement robust error handling around the `Async` methods, as they interact with external network-bound resources and are subject to transient network failures, timeouts, and authorization errors.
