# CacheStatisticsAggregator

The `CacheStatisticsAggregator` class provides a thread‑safe mechanism for collecting and exposing cache performance metrics such as hits, misses, and errors. It is designed to be used as a lightweight, in‑memory aggregator that can be shared across multiple cache operations, allowing callers to query a snapshot of the current statistics at any point and to reset the counters when needed. The class implements `IDisposable` to release any underlying resources.

## API

### `void IncrementHits()`

Increments the total number of cache hits by one and records the metric in the OpenTelemetry meter.

**Parameters:** None.
**Returns:** Nothing.
**Throws:** `ObjectDisposedException` if the instance has been disposed.

### `void IncrementMisses()`

Increments the total number of cache misses by one and records the metric in the OpenTelemetry meter.

**Parameters:** None.
**Returns:** Nothing.
**Throws:** `ObjectDisposedException` if the instance has been disposed.

### `void IncrementErrors()`

Increments the total number of cache errors by one and records the metric in the OpenTelemetry meter.

**Parameters:** None.
**Returns:** Nothing.
**Throws:** `ObjectDisposedException` if the instance has been disposed.

### `void RecordOperationDuration(double durationMilliseconds)`

Records the duration of a cache operation in milliseconds for latency histogram metrics.

**Parameters:**
- `durationMilliseconds`: Duration of the operation in milliseconds.

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

Releases all resources used by the `CacheStatisticsAggregator`, including the OpenTelemetry meter.

**Parameters:** None.
**Returns:** Nothing.
**Throws:** None (safe to call multiple times).

### `Meter Meter`

Gets the OpenTelemetry meter instance used for metrics collection.

**Returns:** The `Meter` instance for recording metrics.

## Metrics Exposed via System.Diagnostics.Metrics

The `CacheStatisticsAggregator` now exports the following metrics via OpenTelemetry-compatible `System.Diagnostics.Metrics`:

| Metric Name | Type | Description | Unit |
|-------------|------|-------------|------|
| `cache.hits` | Counter<long> | Number of cache hits | hits |
| `cache.misses` | Counter<long> | Number of cache misses | misses |
| `cache.errors` | Counter<long> | Number of cache errors | errors |
| `cache.operation.duration` | Histogram<double> | Duration of cache operations in milliseconds | milliseconds |
| `cache.hit_ratio` | ObservableGauge<double> | Cache hit ratio (0-1) | ratio |

These metrics can be consumed by:
- OpenTelemetry collectors
- Prometheus via OpenTelemetry Collector
- Application Insights
- Any OpenTelemetry-compatible monitoring system

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

### Example 3: Recording operation durations

```csharp
public class InstrumentedCacheService
{
    private readonly CacheStatisticsAggregator _stats = CacheStatisticsAggregator.Instance;

    public async Task<T> GetOrLoadAsync<T>(string key, Func<Task<T>> loadFn)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var value = await _cache.GetOrLoadAsync(key, loadFn);
            _stats.IncrementHits();
            _stats.RecordOperationDuration(stopwatch.ElapsedMilliseconds);
            return value;
        }
        catch
        {
            _stats.IncrementErrors();
            throw;
        }
    }
}
```

### Example 4: Using with OpenTelemetry

```csharp
// In your application startup
var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter(CacheStatisticsAggregator.Instance.Meter.Name)
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
    })
    .Build();

// Don't forget to dispose when application shuts down
meterProvider.Dispose();
```

## Notes

- **Thread safety:** All public methods are thread‑safe and can be called concurrently from multiple threads without additional synchronization. The implementation uses atomic operations (e.g., `Interlocked`) to ensure consistency of the counters.
- **Disposal:** After `Dispose()` is called, the instance enters a disposed state. Any subsequent call to `IncrementHits`, `IncrementMisses`, `IncrementErrors`, `GetStatistics`, or `Reset` will throw an `ObjectDisposedException`. The `Dispose` method itself can be called multiple times safely.
- **Reset behavior:** Calling `Reset()` while other threads are concurrently incrementing counters is safe; however, the exact moment at which the counters are zeroed is not atomic with respect to the increments. Some increments that occur during the reset may be lost or may appear in the next snapshot. For most monitoring scenarios this is acceptable.
- **Snapshot consistency:** `GetStatistics()` returns a consistent snapshot of the counters at the time of the call. The returned `CacheStatistics` object is independent of the aggregator and will not change after it is created.
- **Resource management:** The `Dispose` method is provided primarily for scenarios where the aggregator holds external resources (e.g., logging sinks, performance counters). In the simplest in‑memory implementation, disposal may be a no‑op, but callers should still follow the disposable pattern.
- **Metrics export:** The meter is automatically registered with the .NET runtime's metrics system. To export these metrics to monitoring systems, you need to configure an OpenTelemetry exporter in your application.

## Metrics Visualization

The exported metrics can be visualized in various monitoring systems:

### Prometheus + Grafana
```yaml
# Prometheus scrape config
scrape_configs:
  - job_name: 'cache-stats'
    static_configs:
      - targets: ['localhost:9184']
```

### OpenTelemetry Collector
```yaml
receivers:
  otlp:
    protocols:
      grpc:
      http:

processors:
  batch:

exporters:
  prometheus:
    endpoint: "0.0.0.0:8889"

service:
  pipelines:
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus]
```

### Metrics Available for Query
- `rate(cache_hits_total[5m])` - Hits per second
- `rate(cache_misses_total[5m])` - Misses per second  
- `rate(cache_errors_total[5m])` - Errors per second
- `histogram_quantile(0.95, sum(rate(cache_operation_duration_bucket[5m]))` - 95th percentile latency
- `cache_hit_ratio` - Current hit ratio (0-1)