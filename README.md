// ... (rest of the file remains the same)

## CacheMonitor

The `CacheMonitor` class is responsible for tracking cache performance and health. It provides methods to retrieve cache statistics, print statistics, track cache entries, and calculate average hit rates. 

Here is an example of how to use the `CacheMonitor` class:

```csharp
var cacheMonitor = new CacheMonitor(cacheService, logger);
var stats = await cacheMonitor.GetStatisticsAsync();
await cacheMonitor.PrintStatisticsAsync();

// Track a cache entry
cacheMonitor.TrackEntry(new CacheEntry { HitRate = 0.8, SizeInBytes = 1024 });

// Get tracked entries
var entries = cacheMonitor.GetTrackedEntries();

// Get average hit rate
var averageHitRate = await cacheMonitor.GetAverageHitRateAsync();

// Get total cache size
var totalCacheSize = cacheMonitor.GetTotalCacheSize();

// Get entries by hit rate
var highHitRateEntries = cacheMonitor.GetEntriesByHitRate(0.5);

// Get cold entries
var coldEntries = cacheMonitor.GetColdEntries(TimeSpan.FromHours(1));
```

## CompressionUtil

`CompressionUtil` provides high‑performance helpers for compressing and decompressing data using GZIP while reusing buffers from `ArrayPool<byte>.Shared` to minimise allocations and GC pressure. It also offers utilities to evaluate compression effectiveness.

```csharp
using RedisCachePatterns.Utilities;

string original = "Hello, world!";

// Compress a string to a byte array
byte[] compressed = CompressionUtil.CompressString(original);

// Decompress back to the original string
string roundTrip = CompressionUtil.DecompressString(compressed);

// Compress a raw byte span
byte[] rawData = Encoding.UTF8.GetBytes(original);
byte[] compressedBytes = CompressionUtil.CompressBytes(rawData);

// Decompress the raw bytes
byte[] decompressed = CompressionUtil.DecompressBytes(compressedBytes);

// Evaluate compression ratio and whether it is worthwhile
double ratio = CompressionUtil.GetCompressionRatio(original.Length, compressed.Length);
bool worthwhile = CompressionUtil.IsCompressionWorthwhile(original.Length, compressed.Length);
```

## RepositoryExtensions

The `RepositoryExtensions` class provides a set of convenient extension methods for the `IRepository<T>` interface, offering common LINQ-style operations like filtering, searching, and existence checks without requiring direct access to the underlying data store. These methods simplify repository usage by providing familiar patterns similar to Entity Framework's query methods.

Here is an example of how to use the `RepositoryExtensions` with a sample `Product` entity:

```csharp
using RedisCachePatterns.Infrastructure.Repositories;

// Assume we have a repository for Product entities
var productRepository = new ProductRepository();

// Check if any products exist
bool hasProducts = await productRepository.AnyAsync();

// Get a product by ID (returns null if not found)
var product = await productRepository.FirstOrDefaultAsync(1);

// Find all products matching a condition
var expensiveProducts = await productRepository.WhereAsync(p => p.Price > 100);

// Get the first product matching a condition (returns null if not found)
var firstExpensiveProduct = await productRepository.FirstOrDefaultAsync(p => p.Price > 100);

// Get a single product matching a condition (throws if none or multiple found)
var singleProduct = await productRepository.SingleAsync(p => p.Id == 5);

// Get a single product matching a condition (returns null if not found)
var singleOrDefaultProduct = await productRepository.SingleOrDefaultAsync(p => p.Id == 5);

// Get the only product in the repository (throws if none or multiple found)
var onlyProduct = await productRepository.SingleAsync();

// Get the only product in the repository (returns null if not found)
var onlyOrDefaultProduct = await productRepository.SingleOrDefaultAsync();
```

## ServiceCollectionExtensionsJsonExtensions

The `ServiceCollectionExtensionsJsonExtensions` class provides extension methods for serializing objects to JSON and deserializing service collection configuration patterns using System.Text.Json. It includes methods for both strict and forgiving JSON parsing, with support for camelCase property naming and configurable indentation.

Here is an example of how to use the `ServiceCollectionExtensionsJsonExtensions` methods:

