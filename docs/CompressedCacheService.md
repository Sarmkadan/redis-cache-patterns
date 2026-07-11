# CompressedCacheService

The `CompressedCacheService` wraps a Redis database and transparently compresses cached payloads before storage, decompressing them on retrieval. It provides a type‑safe, asynchronous API for common cache‑aside patterns, sliding and early expiration policies, distributed locking, policy management, and key enumeration while keeping the underlying compression details hidden from callers.

## API

### GetOrLoadAsync<T>
**Purpose** – Retrieves a value of type `T` from the cache; if the entry is missing or expired, invokes the supplied loader delegate to obtain the value, stores it, and returns it.  
**Parameters**  
- `key` – Cache key used to identify the entry.  
- `loader` – Async function that produces the value when the cache miss occurs.  
- `expiration` *(optional)* – Absolute lifetime to assign to the newly cached entry.  
- `policy` *(optional)* – Cache policy overriding defaults for this entry.  
- `cancellationToken` *(optional)* – Token to observe for cancellation.  
**Return** – `Task<T?>` yielding the cached value, or `null` if the loader returns `null` and the cache does not store nulls.  
**Exceptions** –  
- `ArgumentNullException` if `key` or `loader` is `null`.  
- `InvalidOperationException` if serialization or compression of the value fails.  
- `RedisException` (or derived) if the underlying Redis connection fails.  

### GetAsync<T>
**Purpose** – Attempts to read a cached value of type `T` without invoking a fallback loader.  
**Parameters**  
- `key` – Cache key to read.  
- `cancellationToken` *(optional)* – Cancellation token.  
**Return** – `Task<T?>` with the deserialized value, or `null` if the key does not exist or is expired.  
**Exceptions** – Same as `GetOrLoadAsync<T>` for argument validation, serialization errors, and Redis connectivity issues.

### SetAsync<T>
**Purpose** – Inserts or replaces a value of type `T` in the cache with optional expiration and policy.  
**Parameters**  
- `key` – Cache key.  
- `value` – Object to store; may be `null` depending on the serializer configuration.  
- `expiration` *(optional)* – Absolute lifetime for the entry.  
- `policy` *(optional)* – Cache policy to apply.  
- `cancellationToken` *(optional)* – Cancellation token.  
**Return** – `Task` completing when the operation is persisted to Redis.  
**Exceptions** –  
- `ArgumentNullException` if `key` is `null`.  
- `InvalidOperationException` on serialization/compression failure.  
- `RedisException` for transport problems.

### WriteAsync<T>
**Purpose** – Updates an existing cached value by applying an async transformation function; returns the updated value.  
**Parameters**  
- `key` – Cache key of the entry to modify.  
- `updater` – Async function receiving the current value (or `default(T)` if missing) and returning the new value.  
- `expiration` *(optional)* – Lifetime to assign after the write.  
- `cancellationToken` *(optional)* – Cancellation token.  
**Return** – `Task<T>` with the value resulting from the updater.  
**Exceptions** –  
- `ArgumentNullException` if `key` or `updater` is `null`.  
- `InvalidOperationException` if serialization/compression fails.  
- `RedisException` on Redis errors.

### GetOrLoadWithSlidingExpirationAsync<T>
**Purpose** – Like `GetOrLoadAsync<T>` but applies a sliding expiration that is renewed on each successful read.  
**Parameters**  
- `key` – Cache key.  
- `loader` – Async factory for the value on miss.  
- `slidingExpiration` – Time span after which the entry expires if not accessed.  
- `cancellationToken` *(optional)* – Cancellation token.  
**Return** – `Task<T?>` with the cached or newly loaded value.  
**Exceptions** – Same as `GetOrLoadAsync<T>` plus `ArgumentOutOfRangeException` if `slidingExpiration` is zero or negative.

### RemoveAsync
**Purpose** – Deletes a single key from the cache.  
**Parameters**  
- `key` – Cache key to remove.  
- `cancellationToken` *(optional)* – Cancellation token.  
**Return** – `Task` completing when the deletion is acknowledged.  
**Exceptions** –  
- `ArgumentNullException` if `key` is `null`.  
- `RedisException` on failure.

### RemoveByPatternAsync
**Purpose** – Deletes all keys matching a glob‑style pattern (e.g., `"user:*"`).  
**Parameters**  
- `pattern` – Pattern to match keys against.  
- `cancellationToken` *(optional)* – Cancellation token.  
**Return** – `Task` completing when the bulk removal finishes.  
**Exceptions** –  
- `ArgumentNullException` if `pattern` is `null`.  
- `RedisException` on transport errors.

