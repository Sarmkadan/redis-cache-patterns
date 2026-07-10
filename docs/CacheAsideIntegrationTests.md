# CacheAsideIntegrationTests

Integration test suite validating the correctness, resilience, and performance characteristics of the cache-aside pattern implementation. Covers core read/write paths, distributed locking semantics, compression, retry policies, circuit breaker behavior, idempotency guarantees, and composite validation workflows under concurrent and failure conditions.

## API

### `public async Task CacheAsidePattern_LoadFromSourceOnFirstCall`
Verifies that when the cache is empty, the system correctly falls back to the underlying data source, loads the value, and populates the cache. Ensures the returned value matches the source-of-truth.

### `public async Task CacheAsidePattern_ReturnsFromCacheOnSecondCall`
Confirms that a subsequent request for the same key retrieves the value directly from cache without contacting the data source. Validates the fundamental cache-hit path.

### `public async Task MultipleThreads_SimultaneousCacheAccess_AllSucceed`
Stress test launching multiple concurrent tasks that read and write through the cache-aside layer. Asserts that all operations complete without deadlocks, data corruption, or unexpected exceptions.

### `public async Task DistributedLock_ProtectsSharedResource`
Demonstrates that the distributed lock mechanism serializes access to a shared resource. Concurrent contenders are forced to wait, and only one holder enters the critical section at a time.

### `public async Task DistributedLock_LockReleaseGuarantee_ReleasesEvenOnException`
Validates that the distributed lock is released even when the protected operation throws an exception. Prevents orphaned locks that would permanently block other consumers.

### `public void LargeDataset_CompressionAchievesMeaningfulReduction`
Measures the size of a representative large payload before and after compression. Asserts that the compressed form is meaningfully smaller, justifying the compression step in the cache pipeline.

### `public void CompressionRoundTrip_PreservesDataIntegrity`
Compresses an object and then decompresses it, comparing the result to the original. Guarantees that the compression/decompression cycle is lossless for the data types used.

### `public async Task RetryPolicy_RecoverFromTransientFailures`
Injects transient failures (e.g., simulated network blips) and confirms that the configured retry policy eventually succeeds within the expected number of attempts. Ensures the policy does not retry on non-transient errors.

### `public async Task CircuitBreaker_ProtectsDownstreamService`
Forces repeated failures to trip the circuit breaker, then verifies that subsequent calls fail fast without invoking the downstream service. After the recovery window, asserts the circuit transitions back to a healthy state.

### `public void CompleteProductValidation_Workflow`
End-to-end test of a product validation workflow that aggregates multiple validation rules. Expects a valid product to pass all checks and return a success result with no error messages.

### `public void ProductValidationWithErrors_CollectsAllErrors`
Feeds an invalid product through the same validation workflow and asserts that all applicable errors are collected in a single result, rather than failing on the first violation.

### `public void IdempotencyHelper_PreventsOrOperationDuplication`
Verifies that an idempotency key prevents duplicate execution of an operation. The second call with the same key returns the original result without re-executing the side-effect-producing logic.

### `public void IdempotencyHelper_RetrievesStoredResult`
Confirms that after an idempotent operation completes, a subsequent request with the same key can retrieve the previously stored result from the idempotency store.

### `public async Task<T?> GetOrLoadAsync<T>`
Core cache-aside read method. Returns the cached value if present; otherwise loads from the source, caches it, and returns the result. Returns `null` when the source itself returns `null` for the given key. Throws if the source access fails and retries are exhausted or the circuit breaker is open.

### `public async Task<T?> GetAsync<T>`
Reads a value directly from the cache without fallback to the source. Returns the deserialized object or `null` if the key is absent or expired. Throws on deserialization failures or cache infrastructure errors.

### `public Task SetAsync<T>`
Writes a value directly into the cache with the configured default time-to-live. Overwrites any existing entry for the same key. Throws if the serialization or cache write operation fails.

### `public Task<T?> GetOrLoadWithSlidingExpirationAsync<T>`
Variant of `GetOrLoadAsync` that sets a sliding expiration window on the cached entry. Each cache hit within the window resets the expiration timer. Same return and exception semantics as `GetOrLoadAsync`.

### `public Task<T?> GetOrLoadWithEarlyExpirationAsync<T>`
Variant of `GetOrLoadAsync` that proactively refreshes the cached entry before its absolute expiration, based on a configurable early-expiration threshold. Returns the existing cached value while the refresh happens asynchronously. Throws identically to `GetOrLoadAsync`.

### `public Task<CacheKeyMetadata?> GetKeyMetadataAsync`
Retrieves metadata for a cache key (e.g., time-to-live remaining, last access time, size) without deserializing the full value. Returns `null` if the key does not exist. Throws on cache infrastructure failures.

### `public async Task<T> WriteAsync<T>`
Performs a write-through or write-behind operation to both the cache and the underlying data source, depending on configuration. Returns the written entity as confirmed by the source. Throws if either the cache or source write fails irrecoverably.

## Usage

### Basic cache-aside read with fallback

```csharp
var tests = new CacheAsideIntegrationTests();

// First call — cache miss, loads from source
Product? product = await tests.GetOrLoadAsync<Product>("sku:12345");
Console.WriteLine(product?.Name);

// Second call — cache hit, no source access
Product? cached = await tests.GetOrLoadAsync<Product>("sku:12345");
Console.WriteLine(cached?.Name);
```

### Resilient write with idempotency guard

```csharp
var tests = new CacheAsideIntegrationTests();
string idempotencyKey = "order-create:abc-67890";

// First attempt — executes and stores result
Order order = await tests.WriteAsync<Order>(new Order { Id = 67890 });
tests.IdempotencyHelper_PreventsOrOperationDuplication();

// Duplicate attempt with same key — retrieves stored result, no duplicate write
tests.IdempotencyHelper_RetrievesStoredResult();
```

## Notes

- **Thread safety**: Methods such as `GetOrLoadAsync` and its variants rely on distributed locking to prevent cache stampedes when multiple threads request the same absent key simultaneously. The lock release guarantee is tested explicitly to ensure no orphaned locks under exceptional conditions.
- **Null handling**: `GetAsync<T>` and `GetOrLoadAsync<T>` return `null` for missing keys; callers must distinguish between a genuinely absent value and a cache infrastructure failure (which throws).
- **Compression**: Compression is applied only to payloads exceeding a size threshold. `LargeDataset_CompressionAchievesMeaningfulReduction` validates that the threshold logic triggers compression appropriately; small payloads may remain uncompressed.
- **Circuit breaker state**: The circuit breaker protects the downstream source, not the cache itself. Cache-only operations like `GetAsync` and `SetAsync` are not gated by the circuit breaker.
- **Idempotency scope**: Idempotency keys are scoped to the operation type and key space; reusing a key across different operation types may lead to incorrect result retrieval.
- **Early expiration**: `GetOrLoadWithEarlyExpirationAsync` may return a stale value while the background refresh is in flight. Consumers tolerant of eventual consistency should prefer this variant for hot keys.
