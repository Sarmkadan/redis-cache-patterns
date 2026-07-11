# RedisCacheService

`RedisCacheService` is the central abstraction for interacting with a Redis-backed cache in the `redis-cache-patterns` project. It provides asynchronous operations for reading, writing, and removing cache entries, supports multiple expiration strategies (absolute, sliding, early expiration), offers distributed locking primitives, and exposes diagnostic capabilities such as key scanning, statistics retrieval, and policy inspection. The service is designed to encapsulate common caching patterns while allowing callers to configure behaviour on a per-key or global basis through cache policies.

## API

### Constructors

#### `public RedisCacheService`
Creates a new instance of the service. The underlying Redis connection and serialization settings are expected to be injected or configured at construction time. Details of constructor parameters are implementation-specific and not part of the public documented surface beyond the presence of a public constructor.

### Read Operations

#### `public async Task<T?> GetOrLoadAsync<T>(...)`
Retrieves a value from the cache by key. If the key is absent, invokes a caller-supplied factory to produce the value, stores it in the cache according to the currently active policy, and returns it. Returns `null` when the factory itself returns `null` or when the cached value is a null sentinel. Throws if the factory throws or if the underlying Redis connection fails irrecoverably.

#### `public async Task<T?> GetOrLoadWithSlidingExpirationAsync<T>(...)`
Behaves like `GetOrLoadAsync<T>` but explicitly enforces a sliding expiration window: each cache hit resets the key’s time-to-live to the configured sliding interval. Useful for data that should remain cached only while actively accessed. Throws under the same conditions as `GetOrLoadAsync<T>`.

#### `public async Task<T?> GetOrLoadWithEarlyExpirationAsync<T>(...)`
Retrieves a value from the cache. When the key’s remaining time-to-live drops below a configured early-expiration threshold, the service returns the stale cached value immediately while asynchronously refreshing the entry in the background. This prevents cache misses under load for frequently accessed keys nearing expiration. Throws if the factory fails during the background refresh (the exception may be surfaced on a subsequent call) or if Redis connectivity is lost.

#### `public async Task<T?> GetAsync<T>(...)`
Reads a value directly from the cache without invoking any factory. Returns the deserialized value, or `null` if the key does not exist. Does not modify expiration. Throws on serialization errors or Redis failures.

### Write Operations

#### `public async Task SetAsync<T>(...)`
Places a value into the cache at the specified key, overwriting any existing entry. Expiration behaviour is governed by the policy in effect at call time. Returns a completed task on success. Throws if serialization fails or the Redis command is rejected.

#### `public async Task<T> WriteAsync<T>(...)`
Writes a value to the cache and returns the same value to the caller, enabling fluent composition where the written object is needed immediately. Expiration and policy handling are identical to `SetAsync<T>`. Throws under the same conditions as `SetAsync<T>`.

### Remove Operations

#### `public async Task RemoveAsync(...)`
Removes a single key from the cache. Succeeds silently if the key does not exist. Throws on Redis command failures.

#### `public async Task RemoveByPatternAsync(...)`
Removes all keys matching a glob-style pattern (e.g., `user:*:tokens`). The implementation typically scans the keyspace and issues batch deletions. Throws if the pattern is invalid or Redis becomes unavailable during the scan.

#### `public async Task FlushAsync(...)`
Removes all keys from the cache database. Equivalent to a full cache flush. Throws if the Redis server rejects the command or connectivity is lost.

### Existence and Expiration

#### `public async Task<bool> ExistsAsync(...)`
Returns `true` if the specified key currently exists in the cache, `false` otherwise. Throws on Redis connectivity errors.

#### `public async Task<TimeSpan?> GetExpirationAsync(...)`
Returns the remaining time-to-live for a key, or `null` if the key does not exist or has no associated expiration. Throws on Redis command failures.

### Locking

#### `public async Task<bool> AcquireLockAsync(...)`
Attempts to acquire a distributed lock identified by a resource key. Returns `true` if the lock was successfully acquired, `false` if another holder already owns it. The lock is typically implemented using `SETNX` semantics with an automatic expiry to prevent deadlocks. Throws on Redis failures.

#### `public async Task<bool> ReleaseLockAsync(...)`
Releases a distributed lock previously acquired by this instance. Returns `true` if the lock was released successfully, `false` if the lock was not held (e.g., already expired or owned by another party). Throws on Redis failures.

