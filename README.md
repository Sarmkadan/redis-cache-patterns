// ... (rest of the file remains the same)

## PageHelper

The `PageHelper` class provides a set of static methods for handling pagination in Redis cache operations. It includes methods for validating and normalizing pagination parameters, applying pagination to a collection, and getting the offset value for database queries.

Here is an example of how to use the `PageHelper` class:
```csharp
// Validate pagination parameters
var (pageNumber, pageSize) = PageHelper.ValidatePaginationParams(2, 10);
Console.WriteLine($"Validated page number: {pageNumber}, page size: {pageSize}");

// Paginate a collection
var items = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
var pagedResult = PageHelper.Paginate(items, 2, 5);
Console.WriteLine($"Page number: {pagedResult.PageNumber}, page size: {pagedResult.PageSize}, total count: {pagedResult.TotalCount}");

// Get the offset value for database queries
var offset = PageHelper.GetOffset(2, 5);
Console.WriteLine($"Offset: {offset}");
```

## CacheKeyHelper

The `CacheKeyHelper` provides consistent key naming conventions for Redis cache operations. It includes methods for building entity keys, collection keys, wildcard patterns, and temporary keys, with built-in validation and normalization utilities.

Here is an example of how to use the `CacheKeyHelper` class:
```csharp
// Build entity key
string productKey = CacheKeyHelper.BuildEntityKey<Product>(123);
Console.WriteLine(productKey); // "product:entity:123"

// Build collection key
string productsKey = CacheKeyHelper.BuildCollectionKey<Product>();
Console.WriteLine(productsKey); // "product:collection"

// Build collection with filter
string filteredProductsKey = CacheKeyHelper.BuildCollectionKey<Product>("active=true");
Console.WriteLine(filteredProductsKey); // "product:collection:active=true"

// Build custom key
string customKey = CacheKeyHelper.BuildKey("order", 456, "details");
Console.WriteLine(customKey); // "order:456:details"

// Build pattern for matching
string pattern = CacheKeyHelper.BuildEntityPattern<Product>();
Console.WriteLine(pattern); // "product:entity:*"

// Validate and normalize keys
bool isValid = CacheKeyHelper.IsValidKey(productKey);
string normalized = CacheKeyHelper.NormalizeKey("  PRODUCT:ENTITY:123  ");
Console.WriteLine(normalized); // "product:entity:123"

// Parse key components
string[] parts = CacheKeyHelper.ParseKey(productKey);
Console.WriteLine(string.Join(" | ", parts)); // "product | entity | 123"

// Distributed locks
string lockKey = CacheKeyHelper.BuildLockKey("order:123:lock");
Console.WriteLine(lockKey); // "lock:order:123:lock"

// Temporary data
string tempKey = CacheKeyHelper.BuildTemporaryKey("session");
Console.WriteLine(tempKey); // "temp:session:<guid>"
```

## HealthCheckService

The `HealthCheckService` is responsible for monitoring the health of the application and its cache system. It provides diagnostics for all critical components, including the Redis connection and memory usage. The service can be used to check the overall health of the system and to determine if it is ready to handle requests.

Here is an example of how to use the `HealthCheckService`:
```csharp
var healthCheckService = new HealthCheckService(redisConnection, logger);
var healthStatus = await healthCheckService.CheckHealthAsync();
Console.WriteLine($"Overall Health: {healthStatus.Overall}");
Console.WriteLine($"Redis Connected: {healthStatus.RedisConnected}");
Console.WriteLine($"Components: {string.Join(", ", healthStatus.Components)}");
Console.WriteLine($"Issues: {string.Join(", ", healthStatus.Issues)}");
Console.WriteLine($"Checked At: {healthStatus.CheckedAt}");
var isReady = await healthCheckService.IsReadyAsync();
Console.WriteLine($"Is Ready: {isReady}");
```

## IdempotencyHelper

The `IdempotencyHelper` ensures operations are executed only once in distributed systems by preventing duplicate processing of requests with the same idempotency key. It tracks processed operations, stores results for retrieval, and automatically cleans up expired records.

Here is an example of how to use the `IdempotencyHelper`:

