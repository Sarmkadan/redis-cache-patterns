# LoggingHelper

`LoggingHelper` is a centralized logging utility for the `redis-cache-patterns` project. It provides standardized methods to record performance metrics, cache interactions, business-level operations, and exceptions, while also generating correlation identifiers for tracing operations across distributed components. The type is designed to produce consistent, structured log output that can be consumed by monitoring and diagnostics systems.

## API

### `public static void LogOperationPerformance`

Logs timing and performance data for an operation.

| Parameter | Type | Description |
|---|---|---|
| `operationName` | `string` | The name of the operation being measured. |
| `elapsedMilliseconds` | `long` | The total execution time in milliseconds. |
| `correlationId` | `string` | A unique identifier used to correlate related log entries. |
| `additionalData` | `Dictionary<string, object>` | Optional supplementary key-value pairs to include in the log entry. |

**Returns:** Nothing.

**Throws:** `ArgumentNullException` when `operationName` or `correlationId` is `null`.

---

### `public static void LogCacheOperation`

Records a cache-related event such as a hit, miss, set, or invalidation.

| Parameter | Type | Description |
|---|---|---|
| `cacheKey` | `string` | The key involved in the cache operation. |
| `operation` | `string` | The type of cache operation (e.g., `"GET"`, `"SET"`, `"REMOVE"`). |
| `outcome` | `string` | The result of the operation (e.g., `"Hit"`, `"Miss"`, `"Stored"`). |
| `correlationId` | `string` | A unique identifier used to correlate related log entries. |
| `latencyMs` | `long?` | Optional latency of the cache call in milliseconds. |

**Returns:** Nothing.

**Throws:** `ArgumentNullException` when `cacheKey`, `operation`, `outcome`, or `correlationId` is `null`.

---

### `public static void LogBusinessOperation`

Logs a high-level business event that is not purely technical in nature.

| Parameter | Type | Description |
|---|---|---|
| `businessEvent` | `string` | A descriptive name for the business event. |
| `correlationId` | `string` | A unique identifier used to correlate related log entries. |
| `metadata` | `Dictionary<string, object>` | Optional contextual data relevant to the business event. |

**Returns:** Nothing.

**Throws:** `ArgumentNullException` when `businessEvent` or `correlationId` is `null`.

---

### `public static string GenerateCorrelationId`

Creates a new, unique correlation identifier suitable for tracing a chain of operations.

| Parameter | Type | Description |
|---|---|---|
| _(none)_ | | |

**Returns:** `string` — a newly generated correlation ID (typically a GUID-based string).

**Throws:** Nothing.

---

### `public static void LogException`

Logs an exception with contextual information for diagnostic purposes.

| Parameter | Type | Description |
|---|---|---|
| `exception` | `Exception` | The exception to log. |
| `correlationId` | `string` | A unique identifier used to correlate related log entries. |
| `context` | `string` | A human-readable description of what was happening when the exception occurred. |

**Returns:** Nothing.

**Throws:** `ArgumentNullException` when `exception` or `correlationId` is `null`. The `context` parameter may be `null` or empty.

## Usage

### Example 1: Measuring and logging a cache-backed data retrieval

```csharp
var correlationId = LoggingHelper.GenerateCorrelationId();
var stopwatch = Stopwatch.StartNew();

try
{
    // Attempt cache retrieval
    var cacheKey = $"user:profile:{userId}";
    var cachedItem = cache.Get(cacheKey);

    if (cachedItem != null)
    {
        LoggingHelper.LogCacheOperation(cacheKey, "GET", "Hit", correlationId, latencyMs: 2);
        return cachedItem;
    }

    LoggingHelper.LogCacheOperation(cacheKey, "GET", "Miss", correlationId, latencyMs: 2);

    // Fetch from primary store
    var profile = database.QueryUserProfile(userId);
    cache.Set(cacheKey, profile, TimeSpan.FromMinutes(10));
    LoggingHelper.LogCacheOperation(cacheKey, "SET", "Stored", correlationId);

    LoggingHelper.LogBusinessOperation("UserProfileRetrieved", correlationId, new Dictionary<string, object>
    {
        ["userId"] = userId,
        ["source"] = "database"
    });

    return profile;
}
catch (Exception ex)
{
    LoggingHelper.LogException(ex, correlationId, $"Failed to retrieve profile for user {userId}");
    throw;
}
finally
{
    stopwatch.Stop();
    LoggingHelper.LogOperationPerformance("GetUserProfile", stopwatch.ElapsedMilliseconds, correlationId);
}
```

### Example 2: Logging a standalone business event with exception handling

```csharp
var correlationId = LoggingHelper.GenerateCorrelationId();

try
{
    paymentService.ProcessRefund(orderId, amount);

    LoggingHelper.LogBusinessOperation("RefundProcessed", correlationId, new Dictionary<string, object>
    {
        ["orderId"] = orderId,
        ["amount"] = amount,
        ["currency"] = "USD"
    });
}
catch (Exception ex)
{
    LoggingHelper.LogException(ex, correlationId, "Refund processing failed for order");
    throw;
}
```

## Notes

- **Null handling:** All methods that accept `string` parameters marked as required will throw `ArgumentNullException` if `null` is passed. The `LogException` method tolerates a `null` or empty `context` string, but the `exception` and `correlationId` arguments must not be `null`.
- **Correlation ID lifecycle:** `GenerateCorrelationId` produces a unique value on each call. It is the caller's responsibility to pass the same correlation ID through all log invocations belonging to a single logical operation. The method itself does not store or track generated IDs.
- **Thread safety:** All public members are static and do not mutate shared state. They are safe to call concurrently from multiple threads without external synchronization, provided the underlying logging infrastructure is itself thread-safe.
- **Performance logging granularity:** `LogOperationPerformance` expects the caller to measure elapsed time externally (e.g., via `Stopwatch`). The method does not start or stop timers internally, giving callers full control over what portion of execution is measured.
- **Optional parameters:** `LogOperationPerformance` and `LogBusinessOperation` accept optional `Dictionary<string, object>` parameters. Passing `null` for these is acceptable and will result in no additional data being attached to the log entry. `LogCacheOperation` accepts a nullable `long?` for latency; a `null` value indicates that latency was not measured or is not applicable.
