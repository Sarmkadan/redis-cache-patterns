# CircuitBreakerCacheService

`CircuitBreakerCacheService` is a thread-safe decorator for any `ICacheService` implementation. It tracks consecutive `CacheException` failures and temporarily bypasses the wrapped cache after the configured threshold is reached. Reads fail open with an empty result, writes become no-ops, and operations that have a backing-store delegate invoke that delegate directly while the circuit is open.

## Circuit states

- `Closed` – Calls are delegated to the inner cache. A successful call resets `ConsecutiveFailures`; a `CacheException` increments it.
- `Open` – Cache calls are bypassed for `BreakDuration`. Each operation returns its documented fail-open result instead.
- `HalfOpen` – Entered lazily by the next operation after the break duration has elapsed. A successful cache call closes the circuit, while a `CacheException` opens it again.

State changes and counters are protected by an internal lock. The cooldown is evaluated when an operation calls the service; reading `State` alone does not trigger a transition from `Open` to `HalfOpen`.

Only `CacheException` failures from the wrapped cache are counted. Those exceptions are rethrown after the failure is recorded. Other exception types propagate without changing the breaker state. When the circuit is open, exceptions from a directly invoked loader or persistence delegate also propagate without changing the breaker state.

## API

### `CircuitBreakerCacheService(ICacheService inner, int failureThreshold = 5, TimeSpan? breakDuration = null, ILogger<CircuitBreakerCacheService>? logger = null)`

Creates a circuit-breaker decorator around `inner`. The default threshold is five consecutive failures and the default break duration is 30 seconds. Logging is optional.

**Throws**

- `ArgumentNullException` if `inner` is `null`.
- `ArgumentOutOfRangeException` if `failureThreshold` is not positive.

### Properties

- `FailureThreshold` – Number of consecutive `CacheException` failures required to open the circuit.
- `BreakDuration` – Time the circuit remains open before an operation may move it to half-open.
- `State` – Current `CacheCircuitState` (`Closed`, `Open`, or `HalfOpen`).
- `ConsecutiveFailures` – Failure count since the last successful closed or half-open cache call.
- `OpenedAtUtc` – UTC timestamp at which the circuit most recently opened, or `null` after it closes or is reset.

### `GetOrLoadAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan? expiration = null)`

Delegates cache-aside retrieval to the inner service. When the circuit is open, skips the cache and invokes `loadFn` directly.

**Throws**

- `ArgumentNullException` if `key` or `loadFn` is `null`.

### `GetAsync<T>(string key)`

Retrieves a cached value through the inner service. Returns `default(T)` without calling the cache while the circuit is open.

### `SetAsync<T>(string key, T value, TimeSpan? expiration = null)`

Stores a value through the inner service. Becomes a completed no-op while the circuit is open.

### `RemoveAsync(string key)`

Removes an exact cache key. Becomes a completed no-op while the circuit is open.

### `GetWithSlidingExpirationAsync<T>(string key, TimeSpan slidingExpiration)`

Retrieves a value and refreshes its TTL through the inner service. Returns `default(T)` while the circuit is open.

### `GetOrLoadWithSlidingExpirationAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan slidingExpiration)`

Performs cache-aside retrieval with sliding expiration. When the circuit is open, bypasses the cache and invokes `loadFn` directly.

### `GetOrLoadWithEarlyExpirationAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan expiration, double beta = 1.0)`

Delegates probabilistic early-expiration handling to the inner service. When the circuit is open, bypasses the cache and invokes `loadFn` directly.

### `WriteAsync<T>(string key, T value, Func<Task<T>> persistFn, TimeSpan? expiration = null)`

Delegates the write-through operation to the inner service. When the circuit is open, skips the cache and returns the result of `persistFn`.

**Throws**

- `ArgumentNullException` if `key`, `value`, or `persistFn` is `null`.

### `ExistsAsync(string key)`

Checks whether a key exists. Returns `false` while the circuit is open.

