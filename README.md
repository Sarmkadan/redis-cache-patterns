// README.md
## StampedeProtectedCacheService

The `StampedeProtectedCacheService` class provides a set of extension methods for the `ICacheService` interface, offering additional convenience and batch operations for managing cache entries. These methods enable you to retrieve cache entries by key, set cache entries with expiration, remove cache entries, and check if a key exists in the cache.

Here is an example of how to use the `StampedeProtectedCacheService` methods:
```csharp
using RedisCachePatterns.Services;

// Assume an existing ICacheService instance (implementation details omitted)
ICacheService cacheService = /* obtain cache service instance */;

// Try to get a cache entry by its key
var entry = await cacheService.GetAsync<string>("key:123");

// Set a cache entry with expiration
await cacheService.SetAsync("key:123", "value", TimeSpan.FromHours(1));

// Remove a cache entry
await cacheService.RemoveAsync("key:123");
```


## BulkGetRequest

The `BulkGetRequest` class defines a structure for requesting multiple cache entries in a single operation. By providing a list of keys, it allows for efficient batch retrieval of data from the cache.

Example usage:
```csharp
using RedisCachePatterns.Domain;

// Define the keys to retrieve
var request = new BulkGetRequest
{
    Keys = new List<string> { "key:1", "key:2", "key:3" },
    ReturnNullForMissing = true
};

// Use the request with a bulk operation service
// Assuming an existing implementation:
// BulkGetResponse<string> response = await bulkCacheService.GetAsync<string>(request);

// Inspect the results
// foreach (var result in response.Results)
// {
//     Console.WriteLine($"Key: {result.Key}, Found: {result.Found}, Value: {result.Value}");
// }
```


## RedisCacheServiceTests

`RedisCacheServiceTests` provides a comprehensive test suite for the `RedisCacheService`, covering critical cache operations such as retrieval, storage, removal, and existence checks. The suite also validates advanced features, including `GetOrLoadAsync` with deserialization failure handling and `GetWithSlidingExpirationAsync` logic, ensuring reliable performance across diverse cache hit and miss scenarios.

Example usage in a test context:
```csharp
using RedisCachePatterns.Tests.Services;
using System.Threading.Tasks;

// Example of how the tests are structured to validate RedisCacheService:
public class RedisCacheServiceUsageExamples
{
    public async Task DemonstrateTestCapabilities(RedisCacheServiceTests tests)
    {
        // Validating basic storage and retrieval functionality
        await tests.SetAsync_StoresValueInCache();
        await tests.GetAsync_WhenKeyExists_ReturnsDeserializedValue();

        // Validating GetOrLoad logic and cache eviction on failure
        await tests.GetOrLoadAsync_WhenCacheMiss_CallsLoadFnAndCachesResult();

        // Validating sliding expiration behavior
        await tests.GetWithSlidingExpirationAsync_WhenCacheHit_ReturnsValueAndResetsTTL();
    }
}
```


## CacheTagServiceTests

The `CacheTagServiceTests` class validates the behavior of cache‑tag operations, ensuring that keys can be associated with tags, retrieved by tag, and invalidated correctly. It covers scenarios such as adding keys to tags, handling invalid arguments, and bulk invalidation of multiple tags.

Example usage:
```csharp
using RedisCachePatterns.Tests.Services;
using System.Threading.Tasks;

public class CacheTagServiceUsageExamples
{
    private readonly CacheTagServiceTests _tests = new CacheTagServiceTests();

    public async Task RunAllExamples()
    {
        // Write a value and associate it with multiple tags
        await _tests.SetWithTagsAsync_WritesValueAndAddsToAllTagSets();

        // Add a single key to a tag
        await _tests.TagKeyAsync_AddsKeyToTagSet();

        // Remove a key from a tag and check the result
        bool removed = await _tests.UntagKeyAsync_RemovesKeyFromTagSet_ReturnsTrueWhenMember();

        // Retrieve all keys associated with a specific tag
        var keys = await _tests.GetKeysByTagAsync_ReturnsKeysForTag();

        // Invalidate a tag, removing all its keys
        long removedCount = await _tests.InvalidateTagAsync_RemovesAllKeysAndDeletesTagSet_ReturnsCount();

        // Invalidate multiple tags at once
        long totalRemoved = await _tests.InvalidateTagsAsync_InvalidatesMultipleTags_ReturnsTotalCount();
    }
}
```


