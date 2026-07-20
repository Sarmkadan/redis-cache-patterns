# StampedeProtectedCacheService

Provides a caching layer over Redis that mitigates cache stampede effects. When multiple concurrent requests attempt to load the same key, only one request performs the underlying load operation; the others wait for that result. The service supports multiple expiration strategies, distributed locking, cache policy management, and key metadata tracking.

## API

### `StampedeProtectedCacheService(IConnectionMultiplexer connection, IOptions<CacheOptions> options)`
Initializes a new instance with a Redis connection and configuration options.

**Parameters**  
- `connection` – A Redis `IConnectionMultiplexer` instance.  
- `options` – Cache options including default expiration, lock timeout, and serialization settings.

**Throws**  
- `ArgumentNullException` – if `connection` or `options` is `null`.

---

### `async Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T>> loadFactory, CachePolicy? policy = null)`
Returns the cached value for `key`. If the key does not exist or has expired, invokes `loadFactory` to produce a value, stores it with the specified or default policy, and returns the value. Only one concurrent caller per key executes the factory; others await the result.

**Type Parameters**  
- `T` – The type of the cached value.

**Parameters**  
- `key` – The cache key.  
- `loadFactory` – An asynchronous delegate that produces the value when a cache miss occurs.  
- `policy` – Optional cache policy (expiration, priority). If `null`, the default policy from configuration is used.

**Returns**  
- The cached value, or `default(T)` if the factory returns `null` and caching of null values is disabled.

**Throws**  
- `ArgumentNullException` – if `key` or `loadFactory` is `null`.  
- `RedisConnectionException` – if the Redis server is unreachable.  
- `OperationCanceledException` – if the operation is cancelled via the cancellation token (not shown, but supported internally).

---

### `async Task<T?> GetOrLoadWithSlidingExpirationAsync<T>(string key, Func<Task<T>> loadFactory, TimeSpan slidingWindow)`
Same as `GetOrLoadAsync` but uses sliding expiration: the key’s TTL is reset on every read. The `slidingWindow` parameter defines the maximum idle time before the entry expires.

**Parameters**  
- `key` – The cache key.  
- `loadFactory` – Factory to produce the value on miss.  
- `slidingWindow` – The sliding expiration interval.

**Throws**  
- `ArgumentNullException` – if `key` or `loadFactory` is `null`.  
- `ArgumentOutOfRangeException` – if `slidingWindow` is less than or equal to `TimeSpan.Zero`.

---

### `async Task<T?> GetOrLoadWithEarlyExpirationAsync<T>(string key, Func<Task<T>> loadFactory, TimeSpan absoluteExpiration, double earlyExpirationFactor = 0.2)`
Uses probabilistic early expiration to reduce stampede risk. The entry is considered stale after `absoluteExpiration * (1 - earlyExpirationFactor)` and a background refresh is triggered if the entry is accessed after that point. The factory is invoked asynchronously without blocking the caller if the entry is still valid.

**Parameters**  
- `key` – The cache key.  
- `loadFactory` – Factory to produce the value.  
- `absoluteExpiration` – The absolute TTL from the time of creation.  
- `earlyExpirationFactor` – Fraction (0–1) of the TTL after which early refresh is attempted. Default 0.2.

**Throws**  
- `ArgumentNullException` – if `key` or `loadFactory` is `null`.  
- `ArgumentOutOfRangeException` – if `absoluteExpiration` is not positive, or `earlyExpirationFactor` is outside [0,1].

---

### `async Task<T?> GetAsync<T>(string key)`
Retrieves the cached value for `key` without attempting to load it if missing.

**Returns**  
- The cached value, or `default(T)` if the key does not exist.

**Throws**  
- `ArgumentNullException` – if `key` is `null`.

---

### `async Task SetAsync<T>(string key, T value, CachePolicy? policy = null)`
Stores `value` under `key` with the specified or default policy. Overwrites any existing entry.

**Throws**  
- `ArgumentNullException` – if `key` is `null`.

---

### `async Task<T> WriteAsync<T>(string key, T value, CachePolicy? policy = null)`
Stores `value` under `key` and returns the same value. Useful for fluent chaining.

**Returns**  
- The `value` that was written.

**Throws**  
- `ArgumentNullException` – if `key` is `null`.

---

### `async Task RemoveAsync(string key)`
Deletes the cache entry for `key`.

**Throws**  
- `ArgumentNullException` – if `key` is `null`.

---

### `async Task RemoveByPatternAsync(string pattern)`
Deletes all keys matching the Redis glob-style `pattern` (e.g., `user:*`). Uses `SCAN` to avoid blocking the server.

**Throws**  
- `ArgumentNullException` – if `pattern` is `null`.  
- `RedisConnectionException` – if the scan operation fails.

---

### `async Task<bool> ExistsAsync(string key)`
Checks whether `key` exists in the cache.

**Returns**  
- `true` if the key exists and has not expired; otherwise `false`.

**Throws**  
- `ArgumentNullException` – if `key` is `null`.

---

### `async Task<TimeSpan?> GetExpirationAsync(string key)`
Returns the remaining time-to-live for `key`, or `null` if the key does not exist or has no expiration.

**Throws**  
- `ArgumentNullException` – if `key` is `null`.

---

### `async Task<bool> AcquireLockAsync(string lockKey, string lockValue, TimeSpan expiry)`
Attempts to acquire a distributed lock identified by `lockKey` with the given `lockValue` (used for ownership verification). The lock expires after `expiry`.

