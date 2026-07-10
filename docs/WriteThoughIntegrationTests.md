# WriteThoughIntegrationTests

This class contains integration‑test methods that validate the behavior of various caching patterns implemented with Redis in the `redis-cache-patterns` project. Each test exercises a specific pattern—such as write‑through, cache‑aside, distributed locking, workflow orchestration, key invalidation, bulk operations, and concurrent stress scenarios—to ensure that the implementation correctly interacts with the data source and cache under normal and failure conditions.

## API

| Method | Purpose | Parameters | Return Value | Throws When |
|--------|---------|------------|--------------|-------------|
| `WriteThroughPattern_UpdatesDataSourceAndCacheSynchronously` | Verifies that a write‑through operation updates both the underlying data source and the Redis cache in a single, synchronous step. | none | `Task` (completes when the test finishes) | If the data source update fails, the cache is not updated, or an assertion mismatch occurs; also throws on Redis connection or command execution errors. |
| `WriteThroughPattern_FailureInDataSource_PreventsCaching` | Confirms that when the data source throws an exception during a write‑through, the cache is left unchanged (no stale data is stored). | none | `Task` | If the cache is inadvertently updated despite the data source failure, or if the expected exception is not propagated; also throws on unrelated Redis errors. |
| `CacheAsidePattern_MultipleThreadsAccessingSameKey_DataLoadedOnceOnly` | Ensures that, under concurrent cache‑aside reads, the underlying data source is invoked only once for a missing key, with subsequent threads receiving the cached value. | none | `Task` | If the data source is called more than once, or if threads observe inconsistent values; also throws on synchronization or Redis errors. |
| `CacheAsidePattern_StaleDataRefreshed_OnSubsequentMiss` | Validates that after the cached entry expires or is evicted, a cache‑aside miss triggers a reload from the data source, refreshing the stale data. | none | `Task` | If the cache returns stale data after a miss, or if the data source is not consulted; also throws on Redis TTL or retrieval errors. |
| `DistributedLock_ProtectsSharedResourceFromConcurrentAccess` | Tests that a distributed lock obtained via Redis prevents multiple threads from entering a critical section simultaneously. | none | `Task` | If two or more threads succeed in acquiring the lock concurrently, or if lock release fails; also throws on lock acquisition timeouts or Redis communication issues. |
| `DistributedLock_MultipleCompetingWorkersWithFailover` | Checks that when a worker holding a lock fails (e.g., process crash), another worker can acquire the lock after the configured timeout, ensuring failover. | none | `Task` | If the lock is not released after failure, or if a new worker cannot acquire the lock within the expected window; also throws on Redis session loss or timeout misconfiguration. |
| `CompleteOrderWorkflow_CreateRetrieveUpdateDelete` | Exercises a full lifecycle workflow (create, read, update, delete) for an order entity, asserting that each step correctly updates both the data source and the cache. | none | `Task` | If any step returns unexpected data, or if cache and data source diverge after an operation; also throws on Redis or database errors during the workflow. |
| `InventoryWorkflow_ReserveAndRelease` | Verifies that inventory reservation and subsequent release correctly adjust counts in the data source and are reflected in the cache. | none | `Task` | If the reserved quantity is not deducted, or if the release does not restore the original count; also throws on concurrency‑related exceptions or Redis failures. |
| `TagBasedInvalidation_RemovesAllRelatedEntries` | Ensures that invalidating a cache tag removes all keys associated with that tag, leaving unrelated keys untouched. | none | `Task` | If any tagged key remains after invalidation, or if an untagged key is incorrectly removed; also throws on tag‑management command errors. |
| `PatternBasedRemoval_MatchesAndRemovesKeys` | Confirms that a pattern‑based removal (e.g., using a wildcard) deletes only keys matching the pattern and preserves others. | none | `Task` | If non‑matching keys are removed, or matching keys persist after the operation; also throws on pattern‑matching or command execution errors. |
| `BulkCacheOperations_ThousandKeysStoreAndRetrieve` | Measures the correctness of storing and retrieving a large batch (≈1000) of keys, ensuring all values are round‑tripped accurately. | none | `Task` | If any key‑value pair is missing or corrupted after the bulk operation; also throws on pipeline or memory‑pressure related Redis errors. |
| `ConcurrentReadWriteOperations_StressTest` | Subjects the cache to a high volume of concurrent reads and writes to detect race conditions, deadlocks, or performance degradation. | none | `Task` | If inconsistencies appear between cache and data source, or if exceptions arise from lock contention or timeouts; also throws on Redis server overload or network faults. |

## Usage

```csharp
using System.Threading.Tasks;
using RedisCachePatterns.Tests; // namespace containing WriteThoughIntegrationTests

public class ExampleRunner
{
    public static async Task Main()
    {
        var tests = new WriteThoughIntegrationTests();

        // Run a write‑through validation test.
        await tests.WriteThroughPattern_UpdatesDataSourceAndCacheSynchronously();

        // Run a distributed lock failover test.
        await tests.DistributedLock_MultipleCompetingWorkersWithFailover();
    }
}
```

```csharp
using System.Threading.Tasks;
using NUnit.Framework;
using RedisCachePatterns.Tests;

[TestFixture]
public class WriteThoughIntegrationTestsFixture
{
    private WriteThoughIntegrationTests _sut;

    [SetUp]
    public void SetUp()
    {
        _sut = new WriteThoughIntegrationTests();
    }

    [Test]
    public async Task CacheAside_ConcurrentLoad_IsThreadSafe()
    {
        await _sut.CacheAsidePattern_MultipleThreadsAccessingSameKey_DataLoadedOnceOnly();
        // Test passes if no exception is thrown.
    }

    [Test]
    public async Task BulkOperations_PatternBasedRemoval_WorksCorrectly()
    {
        await _sut.PatternBasedRemoval_MatchesAndRemovesKeys();
    }
}
```

## Notes

- All tests assume an accessible Redis instance configured according to the project’s test environment (typically `localhost:6379` with no authentication). If the Redis server is unavailable or misconfigured, each method will throw a `RedisConnectionException` or similar.
- The test methods do not accept parameters; any required test data (keys, values, tags) is generated internally and cleaned up at the end of the test to avoid polluting the cache.
- Because the tests execute concurrent operations, they are **not** thread‑safe for simultaneous invocation from multiple callers. Each test should be run sequentially or with proper isolation (e.g., separate test runs) to prevent cross‑test interference.
- Failures are reported via exceptions; the test framework treats any thrown exception as a test failure. No specific exception types are documented beyond the general conditions listed in the API table.
- The methods return `Task` to enable `await` usage; they do not produce a meaningful result value—success is indicated by completion without exception.
