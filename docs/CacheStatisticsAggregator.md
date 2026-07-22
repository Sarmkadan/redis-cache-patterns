# CacheStatisticsAggregator

The `CacheStatisticsAggregator` class provides a thread‑safe mechanism for collecting and exposing cache performance metrics such as hits, misses, and errors. It is designed to be used as a lightweight, in‑memory aggregator that can be shared across multiple cache operations, allowing callers to query a snapshot of the current statistics at any point and to reset the counters when needed. The class implements `IDisposable` to release any underlying resources.

## API

### `void IncrementHits()`
Increments the total number of cache hits by one.  
**Parameters:** None.  
**Returns:** Nothing.  
**Throws:** `ObjectDisposedException` if the instance has been disposed.

### `void IncrementMisses()`
Increments the total number of cache misses by one.  
**Parameters:** None.  
**Returns:** Nothing.  
**Throws:** `ObjectDisposedException` if the instance has been disposed.

### `void IncrementErrors()`
Increments the total number of cache errors by one.  
**Parameters:** None.  
**Returns:** Nothing.  
**Throws:** `ObjectDisposedException` if the instance has been disposed.

### `CacheStatistics GetStatistics()`
Returns a snapshot of the current aggregated statistics. The returned `CacheStatistics` object contains the cumulative values for hits, misses, and errors at the moment the method is called.  
**Parameters:** None.  
**Returns:** A `CacheStatistics` instance (typically a struct or immutable object) with the current counters.  
**Throws:** `ObjectDisposedException` if the instance has been disposed.

### `void Reset()`
Resets all internal counters (hits, misses, errors) to zero.  
**Parameters:** None.  
**Returns:** Nothing.  
**Throws:** `ObjectDisposedException` if the instance has been disposed.

### `void Dispose()`
Releases all resources used by the `CacheStatisticsAggregator`. After disposal, any further calls to other public methods will throw an `ObjectDisposedException`.  
**Parameters:** None.  
**Returns:** Nothing.  
**Throws:** None (safe to call multiple times).

## Usage

### Example 1: Basic integration with a cache service

```csharp
public class MyCacheService
{
    private readonly CacheStatisticsAggregator _stats = new CacheStatisticsAggregator();

    public object Get(string key)
    {
        object value = FetchFromCache(key);
        if (value != null)
        {
            _stats.IncrementHits();
            return value;
        }

        _stats.IncrementMisses();
        value = LoadFromSource(key);
        StoreInCache(key, value);
        return value;
    }

    public CacheStatistics GetStatistics() => _stats.GetStatistics();

    public void Dispose() => _stats.Dispose();
}
```

### Example 2: Periodic reset and reporting

```csharp
public class StatisticsReporter : IDisposable
{
    private readonly CacheStatisticsAggregator _aggregator;
    private readonly Timer _timer;

    public StatisticsReporter(CacheStatisticsAggregator aggregator, TimeSpan interval)
    {
        _aggregator = aggregator;
        _timer = new Timer(ReportAndReset, null, interval, interval);
    }

    private void ReportAndReset(object state)
    {
        var snapshot = _aggregator.GetStatistics();
        Console.WriteLine($"Hits: {snapshot.Hits}, Misses: {snapshot.Misses}, Errors: {snapshot.Errors}");
        _aggregator.Reset();
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
```

## Notes

- **Thread safety:** All public methods are thread‑safe and can be called concurrently from multiple threads without additional synchronization. The implementation uses atomic operations (e.g., `Interlocked`) to ensure consistency of the counters.
- **Disposal:** After `Dispose()` is called, the instance enters a disposed state. Any subsequent call to `IncrementHits`, `IncrementMisses`, `IncrementErrors`, `GetStatistics`, or `Reset` will throw an `ObjectDisposedException`. The `Dispose` method itself can be called multiple times safely.
- **Reset behavior:** Calling `Reset()` while other threads are concurrently incrementing counters is safe; however, the exact moment at which the counters are zeroed is not atomic with respect to the increments. Some increments that occur during the reset may be lost or may appear in the next snapshot. For most monitoring scenarios this is acceptable.
- **Snapshot consistency:** `GetStatistics()` returns a consistent snapshot of the counters at the time of the call. The returned `CacheStatistics` object is independent of the aggregator and will not change after it is created.
- **Resource management:** The `Dispose` method is provided primarily for scenarios where the aggregator holds external resources (e.g., logging sinks, performance counters). In the simplest in‑memory implementation, disposal may be a no‑op, but callers should still follow the disposable pattern.
