# DiagnosticsProvider

The `DiagnosticsProvider` class captures and exposes diagnostic information about the application, the underlying system, and the Redis cache infrastructure used by the `redis-cache-patterns` library. It provides methods to generate a structured `DiagnosticReport` object or an HTML-formatted string representation of the same data. After a report is generated, the provider’s properties are populated with the latest snapshot of diagnostics, including any warnings that were encountered during collection.

## API

### `public DiagnosticsProvider()`

Initializes a new instance of the `DiagnosticsProvider` class. No parameters are required. After construction, the diagnostic properties (`ApplicationInfo`, `SystemInfo`, `CacheInfo`, `Warnings`, and `GeneratedAt`) contain default or empty values until a report is generated.

### `public async Task<DiagnosticReport> GenerateReportAsync()`

Generates a comprehensive diagnostic report and returns it as a `DiagnosticReport` object. This method also updates the provider’s own properties (`ApplicationInfo`, `SystemInfo`, `CacheInfo`, `Warnings`, and `GeneratedAt`) with the same data.

- **Parameters**: None.
- **Returns**: A `Task<DiagnosticReport>` representing the asynchronous operation. The `DiagnosticReport` object contains the same information exposed by the provider’s properties.
- **Throws**: May throw exceptions related to Redis connectivity, permission issues, or other infrastructure failures encountered while gathering diagnostics. Specific exception types are not guaranteed.

### `public async Task<string> GenerateHtmlReportAsync()`

Generates a diagnostic report and returns it as an HTML-formatted string. This method also updates the provider’s properties with the latest diagnostic data.

- **Parameters**: None.
- **Returns**: A `Task<string>` representing the asynchronous operation. The resulting string is a complete HTML document containing the diagnostic information in a human-readable layout.
- **Throws**: Same as `GenerateReportAsync` – may throw exceptions due to Redis or system failures.

### `public DateTime GeneratedAt`

Gets the timestamp (in UTC) when the most recent report was generated. If no report has been generated, the value is `DateTime.MinValue`.

### `public Dictionary<string, string> ApplicationInfo`

Gets a dictionary of key-value pairs describing the application context (e.g., assembly version, application name, environment). This property is populated after a successful call to either `GenerateReportAsync` or `GenerateHtmlReportAsync`. If no report has been generated, the dictionary is empty.

### `public Dictionary<string, string> SystemInfo`

Gets a dictionary of key-value pairs describing the runtime system (e.g., operating system, processor architecture, memory). Populated after a successful report generation. If no report has been generated, the dictionary is empty.

### `public Dictionary<string, string> CacheInfo`

Gets a dictionary of key-value pairs describing the Redis cache configuration and state (e.g., server endpoint, connection status, cache hit/miss counts). Populated after a successful report generation. If no report has been generated, the dictionary is empty.

### `public List<string> Warnings`

Gets a list of warning messages that were raised during the last report generation. Warnings may indicate non-critical issues such as missing optional configuration values or degraded cache performance. If no report has been generated, the list is empty.

## Usage

### Example 1: Generating a structured report and inspecting properties

```csharp
using redis_cache_patterns.Diagnostics;

public async Task InspectDiagnosticsAsync()
{
    var provider = new DiagnosticsProvider();

    // Generate the report; the provider's properties are updated automatically.
    DiagnosticReport report = await provider.GenerateReportAsync();

    Console.WriteLine($"Report generated at: {provider.GeneratedAt:O}");
    Console.WriteLine($"Application: {provider.ApplicationInfo["Name"]}");
    Console.WriteLine($"Cache server: {provider.CacheInfo["Endpoint"]}");

    if (provider.Warnings.Count > 0)
    {
        Console.WriteLine("Warnings:");
        foreach (var warning in provider.Warnings)
        {
            Console.WriteLine($"  - {warning}");
        }
    }
}
```

### Example 2: Generating an HTML report for display or export

```csharp
using redis_cache_patterns.Diagnostics;

public async Task ExportDiagnosticsAsync()
{
    var provider = new DiagnosticsProvider();

    // Generate an HTML report string.
    string htmlReport = await provider.GenerateHtmlReportAsync();

    // Save to file or serve via HTTP.
    await File.WriteAllTextAsync("diagnostics.html", htmlReport);

    // The provider's properties are also updated.
    Console.WriteLine($"Report includes {provider.SystemInfo.Count} system entries.");
}
```

## Notes

- **Statefulness**: The `DiagnosticsProvider` is stateful. The properties `ApplicationInfo`, `SystemInfo`, `CacheInfo`, `Warnings`, and `GeneratedAt` reflect the data from the most recent call to `GenerateReportAsync` or `GenerateHtmlReportAsync`. Calling a generation method overwrites any previously stored values.
- **Empty state**: Before any report is generated, the dictionary properties are empty (count zero), `Warnings` is an empty list, and `GeneratedAt` is `DateTime.MinValue`. Attempting to access a key in an empty dictionary will throw a `KeyNotFoundException`.
- **Thread safety**: This class is not thread-safe. Concurrent calls to `GenerateReportAsync` or `GenerateHtmlReportAsync` from multiple threads may result in inconsistent state or exceptions. If concurrent access is required, external synchronization (e.g., a lock) must be used.
- **Exceptions**: Both generation methods may throw exceptions if the Redis cache is unreachable, if system information cannot be retrieved, or if other infrastructure errors occur. Callers should handle these exceptions appropriately (e.g., `RedisConnectionException`, `UnauthorizedAccessException`).
- **Performance**: Generating a report involves querying the Redis server and the local system. In high-latency environments or when the cache is under heavy load, the asynchronous methods may take longer to complete. Consider using a timeout or cancellation token if available (not exposed in the current API).