## CacheStatisticsAggregator

`CacheStatisticsAggregator` aggregates cache operation metrics such as hits, misses, and errors, providing a snapshot of current statistics and supporting reset and disposal semantics. It is typically used to collect and expose runtime cache health data for monitoring or diagnostics.

Example usage:
```csharp
using System;
using RedisCachePatterns.Monitoring;

using var aggregator = new CacheStatisticsAggregator();

// Record some cache activity
aggregator.IncrementHits();
aggregator.IncrementMisses();
aggregator.IncrementErrors();

// Retrieve the aggregated statistics
CacheStatistics stats = aggregator.GetStatistics();
Console.WriteLine($"Hits: {stats.Hits}, Misses: {stats.Misses}, Errors: {stats.Errors}");

// Reset the counters if needed
aggregator.Reset();

// When finished, the aggregator can be disposed (handled by the using statement above)
```


## CsvFormatter

`CsvFormatter` is an `IOutputFormatter` that renders data as CSV text with the content type `text/csv`. It uses reflection to derive column headers from the public properties of the data type, and it handles complex types by reading their public properties. Values that contain the delimiter, a double quote, or a newline are wrapped in quotes, with embedded quotes doubled. The delimiter defaults to a comma and can be replaced through the constructor; an empty delimiter is rejected with an `ArgumentException`.

Example usage:
```csharp
using System.Collections.Generic;
using RedisCachePatterns.Formatters;

var products = new List<Product>
{
    new Product { /* ... populate properties ... */ },
    new Product { /* ... populate properties ... */ }
};

// Default comma-delimited output
var formatter = new CsvFormatter();
string csv = formatter.Format(products);
string contentType = formatter.ContentType; // "text/csv"

// Semicolon-delimited output
var semicolonFormatter = new CsvFormatter(";");
string csvWithSemicolons = semicolonFormatter.Format(products);
```

> **Known issue:** property discovery in `CsvFormatter` calls `GetProperties(BindingFlags.Public | BindingFlags.IgnoreCase)` without `BindingFlags.Instance`, so instance properties are not found and the generated CSV currently contains no columns. The example above describes the intended behavior.


## CacheCircuitBreakerServiceTests

The `CacheCircuitBreakerServiceTests` class provides a suite of unit tests for the `CacheCircuitBreakerService` class, covering circuit breaker behavior such as opening after threshold failures, fail-open semantics, and manual reset.

Example usage in a test context:
```csharp
using RedisCachePatterns.Tests.Services;
using System.Threading.Tasks;

// Example of how the tests are structured to validate CacheCircuitBreakerService:
public class CacheCircuitBreakerServiceUsageExamples
{
    public async Task DemonstrateTestCapabilities(CacheCircuitBreakerServiceTests tests)
    {
        // Validating circuit opening after threshold failures
        await tests.GetOrLoadAsync_AfterThresholdFailures_CircuitOpens();

        // Validating fail-open behavior for GetAsync
        await tests.GetAsync_WhenCircuitOpen_ReturnsDefault();

        // Validating manual reset
        tests.Reset_ClosesCircuitAndResetsFailures();
    }
}
```


## EncryptionHelper

`EncryptionHelper` is a static utility class in `RedisCachePatterns.Utilities` for hashing and securing sensitive data. Its main functionality is SHA256 hashing: `HashSha256` returns the uppercase hexadecimal digest of a UTF-8 string, and `VerifyHash` checks whether an input matches a previously computed hash (the comparison ignores case). The class also provides `HashMd5` (for non-security-critical purposes such as cache keys only), `GenerateRandomBytes` and `GenerateRandomString` backed by a cryptographically secure random number generator, and `MaskSensitiveData` for safely logging values such as `pa****rd`.

Example usage:
```csharp
using RedisCachePatterns.Utilities;

// Hash a value before storing it
string hash = EncryptionHelper.HashSha256("my-secret-value");

// Later, verify a candidate value against the stored hash
bool isMatch = EncryptionHelper.VerifyHash("my-secret-value", hash); // true

// Mask a sensitive value before writing it to logs
string masked = EncryptionHelper.MaskSensitiveData("password123"); // "pa*********"
```


## AuditingService

