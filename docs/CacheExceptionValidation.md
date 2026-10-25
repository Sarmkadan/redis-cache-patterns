# CacheExceptionValidation

The `CacheExceptionValidation` class provides a set of static extension methods for validating `CacheException` instances and its derived types within the `redis-cache-patterns` library. It follows the same `Validate` / `IsValid` / `EnsureValid` shape used by other validation helpers, so callers can check an exception's state in a consistent way.

The checks cover the base `CacheException` properties (`ErrorCode`, `OccurredAt`) and, for derived types, the properties specific to `CacheTimeoutException` (`Timeout`) and `CacheKeyNotFoundException` (`CacheKey`). Validation only reads the exception; it does not change any of its properties.

`CacheException` is declared in `Exceptions/CacheException.cs`. This validation class is in `Exceptions/CacheExceptionValidation.cs`.

## API

### `Validate`

Validates a `CacheException` instance and returns a list of human-readable validation problems.

- **Parameters**:
  - `value` (CacheException): The exception instance to validate. Cannot be `null`.
- **Return value**: `IReadOnlyList<string>` containing one message per failed rule, in the order the rules are checked. The list is empty when the exception is valid. Each call returns a new list.
- **Exceptions**: Throws `ArgumentNullException` if `value` is `null`.

The rules are checked in this order:

| Property | Applies to | Rule | Problem message |
|---|---|---|---|
| `ErrorCode` | All `CacheException` types | Must not be null, empty, or whitespace | `ErrorCode cannot be null or empty.` |
| `OccurredAt` | All `CacheException` types | Must not be more than 5 minutes after the current UTC time | `OccurredAt cannot be in the future.` |
| `OccurredAt` | All `CacheException` types | Must not be more than 1 year before the current UTC time | `OccurredAt appears to be too old to be valid.` |
| `Timeout` | `CacheTimeoutException` | Must be greater than `TimeSpan.Zero` | `Timeout must be a positive TimeSpan.` |
| `CacheKey` | `CacheKeyNotFoundException` | Must not be null, empty, or whitespace | `CacheKey cannot be null or empty.` |

The type-specific rules are selected with an `is` pattern on the runtime type, so they apply to any instance of those types regardless of the static type of the variable that holds it.

The `OccurredAt` checks are evaluated against `DateTime.UtcNow` at the moment of the call. An exception can therefore pass on one day and fail on a later one as it ages past the one-year limit. The future and too-old checks are mutually exclusive, so at most one of them is reported for a given value. The `ErrorCode` check also rejects whitespace, even though its message says "empty".

### `IsValid`

Checks whether a `CacheException` instance is valid.

- **Parameters**:
  - `value` (CacheException): The exception instance to check.
- **Return value**: `bool` - `true` if `Validate` returns no problems; otherwise, `false`.
- **Exceptions**: Throws `ArgumentNullException` if `value` is `null`, because it calls `Validate`.

### `EnsureValid`

Ensures that a `CacheException` instance is valid, throwing an exception if it is not.

- **Parameters**:
  - `value` (CacheException): The exception instance to validate. Cannot be `null`.
- **Return value**: `void`.
- **Exceptions**:
  - Throws `ArgumentNullException` if `value` is `null`.
  - Throws `ArgumentException` if `Validate` returns one or more problems. The message has the form `CacheException is invalid: <problem>; <problem> ...`, with the problems joined by `; ` on one line.

## Defaults and Common Failures

A newly constructed `CacheException` depends on the constructor used:

- `OccurredAt` defaults to `DateTime.UtcNow`, so it passes.
- `ErrorCode` is only set by the constructors that take an `errorCode` argument. The `CacheException(string message)` and `CacheException(string message, Exception innerException)` constructors leave it `null`, so those instances fail.
- The derived types always set their own `ErrorCode`. `CacheConnectionException`, `CacheTimeoutException`, `CacheKeyNotFoundException`, `CacheDecompressionException`, `CacheSizeLimitExceededException`, and `CacheSerializationException` therefore pass the `ErrorCode` rule.

This means `new CacheException("Redis unavailable").IsValid()` returns `false`, while `new CacheException("Redis unavailable", "CACHE_CONNECTION_FAILED").IsValid()` returns `true`.

A `CacheTimeoutException` created with `TimeSpan.Zero` fails the `Timeout` rule. A `CacheKeyNotFoundException` fails the `CacheKey` rule only when its key is set to an empty or whitespace string, which the constructor allows.

## Usage

### Example 1: Checking Validity

```csharp
var exception = new CacheConnectionException("Connection to Redis failed.");

if (exception.IsValid())
{
    Console.WriteLine("CacheException is ready to log.");
}
```

### Example 2: Collecting Validation Problems

```csharp
var exception = new CacheKeyNotFoundException(string.Empty);

var problems = exception.Validate();

foreach (var problem in problems)
{
    Console.WriteLine($"Validation problem: {problem}");
}
// Validation problem: CacheKey cannot be null or empty.
```

### Example 3: Failing Fast

```csharp
var exception = new CacheException("Redis unavailable");

// Throws ArgumentException:
// "CacheException is invalid: ErrorCode cannot be null or empty."
exception.EnsureValid();
```

## Notes

*   **Extension Methods:** All members are extension methods on `CacheException`. They are called on an instance (for example, `exception.IsValid()`) rather than on the class. The receiver is not nullable, so `Validate`, `IsValid`, and `EnsureValid` all throw `ArgumentNullException` when called on a `null` reference.
*   **Exception Format:** `EnsureValid` throws `ArgumentException`, not the exception being validated. Callers that need to re-throw a cache failure should check `IsValid()` first instead of relying on the thrown message.
*   **Derived Types:** The base rules apply to every `CacheException`. The `Timeout` and `CacheKey` rules apply only to their specific types, so a derived type without a dedicated rule is checked only for `ErrorCode` and `OccurredAt`.
*   **Time-Based Rules:** The `OccurredAt` window is relative to the local clock of the process running the check. The value is compared as stored, so the check does not look at `DateTimeKind`. An exception created on a machine whose clock is more than 5 minutes ahead of the validating process will fail the future check.
*   **Scope:** `CacheKey` is checked only for emptiness, not for the key format used by `CacheKeyValidation`. `ErrorCode` is not compared against a list of known codes.
*   **Future Rules:** If `CacheException` or one of its derived types gains new properties that need checking, add the rules to `Validate`. `IsValid` and `EnsureValid` are built on top of it and pick up new rules automatically.
