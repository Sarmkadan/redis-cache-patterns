# CacheInvalidationEvent

Represents a single cache invalidation occurrence within the system, capturing the identity of the event, the target cache entry or pattern, the reason for invalidation, timing, origin, and any supplementary contextual metadata. Instances of this type are typically produced by cache invalidation publishers and consumed by subscribers or logging pipelines.

## API

### `public string EventId`
A unique identifier for this invalidation event. This value is assigned at creation and serves as a correlation handle across distributed components. It is never null.

### `public string? CacheKey`
The exact key of the cache entry that was invalidated. When the invalidation targets a single entry, this field holds that key; otherwise it is `null`. Consumers should check for `null` before using it as a direct lookup key.

### `public string? KeyPattern`
A glob-style or regex pattern that was used to match and invalidate multiple cache keys simultaneously. When the invalidation is pattern-based, this field contains the pattern string; otherwise it is `null`. Mutually exclusive with a non-null `CacheKey` in typical usage.

### `public InvalidationReason Reason`
An enumeration value indicating the cause of the invalidation (e.g. `Manual`, `TimeToLiveExpired`, `DependencyChanged`, `Backpressure`). This allows downstream handlers to branch logic based on why the cache entry was removed.

### `public DateTime OccurredAt`
The UTC timestamp at which the invalidation event was raised. Recorded at the point of emission, it provides an ordering guarantee when combined with `EventId` for tracing and debugging.

### `public string Source`
An identifier for the component or service that originated the invalidation. Typically a machine name, pod identifier, or logical service name. This field is never null and aids in auditing and troubleshooting in multi-producer environments.

### `public Dictionary<string, string> Metadata`
A flexible dictionary of additional contextual information attached to the event. May include correlation IDs, tenant identifiers, cache provider version, or custom tags. The dictionary is never null but may be empty. Modifications to the returned dictionary after the event is published will not be reflected in already-emitted events if the publisher copies it defensively.

## Usage

### Example 1: Handling a single-key invalidation in a subscriber

```csharp
void OnCacheInvalidated(CacheInvalidationEvent e)
{
    Console.WriteLine($"[{e.OccurredAt:O}] Event {e.EventId} from {e.Source}");

    if (e.CacheKey is not null)
    {
        // Direct key invalidation — remove from local L1 cache
        localCache.Remove(e.CacheKey);
        Console.WriteLine($"Removed local entry for key '{e.CacheKey}'");
    }
    else if (e.KeyPattern is not null)
    {
        // Pattern-based invalidation — scan and purge matching keys
        var matchedKeys = localCache.Keys.Where(k => MatchesPattern(k, e.KeyPattern));
        foreach (var key in matchedKeys)
        {
            localCache.Remove(key);
        }
        Console.WriteLine($"Purged {matchedKeys.Count()} keys matching pattern '{e.KeyPattern}'");
    }
}
```

### Example 2: Publishing an event with metadata for auditing

```csharp
var invalidationEvent = new CacheInvalidationEvent
{
    EventId = Guid.NewGuid().ToString("N"),
    CacheKey = "user:session:42a7f8",
    Reason = InvalidationReason.Manual,
    OccurredAt = DateTime.UtcNow,
    Source = Environment.MachineName,
    Metadata = new Dictionary<string, string>
    {
        ["CorrelationId"] = Activity.Current?.Id ?? "N/A",
        ["Initiator"] = "AdminDashboard",
        ["Tenant"] = "TenantA"
    }
};

await cacheInvalidationPublisher.PublishAsync(invalidationEvent);
```

## Notes

- When both `CacheKey` and `KeyPattern` are non-null, the event represents a dual-target invalidation. Consumers should handle this scenario explicitly rather than assuming mutual exclusivity.
- `OccurredAt` uses UTC; subscribers that compare timestamps across events must convert their own local times to UTC to maintain correct ordering.
- The `Metadata` dictionary is not thread-safe for concurrent writes after the event is published. If multiple threads need to enrich metadata before publication, synchronize access externally or build the dictionary on a single thread prior to assignment.
- `EventId` uniqueness is the responsibility of the producer. Duplicate identifiers across different sources can weaken correlation; prefixing with a source-specific short code is recommended in multi-service topologies.
- `CacheKey` and `KeyPattern` may both be `null` in cases of full-cache flushes or when the invalidation reason does not target specific entries (e.g., a global reset signal). Defensive null checks are required before key-based operations.
