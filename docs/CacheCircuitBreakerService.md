# CacheCircuitBreakerService

A thread-safe circuit breaker implementation for Redis-backed cache operations that prevents cascading failures by temporarily halting cache operations after a configurable number of consecutive failures. It tracks cache state transitions (closed, open, half-open) and automatically recovers after a cooldown period.

## API

### `public int FailureThreshold`
The number of consecutive failures required to trip the circuit breaker into the open state. Once this threshold is exceeded, the circuit opens and remains open for the configured cooldown period.

### `public CacheCircuitState State`
Gets the current state of the circuit breaker (`Closed`, `Open`, or `HalfOpen`). Determines whether cache operations are allowed or blocked.

### `public int ConsecutiveFailures`
The current count of consecutive failures since the last success. Resets to zero on success and increments on failure. Used to evaluate whether the failure threshold has been reached.

### `public DateTime? OpenedAtUtc`
The UTC timestamp when the circuit last transitioned to the open state. `null` if the circuit is closed or half-open. Used to calculate cooldown expiration.

### `public CacheCircuitBreakerService`
Constructs a new circuit breaker instance with the specified failure threshold and cooldown duration.

**Parameters:**
- `failureThreshold` (int): Minimum consecutive failures to open the circuit.
- `cooldownDuration` (TimeSpan): Duration the circuit remains open after tripping before allowing retries.

**Throws:**
- `ArgumentOutOfRangeException`: If `failureThreshold` is less than 1 or `cooldownDuration` is non-positive.

### `public async Task<T?> GetOrLoadAsync<T>`
Attempts to retrieve a value from cache; if missing or on failure, loads it via a fallback function and stores the result in cache.

**Parameters:**
- `key` (string): Cache key to retrieve.
- `fallback` (Func<Task<T>>): Async function to load the value if missing or on failure.

**Return value:**
- `Task<T?>`: The cached value if present and valid, the fallback result if loaded, or `null` on circuit-open or fallback failure.

**Throws:**
- `InvalidOperationException`: If the circuit is open and no cached value exists.

### `public async Task<T?> GetAsync<T>`
Retrieves a value from cache if available and the circuit is closed.

**Parameters:**
- `key` (string): Cache key to retrieve.

**Return value:**
- `Task<T?>`: The cached value if present and valid, otherwise `null`.

**Throws:**
- `InvalidOperationException`: If the circuit is open.

### `public async Task SetAsync<T>`
Stores a value in cache if the circuit is closed.

**Parameters:**
- `key` (string): Cache key to store.
- `value` (T): Value to cache.
- `ttl` (TimeSpan): Time-to-live for the cached entry.

**Throws:**
- `InvalidOperationException`: If the circuit is open.

### `public async Task RemoveAsync`
Removes a cached entry if the circuit is closed.

**Parameters:**
- `key` (string): Cache key to remove.

**Throws:**
- `InvalidOperationException`: If the circuit is open.

### `public void RecordSuccess`
Records a successful operation and resets the failure counter. If the circuit was open, transitions it to half-open for the next operation.

### `public void RecordFailure`
Records a failed operation and increments the failure counter. If the failure threshold is exceeded and the circuit is closed, transitions it to open and records the open timestamp.

### `public void Reset`
Forcibly resets the circuit breaker to the closed state, clearing failure history and allowing all operations. Useful for manual recovery or testing.

## Usage
