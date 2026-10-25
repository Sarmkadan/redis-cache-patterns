# CacheConfigurationBuilderExtensions

The `CacheConfigurationBuilderExtensions` class is a static collection of extension methods for `CacheConfigurationBuilder`. It adds convenience overloads for registering cache policies: a human-readable timespan string (for example `"2h 30m"`), individual day/hour/minute/second components, and bulk registration from a sequence of key-expiration pairs. Each method returns the builder instance, so calls can be chained with the rest of the fluent configuration API.

## API

### `AddPolicy(string keyPattern, string timeSpan)`

Adds a policy whose expiration is parsed from a human-readable timespan string. The key pattern is stored as-is; placeholders such as `{0}` are not expanded here.

*   **Parameters:**
    *   `this CacheConfigurationBuilder builder` - The builder to configure.
    *   `keyPattern` - The key pattern with optional placeholders (e.g., `"user:{0}:profile"`).
    *   `timeSpan` - The expiration in human-readable form (e.g., `"2h 30m"`, `"1.5d"`, `"30s"`).
*   **Returns:** The same `CacheConfigurationBuilder` instance.
*   **Throws:**
    *   `ArgumentNullException` if `builder`, `keyPattern`, or `timeSpan` is `null`.
    *   `ArgumentException` if `keyPattern` or `timeSpan` is an empty string.
    *   `FormatException` if any token in `timeSpan` is not a number followed by a supported unit.

### `AddPolicy(string keyPattern, int days = 0, int hours = 0, int minutes = 0, int seconds = 0)`

Adds a policy whose expiration is built from numeric components. The components are summed into a single `TimeSpan`, so `hours: 30` means thirty hours, not "hours past the day count".

*   **Parameters:**
    *   `this CacheConfigurationBuilder builder` - The builder to configure.
    *   `keyPattern` - The key pattern with optional placeholders.
    *   `days` - The expiration in days. Defaults to `0`.
    *   `hours` - Additional hours. Defaults to `0`.
    *   `minutes` - Additional minutes. Defaults to `0`.
    *   `seconds` - Additional seconds. Defaults to `0`.
*   **Returns:** The same `CacheConfigurationBuilder` instance.
*   **Throws:**
    *   `ArgumentNullException` if `builder` or `keyPattern` is `null`.
    *   `ArgumentException` if `keyPattern` is an empty string.
    *   The out-of-range exception of the `TimeSpan` constructor if the summed components exceed the `TimeSpan` range.

### `AddPolicies(IEnumerable<(string KeyPattern, TimeSpan Expiration)> policies)`

Registers several policies in one call. Each pair is forwarded, in order, to the builder's `AddPolicy(string, TimeSpan)` method.

*   **Parameters:**
    *   `this CacheConfigurationBuilder builder` - The builder to configure.
    *   `policies` - A sequence of key patterns and their expiration `TimeSpan` values.
*   **Returns:** The same `CacheConfigurationBuilder` instance.
*   **Throws:**
    *   `ArgumentNullException` if `builder` or `policies` is `null`.

### `EnableCompression(int thresholdBytes)`

Enables compression for cache entries at or above the given size in bytes.

*   **Parameters:**
    *   `this CacheConfigurationBuilder builder` - The builder to configure.
    *   `thresholdBytes` - The compression threshold in bytes.
*   **Returns:** The same `CacheConfigurationBuilder` instance.
*   **Throws:**
    *   `ArgumentNullException` if `builder` is `null`.
    *   `ArgumentOutOfRangeException` if `thresholdBytes` is negative (see the note below; this check is not reached through normal instance-call syntax).

## Usage

### Human-Readable Expiration
```csharp
using RedisCachePatterns.Configuration;

var builder = new CacheConfigurationBuilder()
    .AddPolicy("product:{0}", "2h 30m")
    .AddPolicy("product:{0}:inventory", "90s")
    .AddPolicy("session:{0}", "1.5d");

var options = builder.Build();
```

### Component-Based Expiration
```csharp
using RedisCachePatterns.Configuration;

var builder = new CacheConfigurationBuilder()
    .AddPolicy("order:{0}:summary", days: 1)
    .AddPolicy("user:{0}:profile", minutes: 15, seconds: 30)
    .EnableCompression(2048);

var options = builder.Build();
```

### Bulk Registration
```csharp
using RedisCachePatterns.Configuration;

var builder = new CacheConfigurationBuilder();

builder.AddPolicies(new[]
{
    ("user:{0}:profile", TimeSpan.FromMinutes(30)),
    ("product:{0}", TimeSpan.FromHours(2)),
    ("category:all", TimeSpan.FromDays(1))
});

var options = builder.Build();
```

## Notes

*   **Timespan string format:** Tokens are separated by spaces. Each token is a number followed by one unit suffix, matched case-insensitively: `d` (days), `h` (hours), `m` (minutes), `s` (seconds). Numbers use the invariant culture, so the decimal separator is `.`. Repeated units are summed (`"1h 1h"` is two hours). Tokens such as `"1d2h"` (no space) or a bare number such as `"30"` are rejected with a `FormatException`. There is no millisecond unit, and `m` always means minutes.
*   **Input not fully validated:** The string overload rejects only null, empty, and malformed input. A whitespace-only string is not rejected and yields `TimeSpan.Zero`. Negative values (e.g., `"-1h"`, or `days: -1`) and zero are accepted and passed through to the builder without checks. Validate durations before calling these methods if they come from untrusted input.
*   **`EnableCompression` validation is effectively unreachable:** The builder class already declares an instance `EnableCompression(int thresholdBytes = 1024)` method. C# prefers instance methods over extension methods, so `builder.EnableCompression(-1)` binds to the instance method, which does not validate the value. The `ArgumentOutOfRangeException` check only runs if the extension is called statically, as `CacheConfigurationBuilderExtensions.EnableCompression(builder, -1)`.
*   **Partial application in `AddPolicies`:** Items are added one at a time. If enumerating `policies` throws partway through, the items already added remain in the builder. Individual `KeyPattern` values are not null-checked, unlike the single-policy overloads.
*   **Thread safety:** The extension methods are stateless, but they mutate the `CacheConfigurationBuilder` they receive, and that class is not thread-safe. Configure a builder from a single thread, or synchronize access.
*   **Overload resolution:** A string second argument selects the `string timeSpan` overload. Named or positional `int` arguments select the component overload. Passing `TimeSpan` values selects the builder's own `AddPolicy(string, TimeSpan)` method directly.