#### `public async Task<bool> RenewLockAsync(...)`
Extends the expiry of an existing lock without releasing and re-acquiring it. Returns `true` if the renewal succeeded, `false` if the lock is no longer held. Throws on Redis failures.

### Diagnostics and Administration

#### `public async Task<IEnumerable<string>> GetKeysByPatternAsync(...)`
Returns a collection of keys matching a glob-style pattern. The underlying implementation uses the Redis `SCAN` command to avoid blocking the server. Throws if the pattern is malformed or Redis is unreachable.

#### `public async Task<CacheStatistics> GetStatisticsAsync(...)`
Gathers and returns a `CacheStatistics` object containing metrics such as hit rate, miss rate, total keys, and average time-to-live. The exact fields depend on the `CacheStatistics` type definition. Throws if statistics cannot be computed due to Redis unavailability.

#### `public ValueTask SetPolicyAsync(...)`
Associates a `CachePolicy` with a specific key or key pattern, controlling expiration duration, sliding behaviour, early-expiration thresholds, and other caching strategies for subsequent operations on matching keys. Returns a completed `ValueTask`. Throws if the policy object is invalid.

#### `public ValueTask<CachePolicy?> GetPolicyAsync(...)`
Retrieves the `CachePolicy` currently associated with a key or pattern, or `null` if no explicit policy has been set. Returns a completed `ValueTask` wrapping the result.

#### `public async Task<CacheKeyMetadata?> GetKeyMetadataAsync(...)`
Returns metadata about a cached key, such as creation timestamp, last access time, expiration details, and size estimate, or `null` if the key does not exist. Throws on Redis failures.

## Usage

### Example 1: Read-through with sliding expiration and early refresh

```csharp
var cache = new RedisCacheService(connectionMultiplexer, serializer);

// Configure a policy with sliding expiration and early refresh 30 seconds before expiry.
var policy = new CachePolicy
{
    SlidingExpiration = TimeSpan.FromMinutes(10),
    EarlyExpirationThreshold = TimeSpan.FromSeconds(30)
};
await cache.SetPolicyAsync("product:*", policy);

// Retrieve a product, loading from the database on miss.
Product? product = await cache.GetOrLoadWithEarlyExpirationAsync<Product>(
    "product:4201",
    async () => await dbContext.Products.FindAsync(4201)
);

// Subsequent calls within the sliding window reset the TTL.
// When TTL drops below 30 seconds, the stale value is returned while a background refresh occurs.
```

### Example 2: Distributed lock for a critical section

```csharp
var cache = new RedisCacheService(connectionMultiplexer, serializer);
string lockKey = "locks:order-processing:789";

bool acquired = await cache.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(30));
if (!acquired)
{
    // Another process is already handling this order.
    return;
}

try
{
    // Process the order with exclusive access.
    await ProcessOrderAsync(789);

    // Extend the lock if processing takes longer than expected.
    await cache.RenewLockAsync(lockKey, TimeSpan.FromSeconds(30));
}
finally
{
    await cache.ReleaseLockAsync(lockKey);
}
```

## Notes

- **Thread safety:** All public methods are asynchronous and designed to be called concurrently from multiple threads. The underlying Redis client multiplexer is itself thread-safe. However, composite operations (e.g., check-then-act patterns using `ExistsAsync` followed by `GetOrLoadAsync`) are not atomic across calls; use the distributed locking primitives when strict consistency is required.
- **Null handling:** `GetOrLoadAsync<T>` and its variants treat a `null` factory result as a valid cache value. A null sentinel is stored to distinguish “cached null” from “key absent”. `GetAsync<T>` returns `null` for both missing keys and cached nulls.
- **Early expiration:** `GetOrLoadWithEarlyExpirationAsync<T>` may return a stale value while a background refresh is in flight. Callers must tolerate slightly outdated data when using this pattern. If the background refresh fails, the stale entry remains until its absolute expiration, and the exception may be observed on the next invocation.
- **Lock expiry:** Locks acquired via `AcquireLockAsync` are volatile and will auto-expire if not explicitly released or renewed. Always use a `try/finally` block to release locks, and set the lock timeout longer than the expected operation duration.
- **Pattern operations:** `RemoveByPatternAsync` and `GetKeysByPatternAsync` use server-side scanning and may not reflect keys written during the scan. They are suitable for administrative tasks but should not be relied upon for precise real-time consistency.
- **Statistics:** The `CacheStatistics` object returned by `GetStatisticsAsync` reflects a point-in-time snapshot. Metrics such as hit rate depend on internal counters that may have been reset or may wrap depending on the implementation.