**Returns**  
- `true` if the lock was acquired; `false` if it is already held by another owner.

**Throws**  
- `ArgumentNullException` – if `lockKey` or `lockValue` is `null`.  
- `ArgumentOutOfRangeException` – if `expiry` is not positive.

---

### `async Task<bool> ReleaseLockAsync(string lockKey, string lockValue)`
Releases the lock identified by `lockKey` only if the current `lockValue` matches the stored value (ensures only the owner can release).

**Returns**  
- `true` if the lock was released; `false` if the lock was not held or the value did not match.

**Throws**  
- `ArgumentNullException` – if `lockKey` or `lockValue` is `null`.

---

### `async Task<bool> RenewLockAsync(string lockKey, string lockValue, TimeSpan newExpiry)`
Extends the TTL of an existing lock if the `lockValue` matches the stored value.

**Returns**  
- `true` if the lock was renewed; `false` if the lock does not exist or the value does not match.

**Throws**  
- `ArgumentNullException` – if `lockKey` or `lockValue` is `null`.  
- `ArgumentOutOfRangeException` – if `newExpiry` is not positive.

---

### `async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern)`
Returns all keys matching the given glob pattern. Uses `SCAN` and may return a large enumeration.

**Throws**  
- `ArgumentNullException` – if `pattern` is `null`.

---

### `async Task FlushAsync()`
Removes all cache entries from the Redis database used by this service. Use with caution.

**Throws**  
- `RedisConnectionException` – if the `FLUSHDB` command fails.

---

### `async Task<CacheStatistics> GetStatisticsAsync()`
Returns a snapshot of cache performance counters: total requests, hits, misses, average load duration, and lock contention counts.

**Throws**  
- `InvalidOperationException` – if statistics collection is not enabled in configuration.

---

### `ValueTask SetPolicyAsync(string key, CachePolicy policy)`
Associates a `CachePolicy` with `key` without modifying the cached value. The policy takes effect on the next read or write.

**Throws**  
- `ArgumentNullException` – if `key` or `policy` is `null`.

---

### `ValueTask<CachePolicy?> GetPolicyAsync(string key)`
Retrieves the current cache policy for `key`, or `null` if no policy has been explicitly set.

**Throws**  
- `ArgumentNullException` – if `key` is `null`.

---

### `async Task<CacheKeyMetadata?> GetKeyMetadataAsync(string key)`
Returns metadata for `key` (creation time, last access time, number of accesses, lock state) if tracking is enabled.

**Throws**  
- `ArgumentNullException` – if `key` is `null`.

---

## Usage

### Example 1: Basic stampede-protected loading with sliding expiration

```csharp
public class ProductService
{
    private readonly StampedeProtectedCacheService _cache;

    public ProductService(StampedeProtectedCacheService cache)
    {
        _cache = cache;
    }

    public async Task<Product> GetProductAsync(int productId)
    {
        var key = $"product:{productId}";
        return await _cache.GetOrLoadWithSlidingExpirationAsync(
            key,
            async () => await LoadProductFromDatabase(productId),
            TimeSpan.FromMinutes(5)
        );
    }

    private async Task<Product> LoadProductFromDatabase(int productId)
    {
        // Simulate expensive database call
        await Task.Delay(200);
        return new Product { Id = productId, Name = "Sample" };
    }
}
```

### Example 2: Distributed locking for a critical section

```csharp
public class OrderService
{
    private readonly StampedeProtectedCacheService _cache;

    public OrderService(StampedeProtectedCacheService cache)
    {
        _cache = cache;
    }

    public async Task<bool> ProcessOrderAsync(string orderId)
    {
        var lockKey = $"lock:order:{orderId}";
        var lockValue = Guid.NewGuid().ToString();
        var acquired = await _cache.AcquireLockAsync(lockKey, lockValue, TimeSpan.FromSeconds(10));

        if (!acquired)
            return false; // Another instance is processing this order

        try
        {
            // Critical section – only one instance executes this
            var order = await _cache.GetOrLoadAsync(orderId, () => FetchOrderAsync(orderId));
            // ... process order ...
            await _cache.SetAsync(orderId, order);
            return true;
        }
        finally
        {
            await _cache.ReleaseLockAsync(lockKey, lockValue);
        }
    }

    private async Task<Order> FetchOrderAsync(string orderId) => new Order();
}
```

## Notes

- **Thread safety**: All public methods are thread-safe. The service uses Redis atomic operations and internal synchronization for local state. Multiple threads or processes can safely call methods concurrently.
- **Null values**: By default, `null` return values from factories are not cached. This behavior can be overridden in `CacheOptions`.
- **Lock ownership**: `ReleaseLockAsync` and `RenewLockAsync` require the exact `lockValue` that was used to acquire the lock. If the lock expires or is released by another owner, these methods return `false`.
- **Pattern removal**: `RemoveByPatternAsync` uses `SCAN` and may not be atomic. Keys added during the scan may be missed. For large datasets, consider batching or using Redis `UNLINK`.
- **Early expiration**: The probabilistic early expiration mechanism does not guarantee that a stale value is never returned; it only reduces the probability of a stampede. The factory may be invoked multiple times if the early refresh window is very short.
- **Statistics**: `GetStatisticsAsync` returns data from in-memory counters. Counters are reset when the service instance is created. They are not persisted across restarts.
- **Policy changes**: `SetPolicyAsync` only affects future reads/writes. Existing cached entries retain their original expiration until they are refreshed or overwritten.
- **Key metadata**: Metadata tracking is opt-in and adds overhead. Enable it only when needed.
