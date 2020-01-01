# CacheMetricsCollector

`CacheMetricsCollector` is a utility class designed to track and report cache operation metrics such as hits, misses, evictions, and errors. It provides real-time visibility into cache performance and can be used to monitor cache efficiency, diagnose issues, and optimize cache configurations.

## API

### `CacheMetricsCollector()`
Constructs a new instance of `CacheMetricsCollector` with all metrics initialized to zero or default values.

### `void RecordHit()`
Records a cache hit event. Increments the hit counter and updates the last occurrence timestamp.

### `void RecordMiss()`
Records a cache miss event. Increments the miss counter and updates the last occurrence timestamp.

### `void RecordEviction()`
Records an eviction event. Increments the eviction counter and updates the last occurrence timestamp.

### `void RecordError()`
Records an error event. Increments the error counter and updates the last occurrence timestamp.

### `CacheMetrics GetMetrics()`
Returns a snapshot of the current metrics as a `CacheMetrics` object. The returned object is a read-only snapshot and will not reflect subsequent changes.

**Returns:** `CacheMetrics` containing the current state of all tracked metrics.

### `void Reset()`
Resets all metrics to their initial zero values. This includes counters, timestamps, and latency measurements.

### `long Count`
Gets the total number of recorded events (hits, misses, evictions, errors).

**Returns:** The total number of recorded events.

### `long TotalMs`
Gets the total elapsed time in milliseconds since the collector was created or last reset.

**Returns:** The total elapsed time in milliseconds.

### `DateTime LastOccurrence`
Gets the timestamp of the most recent recorded event.

**Returns:** The `DateTime` of the last recorded event.

### `long TotalHits`
Gets the total number of cache hits recorded.

**Returns:** The total number of cache hits.

### `long TotalMisses`
Gets the total number of cache misses recorded.

**Returns:** The total number of cache misses.

### `double HitRate`
Calculates the cache hit rate as a value between 0.0 and 1.0.

**Returns:** The hit rate, or 0.0 if no events have been recorded.

### `long Evictions`
Gets the total number of cache evictions recorded.

**Returns:** The total number of evictions.

### `long Errors`
Gets the total number of errors recorded.

**Returns:** The total number of errors.

### `long AverageHitLatencyMs`
Gets the average latency in milliseconds for cache hits. Returns 0 if no hits have been recorded.

**Returns:** The average hit latency in milliseconds.

### `long AverageMissLatencyMs`
Gets the average latency in milliseconds for cache misses. Returns 0 if no misses have been recorded.

**Returns:** The average miss latency in milliseconds.

### `double UptimeSeconds`
Gets the total uptime in seconds since the collector was created or last reset.

**Returns:** The uptime in seconds.

### `override string ToString()`
Returns a human-readable string representation of the current metrics, including hit rate, uptime, and event counts.

**Returns:** A formatted string containing key metrics.

## Usage

### Basic Usage Example
