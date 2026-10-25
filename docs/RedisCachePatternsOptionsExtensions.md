# RedisCachePatternsOptionsExtensions

The `RedisCachePatternsOptionsExtensions` class is a static collection of extension methods for `RedisCachePatternsOptions`. It checks whether the options are usable for a connection, returns the stricter of the two timeouts, converts the cache size limit to megabytes, and produces a connection string that is safe to log. The type lives in the `RedisCachePatterns.Configuration` namespace. The methods do not change the options instance.

## API

### IsValid

```csharp
public static bool IsValid(this RedisCachePatternsOptions options)
```

Checks the options that the connection depends on. It returns `true` only when all of these hold:

- `ConnectionString` is not null, empty, or whitespace.
- `DatabaseId` is `0` or greater.
- `ConnectTimeoutMs` is between `100` and `30000` inclusive.
- `SyncTimeoutMs` is between `100` and `30000` inclusive.
- `EvictionPolicy` is not null, empty, or whitespace.

- **options**: The options to check.
- **Returns**: `true` if the configuration is valid; otherwise, `false`.
- **Throws**: `ArgumentNullException` when `options` is null.

The method does not check the upper bound of `DatabaseId`, although the property's `[Range(0, 16)]` attribute allows values only up to 16. It does not check `MaxCacheSizeBytes`, and it does not check that `EvictionPolicy` is a known policy name. Data annotations are not evaluated here.

### GetEffectiveTimeoutMs

```csharp
public static int GetEffectiveTimeoutMs(this RedisCachePatternsOptions options)
```

Returns the smaller of `ConnectTimeoutMs` and `SyncTimeoutMs`. Use it when one timeout value is needed for code that does not distinguish between connect and sync operations.

- **options**: The options to read.
- **Returns**: The effective timeout in milliseconds. With the defaults (both `5000`), the result is `5000`.
- **Throws**: `ArgumentNullException` when `options` is null.

### GetCacheSizeMb

```csharp
public static double GetCacheSizeMb(this RedisCachePatternsOptions options)
```

Converts `MaxCacheSizeBytes` to mebibytes (bytes divided by 1,048,576) and rounds the result to two decimal places, with midpoints rounded away from zero.

- **options**: The options to read.
- **Returns**: The cache size limit in megabytes. With the default of 100 MB (`104857600` bytes), the result is `100`.
- **Throws**: `ArgumentNullException` when `options` is null.

The method does not format anything, so no culture is involved. The value is returned as a `double`.

### GetRedactedConnectionString

```csharp
public static string GetRedactedConnectionString(this RedisCachePatternsOptions options)
```

Returns `ConnectionString` with the value of any `password=` segment replaced by `***`, so the result can be written to logs. The method splits the string on `;`. Each segment that starts with `password=` (compared case-insensitively) and has more than nine characters is replaced by `password=***`. Every other segment is kept as it is, and the segments are joined again with `;`.

- **options**: The options to read.
- **Returns**: The redacted connection string. An empty string if `ConnectionString` is null, empty, or whitespace.
- **Throws**: `ArgumentNullException` when `options` is null.

Because the split uses `;`, the method only redacts `password=` when it is a `;`-separated segment. StackExchange.Redis connection strings usually separate options with `,`, so for `"localhost:6379,password=secret"` the whole string is one segment that does not start with `password=`. The method returns that string unchanged, and the password is not redacted. See the notes below.

## Usage

### Validating before connecting

```csharp
using RedisCachePatterns.Configuration;

var options = new RedisCachePatternsOptions
{
    ConnectionString = "localhost:6379",
    ConnectTimeoutMs = 2000,
    SyncTimeoutMs = 1500
};

if (!options.IsValid())
{
    throw new InvalidOperationException("RedisCachePatterns options are invalid.");
}
```

### Reading the effective timeout and size

```csharp
using RedisCachePatterns.Configuration;

var options = new RedisCachePatternsOptions
{
    ConnectTimeoutMs = 2000,
    SyncTimeoutMs = 1500,
    MaxCacheSizeBytes = 1536 * 1024
};

int timeout = options.GetEffectiveTimeoutMs();  // 1500
double sizeMb = options.GetCacheSizeMb();       // 1.5
```

### Logging the connection string

```csharp
using RedisCachePatterns.Configuration;

var options = new RedisCachePatternsOptions
{
    ConnectionString = "redis-primary:6379;password=secret;ssl=true"
};

logger.LogInformation("Connecting to {Redis}", options.GetRedactedConnectionString());
// Logs: redis-primary:6379;password=***;ssl=true
```

## Notes

*   **Redaction does not cover comma-separated strings:** `GetRedactedConnectionString` splits on `;` only. A string such as `"localhost:6379,password=secret"` is returned unchanged, so the password appears in the log. This was determined by reading the code, not by running it. Until the method handles `,` as well, do not pass its output to logs when the connection string contains a password in comma form. Do not treat it as a safe redaction for all formats.
*   **Validation is narrower than the attributes:** `IsValid` checks the rules listed above by hand. It does not check `DatabaseId` against its upper bound of 16, and it does not check `MaxCacheSizeBytes`. Its result can therefore be `true` for options that fail the data annotations on `RedisCachePatternsOptions`.
*   **No state changes:** None of the methods modify the options object. They are safe to call from any thread, as long as the options instance is not being modified at the same time.
*   **No callers in this repository:** At the time of writing, no code in the library calls these methods. They are public helpers for consumers that work with `RedisCachePatternsOptions` directly.
*   **Unused import:** The source file imports `System.Globalization`, but no member uses it. The remarks on `GetCacheSizeMb` mention invariant culture, but the code does not format a string.