```csharp
using RedisCachePatterns.Utilities;

// Create idempotency helper with default 24-hour retention
var idempotencyHelper = new IdempotencyHelper();

// Check if operation was already processed
string idempotencyKey = "payment:user-123:order-456";
bool isProcessed = idempotencyHelper.IsProcessed(idempotencyKey);
Console.WriteLine($"Is processed: {isProcessed}");

// Execute operation idempotently - will only run the operation once
var result = await idempotencyHelper.ExecuteIdempotentlyAsync(
    idempotencyKey,
    async () => 
    {
        Console.WriteLine("Executing payment operation...");
        await Task.Delay(100); // Simulate payment processing
        return "Payment completed successfully";
    }
);
Console.WriteLine($"Result: {result}");

// Get cached result for already processed operation
string cachedResult = idempotencyHelper.GetResult<string>(idempotencyKey) ?? "No cached result";
Console.WriteLine($"Cached result: {cachedResult}");

// Manually mark operation as processed and store result
idempotencyHelper.MarkAsProcessed(idempotencyKey, "Manual result");

// Clean up expired records
int expiredCount = idempotencyHelper.CleanupExpiredRecords();
Console.WriteLine($"Cleaned up {expiredCount} expired records");
```

## CacheKeyBuilder

`CacheKeyBuilder` offers a collection of static helpers for constructing Redis cache keys in a consistent, colon‑delimited format. It includes a generic `BuildKey` method for arbitrary parts and specialized methods for common entities such as users, products, orders, inventory, and distributed locks.

```csharp
using RedisCachePatterns.Utilities;

// Build a generic key from arbitrary parts
string genericKey = CacheKeyBuilder.BuildKey("session", Guid.NewGuid(), "data");
Console.WriteLine(genericKey); // e.g. "session:3f2504e0-4f89-11d3-9a0c-0305e82c3301:data"

// Entity‑specific keys
string userKey = CacheKeyBuilder.User(42);
string userByUsername = CacheKeyBuilder.UserByUsername("jdoe");
string userByEmail = CacheKeyBuilder.UserByEmail("john@example.com");
string usersByRole = CacheKeyBuilder.UsersByRole("admin");

string productKey = CacheKeyBuilder.Product(1001);
string productBySku = CacheKeyBuilder.ProductBySku("SKU-12345");
string productsByCategory = CacheKeyBuilder.ProductsByCategory("electronics");
string productSearch = CacheKeyBuilder.ProductSearch("laptop");

string orderKey = CacheKeyBuilder.Order(555);
string orderByNumber = CacheKeyBuilder.OrderByNumber("ORD-2023-001");
string ordersByUser = CacheKeyBuilder.OrdersByUser(42);
string ordersByStatus = CacheKeyBuilder.OrdersByStatus("shipped");

string inventoryKey = CacheKeyBuilder.Inventory(77);
string inventoryByProductAndWarehouse = CacheKeyBuilder.InventoryByProductAndWarehouse(1001, "WH-01");
string inventoryByProduct = CacheKeyBuilder.InventoryByProduct(1001);

string lockKey = CacheKeyBuilder.DistributedLock("order:555:process");

// Pattern for scanning all product keys
string productPattern = CacheKeyBuilder.GeneratePattern("product");
Console.WriteLine(productPattern); // "product:*"
```

## PerformanceMonitor

`PerformanceMonitor` is a lightweight utility for measuring the duration of operations and aggregating timing metrics. It provides a disposable timer via `MeasureOperation` and stores per‑operation statistics such as count, total time, min/max, and average duration.

```csharp
using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RedisCachePatterns.Utilities;

// Create a logger (NullLogger used for simplicity)
ILogger<PerformanceMonitor> logger = NullLogger<PerformanceMonitor>.Instance;

// Instantiate the monitor
var monitor = new PerformanceMonitor(logger);

// Measure an operation using the disposable timer
using (monitor.MeasureOperation("CacheRead"))
{
    // Simulated work
    Thread.Sleep(30);
}

// Directly record an operation without a timer
monitor.RecordOperation("CacheWrite", 45);

// Retrieve metrics for a specific operation
var readMetrics = monitor.GetMetrics("CacheRead");
Console.WriteLine(readMetrics?.ToString());

// Enumerate all collected metrics
foreach (var m in monitor.GetAllMetrics())
{
    Console.WriteLine(m);
}

// Reset metrics for a single operation
monitor.ResetOperation("CacheRead");

// Reset all metrics
monitor.ResetMetrics();
```

## JsonHelper

The `JsonHelper` class provides utility methods for JSON serialization and deserialization with support for safe operations and validation. It includes methods for serializing objects to JSON strings, deserializing JSON strings back to objects, checking if a string is valid JSON, extracting values from JSON, and merging JSON objects.

Here is an example of how to use the `JsonHelper` class:

