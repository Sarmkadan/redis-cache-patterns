# CacheAsideIntegrationTestsExtensions

The `CacheAsideIntegrationTestsExtensions` class provides a suite of asynchronous extension methods designed to simplify the validation of Redis cache-aside patterns within integration test suites. These methods encapsulate common test scenarios, including pre-load verification, expiration handling, bulk data operations, and cache invalidation workflows, promoting consistency and reducing boilerplate code across test projects.

## API

### CacheWarmup_ValidatesCacheState
Verifies that the cache is correctly populated during a warm-up phase and that the stored state accurately reflects the intended data source before subsequent application requests.
*   **Returns:** `Task` that completes when validation is finished.
*   **Throws:** `AssertionException` if the cache state does not match the expected warm-up data.

### CacheExpiration_TriggersReloadOnExpiry
Validates the cache-aside expiration logic by ensuring that after a configured time-to-live (TTL) expires, the next retrieval request bypasses the cache and successfully reloads the fresh data from the underlying data source.
*   **Returns:** `Task` that completes when expiration behavior is validated.
*   **Throws:** `TimeoutException` or `AssertionException` if the data is not successfully reloaded or if the cache returns stale data after expiration.

### BulkOperations_CachesMultipleItems
Confirms that bulk caching operations correctly process and store multiple items simultaneously, ensuring that all keys are correctly mapped to their respective values in Redis.
*   **Returns:** `Task` that completes when bulk validation is finished.
*   **Throws:** `AssertionException` if any key in the bulk operation fails to cache or if values are mapped incorrectly.

### CacheInvalidation_RemovesRelatedKeys
Verifies that a cache invalidation operation correctly removes both the target key and any associated dependent keys, ensuring cache consistency after data mutations.
*   **Returns:** `Task` that completes when invalidation validation is finished.
*   **Throws:** `AssertionException` if the target key or any related keys persist in the cache after the invalidation command is executed.

## Usage

### Example 1: Validating Cache Warm-up in a Test Setup
```csharp
[Fact]
public async Task Service_Should_Return_Warmup_Data()
{
    // Arrange: Assume 'this' is a test class with access to the cache provider
    var cacheKey = "user:123";
    
    // Act & Assert
    await this.CacheWarmup_ValidatesCacheState(cacheKey, expectedUserObject);
}
```

### Example 2: Verifying Expiration Behavior
```csharp
[Fact]
public async Task Cache_Should_Reload_After_Expiration()
{
    // Arrange: Cache an item with short TTL
    var cacheKey = "config:settings";
    
    // Act & Assert
    await this.CacheExpiration_TriggersReloadOnExpiry(cacheKey, dataSourceMock);
}
```

## Notes

*   **Thread Safety:** These methods are designed for use within isolated integration tests. They are not intrinsically thread-safe if multiple tests share the same Redis instance without proper test-isolation (e.g., unique key prefixes or database indices per test).
*   **Dependencies:** These methods rely on an active, configured connection to the Redis instance used by the application under test. Ensure the Redis provider is correctly injected or accessible within the test context.
*   **Time Sensitivity:** `CacheExpiration_TriggersReloadOnExpiry` depends on the system clock and configured TTL settings. Ensure tests are configured with sufficiently small TTLs to allow for rapid execution, or utilize a mockable clock provider if supported by the underlying implementation.
*   **Network Resilience:** As these are network-bound integration tests, they may fail due to transient network issues or Redis service interruptions. These tests are not substitutes for resilience testing; they assume a stable connection during execution.
