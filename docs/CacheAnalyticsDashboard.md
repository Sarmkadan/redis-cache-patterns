# Cache Analytics Dashboard

The types in `Monitoring/CacheAnalyticsDashboard.cs` provide an in-memory, thread-safe view of cache usage. `CacheAnalyticsDashboard` records hits and misses by key and key prefix, creates analytics snapshots, renders text reports, and resets the collected data. The file also defines the `KeyAccessStats`, `PrefixAccessStats`, `AnalyticsSnapshot`, and `AnalyticsDashboardResponse` models used to expose those results.

## Class: `CacheAnalyticsDashboard`

Tracks cache access activity from the most recent reset. The dashboard does not read from Redis directly; callers must invoke `RecordHit` or `RecordMiss` when cache operations complete.

### Constructor

```csharp
CacheAnalyticsDashboard(
    ILogger<CacheAnalyticsDashboard> logger,
    int topNHotKeys = 10,
    double lowHitRateThreshold = 0.3,
    TimeSpan? coldKeyAge = null)
```

| Parameter | Description |
|-----------|-------------|
| `logger` | Logger used for report-rendered and reset messages. Cannot be `null`. |
| `topNHotKeys` | Maximum number of entries returned in each of the hot, cold, and low-hit-rate key lists. Defaults to `10`. |
| `lowHitRateThreshold` | Exclusive upper hit-rate boundary for low-hit-rate keys. Defaults to `0.3` (30%). |
| `coldKeyAge` | Time since the last access after which a key is cold. Defaults to one hour. |

**Throws:** `ArgumentNullException` when `logger` is `null`.

### Methods

#### `void RecordHit(string key)`

Records a successful cache lookup. It increments the key, prefix, and overall hit counters and updates the key's `LastAccessedAt` timestamp.

Whitespace-only, empty, and `null` keys are ignored. A key's prefix is the portion before its final colon. For example, `product:detail:42` is grouped under `product:detail`; a key without a colon is its own prefix.

#### `void RecordMiss(string key)`

Records an unsuccessful cache lookup. It increments the key, prefix, and overall miss counters and updates the key's `LastAccessedAt` timestamp.

Invalid keys are ignored, and prefix extraction follows the same rules as `RecordHit`.

#### `KeyAccessStats? GetKeyStats(string key)`

Returns the live statistics object for `key`, or `null` when that key has not been recorded since the last reset.

#### `AnalyticsSnapshot GetSnapshot()`

Creates a point-in-time analytics view containing:

- Overall hit and miss totals and the overall hit rate.
- The number of distinct tracked keys.
- Prefix statistics ordered by total accesses, descending.
- Hot keys ordered by total accesses, descending.
- Cold keys whose `LastAccessedAt` is older than the configured age, ordered oldest first.
- Low-hit-rate keys with at least five accesses and a hit rate below the configured threshold, ordered by hit rate ascending.

The hot, cold, and low-hit-rate lists are each limited by `topNHotKeys`.

#### `string RenderReport()`

Builds a multi-line text report suitable for console output or logging. The report always contains its title, generation and tracking timestamps, and an overview. Prefix, hot-key, low-hit-rate, and cold-key sections are included only when they contain entries.

Rendering a report also writes a debug log containing the unique-key count and overall hit rate.

#### `void Reset()`

Clears all key and prefix entries, atomically resets overall hits and misses to zero, updates the tracking start time, and writes an information log entry.

## Data Models

### `KeyAccessStats`

Represents activity for one cache key.

| Property | Type | Description |
|----------|------|-------------|
| `Key` | `string` | Tracked cache key. Set when the entry is created. |
| `Hits` | `long` | Atomically read and written hit count. |
| `Misses` | `long` | Atomically read and written miss count. |
| `LastAccessedAt` | `DateTime` | UTC time of the most recent recorded hit or miss. |
| `FirstSeenAt` | `DateTime` | UTC time when the statistics entry was created. |
| `HitRate` | `double` | `Hits / (Hits + Misses)`, or `0` when there are no accesses. |
| `TotalAccesses` | `long` | Sum of hits and misses. |

### `PrefixAccessStats`

Aggregates activity for keys sharing a derived prefix.

| Property | Type | Description |
|----------|------|-------------|
| `Prefix` | `string` | Derived key prefix. |
| `Hits` | `long` | Hit count for the prefix. |
| `Misses` | `long` | Miss count for the prefix. |
| `HitRate` | `double` | Prefix hit rate, or `0` when there are no accesses. |
| `TotalAccesses` | `long` | Sum of prefix hits and misses. |

### `AnalyticsSnapshot`

Represents the data produced by `GetSnapshot`.

| Property | Type | Description |
|----------|------|-------------|
| `CapturedAt` | `DateTime` | UTC time at which the snapshot was created. |
| `OverallHitRate` | `double` | Overall ratio from `0` to `1`; `0` when there are no accesses. |
| `TotalHits` | `long` | Hits recorded since the last reset. |
| `TotalMisses` | `long` | Misses recorded since the last reset. |
| `UniqueKeysTracked` | `int` | Number of distinct keys recorded since the last reset. |
| `PrefixStats` | `IReadOnlyList<PrefixAccessStats>` | Prefix aggregates ordered by total accesses. |
| `HotKeys` | `IReadOnlyList<KeyAccessStats>` | Most-accessed keys. |
| `ColdKeys` | `IReadOnlyList<KeyAccessStats>` | Keys older than the configured cold-key age. |
| `LowHitRateKeys` | `IReadOnlyList<KeyAccessStats>` | Frequently accessed keys below the configured hit-rate threshold. |

### `AnalyticsDashboardResponse`

Provides an API response shape corresponding to `AnalyticsSnapshot`. It exposes the snapshot timestamp, totals, hit rate, unique-key count, prefix statistics, and classified key lists. Its nullable `TextReport` property can also carry a pre-rendered dashboard report.

## Usage Example

```csharp
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Monitoring;

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<CacheAnalyticsDashboard>();

var dashboard = new CacheAnalyticsDashboard(
    logger,
    topNHotKeys: 5,
    lowHitRateThreshold: 0.25,
    coldKeyAge: TimeSpan.FromMinutes(30));

dashboard.RecordHit("product:detail:42");
dashboard.RecordHit("product:detail:42");
dashboard.RecordMiss("product:detail:99");

var keyStats = dashboard.GetKeyStats("product:detail:42");
Console.WriteLine($"Key hit rate: {keyStats?.HitRate:P0}");

var snapshot = dashboard.GetSnapshot();
Console.WriteLine($"Overall hit rate: {snapshot.OverallHitRate:P1}");
Console.WriteLine($"Tracked keys: {snapshot.UniqueKeysTracked}");
Console.WriteLine(dashboard.RenderReport());

dashboard.Reset();
```

## Notes

- **Thread safety:** Concurrent dictionaries and atomic counter operations allow the public methods to be called from multiple threads. A snapshot assembled during concurrent recording or reset is a best-effort view rather than a transactionally consistent capture of every field.
- **Live key objects:** `GetKeyStats` and snapshot key lists contain the tracked `KeyAccessStats` instances. Their counters and `LastAccessedAt` values can change after retrieval as new accesses are recorded.
- **In-memory lifetime:** Statistics exist only for the lifetime of the dashboard instance and are discarded by `Reset`; the class does not persist or query analytics data in Redis.
- **Classification:** A key must have at least five accesses before it can appear in `LowHitRateKeys`. Cold-key classification is based only on elapsed time since `LastAccessedAt`, not access frequency.
- **Prefix grouping:** Prefixes use the final colon as the separator, so `tenant:product:42` is grouped as `tenant:product`.
