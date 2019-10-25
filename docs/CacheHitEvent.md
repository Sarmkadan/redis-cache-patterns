# CacheHitEvent

`CacheHitEvent` is a monitoring and diagnostics type that captures telemetry around cache access patterns. It exposes the key involved in a cache operation, the size of the cached data, the pattern that matched the key, and counts of affected or removed keys. It also provides asynchronous event hooks for cache hits, misses, invalidations, and flushes, along with methods to compute the hit rate and retrieve total hit/miss counts.

## API

### `public string CacheKey`
The specific cache key that was accessed or operated on. This value identifies the exact entry in the cache store.

### `public long DataSize`
The size, in bytes, of the cached data associated with `CacheKey`. This reflects the payload size at the time the event was recorded.

### `public string CacheKeyPattern`
The pattern used to match or group cache keys. This may be a wildcard expression, prefix, or other matching rule that relates `CacheKey` to a broader set of entries.

### `public int KeysAffected`
The number of cache keys that were affected by the current operation. For invalidation or flush events, this indicates how many entries were impacted beyond the single `CacheKey`.

### `public int KeysRemoved`
The number of cache keys actually removed during the operation. This may differ from `KeysAffected` when an operation targets keys that do not exist or when partial removal occurs.

### `public CacheEventListener`
A reference to the listener instance that owns or dispatches this event. It provides context for the event source and may expose additional lifecycle or configuration details.

### `public Task OnCacheHitAsync`
An asynchronous handler invoked when a cache hit occurs. Awaiting this task allows callers to react to successful retrievals, such as logging or updating metrics.

**Parameters:** none (event delegate).  
**Returns:** `Task` representing the asynchronous operation.  
**Throws:** exceptions propagated from subscriber implementations; no built-in validation.

### `public Task OnCacheMissAsync`
An asynchronous handler invoked when a cache miss occurs. It signals that the requested key was not found in the cache.

**Parameters:** none (event delegate).  
**Returns:** `Task` representing the asynchronous operation.  
**Throws:** exceptions propagated from subscriber implementations; no built-in validation.

### `public Task OnCacheInvalidatedAsync`
An asynchronous handler invoked when a cache entry or set of entries is explicitly invalidated. This covers both single-key and pattern-based invalidation.

**Parameters:** none (event delegate).  
**Returns:** `Task` representing the asynchronous operation.  
**Throws:** exceptions propagated from subscriber implementations; no built-in validation.

### `public Task OnCacheFlushedAsync`
An asynchronous handler invoked when the entire cache is flushed. This indicates a complete clearing of all entries.

**Parameters:** none (event delegate).  
**Returns:** `Task` representing the asynchronous operation.  
**Throws:** exceptions propagated from subscriber implementations; no built-in validation.

### `public double GetHitRate`
Computes the cache hit rate as a ratio of total hits to total access attempts (hits plus misses).

**Parameters:** none.  
**Returns:** `double` between 0.0 and 1.0 inclusive. Returns 0.0 if no accesses have been recorded.  
**Throws:** does not throw; returns 0.0 when total attempts are zero to avoid division by zero.

### `public int GetTotalHits`
Returns the cumulative count of cache hits recorded by this event instance.

**Parameters:** none.  
**Returns:** `int` representing the total number of successful cache retrievals.  
**Throws:** never throws.

### `public int GetTotalMisses`
Returns the cumulative count of cache misses recorded by this event instance.

**Parameters:** none.  
**Returns:** `int` representing the total number of failed cache lookups.  
**Throws:** never throws.

## Usage

### Example 1: Subscribing to Cache Events and Computing Hit Rate

```csharp
CacheEventListener listener = new CacheEventListener();
CacheHitEvent hitEvent = new CacheHitEvent
{
    CacheEventListener = listener,
    CacheKey = "user:12345",
    CacheKeyPattern = "user:*",
    DataSize = 2048
};

// Subscribe to hit and miss handlers
hitEvent.OnCacheHitAsync += async () =>
{
    Console.WriteLine($"Hit: {hitEvent.CacheKey}, Size: {hitEvent.DataSize} bytes");
    await Task.CompletedTask;
};

hitEvent.OnCacheMissAsync += async () =>
{
    Console.WriteLine($"Miss: {hitEvent.CacheKey}");
    await Task.CompletedTask;
};

// Simulate a few accesses
await hitEvent.OnCacheHitAsync();
await hitEvent.OnCacheMissAsync();
await hitEvent.OnCacheHitAsync();

double hitRate = hitEvent.GetHitRate();
int totalHits = hitEvent.GetTotalHits();
int totalMisses = hitEvent.GetTotalMisses();

Console.WriteLine($"Hit Rate: {hitRate:P2} ({totalHits} hits, {totalMisses} misses)");
```

### Example 2: Handling Invalidation with Affected and Removed Counts

```csharp
CacheHitEvent invalidationEvent = new CacheHitEvent
{
    CacheKey = "session:abc",
    CacheKeyPattern = "session:*",
    KeysAffected = 5,
    KeysRemoved = 4
};

invalidationEvent.OnCacheInvalidatedAsync += async () =>
{
    Console.WriteLine(
        $"Invalidated pattern '{invalidationEvent.CacheKeyPattern}': " +
        $"{invalidationEvent.KeysAffected} affected, " +
        $"{invalidationEvent.KeysRemoved} removed.");
    await Task.CompletedTask;
};

await invalidationEvent.OnCacheInvalidatedAsync();
```

## Notes

- **Thread safety:** The `GetHitRate`, `GetTotalHits`, and `GetTotalMisses` methods read from counters that may be mutated by concurrent event invocations. If multiple threads subscribe to or invoke the asynchronous handlers simultaneously, external synchronization is required to ensure consistent reads of hit/miss totals and the computed rate.
- **Division by zero:** `GetHitRate` returns `0.0` when no accesses have occurred (total hits plus total misses equals zero). Callers should not rely on this method to distinguish between a true zero hit rate and an uninitialized state.
- **Event handler exceptions:** The `OnCacheHitAsync`, `OnCacheMissAsync`, `OnCacheInvalidatedAsync`, and `OnCacheFlushedAsync` handlers are multicast delegates. If any subscriber throws, the exception propagates to the caller and may prevent remaining subscribers from executing. Consider wrapping individual subscriber logic in try-catch blocks when multiple handlers are attached.
- **`KeysAffected` vs `KeysRemoved`:** `KeysAffected` represents the total number of keys that matched the pattern or operation scope, while `KeysRemoved` reflects the subset that actually existed and were deleted. A difference between these values is expected when some matched keys were already absent from the cache.
- **`CacheKey` duplication:** The API listing includes `CacheKey` twice. This is the same member; there is no overload or separate field. References to `CacheKey` in event handlers and metrics always refer to the single key associated with the current event instance.