### `GetExpirationAsync(string key)`

Returns a key's remaining TTL. Returns `null` while the circuit is open.

### `RemoveByPatternAsync(string pattern)`

Removes keys matching the supplied pattern through the inner service. Becomes a completed no-op while the circuit is open.

### `GetKeysByPatternAsync(string pattern)`

Returns keys matching the supplied pattern. Returns an empty sequence while the circuit is open.

### `GetManyAsync<T>(IEnumerable<string> keys)`

Retrieves multiple values through the inner service. Returns an empty dictionary while the circuit is open.

### `GetKeyMetadataAsync(string key)`

Retrieves metadata associated with a cache key. Returns `null` while the circuit is open.

### `AcquireLockAsync(string lockKey, string lockValue, TimeSpan duration)`

Attempts to acquire a distributed lock through the inner service. Returns `false` while the circuit is open.

### `ReleaseLockAsync(string lockKey, string lockValue)`

Attempts to release a distributed lock through the inner service. Returns `false` while the circuit is open.

### `RenewLockAsync(string lockKey, string lockValue, TimeSpan newDuration)`

Attempts to extend a distributed lock through the inner service. Returns `false` while the circuit is open.

### `FlushAsync()`

Removes all entries through the inner service. Becomes a completed no-op while the circuit is open.

### `GetStatisticsAsync()`

Retrieves statistics from the inner cache. Returns a new, empty `CacheStatistics` instance while the circuit is open.

### `SetPolicyAsync(CachePolicy policy)`

Registers or updates a cache policy through the inner service. Returns a completed `ValueTask` while the circuit is open.

**Throws**

- `ArgumentNullException` if `policy` is `null`.

### `GetPolicyAsync(string key)`

Retrieves the policy for a key through the inner service. Returns a completed `ValueTask` containing `null` while the circuit is open.

### `RecordSuccess()`

Manually records a successful cache operation. It resets the failure count and, when half-open, closes the circuit and clears `OpenedAtUtc`.

### `RecordFailure()`

Manually records a failure. In the closed state it increments the counter and opens the circuit when `FailureThreshold` is reached. In the half-open state it immediately reopens the circuit and increments the counter. Calling it while the circuit is already open has no effect.

### `Reset()`

Manually restores the closed state, clears the consecutive-failure count, and clears `OpenedAtUtc`.

Unless noted otherwise, methods accepting a key, lock key, lock value, pattern, key collection, or loader validate that argument for `null` and throw `ArgumentNullException`. Validation of durations, expiration values, and other operation-specific inputs is delegated to the wrapped service.

## Fail-open behavior

| Operation category | Result while open |
| --- | --- |
| `GetOrLoadAsync`, sliding/early-expiration load operations | Calls `loadFn` directly |
| `WriteAsync` | Calls `persistFn` directly |
| Single-value and metadata reads | `default(T)` or `null` |
| Existence and lock operations | `false` |
| Key enumeration and batch reads | Empty collection |
| Statistics | New empty `CacheStatistics` |
| Writes, removals, flush, and policy updates | Completed no-op |

## Usage

```csharp
ICacheService cache = new RedisCacheService(/* dependencies */);

var protectedCache = new CircuitBreakerCacheService(
    inner: cache,
    failureThreshold: 3,
    breakDuration: TimeSpan.FromSeconds(20),
    logger: logger);

var product = await protectedCache.GetOrLoadAsync(
    $"product:{productId}",
    () => productRepository.GetByIdAsync(productId),
    TimeSpan.FromMinutes(10));
```

The decorator can be composed with other `ICacheService` decorators. The outermost decorator observes exceptions produced by every service inside it:

```csharp
ICacheService cache = new CircuitBreakerCacheService(
    new CompressedCacheService(/* dependencies */),
    failureThreshold: 5,
    breakDuration: TimeSpan.FromSeconds(30));
```

Use `Reset()` only when an operator or recovery workflow knows that the cache dependency is healthy and should be retried immediately.
