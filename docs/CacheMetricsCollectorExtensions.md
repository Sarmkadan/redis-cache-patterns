# CacheMetricsCollectorExtensions

Provides extension methods for `CacheMetricsCollector` that add a simplified operation-recording entry point, a formatted metrics report, a hit-rate accessor, and a high-error-rate check. These helpers sit on top of the collector's public API and do not change its state beyond what the underlying calls already do.

## API

### `public static void RecordOperation(this CacheMetricsCollector collector, string operationType, string key, long elapsedMs, bool success)`

**Purpose**  
Records a cache operation in a single call. A successful `"Get"` operation (case-insensitive) is recorded as a hit with its latency. A failed operation of any type is recorded as an error. Any other combination (for example, a successful `"Set"`) is accepted but not recorded.

**Parameters**  
- `collector`: The `CacheMetricsCollector` to record into. Must not be `null`.  
- `operationType`: The operation name (e.g., `"Get"`, `"Set"`, `"Delete"`). Must not be `null` or empty.  
- `key`: The cache key involved in the operation. Must not be `null`.  
- `elapsedMs`: Elapsed time of the operation in milliseconds. Only used for successful `"Get"` operations.  
- `success`: Whether the operation succeeded.

**Return value**  
None (`void`).

**Exceptions**  
- `ArgumentNullException` – Thrown when `collector` or `key` is `null`.  
- `ArgumentException` – Thrown when `operationType` is `null` or empty.

---

### `public static string GetFormattedMetrics(this CacheMetricsCollector collector)`

**Purpose**  
Builds a human-readable, multi-line text report from the collector's current metrics. The report lists hits, misses, hit rate, average hit and miss latency, evictions, errors, and uptime. It ends with a status line derived from the hit rate.

**Parameters**  
- `collector`: The `CacheMetricsCollector` to report on. Must not be `null`.

**Return value**  
A string containing the report. Its status line is:
- `✅ Healthy` when the hit rate is 95% or higher,
- `⚠️ Warning` when the hit rate is 80% or higher but below 95%,
- `❌ Critical` otherwise.

**Exceptions**  
- `ArgumentNullException` – Thrown when `collector` is `null`.

---

### `public static double GetHitRatePercentage(this CacheMetricsCollector collector)`

**Purpose**  
Returns the current cache hit rate, taken from `GetMetrics().HitRate`.

**Parameters**  
- `collector`: The `CacheMetricsCollector` to read from. Must not be `null`.

**Return value**  
The hit rate as a percentage in the range 0–100. Returns `0` when no hits or misses have been recorded.

**Exceptions**  
- `ArgumentNullException` – Thrown when `collector` is `null`.

---

### `public static bool HasHighErrorRate(this CacheMetricsCollector collector, double threshold = 5.0)`

**Purpose**  
Checks whether the error count exceeds a percentage threshold relative to the total number of hits and misses.

**Parameters**  
- `collector`: The `CacheMetricsCollector` to inspect. Must not be `null`.  
- `threshold`: The error-rate threshold as a percentage (0–100). Defaults to `5.0`.

**Return value**  
`true` if `Errors / (TotalHits + TotalMisses) * 100` is greater than `threshold`; otherwise `false`. Returns `false` when there are no hits or misses yet.

**Exceptions**  
- `ArgumentNullException` – Thrown when `collector` is `null`.  
- `ArgumentOutOfRangeException` – Thrown when `threshold` is less than `0` or greater than `100`.

---

## Usage

### Example 1: Recording operations from a cache wrapper

```csharp
var stopwatch = Stopwatch.StartNew();
try
{
    var value = await cache.GetAsync<Product>(key);
    stopwatch.Stop();
    collector.RecordOperation("Get", key, stopwatch.ElapsedMilliseconds, success: true);
    return value;
}
catch (CacheException)
{
    stopwatch.Stop();
    collector.RecordOperation("Get", key, stopwatch.ElapsedMilliseconds, success: false);
    throw;
}
```

### Example 2: Periodic health reporting

```csharp
if (collector.HasHighErrorRate(threshold: 2.0))
{
    logger.LogWarning("Cache error rate is above 2%:\n{Report}", collector.GetFormattedMetrics());
}
else
{
    logger.LogInformation("Cache hit rate: {HitRate:F2}%", collector.GetHitRatePercentage());
}
```

## Notes

- `RecordOperation` never records misses. A successful `"Get"` is always counted as a hit, so `GetFormattedMetrics` and `GetHitRatePercentage` only reflect misses if they are recorded through the collector's own `RecordMiss` method.
- `RecordOperation` ignores successful operations other than `"Get"`. Their latency is not stored, and they do not appear in any metric.
- A failed operation is recorded as an error regardless of its type. `RecordError` also writes a warning to the collector's logger for each error.
- The hit rate is expressed as a percentage (0–100) by `GetHitRatePercentage` and in the `HitRate` value of `CacheMetrics`. It is not a 0–1 fraction.
- `HasHighErrorRate` divides errors by hits and misses only; errors are not added to the denominator. An error rate can therefore exceed 100% when errors outnumber successful lookups.
- All methods read or write the collector's thread-safe internal state. They can be called concurrently, but the values in a report are a point-in-time snapshot and may not match each other exactly under concurrent updates.