### ExistsAsync
**Purpose** – Checks whether a key is present and not expired.  
**Parameters**  
- `key` – Cache key to test.  
- `cancellationToken` *(optional)* – Cancellation token.  
**Return** – `Task<bool>` – `true` if the key exists, otherwise `false`.  
**Exceptions** –  
- `ArgumentNullException` if `key` is `null`.  
- `RedisException` on communication problems.

### GetExpirationAsync
**Purpose** – Retrieves the remaining time‑to‑live for a key, if any.  
**Parameters**  
- `key` – Cache key to inspect.  
- `cancellationToken` *(optional)* – Cancellation token.  
**Return** – `Task<TimeSpan?>` – the TTL, or `null` if the key does not exist or is persistent.  
**Exceptions** –  
- `ArgumentNullException` if `key` is `null`.  
- `RedisException` on failure.

### AcquireLockAsync
**Purpose** – Attempts to obtain a distributed lock identified by `resource`.  
**Parameters**  
- `resource` – Lock identifier (often a cache key).  
- `expiry` *(optional)* – Maximum time the lock should be held; if `null` a server‑side default is used.  
- `cancellationToken` *(optional)* – Cancellation token.  
**Return** – `Task<bool>` – `true` if the lock was acquired, `false` otherwise.  
**Exceptions** –  
- `ArgumentNullException` if `resource` is `null`.  
- `RedisException` on Redis errors.

### ReleaseLockAsync
**Purpose** – Releases a previously acquired lock, requiring the exact lock value returned by `AcquireLockAsync`.  
**Parameters**  
- `resource` – Lock identifier.  
- `lockValue` – The opaque token that represents the held lock.  
- `cancellationToken` *(optional)* – Cancellation token.  
**Return** – `Task<bool>` – `true` if the lock was released, `false` if the value did not match.  
**Exceptions** –  
- `ArgumentNullException` if `resource` or `lockValue` is `null`.  
- `RedisException` on failure.

### RenewLockAsync
**Purpose** – Extends the expiry of an existing lock without releasing it.  
**Parameters**  
- `resource` – Lock identifier.  
- `lockValue` – Current lock token.  
- `expiry` *(optional)* – New expiry interval; if `null` the existing expiry is refreshed to the server default.  
- `cancellationToken` *(optional)* – Cancellation token.  
**Return** – `Task<bool>` – `true` if the expiry was updated, `false` if the lock is not held or the token mismatches.  
**Exceptions** – Same as `ReleaseLockAsync`.

### GetKeysByPatternAsync
**Purpose** – Enumerates keys that match a supplied pattern.  
**Parameters**  
- `pattern` – Glob‑style pattern to match.  
- `cancellationToken` *(optional)* – Cancellation token.  
**Return** – `Task<IEnumerable<string>>` containing the matching keys.  
**Exceptions** –  
- `ArgumentNullException` if `pattern` is `null`.  
- `RedisException` on transport errors.

### FlushAsync
**Purpose** – Removes all keys from the selected Redis database (use with caution).  
**Parameters**  
- `cancellationToken` *(optional)* – Cancellation token.  
**Return** – `Task` completing when the flush operation is acknowledged.  
**Exceptions** – `RedisException` on failure.

### GetStatisticsAsync
**Purpose** – Retrieves diagnostic counters (hits, misses, memory usage, etc.) from the cache layer.  
**Parameters**  
- `cancellationToken` *(optional)* – Cancellation token.  
**Return** – `Task<CacheStatistics>` containing the current metrics.  
**Exceptions** – `RedisException` if the stats cannot be fetched.

### SetPolicyAsync
**Purpose** – Associates a `Domain.CachePolicy` with a specific key, overriding global defaults for that entry.  
**Parameters**  
- `key` – Cache key to which the policy applies.  
- `policy` – Policy instance to store; `null` clears any per‑key policy.  
- `cancellationToken` *(optional)* – Cancellation token.  
**Return** – `ValueTask` completing when the policy is persisted.  
**Exceptions** –  
- `ArgumentNullException` if `key` is `null`.  
- `RedisException` on failure.

