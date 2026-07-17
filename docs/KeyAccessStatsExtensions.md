# KeyAccessStatsExtensions

Provides extension methods for analyzing key access statistics, enabling determination of access patterns, efficiency, and eviction decisions. All methods operate on an instance of `KeyAccessStats` (or a compatible type) and are designed to be used in cache monitoring and policy evaluation workflows.

## API

### `GetMisses`

```csharp
public static long GetMisses(this KeyAccessStats stats)
```

Returns the total number of cache misses recorded for the key.

- **Parameters**  
  `stats` – The key access statistics instance. Must not be `null`.
- **Returns**  
  `long` – The miss count.
- **Exceptions**  
  `ArgumentNullException` – Thrown if `stats` is `null`.

### `GetHits`

```csharp
public static long GetHits(this KeyAccessStats stats)
```

Returns the total number of cache hits recorded for the key.

- **Parameters**  
  `stats` – The key access statistics instance. Must not be `null`.
- **Returns**  
  `long` – The hit count.
- **Exceptions**  
  `ArgumentNullException` – Thrown if `stats` is `null`.

### `IsHotKey`

```csharp
public static bool IsHotKey(this KeyAccessStats stats)
```

Determines whether the key is considered a hot key based on its access frequency and recency.

- **Parameters**  
  `stats` – The key access statistics instance. Must not be `null`.
- **Returns**  
  `bool` – `true` if the key is hot; otherwise `false`.
- **Exceptions**  
  `ArgumentNullException` – Thrown if `stats` is `null`.

### `IsColdKey`

```csharp
public static bool IsColdKey(this KeyAccessStats stats)
```

Determines whether the key is considered a cold key, typically indicating infrequent or stale access.

- **Parameters**  
  `stats` – The key access statistics instance. Must not be `null`.
- **Returns**  
  `bool` – `true` if the key is cold; otherwise `false`.
- **Exceptions**  
  `ArgumentNullException` – Thrown if `stats` is `null`.

### `HasPoorEfficiency`

```csharp
public static bool HasPoorEfficiency(this KeyAccessStats stats)
```

Evaluates whether the key’s hit-to-miss ratio falls below an acceptable threshold, indicating poor cache efficiency.

- **Parameters**  
  `stats` – The key access statistics instance. Must not be `null`.
- **Returns**  
  `bool` – `true` if efficiency is poor; otherwise `false`.
- **Exceptions**  
  `ArgumentNullException` – Thrown if `stats` is `null`.

### `GetAge`

```csharp
public static TimeSpan GetAge(this KeyAccessStats stats)
```

Returns the age of the key, typically measured from the time it was first cached or last reset.

- **Parameters**  
  `stats` – The key access statistics instance. Must not be `null`.
- **Returns**  
  `TimeSpan` – The age duration.
- **Exceptions**  
  `ArgumentNullException` – Thrown if `stats` is `null`.

### `ToMachineString`

```csharp
public static string ToMachineString(this KeyAccessStats stats)
```

Produces a compact, machine-readable string representation of the statistics, suitable for logging or serialization.

- **Parameters**  
  `stats` – The key access statistics instance. Must not be `null`.
- **Returns**  
  `string` – A formatted string containing key metrics.
- **Exceptions**  
  `ArgumentNullException` – Thrown if `stats` is `null`.

### `ShouldEvict`

```csharp
public static bool ShouldEvict(this KeyAccessStats stats)
```

Determines whether the key should be evicted from the cache based on its access pattern, age, and efficiency.

- **Parameters**  
  `stats` – The key access statistics instance. Must not be `null`.
- **Returns**  
  `bool` – `true` if eviction is recommended; otherwise `false`.
- **Exceptions**  
  `ArgumentNullException` – Thrown if `stats` is `null`.

### `ToSummaryString`

```csharp
public static string ToSummaryString(this KeyAccessStats stats)
```

Produces a human-readable summary of the key’s access statistics, including hits, misses, age, and derived flags.

- **Parameters**  
  `stats` – The key access statistics instance. Must not be `null`.
- **Returns**  
  `string` – A formatted summary string.
- **Exceptions**  
  `ArgumentNullException` – Thrown if `stats` is `null`.

## Usage

### Example 1: Checking hot key and deciding eviction

```csharp
using RedisCachePatterns;

var stats = cache.GetKeyStats("user:1234");

if (stats.IsHotKey())
{
    Console.WriteLine("Key is hot – keep in cache.");
}
else if (stats.ShouldEvict())
{
    Console.WriteLine("Key is a candidate for eviction.");
    cache.Remove("user:1234");
}
```

### Example 2: Logging a summary for monitoring

```csharp
using RedisCachePatterns;

var stats = cache.GetKeyStats("session:abc");

if (stats.HasPoorEfficiency())
{
    var summary = stats.ToSummaryString();
    logger.Warn("Poor cache efficiency for key: {Summary}", summary);
}
else
{
    var machine = stats.ToMachineString();
    logger.Info("Key stats: {Machine}", machine);
}
```

## Notes

- All methods throw `ArgumentNullException` if the `stats` parameter is `null`. Always ensure the statistics object is not null before calling these extensions.
- The classification thresholds for hot/cold keys and poor efficiency are implementation-defined and may vary based on configuration or internal heuristics.
- `GetAge` returns `TimeSpan.Zero` if the key has no recorded creation time or if the statistics are empty.
- These methods are thread-safe as they do not modify the underlying statistics object. However, if the `KeyAccessStats` instance is mutable and being updated concurrently, the caller should synchronize access or use a snapshot to avoid inconsistent reads.
