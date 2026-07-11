# InvalidationHistoryEntry

Represents a single entry in the invalidation history log. Each entry records an invalidation event that was either triggered locally or received from a distributed broadcaster. The entry captures the event identifier, the affected cache key or pattern, the reason for invalidation, the source component, the timestamp, and the number of nodes that were notified. This type is used by the `DistributedInvalidationBroadcaster` to maintain an audit trail and to support retrieval of recent invalidation history via the `GetHistory` property.

## API

### `public string EventId`
A unique identifier for the invalidation event. This value is assigned when the entry is created and is intended to correlate the entry with other logs or telemetry.

### `public string? CacheKey`
The exact cache key that was invalidated, if the invalidation targeted a single key. May be `null` when the invalidation was pattern-based.

### `public string? KeyPattern`
The key pattern that was invalidated, if the invalidation targeted a set of keys matching a pattern. May be `null` when the invalidation targeted a single key.

### `public InvalidationReason Reason`
The reason why the invalidation occurred. The value is one of the members of the `InvalidationReason` enumeration (e.g., `Expired`, `Evicted`, `ManuallyInvalidated`, `DependencyChanged`).

### `public string Source`
Identifies the component or service that initiated the invalidation. This is typically a machine name, application instance identifier, or a logical name.

### `public DateTime OccurredAt`
The UTC timestamp when the invalidation event was recorded.

### `public long NodesNotified`
The number of distributed nodes that were successfully notified of this invalidation event. A value of zero indicates that no nodes were notified (e.g., the broadcaster was not configured or the event was local only).

### `public DistributedInvalidationBroadcaster`
Gets the `DistributedInvalidationBroadcaster` instance that created or processed this entry. This property can be used to access the broadcaster’s methods, such as `BroadcastAsync` or `GetHistory`.

### `public Task BroadcastAsync()`
Asynchronously rebroadcasts the invalidation event represented by this entry to all connected nodes.  
**Returns:** A `Task` that completes when the broadcast has been sent.  
**Exceptions:** May throw `ObjectDisposedException` if the underlying broadcaster has been disposed.

### `public Task BroadcastPatternAsync()`
Asynchronously rebroadcasts the invalidation pattern (if any) represented by this entry to all connected nodes. If the entry does not contain a pattern (`KeyPattern` is `null`), this method has no effect and returns a completed task.  
**Returns:** A `Task` that completes when the pattern broadcast has been sent.  
**Exceptions:** May throw `ObjectDisposedException` if the underlying broadcaster has been disposed.

### `public async Task SubscribeAsync()`
Asynchronously subscribes to future invalidation events from the distributed broadcaster. After calling this method, the broadcaster will begin delivering invalidation notifications to the local cache. This method is typically called once during application startup.  
**Returns:** A `Task` that completes when the subscription is established.  
**Exceptions:** May throw `InvalidOperationException` if the broadcaster is already subscribed, or `ObjectDisposedException` if the broadcaster has been disposed.

### `public IReadOnlyList<InvalidationHistoryEntry> GetHistory`
Gets a read-only list of recent invalidation history entries recorded by the associated broadcaster. The list is ordered from most recent to oldest, and its maximum size is determined by the broadcaster’s configuration. This property returns a snapshot of the history at the time of access.

## Usage

### Example 1: Recording and inspecting an invalidation event

```csharp
var broadcaster = new DistributedInvalidationBroadcaster(configuration);
var entry = new InvalidationHistoryEntry
{
    EventId = Guid.NewGuid().ToString(),
    CacheKey = "user:123",
    Reason = InvalidationReason.ManuallyInvalidated,
    Source = "AdminPanel",
    OccurredAt = DateTime.UtcNow,
    NodesNotified = 3
};

// Later, retrieve the history
foreach (var historyEntry in broadcaster.GetHistory)
{
    Console.WriteLine($"[{historyEntry.OccurredAt}] {historyEntry.EventId}: {historyEntry.CacheKey ?? historyEntry.KeyPattern} ({historyEntry.Reason})");
}
```

### Example 2: Rebroadcasting a previous invalidation

```csharp
// Assume we have an entry from the history
InvalidationHistoryEntry lastEntry = broadcaster.GetHistory.FirstOrDefault();
if (lastEntry != null)
{
    // Rebroadcast the same invalidation to all nodes
    await lastEntry.BroadcastAsync();
    
    // If the entry had a pattern, rebroadcast the pattern as well
    if (lastEntry.KeyPattern != null)
    {
        await lastEntry.BroadcastPatternAsync();
    }
}
```

## Notes

- **Nullability:** `CacheKey` and `KeyPattern` are nullable. At most one of them should be non-null for a given entry; if both are `null`, the entry represents a global invalidation (all keys).
- **Thread safety:** Instances of `InvalidationHistoryEntry` are intended to be immutable after creation. Reading properties is safe from multiple threads concurrently. The `GetHistory` property returns a snapshot; the underlying list is not modified while being enumerated. The `BroadcastAsync` and `BroadcastPatternAsync` methods are thread-safe and can be called concurrently, but the broadcaster itself may enforce serialization of outbound messages.
- **Subscription lifecycle:** `SubscribeAsync` should be called only once per broadcaster instance. Calling it multiple times will throw `InvalidOperationException`. The subscription persists until the broadcaster is disposed.
- **History capacity:** The `GetHistory` list is bounded. Older entries are automatically removed when the configured maximum is reached. The exact capacity is determined by the broadcaster’s `HistorySize` option.
- **Timestamp precision:** `OccurredAt` is stored as a `DateTime` with millisecond precision. For high-throughput scenarios, consider using `DateTime.UtcNow` with `DateTimeKind.Utc` to avoid ambiguity.