```csharp
using System;
using Microsoft.Extensions.DependencyInjection;
using RedisCachePatterns.Extensions;

// Sample configuration object
var config = new ServiceCollectionPatterns
{
    AuditingEnabled = true,
    BatchProcessingConfigured = true,
    IdempotencyEnabled = false,
    PerformanceMonitoringEnabled = true
};

// Serialize to JSON string
string json = config.ToJson(); // Compact JSON
string prettyJson = config.ToJson(indented: true); // Pretty-printed JSON

Console.WriteLine(json);

// Deserialize from JSON string
ServiceCollectionPatterns? deserialized = ServiceCollectionExtensionsJsonExtensions.FromJson(json);
if (deserialized is not null)
{
    Console.WriteLine($"Auditing enabled: {deserialized.AuditingEnabled}");
    Console.WriteLine($"Batch processing configured: {deserialized.BatchProcessingConfigured}");
}

// Try to deserialize with error handling
if (ServiceCollectionExtensionsJsonExtensions.TryFromJson(json, out var result))
{
    Console.WriteLine("Deserialization successful!");
}
else
{
    Console.WriteLine("Failed to deserialize JSON");
}

// Deserialize with null handling
string? emptyJson = null;
var nullResult = ServiceCollectionExtensionsJsonExtensions.FromJson(emptyJson); // Returns null
```

## IOutputFormatter

The `IOutputFormatter` interface defines a contract for serializing objects into string representations in various formats (e.g., JSON, XML). It exposes two overloads of `Format` – one for a single object and one for a collection – and a `ContentType` property that indicates the MIME type of the output. The `FormatterRegistry` class lets you register and retrieve formatters by name, query available formats, and check for existence.

```csharp
using RedisCachePatterns.Formatters;

// Create a registry and register a JSON formatter
var registry = new FormatterRegistry()
    .RegisterFormatter("json", new JsonOutputFormatter());

// Retrieve the formatter
var jsonFormatter = registry.GetFormatter("json");

// Format a single object
var user = new { Id = 1, Name = "Alice" };
string json = jsonFormatter.Format(user);

// Format a collection
var users = new[] { user, new { Id = 2, Name = "Bob" } };
string jsonArray = jsonFormatter.Format(users);

// Wrap the result in a FormattedResponse
var response = new FormattedResponse<object>(user, "json");

// Inspect the response
Console.WriteLine(response); // [json] { Id = 1, Name = Alice }
```

## CacheEndpointExtensions

`CacheEndpointExtensions` adds higher‑level operations to `CacheEndpoint`, such as pattern‑based invalidation with optional prefixes, enriched statistics, paginated key retrieval, and flush operations that return detailed metrics. These helpers return `ApiResponse<T>` objects, making it easy to handle success, errors, and HTTP‑style status codes in a uniform way.

## AnalyticsEndpointExtensions

`AnalyticsEndpointExtensions` provides extension methods for `AnalyticsEndpoint` that enable advanced analytics operations for cache monitoring. These methods allow you to retrieve filtered snapshots of cache performance data (hot keys, cold keys, poor efficiency keys), get formatted key statistics, reset analytics counters, and generate comprehensive efficiency summaries. All methods return `ApiResponse<T>` objects for consistent error handling and status reporting.