`AuditingService` in `RedisCachePatterns.Services` keeps an in-memory audit trail of system operations for compliance and debugging. `LogOperation` records an `AuditEntry` (operation type, resource ID, user ID, optional details and a UTC timestamp) and writes it to the logger as well. A missing user ID is recorded as `system`, and an empty operation type or resource ID is rejected with an `ArgumentException`. The log is bounded: once it holds more entries than the configured maximum (10,000 by default), the oldest entries are dropped. `GetAuditLog` returns a snapshot filtered by operation type and/or resource ID, optionally limited to the last N entries. `ClearAuditLog` empties the trail and `GetAuditLogSize` returns the current entry count. All methods are thread-safe. The trail lives only in process memory and is not persisted across restarts.

Example usage:
```csharp
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Services;

// Logger is typically injected; the optional third constructor argument sets the maximum number of entries
var auditing = new AuditingService(logger);

// Record an operation performed by a user
auditing.LogOperation("Update", "product:42", userId: "user-17", details: "Price changed from 9.99 to 12.49");

// Record a system-initiated operation (userId defaults to "system")
auditing.LogOperation("CacheWarm", "product:42");

// All entries for a single resource
IEnumerable<AuditingService.AuditEntry> history = auditing.GetAuditLog(resourceId: "product:42");

// The last 10 "Update" operations, across all resources
List<AuditingService.AuditEntry> recentUpdates = auditing.GetAuditLog(operationType: "Update", lastNEntries: 10).ToList();
```


## RetryHelper

`RetryHelper` is a static utility class in `RedisCachePatterns.Utilities` for handling transient failures. It provides two features:

- **Exponential backoff:** `ExecuteWithRetryAsync` runs an async operation up to `maxRetries` times (the total number of attempts, 3 by default). Between attempts it waits `initialDelayMs * 2^attempt` milliseconds (100 ms initial delay by default) plus random jitter of up to `initialDelayMs`, which spreads out retries from many callers. Every exception triggers a retry. When all attempts fail, it throws an `InvalidOperationException` whose inner exception is the last failure.
- **Circuit breaker:** `RetryHelper.CircuitBreaker.ExecuteAsync` tracks failures per circuit name. After `failureThreshold` consecutive failures (5 by default) the circuit opens, and calls fail immediately with an `InvalidOperationException` without invoking the operation. Once `resetTimeoutSeconds` (60 by default) have passed since the last failure, the next call is allowed through and the circuit closes again. A successful call resets the failure count. `CircuitBreaker.Reset` clears the state of a named circuit. The state is static and process-wide, so all callers that share a circuit name share its state. There is no separate half-open state: while the reset timeout is running the circuit rejects all calls, and afterwards it accepts all calls until the failure threshold is reached again.

A null operation is rejected with an `ArgumentNullException`, and an empty circuit name with an `ArgumentException`. The optional `ILogger` receives warnings for each retry and information or error messages when a circuit resets or opens.

Example usage:
```csharp
using System;
using System.Threading.Tasks;
using RedisCachePatterns.Utilities;

// Retry up to 3 attempts, waiting about 100 ms and then about 200 ms (plus jitter) between them
var product = await RetryHelper.ExecuteWithRetryAsync(
    () => FetchProductAsync(42),   // any Func<Task<T>>
    maxRetries: 3,
    initialDelayMs: 100,
    logger: logger);

// Fail fast after 5 consecutive failures of the "product-api" dependency for 60 seconds
var payload = await RetryHelper.CircuitBreaker.ExecuteAsync(
    "product-api",
    () => FetchPayloadAsync(),
    failureThreshold: 5,
    resetTimeoutSeconds: 60,
    logger: logger);

// Clear the state of the circuit, for example in a test or after a manual recovery
RetryHelper.CircuitBreaker.Reset("product-api");
```


## PageHelper

`PageHelper` is a static utility class in `RedisCachePatterns.Utilities` for pagination of large result sets. It supports offset-based paging, and `GetOffset` returns the offset for database queries such as `Skip`/`Take`. Its constants are `DefaultPageSize` (20), `MaxPageSize` (100) and `MinPageSize` (1). Its methods are:

- **`ValidatePaginationParams`** normalizes raw paging input. A missing or non-positive page number becomes 1, and a missing page size becomes 20. Page sizes are clamped to the range 1 to 100.
- **`Paginate`** applies paging to an in-memory `IEnumerable<T>` and returns a `PagedResult<T>`. An overload takes a `sortFn` that is applied before paging. A null collection is rejected with an `ArgumentNullException`.
- **`GetOffset`** returns `(pageNumber - 1) * pageSize` after validation.

