# CacheEventListener

`CacheEventListener` listens to cache events and maintains statistics for cache hit/miss ratios and other monitoring metrics. It provides asynchronous handlers for cache hits, misses, invalidations, and flushes, along with methods to compute hit rates and retrieve total counts.

## API

### `public CacheEventListener(ILogger<CacheEventListener> logger)

Creates a new instance of the cache event listener.

**Parameters:**
- `logger`: The logger instance used for recording cache events.

### `public Task OnCacheHitAsync(CacheHitEvent @event)`

Handles cache hit events by incrementing the hit counter and logging the event.

**Parameters:**
- `@event`: The cache hit event containing the cache key and data size.

**Returns:** `Task` representing the asynchronous operation.

**Exceptions:**
- `ArgumentNullException` if `@event` is null.
- `ArgumentException` if `@event.CacheKey` is null or empty.

### `public Task OnCacheMissAsync(CacheMissEvent @event)`

Handles cache miss events by incrementing the miss counter and logging the event.

**Parameters:**
- `@event`: The cache miss event containing the cache key.

**Returns:** `Task` representing the asynchronous operation.

**Exceptions:**
- `ArgumentNullException` if `@event` is null.
- `ArgumentException` if `@event.CacheKey` is null or empty.

### `public Task OnCacheInvalidatedAsync(CacheInvalidatedEvent @event)`

Handles cache invalidation events by logging the invalidation details.

**Parameters:**
- `@event`: The cache invalidation event containing the cache key pattern and count of affected keys.

**Returns:** `Task` representing the asynchronous operation.

**Exceptions:**
- `ArgumentNullException` if `@event` is null.
- `ArgumentException` if `@event.CacheKeyPattern` is null or empty.

### `public Task OnCacheFlushedAsync(CacheFlushEvent @event)`

Handles cache flush events by logging the flush details and resetting hit/miss counters.

**Parameters:**
- `@event`: The cache flush event containing the number of keys removed.

**Returns:** `Task` representing the asynchronous operation.

**Exceptions:**
- `ArgumentNullException` if `@event` is null.

### `public double GetHitRate()`

Computes the cache hit rate as a percentage of total hits to total access attempts (hits plus misses).

**Parameters:** none.

**Returns:** `double` representing the hit rate percentage (0-100). Returns 0 if no accesses have been recorded.

### `public int GetTotalHits()`

Returns the cumulative count of cache hits recorded by this listener.

**Parameters:** none.

**Returns:** `int` representing the total number of successful cache retrievals.

### `public int GetTotalMisses()`

Returns the cumulative count of cache misses recorded by this listener.

**Parameters:** none.

**Returns:** `int` representing the total number of failed cache lookups.

## Usage

### Example: Monitoring Cache Performance

```csharp
// Create listener with logger
var loggerFactory = LoggerFactory.Create(builder => 
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
ILogger<CacheEventListener> logger = loggerFactory.CreateLogger<CacheEventListener>();
var listener = new CacheEventListener(logger);

// Simulate cache operations
var hitEvent = new CacheHitEvent { CacheKey = "user:123", DataSize = 1024 };
var missEvent = new CacheMissEvent { CacheKey = "user:456" };
var invalidationEvent = new CacheInvalidatedEvent 
{ 
    CacheKeyPattern = "user:*", 
    KeysAffected = 5 
};
var flushEvent = new CacheFlushEvent { KeysRemoved = 10 };

// Process events
await listener.OnCacheHitAsync(hitEvent);
await listener.OnCacheHitAsync(hitEvent);
await listener.OnCacheMissAsync(missEvent);
await listener.OnCacheInvalidatedAsync(invalidationEvent);
await listener.OnCacheFlushedAsync(flushEvent);

// Retrieve statistics
double hitRate = listener.GetHitRate();
int totalHits = listener.GetTotalHits();
int totalMisses = listener.GetTotalMisses();

Console.WriteLine($"Cache Hit Rate: {hitRate}% ({totalHits} hits, {totalMisses} misses)");
// After flush, counters are reset so hit rate will be 0%
```

## Notes

- **Thread safety:** This class is not thread-safe for concurrent access from multiple threads. If used in a multi-threaded environment, external synchronization is required.
- **Counter reset:** The `OnCacheFlushedAsync` method resets both hit and miss counters to zero when a cache flush occurs.
- **Logging levels:** Events are logged at different levels: Debug for hits/misses, Information for invalidations, and Warning for flushes.
- **Validation:** All event handler methods validate their arguments and throw appropriate exceptions for null or invalid inputs.