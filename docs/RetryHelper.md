# RetryHelper

`RetryHelper` provides a resilient execution framework for operations that may fail transiently, such as network calls or I/O-bound work. It wraps a configurable circuit-breaker pattern combined with automatic retry logic, allowing callers to execute delegates with built-in fault tolerance while exposing diagnostic state about recent failures.

## API

### public static async Task\<T\> ExecuteWithRetryAsync\<T\>

Executes a given asynchronous function with automatic retries on failure. When the operation throws, the helper waits a short delay and re-attempts execution up to a configured maximum number of times. If all retries are exhausted, the last captured exception is re-thrown.

- **Parameters:**  
  `Func<Task<T>> operation` — the asynchronous operation to execute and potentially retry.
- **Returns:**  
  `Task<T>` — the result produced by a successful invocation of `operation`.
- **Throws:**  
  Re-throws the last exception encountered if all retry attempts fail. The specific exception type depends on the delegate.

### public static async Task\<T\> ExecuteAsync\<T\>

Executes a given asynchronous function with circuit-breaker awareness. If the circuit is open (i.e. `IsOpen` returns `true`), the call fails immediately without invoking the delegate. Otherwise, the operation is attempted; a failure increments the internal failure count and records the failure time, potentially opening the circuit.

- **Parameters:**  
  `Func<Task<T>> operation` — the asynchronous operation to execute under circuit-breaker protection.
- **Returns:**  
  `Task<T>` — the result produced by `operation` when it succeeds.
- **Throws:**  
  Throws immediately if the circuit is open. Otherwise, exceptions from the delegate propagate to the caller and trigger circuit-state transitions.

### public bool IsOpen

Gets a value indicating whether the circuit is currently open. When `true`, calls through `ExecuteAsync<T>` are rejected without invoking the delegate. The circuit closes again after a configured timeout elapses without further failures.

- **Type:** `bool` (read-only property).

### public int FailureCount

Gets the number of consecutive failures recorded since the last successful execution or since the circuit was last reset. This count drives the decision to open the circuit once it reaches a configured threshold.

- **Type:** `int` (read-only property).

### public DateTime LastFailureTime

Gets the timestamp of the most recent failure. Used together with the failure count to determine when the circuit should transition from open to half-open or closed.

- **Type:** `DateTime` (read-only property).

### public static void Reset

Resets all circuit-breaker state to its initial values. Sets `FailureCount` to zero, clears the last failure timestamp, and forces the circuit to the closed state so that subsequent `ExecuteAsync<T>` calls will attempt execution normally.

- **Parameters:** None.
- **Returns:** Void.
- **Throws:** Does not throw.

## Usage

### Example 1: Retry with fallback value

```csharp
async Task<string> FetchConfigurationWithRetryAsync()
{
    try
    {
        return await RetryHelper.ExecuteWithRetryAsync(async () =>
        {
            // Simulated flaky remote call
            return await RemoteConfigService.GetSettingAsync("feature-flag");
        });
    }
    catch (Exception ex)
    {
        // All retries exhausted — log and return safe default
        Console.WriteLine($"Could not fetch config: {ex.Message}");
        return "default-value";
    }
}
```

### Example 2: Circuit-breaker guarded database call

```csharp
async Task<Order> LoadOrderWithCircuitBreakerAsync(int orderId)
{
    if (RetryHelper.IsOpen)
    {
        // Circuit is open — avoid hammering the database, return cached or null
        return await CacheService.GetCachedOrderAsync(orderId);
    }

    try
    {
        var order = await RetryHelper.ExecuteAsync(async () =>
        {
            return await Database.FetchOrderAsync(orderId);
        });

        // Success resets failure streak implicitly
        return order;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DB call failed at {RetryHelper.LastFailureTime}, count={RetryHelper.FailureCount}");
        throw;
    }
}
```

## Notes

- **Thread safety:** The static methods `ExecuteWithRetryAsync<T>`, `ExecuteAsync<T>`, and `Reset` are designed for concurrent use. Reads of `IsOpen`, `FailureCount`, and `LastFailureTime` reflect a point-in-time snapshot and may be stale by the time they are observed; do not rely on them for mutual exclusion decisions without additional synchronization.
- **Circuit state transitions:** A successful call through `ExecuteAsync<T>` resets the failure count and closes the circuit. If the circuit is open, `ExecuteWithRetryAsync<T>` does not bypass it — the retry loop still delegates to the circuit-breaker logic internally, so an open circuit causes immediate failure even with retries configured.
- **Reset scope:** Calling `Reset` affects all callers sharing the same static state. Ensure no in-flight operations are relying on the previous failure count when resetting in a live system.
- **Exception propagation:** Both execution methods propagate exceptions rather than swallowing them. Callers must handle or log exceptions appropriately, especially when retries are exhausted or the circuit is open.