`PagedResult<T>` holds the `Items` of the current page, the normalized `PageNumber` and `PageSize`, and the `TotalCount` across all pages. Its computed members are `TotalPages`, `HasPreviousPage` and `HasNextPage`, and `ToString` returns a summary such as `Page 2/5 (20/93 items)`.

Example usage:
```csharp
using System.Linq;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Utilities;

// Normalize user-supplied paging input: page 0 becomes 1, and size 500 is capped at 100
var (pageNumber, pageSize) = PageHelper.ValidatePaginationParams(0, 500); // (1, 100)

// Paginate an in-memory collection
PagedResult<Product> page = PageHelper.Paginate(allProducts, pageNumber, pageSize);

// Sort first, then take the second page of 20 items
PagedResult<Product> sortedPage = PageHelper.Paginate(
    allProducts,
    pageNumber: 2,
    pageSize: 20,
    sortFn: items => items.OrderBy(p => p.Name));

// Offset for a database query: 40 for the third page of 20 items
int offset = PageHelper.GetOffset(pageNumber: 3, pageSize: 20);

if (sortedPage.HasNextPage)
{
    // Link to or fetch the next page
}
```


## AppConstants

`AppConstants` in `RedisCachePatterns.Configuration` is a static class that holds application-wide constants, grouped into nested static classes. The constants are plain `const` values, except for the cache expiration times, which are `static readonly` `TimeSpan` fields.

- **`Cache`** holds general cache settings: `DEFAULT_CONNECTION` (`localhost:6379`), `DEFAULT_DATABASE` (0), `LOCK_TIMEOUT_SECONDS` (10) and `MAX_CACHE_SIZE` (100 MB, expressed in bytes). It also contains two nested groups:
  - **`Cache.Expiration`** defines default expiration times per entity: `USER` and `ORDER` (1 hour), `PRODUCT` (2 hours), `INVENTORY` and `LIST` (30 minutes), and `SEARCH` (10 minutes).
  - **`Cache.KeyPatterns`** defines key templates with `{id}`-style placeholders, such as `user:{id}`, `user:username:{username}`, `product:{id}`, `product:sku:{sku}`, `order:{id}`, `inventory:{id}` and `lock:{key}`. The placeholders are not filled in by the class itself, so the calling code has to substitute them.
- **`Validation`** holds input limits: username length 3 to 50 characters (`MIN_USERNAME_LENGTH`, `MAX_USERNAME_LENGTH`), minimum password length 8, product name length 2 to 255 characters, and price range 0 to 999999.99 (`MIN_PRICE`, `MAX_PRICE`).
- **`Business`** holds business rules: `DEFAULT_REORDER_LEVEL` (10), `MIN_ORDER_ITEMS` (1), `TAX_RATE` (0.08, or 8%) and `STANDARD_SHIPPING` (10.00).
- **`Pagination`** holds paging defaults: `DEFAULT_PAGE_SIZE` (20), `MAX_PAGE_SIZE` (100) and `DEFAULT_PAGE_NUMBER` (1).

Example usage:
```csharp
using RedisCachePatterns.Configuration;

// Cache settings
string connection = AppConstants.Cache.DEFAULT_CONNECTION;      // "localhost:6379"
TimeSpan productTtl = AppConstants.Cache.Expiration.PRODUCT;    // 2 hours

// Build a key from a template by substituting the placeholder
string productKey = AppConstants.Cache.KeyPatterns.PRODUCT.Replace("{id}", "42"); // "product:42"

// Validation limits
bool nameIsValid = name.Length >= AppConstants.Validation.MIN_PRODUCT_NAME_LENGTH
    && name.Length <= AppConstants.Validation.MAX_PRODUCT_NAME_LENGTH;

// Business rules
decimal tax = subtotal * AppConstants.Business.TAX_RATE;

// Paging defaults
int pageSize = AppConstants.Pagination.DEFAULT_PAGE_SIZE;       // 20
```


## ClusterNode

The types in `Domain/ClusterNode.cs` (namespace `RedisCachePatterns.Domain`) describe a Redis Cluster topology as immutable records. They are plain snapshots and do not talk to Redis themselves; the data is expected to come from `CLUSTER NODES` output.

