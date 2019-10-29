# CacheWarmingStrategiesTests

This test class contains unit tests that verify the behavior of various cache‑warming strategies and the `CacheWarmingScheduler` in the **redis-cache-patterns** library. Each test method exercises a specific scenario—such as handling null factory results, respecting priority ordering, executing work in parallel, or reacting to scheduler misuse—to ensure the production code meets its contractual guarantees.

## API

| Member | Purpose | Parameters | Return Value | Throws |
|--------|---------|------------|--------------|--------|
| `DelegateWarmingStrategy_WhenAllEntriesHaveValues_WarmsAllKeys` | Confirms that the delegate‑based warming strategy invokes the supplied factory for every key and successfully warms all entries when the factory returns non‑null values. | none | `Task` (completed when the test finishes) | May propagate any exception thrown by the factory or the warming mechanism; the test itself does not throw except through assertion failures. |
| `DelegateWarmingStrategy_WhenFactoryReturnsNull_SkipsKey` | Verifies that when the factory returns `null` for a particular key, that key is omitted from the warming operation and the overall count reflects only the successfully warmed entries. | none | `Task` | May propagate exceptions from the factory or warming logic; the test fails on unexpected exceptions. |
| `DelegateWarmingStrategy_WhenFactoryThrows_ContinuesAndReturnsPartialCount` | Ensures that an exception thrown by the factory for a specific key does not abort the entire warming process; the strategy continues with remaining keys and returns a count of successful warmings. | none | `Task` | May propagate exceptions from non‑faulty factory invocations; the test expects the strategy to swallow the factory exception and complete. |
| `PriorityWarmingStrategy_ExecutesCriticalBeforeNormalEntries` | Checks that entries marked with a higher priority (e.g., `Critical`) are processed before those with lower priority (e.g., `Normal`) when the strategy respects ordering. | none | `Task` | May throw if the underlying priority queue or warming action fails; the test asserts ordering, not exception handling. |
| `PriorityWarmingStrategy_WarmsTotalCountAcrossAllPriorities` | Validates that the total number of warmed entries equals the sum of entries across all priority levels when the strategy processes every queue. | none | `Task` | May propagate exceptions from any warming action; the test fails on unexpected errors. |
| `ParallelWarmingStrategy_WarmsAllEntriesConcurrently` | Asserts that the parallel warming strategy launches warming operations for all entries concurrently and awaits completion of all of them. | none | `Task` | May throw if any of the parallel tasks fault; the test expects all tasks to succeed. |
| `ParallelWarmingStrategy_WhenSomeEntriesFail_ReturnsSuccessfulCount` | Confirms that when a subset of warming tasks fault, the strategy continues, awaits the remaining tasks, and returns a count reflecting only the successful warmings. | none | `Task` | May propagate exceptions from tasks that are not expected to fail; the test verifies that failed tasks are swallowed and the result is correct. |
| `PatternRefreshWarmingStrategy_RefreshesEachMatchingKey` | Ensures that the pattern‑based refresh strategy scans the cache for keys matching a given pattern and invokes the refresh operation on each matched key exactly once. | none | `Task` | May throw if the pattern scan or refresh operation fails; the test validates correct invocation counts. |
| `PatternRefreshWarmingStrategy_WhenPatternScanFails_ReturnsZero` | Verifies that if the underlying pattern scan throws an exception, the strategy treats the operation as having warmed zero keys and propagates or handles the failure according to its contract. | none | `Task` | May throw the scan exception; the test expects a zero result or appropriate error handling. |
| `CacheWarmingScheduler_StartTwice_ThrowsInvalidOperationException` | Confirms that calling `Start` on the scheduler while it is already running throws an `InvalidOperationException` to prevent re‑entrant initialization. | none | `void` | Throws `InvalidOperationException` on the second `Start` call. |
| `CacheWarmingScheduler_StopBeforeStart_DoesNotThrow` | Ensures that invoking `Stop` before the scheduler has been started does not throw and is treated as a no‑op. | none | `void` | Does not throw under any circumstance. |

## Usage

### Example 1: Running the test suite
```csharp
// Assuming the project targets .NET 6+ and uses xUnit.
// From the command line:
dotnet test redis-cache-patterns.Tests/bin/Debug/net6.0/redis-cache-patterns.Tests.dll --filter FullyQualifiedName~CacheWarmingStrategiesTests
```
This command discovers and executes all test methods in `CacheWarmingStrategiesTests`, reporting pass/fail outcomes for each scenario.

### Example 2: Using the `DelegateWarmingStrategy` in production code
```csharp
// Factory that retrieves a value from a data source.
Func<string, Task<string>> factory = async key =>
{
    var value = await GetFromDatabaseAsync(key);
    return value; // returns null if the key is missing.
};

var strategy = new DelegateWarmingStrategy(factory);
var keys = new[] { "user:1000", "user:1001", "user:1002" };

// Warm the cache; returns the number of successfully warmed entries.
int warmed = await strategy.WarmAsync(keys);
// warmed will be 2 if one factory call returns null.
```
This snippet mirrors the arrangements verified by `DelegateWarmingStrategy_WhenAllEntriesHaveValues_WarmsAllKeys` and `DelegateWarmingStrategy_WhenFactoryReturnsNull_SkipsKey`.

## Notes
- All asynchronous test methods are safe to run concurrently; they do not share mutable state.
- The `CacheWarmingScheduler` is **not** thread‑safe for repeated `Start` calls; invoking `Start` a second time without an intervening `Stop` results in an `InvalidOperationException`, as validated by `CacheWarmingScheduler_StartTwice_ThrowsInvalidOperationException`.
- Calling `Stop` before `Start` is permitted and results in a no‑op, which is useful for defensive cleanup code.
- Strategies that swallow exceptions (e.g., `DelegateWarmingStrategy_WhenFactoryThrows_ContinuesAndReturnsPartialCount`) continue processing remaining keys; callers should inspect the returned count to determine partial success.
- Pattern‑based refresh strategies assume the underlying scan operation is idempotent; a failing scan results in a zero‑count outcome, as exercised by `PatternRefreshWarmingStrategy_WhenPatternScanFails_ReturnsZero`.