```csharp
using RedisCachePatterns.Utilities;

// Serialize an object to JSON
var person = new { Name = "John Doe", Age = 30, Email = "john@example.com" };
string json = JsonHelper.Serialize(person);
Console.WriteLine(json);
// {"Name":"John Doe","Age":30,"Email":"john@example.com"}

// Deserialize JSON back to an object
var deserialized = JsonHelper.Deserialize<Person>(json);
Console.WriteLine($"Name: {deserialized?.Name}, Age: {deserialized?.Age}");

// Deserialize with error handling
var safeDeserialized = JsonHelper.DeserializeSafe<Person>(json);
Console.WriteLine(safeDeserialized != null ? "Successfully deserialized" : "Failed to deserialize");

// Check if a string is valid JSON
bool isValid = JsonHelper.IsValidJson(json);
Console.WriteLine($"Is valid JSON: {isValid}");

// Extract a value from JSON
object? value = JsonHelper.GetValue(json, "Name");
Console.WriteLine($"Extracted value: {value}");

// Merge two JSON strings
string json1 = "{\"Name\":\"John\",\"Age\":30}";
string json2 = "{\"Email\":\"john@example.com\",\"City\":\"New York\"}";
string merged = JsonHelper.Merge(json1, json2);
Console.WriteLine(merged);
// {"Name":"John","Age":30,"Email":"john@example.com","City":"New York"}
```

## LoggingHelper

The `LoggingHelper` class provides utilities for structured logging with consistent formatting and context tracking. It includes methods for logging operation performance metrics, cache operations, business operations, generating correlation IDs for request tracking, and logging exceptions with automatic sanitization of sensitive data.

Here is an example of how to use the `LoggingHelper` class:

```csharp
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Utilities;

// Create a logger
ILogger<LoggingHelper> logger = loggerFactory.CreateLogger<LoggingHelper>();

// Log operation performance
LoggingHelper.LogOperationPerformance(logger, "ProcessOrders", 150, 1250);
// Output: Operation completed: ProcessOrders | Duration: 150ms | Items: 1250 | Throughput: 8333 items/sec

// Log cache operation
LoggingHelper.LogCacheOperation(logger, "Get", "user:123:profile", true, 12);
// Output: Cache operation: Get | Key: user:123:profile | Status: Success | Duration: 12ms

// Log business operation with context
var context = new Dictionary<string, object> {
    { "userId", 123 },
    { "action", "update" },
    { "result", "success" }
};
LoggingHelper.LogBusinessOperation(logger, "UserUpdate", "123", context);
// Output: Business operation: UserUpdate | Resource: 123 | Context: userId=123, action=update, result=success

// Generate correlation ID for tracking
string correlationId = LoggingHelper.GenerateCorrelationId();
Console.WriteLine(correlationId);
// Output: 20260718144235-3f2a1b4c

// Log exception with context
try {
    // Some operation that throws
} catch (Exception ex) {
    LoggingHelper.LogException(logger, ex, "OrderProcessing", new Dictionary<string, string> {
        { "apiKey", @"key-\d{8}" },
        { "password", @"[Pp]ass[Ww]ord?" }
    });
}
```

## EncryptionHelper

The `EncryptionHelper` class provides utility methods for cryptographic operations including SHA256 hashing, MD5 hashing, and data masking. It offers secure random byte and string generation for encryption keys and tokens, as well as utilities for verifying hashes and masking sensitive data in logs.

Here is an example of how to use the `EncryptionHelper` class:

```csharp
using RedisCachePatterns.Utilities;

// Hash a password using SHA256
string password = "SecurePassword123!";
string hashedPassword = EncryptionHelper.HashSha256(password);
Console.WriteLine($"Hashed password: {hashedPassword}");

// Verify a password against its hash
bool isValid = EncryptionHelper.VerifyHash("SecurePassword123!", hashedPassword);
Console.WriteLine($"Password verification: {isValid}");

// Generate MD5 hash for cache key (non-security-critical)
string cacheKey = EncryptionHelper.HashMd5("user:123:profile");
Console.WriteLine($"Cache key: {cacheKey}");

// Generate cryptographically secure random bytes for encryption
byte[] randomBytes = EncryptionHelper.GenerateRandomBytes(32);
Console.WriteLine($"Random bytes length: {randomBytes.Length}");

// Generate a random string token
string apiToken = EncryptionHelper.GenerateRandomString(64);
Console.WriteLine($"API token: {apiToken}");

// Mask sensitive data for logging
string creditCard = "4111111111111111";
string maskedCard = EncryptionHelper.MaskSensitiveData(creditCard);
Console.WriteLine($"Masked card: {maskedCard}"); // Output: "41****"

string passwordForLog = "MySecret123";
string maskedPassword = EncryptionHelper.MaskSensitiveData(passwordForLog, 3);
Console.WriteLine($"Masked password: {maskedPassword}"); // Output: "MyS******"
```

