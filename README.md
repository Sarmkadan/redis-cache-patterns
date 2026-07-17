// ... (rest of the file remains the same)

## InvalidationHistoryEntry

Represents a single entry in the distributed invalidation history log, capturing details about an invalidation event, including the affected cache key, reason for invalidation, and timestamp.

### Usage Example

```csharp
var entry = new InvalidationHistoryEntry
{
    EventId    = Guid.NewGuid().ToString(),
    CacheKey   = "product:123",
    KeyPattern = null,
    Reason     = InvalidationReason.DataUpdate,
    Source     = "MyService",
    OccurredAt = DateTime.UtcNow,
    NodesNotified = 5
};

Console.WriteLine($"Event ID: {entry.EventId}");
Console.WriteLine($"Cache Key: {entry.CacheKey}");
Console.WriteLine($"Reason: {entry.Reason}");
Console.WriteLine($"Source: {entry.Source}");
Console.WriteLine($"Occurred At: {entry.OccurredAt}");
Console.WriteLine($"Nodes Notified: {entry.NodesNotified}");
