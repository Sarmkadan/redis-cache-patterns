# DateTimeHelper

`DateTimeHelper` provides a centralized set of utility methods for parsing, formatting, and performing time‑based calculations commonly required in caching scenarios. It handles flexible date‑time input, ISO 8601 output, relative time descriptions, day‑boundary computation, and expiration logic, reducing duplication across the `redis-cache-patterns` codebase.

## API

### `public static bool TryParseFlexible`
Attempts to parse a string representation of a date and time using a range of common formats, without requiring a single rigid pattern.

- **Parameters**  
  `string input` – The raw date‑time string to parse.  
  `out DateTime result` – When the method returns `true`, contains the parsed `DateTime` value; otherwise `DateTime.MinValue`.

- **Return value**  
  `bool` – `true` if parsing succeeded; `false` if the input was null, empty, or did not match any supported format.

- **Exceptions**  
  None. All failures are communicated through the return value.

---

### `public static string FormatIso8601`
Converts a `DateTime` value to its ISO 8601 round‑trip string representation.

- **Parameters**  
  `DateTime dateTime` – The value to format. `DateTimeKind` is preserved in the output.

- **Return value**  
  `string` – The ISO 8601 representation (e.g. `"2025-03-15T09:30:00.0000000Z"` or `"2025-03-15T09:30:00.0000000+01:00"`).

- **Exceptions**  
  None.

---

### `public static string GetRelativeTime`
Produces a human‑readable relative time string comparing a given timestamp to the current UTC time.

- **Parameters**  
  `DateTime dateTime` – The timestamp to describe. Typically converted to UTC before comparison.

- **Return value**  
  `string` – A relative description such as *“just now”*, *“5 minutes ago”*, *“2 hours ago”*, *“yesterday”*, or *“3 days ago”*.

- **Exceptions**  
  None. Future dates produce a forward‑looking description (e.g. *“in 10 minutes”*).

---

### `public static DateTime GetDayStart`
Returns the instant representing the start of the calendar day for the given date.

- **Parameters**  
  `DateTime dateTime` – Any value whose date component is used.

- **Return value**  
  `DateTime` – A new `DateTime` with the same date but the time set to `00:00:00.0000000` and the same `DateTimeKind` as the input.

- **Exceptions**  
  None.

---

### `public static DateTime GetDayEnd`
Returns the instant representing the end of the calendar day for the given date.

- **Parameters**  
  `DateTime dateTime` – Any value whose date component is used.

- **Return value**  
  `DateTime` – A new `DateTime` with the same date but the time set to `23:59:59.9999999` and the same `DateTimeKind` as the input.

- **Exceptions**  
  None.

---

### `public static DateTime CalculateExpiration`
Computes an absolute expiration timestamp by adding a `TimeSpan` to a base time, with a configurable minimum allowed expiration.

- **Parameters**  
  `DateTime baseTime` – The starting timestamp (often `DateTime.UtcNow`).  
  `TimeSpan duration` – The desired time‑to‑live.  
  `TimeSpan? minimum` – An optional floor; when provided, the result is never earlier than `baseTime + minimum`.

- **Return value**  
  `DateTime` – The calculated expiration timestamp. If `minimum` is supplied and `duration` is smaller, `baseTime + minimum` is returned.

- **Exceptions**  
  `ArgumentOutOfRangeException` – Thrown when `duration` is negative.

---

### `public static bool IsExpired`
Determines whether a given expiration timestamp has already passed relative to a reference time (defaulting to `DateTime.UtcNow`).

- **Parameters**  
  `DateTime expiration` – The absolute expiration timestamp.  
  `DateTime? referenceTime` – The time to compare against; `null` means current UTC time.

- **Return value**  
  `bool` – `true` if `expiration <= referenceTime`; otherwise `false`.

- **Exceptions**  
  None.

---

### `public static TimeSpan? GetTimeRemaining`
Calculates the remaining time until an expiration timestamp, or returns `null` if already expired.

- **Parameters**  
  `DateTime expiration` – The absolute expiration timestamp.  
  `DateTime? referenceTime` – The time to compare against; `null` means current UTC time.

- **Return value**  
  `TimeSpan?` – The positive duration remaining, or `null` when `expiration <= referenceTime`.

- **Exceptions**  
  None.

---

## Usage

### Example 1: Setting a cache entry with a minimum TTL
```csharp
string userInput = "2025-06-15 14:30";
if (DateTimeHelper.TryParseFlexible(userInput, out DateTime parsed))
{
    DateTime baseTime = DateTime.UtcNow;
    TimeSpan requestedTtl = TimeSpan.FromMinutes(5);
    TimeSpan minimumTtl = TimeSpan.FromMinutes(1);

    DateTime expiresAt = DateTimeHelper.CalculateExpiration(baseTime, requestedTtl, minimumTtl);
    string isoExpiration = DateTimeHelper.FormatIso8601(expiresAt);

    // Store in Redis with expiration metadata
    cache.Set("session:123", data, expiresAt);
    Console.WriteLine($"Entry expires at {isoExpiration}");
}
```

### Example 2: Checking cache freshness and displaying relative time
```csharp
DateTime cachedExpiration = cache.GetExpiration("session:123");

if (DateTimeHelper.IsExpired(cachedExpiration))
{
    Console.WriteLine("Cache entry has expired.");
}
else
{
    TimeSpan? remaining = DateTimeHelper.GetTimeRemaining(cachedExpiration);
    string relative = DateTimeHelper.GetRelativeTime(cachedExpiration);
    
    Console.WriteLine($"Expires {relative} ({remaining:hh\\:mm\\:ss} remaining).");
    
    // Refresh if within a grace period
    if (remaining.HasValue && remaining.Value.TotalSeconds < 30)
    {
        cache.Refresh("session:123");
    }
}
```

---

## Notes

- **Edge cases in `TryParseFlexible`**  
  The method does not throw for null or malformed strings; it returns `false` and sets the output to `DateTime.MinValue`. Callers must always check the return value before using the parsed result.

- **`DateTimeKind` awareness**  
  `FormatIso8601`, `GetDayStart`, and `GetDayEnd` preserve the `DateTimeKind` of the input. When comparing timestamps with `IsExpired` or `GetTimeRemaining`, ensure both the expiration and reference time share the same kind (preferably UTC) to avoid offset mismatches.

- **`CalculateExpiration` minimum enforcement**  
  When a `minimum` is provided, it acts as a floor, not a ceiling. If `duration` is larger than `minimum`, the full `duration` is used. Negative `duration` throws `ArgumentOutOfRangeException`; a negative `minimum` is treated as an earlier floor and may produce results earlier than `baseTime`.

- **`GetRelativeTime` granularity**  
  The output is an approximation intended for display (e.g., “3 hours ago”). It is not suitable for precise duration calculations—use `GetTimeRemaining` for programmatic decisions.

- **Thread safety**  
  All members are static and operate on immutable `DateTime` and `TimeSpan` values without shared mutable state. They are safe to call concurrently from multiple threads.