## DateTimeHelper

The `DateTimeHelper` class provides a set of static methods for consistent date and time handling across the application. It includes utilities for parsing flexible datetime strings, formatting dates in ISO 8601 format, calculating relative time differences, and managing cache expiration times.

Here is an example of how to use the `DateTimeHelper` class:

```csharp
using RedisCachePatterns.Utilities;

// Parse datetime string using multiple formats
if (DateTimeHelper.TryParseFlexible("2026-07-18T14:30:00", out DateTime parsedDate))
{
    Console.WriteLine($"Parsed date: {parsedDate}");
}

// Format datetime in ISO 8601 extended format for consistency
DateTime now = DateTime.UtcNow;
string isoDate = DateTimeHelper.FormatIso8601(now);
Console.WriteLine($"ISO 8601 date: {isoDate}");

// Calculate human-readable time difference
string relativeTime = DateTimeHelper.GetRelativeTime(parsedDate);
Console.WriteLine($"Relative time: {relativeTime}");

// Get the start and end of the day in UTC
DateTime dayStart = DateTimeHelper.GetDayStart(now);
DateTime dayEnd = DateTimeHelper.GetDayEnd(now);
Console.WriteLine($"Day start: {dayStart:O}, Day end: {dayEnd:O}");

// Calculate expiration datetime for cache TTL
DateTime expirationTime = DateTimeHelper.CalculateExpiration(TimeSpan.FromHours(1));
Console.WriteLine($"Cache expires at: {expirationTime:O}");

// Check if datetime is in the past
bool isExpired = DateTimeHelper.IsExpired(expirationTime);
Console.WriteLine($"Is expired: {isExpired}");

// Get remaining time until expiration
TimeSpan? timeRemaining = DateTimeHelper.GetTimeRemaining(expirationTime);
if (timeRemaining.HasValue)
{
    Console.WriteLine($"Time remaining: {timeRemaining.Value.TotalMinutes:F1} minutes");
}
```

## ValidationHelper

The `ValidationHelper` provides a collection of static methods for validating common input data such as usernames, emails, passwords, product details, quantities, and generic objects. Each method throws a `ValidationException` when the supplied value does not meet the defined business rules, allowing callers to catch and aggregate validation errors.

```csharp
using RedisCachePatterns.Utilities;

// Validate individual fields
ValidationHelper.ValidateUsername("john_doe");
ValidationHelper.ValidateEmail("john@example.com");
ValidationHelper.ValidatePassword("S3cureP@ss");

// Validate product data
ValidationHelper.ValidateProductName("Super Widget");
ValidationHelper.ValidatePrice(19.99m);
ValidationHelper.ValidateQuantity(5, "Stock");

// Validate generic objects
var product = new Product(); // assume a valid product instance
ValidationHelper.ValidateNotNull(product, nameof(product));
ValidationHelper.ValidateNotNullOrEmpty(product.Name, "Product Name");

// Collect validation errors from a delegate
var errors = ValidationHelper.GetValidationErrors(() =>
{
    ValidationHelper.ValidateUsername("");
    ValidationHelper.ValidateEmail("invalid-email");
});
```

## DistributedLockHelper

The `DistributedLockHelper` class simplifies acquiring a Redis‑based distributed lock, automatically renewing it while held, and guaranteeing its release even when exceptions occur. It implements both `IDisposable` and `IAsyncDisposable`, allowing use with `using` or `await using` blocks.

```csharp
using RedisCachePatterns.Utilities;

// Assume an ICacheService implementation is available as `cacheService`
await using var lockHelper = new DistributedLockHelper(cacheService, "order:123:lock");

// Acquire the lock manually
if (await lockHelper.AcquireAsync())
{
    try
    {
        // Critical section
        await DoWorkAsync();
    }
    finally
    {
        await lockHelper.ReleaseAsync();
    }
}

// Or execute an action with automatic acquire/release
await lockHelper.ExecuteAsync(async () =>
{
    await DoWorkAsync();
});

// Execute a function that returns a result
var result = await lockHelper.ExecuteAsync(async () =>
{
    return await ComputeResultAsync();
});
```