### GetPolicyAsync
**Purpose** – Retrieves the per‑key cache policy, if one has been set.  
**Parameters**  
- `key` – Cache key to query.  
- `cancellationToken` *(optional)* – Cancellation token.  
**Return** – `ValueTask<Domain.CachePolicy?>` – the policy or `null` when none is defined.  
**Exceptions** –  
- `ArgumentNullException` if `key` is `null`.  
- `RedisException` on failure.

### GetOrLoadWithEarlyExpirationAsync<T>
**Purpose** – Loads a value on miss and assigns an *early* expiration that is shorter than the normal TTL, useful for hot‑data pre‑warming.  
**Parameters**  
- `key` – Cache key.  
- `loader` – Async factory for the value on miss.  
- `earlyExpiration` – Time span after which the entry is considered stale (but may still be served until the regular expiry).  
- `cancellationToken` *(optional)* – Cancellation token.  
**Return** – `Task<T?>` with the cached or freshly loaded value.  
**Exceptions** – Same as `GetOrLoadAsync<T>` plus `ArgumentOutOfRangeException` if `earlyExpiration` is zero or negative.

### GetKeyMetadataAsync
**Purpose** – Returns auxiliary information about a key such as creation time, TTL, policy, and compression ratio.  
**Parameters**  
- `key` – Cache key to inspect.  
- `cancellationToken` *(optional)* – Cancellation token.  
**Return** – `Task<Domain.CacheKeyMetadata?>` – metadata struct or `null` if the key does not exist.  
**Exceptions** –  
- `ArgumentNullException` if `key` is `null`.  
- `RedisException` on failure.

## Usage

### Example 1: Cache‑aside retrieval with fallback loader
```csharp
public async Task<UserProfile?> GetUserProfileAsync(string userId)
{
    // The service will compress the profile before storing it.
    return await _cache.GetOrLoadAsync<UserProfile>(
        key: $"user:{userId}",
        loader: async () =>
        {
            // Simulate a database call.
            return await _userRepository.FindByIdAsync(userId);
        },
        expiration: TimeSpan.FromHours(1)); // cache for one hour
}
```

### Example 2: Distributed lock for updating a counter
```csharp
public async Task IncrementCounterAsync(string counterKey)
{
    const string lockResource = $"lock:{counterKey}";
    bool lockTaken = false;

    try
    {
        lockTaken = await _cache.AcquireLockAsync(
            resource: lockResource,
            expiry: TimeSpan.FromSeconds(15));

        if (!lockTaken)
            throw new InvalidOperationException("Unable to acquire lock.");

        // Retrieve, increment, and store the counter atomically.
        var current = await _cache.GetAsync<int>(counterKey) ?? 0;
        await _cache.SetAsync(counterKey, current + 1);
    }
    finally
    {
        if (lockTaken)
        {
            // The lock value is opaque; we ignore it here because ReleaseLockAsync
            // only needs the resource and the value returned by AcquireLockAsync.
            // In a real implementation you would capture the token from AcquireLockAsync.
            await _cache.ReleaseLockAsync(lockResource, lockValue: /* captured token */);
        }
    }
}
```

## Notes
- All methods are safe for concurrent invocation; the service holds no mutable state beyond the underlying Redis connection.  
- Null keys or mandatory delegates (`loader`, `updater`) result in `ArgumentNullException`.  
- Serialization errors (e.g., unsupported types, exceeding the compressor’s input limits) surface as `InvalidOperationException`.  
- Network‑level failures raise `RedisException` (or a derived type); callers should consider retry policies where appropriate.  
- `FlushAsync` affects the entire selected database; use it only in controlled environments such as test suites.  
- Locking methods require the exact token returned by `AcquireLockAsync` to release or renew the lock; using an incorrect value will cause the operation to return `false` without throwing.  
- Expiration-related arguments (`expiration`, `slidingExpiration`, `earlyExpiration`) must be positive `TimeSpan` values; zero or negative spans trigger `ArgumentOutOfRangeException`.  
- The service does not enforce a specific serializer; however, the generic type `T` must be compatible with the configured serializer (commonly `System.Text.Json`).  
- Per‑key policies set via `SetPolicyAsync` override global defaults for that key only; clearing a policy is achieved by passing `null`.  
- Pattern‑based operations (`RemoveByPatternAsync`, `GetKeysByPatternAsync`) rely on Redis’ `KEYS` or `SCAN` internals; avoid using them on large production datasets during peak traffic.
