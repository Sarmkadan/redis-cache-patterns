# KeyAccessStats

`KeyAccessStats` tracks access patterns for individual cache keys and provides aggregated analytics across all monitored keys. It records hits, misses, and temporal metadata to compute hit rates, identify hot/cold keys, and generate diagnostic reports. The type also exposes a `CacheAnalyticsDashboard` for broader cache-level observability.

## API

### Properties

- **`string Key`**  
  The cache key this instance monitors. Read-only after construction.

- **`DateTime LastAccessedAt`**  
  The most recent timestamp when `RecordHit` or `RecordMiss` was called for this key.

- **`DateTime FirstSeenAt`**  
  The timestamp of the first recorded access (hit or miss) for this key.

- **`DateTime CapturedAt`**  
  The moment the current analytics snapshot was taken. Updated when `GetSnapshot` is called.

- **`double OverallHitRate`**  
  The ratio of `TotalHits` to total accesses (`TotalHits + TotalMisses`). Returns `0` if no accesses have been recorded.

- **`long TotalHits`**  
  Cumulative count of successful cache retrievals for this key.

- **`long TotalMisses`**  
  Cumulative count of failed cache retrievals (key not present or expired) for this key.

- **`int UniqueKeysTracked`**  
  The total number of distinct keys currently being monitored by the parent analytics system.

- **`IReadOnlyList<KeyAccessStats> HotKeys`**  
  A snapshot of keys with the highest access frequencies, ordered descending by hit count. The list is immutable and reflects the state at the last `GetSnapshot` call.

- **`IReadOnlyList<KeyAccessStats> ColdKeys`**  
  A snapshot of keys with the lowest access frequencies, ordered ascending by hit count. The list is immutable and reflects the state at the last `GetSnapshot` call.

- **`IReadOnlyList<KeyAccessStats> LowHitRateKeys`**  
  A snapshot of keys whose hit rate falls below a configurable threshold. The list is immutable and reflects the state at the last `GetSnapshot` call.

- **`CacheAnalyticsDashboard`**  
  Provides access to the broader dashboard aggregating metrics across all tracked keys. The exact type and members are defined by the `CacheAnalyticsDashboard` class.

### Methods

- **`void RecordHit()`**  
  Increments `TotalHits` and updates `LastAccessedAt` to the current UTC time. If this is the first access ever, also sets `FirstSeenAt`.

- **`void RecordMiss()`**  
  Increments `TotalMisses` and updates `LastAccessedAt` to the current UTC time. If this is the first access ever, also sets `FirstSeenAt`.

- **`KeyAccessStats? GetKeyStats(string key)`**  
  Retrieves the `KeyAccessStats` instance for the specified key, or `null` if the key is not tracked.  
  *Parameters:* `key` — the cache key to look up.  
  *Returns:* the corresponding stats object, or `null`.

- **`AnalyticsSnapshot GetSnapshot()`**  
  Captures a point-in-time view of all tracked metrics, updates `CapturedAt`, and returns an `AnalyticsSnapshot` containing hit rates, hot/cold key lists, and aggregate counters.  
  *Returns:* a populated `AnalyticsSnapshot` instance.

- **`string RenderReport()`**  
  Generates a human-readable formatted report of current analytics, including overall hit rate, top hot keys, cold keys, and low-hit-rate keys.  
  *Returns:* a multi-line string suitable for logging or console output.

- **`void Reset()`**  
  Resets all counters (`TotalHits`, `TotalMisses`) and timestamps (`LastAccessedAt`, `FirstSeenAt`) to their default values. Does not remove the key from the tracking set.

## Usage

### Example 1: Recording Accesses and Inspecting a Single Key

```csharp
var stats = new KeyAccessStats("user:session:42");

// Simulate cache behavior
stats.RecordHit();   // cache returned value
stats.RecordHit();
stats.RecordMiss();  // cache expired

Console.WriteLine($"Key: {stats.Key}");
Console.WriteLine($"Hit rate: {stats.OverallHitRate:P}");
Console.WriteLine($"First seen: {stats.FirstSeenAt:O}");
Console.WriteLine($"Last accessed: {stats.LastAccessedAt:O}");
```

### Example 2: Generating a Dashboard Report Across Multiple Keys

```csharp
var dashboard = new CacheAnalyticsDashboard();
dashboard.TrackKey("product:detail:100");
dashboard.TrackKey("product:detail:200");
dashboard.TrackKey("user:profile:7");

// Simulate mixed access patterns
dashboard.GetKeyStats("product:detail:100")?.RecordHit();
dashboard.GetKeyStats("product:detail:100")?.RecordHit();
dashboard.GetKeyStats("product:detail:200")?.RecordMiss();
dashboard.GetKeyStats("user:profile:7")?.RecordHit();

var snapshot = dashboard.GetSnapshot();
Console.WriteLine(snapshot.RenderReport());

// Inspect hot keys
foreach (var hot in snapshot.HotKeys)
{
    Console.WriteLine($"Hot key: {hot.Key} ({hot.TotalHits} hits)");
}
```

## Notes

- **Thread Safety:** `RecordHit`, `RecordMiss`, and `Reset` mutate internal state. The public API does not guarantee thread safety; concurrent callers must synchronize externally if multiple threads access the same `KeyAccessStats` instance.
- **Snapshot Consistency:** `GetSnapshot` captures a point-in-time view. Properties like `HotKeys`, `ColdKeys`, and `LowHitRateKeys` reflect the state at the moment of the last snapshot and do not update automatically between snapshots.
- **`OverallHitRate` Edge Case:** When `TotalHits + TotalMisses` equals zero, `OverallHitRate` returns `0` rather than `NaN` or throwing.
- **`GetKeyStats` Nullability:** Always check for `null` before operating on the returned stats, as the requested key may not be registered in the tracking system.
- **`Reset` Semantics:** `Reset` clears counters and timestamps but does not remove the key from the set of tracked keys; `UniqueKeysTracked` remains unchanged.
- **`CapturedAt` Updates:** This property is set to the current UTC time each time `GetSnapshot` is invoked, not continuously.