```csharp
using System;
using System.Threading.Tasks;
using RedisCachePatterns.API;
using RedisCachePatterns.Services;

// Assume an existing AnalyticsEndpoint instance (implementation details omitted)
AnalyticsEndpoint endpoint = /* obtain endpoint instance */;

// 1️⃣ Get a snapshot of hot keys (keys accessed 100+ times)
var hotKeysResult = await endpoint.GetHotKeysSnapshotAsync(minAccessThreshold: 100);
if (hotKeysResult.IsSuccess && hotKeysResult.Data is not null)
{
    var hotKeys = hotKeysResult.Data;
    Console.WriteLine($"Found {hotKeys.HotKeys.Count} hot keys");
    Console.WriteLine($"Overall hit rate: {hotKeys.OverallHitRate:P2}");
}

// 2️⃣ Get a snapshot of cold keys (last accessed more than 1 hour ago)
var coldKeysResult = await endpoint.GetColdKeysSnapshotAsync(maxAgeHours: 1);
if (coldKeysResult.IsSuccess && coldKeysResult.Data is not null)
{
    var coldKeys = coldKeysResult.Data;
    Console.WriteLine($"Found {coldKeys.ColdKeys.Count} cold keys");
}

// 3️⃣ Get keys with poor cache efficiency (hit rate < 50%)
var poorEfficiencyResult = await endpoint.GetPoorEfficiencyKeysAsync(minHitRate: 0.5);
if (poorEfficiencyResult.IsSuccess && poorEfficiencyResult.Data is not null)
{
    var poorKeys = poorEfficiencyResult.Data;
    Console.WriteLine($"Found {poorKeys.LowHitRateKeys.Count} keys with poor efficiency");
}

// 4️⃣ Get formatted statistics for a specific cache key
var keyStatsResult = await endpoint.GetFormattedKeyStatsAsync(
    key: "user:session:12345",
    format: KeyStatsFormat.HumanReadable);
if (keyStatsResult.IsSuccess)
{
    Console.WriteLine(keyStatsResult.Data);
}

// 5️⃣ Reset analytics counters with an optional reason
var resetResult = await endpoint.ResetWithConfirmationAsync(
    reason: "Scheduled maintenance window");
if (resetResult.IsSuccess)
{
    Console.WriteLine(resetResult.Data);
}

// 6️⃣ Get a comprehensive cache efficiency summary report
var summaryResult = await endpoint.GetEfficiencySummaryAsync();
if (summaryResult.IsSuccess)
{
    Console.WriteLine(summaryResult.Data);
}
```

```csharp
using System;
using System.Threading.Tasks;
using RedisCachePatterns.API;
using RedisCachePatterns.Services;

// Assume an existing CacheEndpoint instance (implementation details omitted)
CacheEndpoint endpoint = /* obtain endpoint instance */;

// 1️⃣ Invalidate keys that match a pattern, optionally adding a prefix
var invalidateResult = await endpoint.InvalidateByPatternWithPrefixAsync(
    pattern: "session:*",
    prefix: "myApp");

// 2️⃣ Retrieve enhanced cache statistics with computed hit‑rate and memory usage
var statsResult = await endpoint.GetEnhancedStatisticsAsync();
if (statsResult.IsSuccess && statsResult.Data is not null)
{
    var stats = statsResult.Data;
    Console.WriteLine($"Hit rate: {stats.HitRate:P2}");
    Console.WriteLine($"Memory used: {stats.MemoryUsedMB:F2} MiB");
    Console.WriteLine($"Captured at: {stats.CapturedAt:u}");
}

// 3️⃣ Get cache keys matching a pattern, paged
var keysResult = await endpoint.GetKeysByPatternPagedAsync(
    pattern: "user:*",
    page: 1,
    pageSize: 20);

if (keysResult.IsSuccess && keysResult.Data is not null)
{
    var page = keysResult.Data;
    Console.WriteLine($"Page {page.Page}/{page.TotalPages}, total keys: {page.TotalCount}");
    foreach (var key in page.Items)
    {
        Console.WriteLine($" - {key}");
    }
}

// 4️⃣ Flush the entire cache and obtain operation statistics
var flushResult = await endpoint.FlushWithStatisticsAsync();
if (flushResult.IsSuccess && flushResult.Data is not null)
{
    var flushInfo = flushResult.Data;
    Console.WriteLine($"Flushed {flushInfo.KeysFlushed} keys in {flushInfo.DurationMilliseconds} ms");
    Console.WriteLine($"Before: {flushInfo.KeysBefore}, After: {flushInfo.KeysAfter}");
    Console.WriteLine($"Completed at: {flushInfo.Timestamp:u}");
}
```

The example demonstrates how to call each public extension method and how to read the relevant properties (`Items`, `TotalCount`, `Page`, `PageSize`, `TotalPages`, `TotalKeys`, `Hits`, `Misses`, `HitRate`, `MemoryUsedBytes`, `MemoryUsedMB`, `CapturedAt`, `Success`, `KeysBefore`, `KeysAfter`, `KeysFlushed`) that are part of the result records returned by these helpers.