- **`ClusterSlotRange`** is an inclusive range of hash slots (`Start`, `End`) owned by one node. `Count` gives the number of slots in the range, and `Contains(slot)` checks whether a slot falls inside it.
- **`ClusterNode`** is one node in the cluster: `NodeId`, `EndPoint` (`host:port`), `Role` (a `ClusterNodeRole` of `Master`, `Replica` or `Unknown`), `SlotRanges`, `IsConnected` and `PrimaryNodeId` (set only for replicas). `TotalSlotCount` sums the slots across all ranges, `IsMaster` is a shortcut for the role check, and `OwnsSlot(slot)` tells whether any of the node's ranges covers the slot.
- **`ClusterInfo`** is a point-in-time summary of the whole cluster: `TotalNodes`, `MasterCount`, `ReplicaCount`, `TotalSlots` (always 16 384), `CoveredSlots`, `IsHealthy` (all slots covered by a connected master), `CapturedAt` (UTC) and `SlotCoverage` (a percentage from 0 to 100).

Example usage:
```csharp
using RedisCachePatterns.Domain;

var primary = new ClusterNode
{
    NodeId = "3f1a9c0d2b7e4f6a8c5d1e9b0a2f4c6d8e0b1a2c",
    EndPoint = "10.0.0.11:6379",
    Role = ClusterNodeRole.Master,
    SlotRanges = new[]
    {
        new ClusterSlotRange { Start = 0, End = 5460 },
        new ClusterSlotRange { Start = 10923, End = 11000 }
    },
    IsConnected = true
};

int slots = primary.TotalSlotCount;   // 5461 + 78 = 5539
bool owns = primary.OwnsSlot(42);     // true
bool gap = primary.OwnsSlot(6000);    // false

var replica = new ClusterNode
{
    NodeId = "9b2c4e6f8a0d1c3e5f7a9b1d3c5e7f9a1b3c5d7e",
    EndPoint = "10.0.0.12:6379",
    Role = ClusterNodeRole.Replica,
    SlotRanges = Array.Empty<ClusterSlotRange>(),
    IsConnected = true,
    PrimaryNodeId = primary.NodeId
};

var info = new ClusterInfo
{
    TotalNodes = 2,
    MasterCount = 1,
    ReplicaCount = 1,
    TotalSlots = 16384,
    CoveredSlots = 16384,
    IsHealthy = true,
    CapturedAt = DateTime.UtcNow
};

double coverage = info.SlotCoverage;  // 100
```


## ExternalApiClient

`ExternalApiClient` in `RedisCachePatterns.Integration` is a generic HTTP client for calling external REST APIs. It wraps an injected `HttpClient` and offers typed `GetAsync<T>`, `PostAsync<T>` and `PutAsync<T>` methods that serialize request bodies to JSON and deserialize responses, plus a `DeleteAsync` that returns `true` on success. Endpoints are relative to the `HttpClient.BaseAddress`.

Transient failures are retried: only `HttpRequestException` triggers a retry, up to `maxRetries` attempts in total (3 by default). The wait between attempts grows linearly, `retryDelayMs * attempt` (1000 ms by default), so the first retry waits 1 second and the second waits 2 seconds. When all attempts fail, it throws an `InvalidOperationException`. A non-success HTTP status code is not retried: `EnsureSuccessStatusCode` throws an `HttpRequestException` that propagates to the caller. Every failing call is logged as an error through the injected `ILogger<ExternalApiClient>` before the exception is rethrown. An empty `endpoint` is rejected with an `ArgumentException` in `GetAsync`.

Example usage:
```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Integration;

// HttpClient is typically provided by IHttpClientFactory; the logger is injected
var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
var client = new ExternalApiClient(httpClient, logger, maxRetries: 3, retryDelayMs: 500);

// GET a single object, deserialized from the JSON response (null if the body cannot be parsed)
Product? product = await client.GetAsync<Product>("products/42");

// POST a request body and read the typed response
Order? order = await client.PostAsync<Order>("orders", new { ProductId = 42, Quantity = 2 });

// PUT an updated object
Product? updated = await client.PutAsync<Product>("products/42", product!);

// DELETE a resource; returns true when the request succeeds
bool deleted = await client.DeleteAsync("products/42");
```


## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
