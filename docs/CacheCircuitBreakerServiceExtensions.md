# CacheCircuitBreakerServiceExtensions

The `CacheCircuitBreakerServiceExtensions` class is a static collection of extension methods for `CacheCircuitBreakerService`. It adds three read-only helpers: `IsOpen` (a boolean check for the Open state), `TimeUntilHalfOpen` (the remaining cooldown), and `ToStatusString` (a human-readable status line for logs and health output). The class is in the `RedisCachePatterns.Services` namespace, the same as `CacheCircuitBreakerService`, so no extra `using` is needed. None of the methods change the circuit.

The class is marked `[Obsolete]`. Its message points to the `CircuitBreakerCacheService` decorator, which is the replacement. `CacheCircuitBreakerService` is obsolete for the same reason. The attribute produces compiler warnings only; it does not stop the code from compiling. Keep these helpers only for existing code that still uses the legacy wrapper. New code should use `CircuitBreakerCacheService` and its own state members.

Each method reads the state through the public `State`, `OpenedAtUtc`, and `BreakDuration` members of `CacheCircuitBreakerService`. The helpers therefore report the same values as those members, and they inherit their limits.

## API

### IsOpen

```csharp
public static bool IsOpen(this CacheCircuitBreakerService service)
```

Checks whether the circuit is currently in the `Open` state.

- **service**: The circuit breaker instance.
- **Returns**: `true` when `service.State` is `CacheCircuitState.Open`; otherwise `false`. A circuit in `HalfOpen` returns `false`, because it is ready to accept a probe call.
- **Throws**: `NullReferenceException` when `service` is null. The method does not guard its argument.

### TimeUntilHalfOpen

```csharp
public static TimeSpan TimeUntilHalfOpen(this CacheCircuitBreakerService service)
```

Returns the time left before the circuit moves from `Open` to `HalfOpen`. The value is `BreakDuration` minus the time elapsed since `OpenedAtUtc`, measured against `DateTime.UtcNow`.

- **service**: The circuit breaker instance.
- **Returns**: The remaining time, or `TimeSpan.Zero` in these cases:
  - The circuit is not `Open` (`Closed` or `HalfOpen`).
  - `OpenedAtUtc` is `null`.
  - The break duration has already elapsed.
- **Throws**: `NullReferenceException` when `service` is null.

### ToStatusString

```csharp
public static string ToStatusString(this CacheCircuitBreakerService service)
```

Returns a one-line description of the circuit state.

- **service**: The circuit breaker instance.
- **Returns**: One of these forms:
  - Closed or HalfOpen: the state name only, for example `Closed`.
  - Open: `Open (Opened at <OpenedAtUtc>, Remaining: <hh:mm:ss>)`. `<OpenedAtUtc>` uses the round-trip `"O"` format. It is written as `unknown` when `OpenedAtUtc` is `null`. `<hh:mm:ss>` is the value of `TimeUntilHalfOpen()`.
- **Throws**: `NullReferenceException` when `service` is null.

## Usage

### Guarding a call on the legacy wrapper

```csharp
using RedisCachePatterns.Services;

var breaker = new CacheCircuitBreakerService(innerCache, failureThreshold: 3, breakDuration: TimeSpan.FromSeconds(30));

if (breaker.IsOpen())
{
    // Serve from the database directly while the cache is unavailable.
    return await LoadFromDatabaseAsync(key);
}
```

### Logging the status

```csharp
logger.LogInformation("Cache circuit: {Status}", breaker.ToStatusString());
// Cache circuit: Open (Opened at 2026-10-25T10:15:04.1234567Z, Remaining: 00:00:27)
```

### Waiting for the probe window

```csharp
TimeSpan wait = breaker.TimeUntilHalfOpen();

if (wait > TimeSpan.Zero)
{
    await Task.Delay(wait, cancellationToken);
}
```

## Notes

*   **Obsolete type:** Both this class and `CacheCircuitBreakerService` carry `[Obsolete]`. Code that uses them is flagged by the compiler as obsolete. The project sets `TreatWarningsAsErrors` to `false`, so these flags do not fail the build.
*   **Snapshot per read:** Each member is read separately, and each read can change the state. In `ToStatusString`, the state is read once for the `switch` and again inside `TimeUntilHalfOpen()`. If the break duration elapses between those two reads, the output can show `Open` with `Remaining: 00:00:00`.
*   **Half-open is not reported as open:** `IsOpen()` returns `false` as soon as the break duration has elapsed, even if no probe has run yet. Use `State` if you need to distinguish `Closed` from `HalfOpen`.
*   **Remaining time wraps above 24 hours:** `ToStatusString` formats the remaining time with `hh\:mm\:ss`. The `hh` field holds hours within a day and has no day component. A break duration longer than 24 hours shows a value such as `01:00:00` for 25 hours remaining. Use `TimeUntilHalfOpen()` directly if you need the full value.
*   **No null guards:** None of the methods check `service` for null. Passing `null` throws `NullReferenceException`, not `ArgumentNullException`.
*   **Clock dependence:** `TimeUntilHalfOpen` uses `DateTime.UtcNow`. A clock change can shorten or lengthen the reported remaining time.
*   **Read-only:** The helpers never call `Reset()`, `RecordSuccess()`, or `RecordFailure()`, so they do not change the failure count or the state.
