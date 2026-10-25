# CacheHitEventValidation

The `CacheHitEventValidation` class provides a set of static extension methods for validating `CacheHitEvent` instances within the `redis-cache-patterns` library. It follows the same `Validate` / `IsValid` / `EnsureValid` shape used by other validation helpers, so callers can check an event's state in a consistent way.

The checks cover both the inherited `DomainEvent` properties (`EventId`, `OccurredAt`, `Source`) and the `CacheHitEvent` properties (`CacheKey`, `DataSize`). Validation only reads the event; it does not change any of its properties.

## API

### `Validate`

Validates a `CacheHitEvent` instance and returns a list of human-readable validation problems.

- **Parameters**:
  - `value` (CacheHitEvent?): The event instance to validate.
- **Return value**: `IReadOnlyList<string>` containing one message per failed rule, in the order the rules are checked. The list is empty when the event is valid. Each call returns a new read-only list.
- **Exceptions**: Throws `ArgumentNullException` if `value` is `null`.

The rules are checked in this order:

| Property | Rule | Problem message |
|---|---|---|
| `EventId` | Must not be null, empty, or whitespace | `EventId cannot be null or whitespace.` |
| `EventId` | Must parse as a GUID (`Guid.TryParse`) | `EventId must be a valid GUID.` |
| `OccurredAt` | Must not be more than 5 minutes after the current UTC time | `OccurredAt cannot be in the future.` |
| `OccurredAt` | Must not be more than 1 year before the current UTC time | `OccurredAt appears to be too old to be valid.` |
| `Source` | Must not be null, empty, or whitespace | `Source cannot be null or whitespace.` |
| `CacheKey` | Must not be null, empty, or whitespace | `CacheKey cannot be null or whitespace.` |
| `DataSize` | Must not be negative | `DataSize cannot be negative.` |

The `OccurredAt` checks are evaluated against `DateTime.UtcNow` at the moment of the call. An event can therefore pass on one day and fail on a later one as it ages past the one-year limit. For `EventId`, only one problem is reported: the GUID check runs only when the value is not empty. Likewise, the future and too-old checks for `OccurredAt` are mutually exclusive.

### `IsValid`

Checks whether a `CacheHitEvent` instance is valid.

- **Parameters**:
  - `value` (CacheHitEvent?): The event instance to check.
- **Return value**: `bool` - `true` if the event is not `null` and `Validate` returns no problems; otherwise, `false`.
- **Exceptions**: Does not throw. A `null` instance returns `false`.

### `EnsureValid`

Ensures that a `CacheHitEvent` instance is valid, throwing an exception if it is not.

- **Parameters**:
  - `value` (CacheHitEvent?): The event instance to validate.
- **Return value**: `void`.
- **Exceptions**:
  - Throws `ArgumentNullException` if `value` is `null`.
  - Throws `ArgumentException` if `Validate` returns one or more problems. The message has the form `CacheHitEvent is invalid. Problems: <problem> <problem> ...`. Unlike some other validation helpers, the problems are joined with a single space on one line, not one per line.

## Defaults and Common Failures

A newly constructed `CacheHitEvent` is not valid until the caller sets the required fields:

- `EventId` defaults to a new GUID string and `OccurredAt` defaults to `DateTime.UtcNow`, so both pass.
- `Source` defaults to an empty string and `CacheKey` defaults to an empty string, so both fail until they are set.
- `DataSize` defaults to `0`, which passes.

This means `new CacheHitEvent().IsValid()` returns `false`.

## Usage

### Example 1: Checking Validity

```csharp
var hitEvent = new CacheHitEvent
{
    Source = "ProductService",
    CacheKey = "product:1",
    DataSize = 512
};

if (hitEvent.IsValid())
{
    Console.WriteLine("CacheHitEvent is ready to publish.");
}
```

### Example 2: Collecting Validation Problems

```csharp
var hitEvent = new CacheHitEvent
{
    EventId = "not-a-guid",
    Source = string.Empty,
    CacheKey = "product:1"
};

var problems = hitEvent.Validate();

foreach (var problem in problems)
{
    Console.WriteLine($"Validation problem: {problem}");
}
// Validation problem: EventId must be a valid GUID.
// Validation problem: Source cannot be null or whitespace.
```

### Example 3: Failing Fast

```csharp
var hitEvent = new CacheHitEvent
{
    Source = "ProductService",
    CacheKey = "product:1",
    DataSize = -1
};

// Throws ArgumentException:
// "CacheHitEvent is invalid. Problems: DataSize cannot be negative."
hitEvent.EnsureValid();
```

## Notes

*   **Extension Methods:** All members are extension methods on `CacheHitEvent?`. They are called on an instance (for example, `hitEvent.IsValid()`) rather than on the class. Because the receiver is nullable, `IsValid` can be called on a `null` reference, while `Validate` and `EnsureValid` throw in that case.
*   **GUID Format:** `Guid.TryParse` accepts several GUID formats, including values with or without hyphens and with braces. The check verifies that the value parses, not that it matches the default `D` format that `DomainEvent` produces.
*   **Time-Based Rules:** The `OccurredAt` window is relative to the local clock of the process running the check. Events created on machines with clocks more than 5 minutes ahead of the validating process will fail the future check.
*   **Scope:** Only the properties listed above are checked. `CacheKey` is checked only for emptiness, not for the key format used by `CacheKeyValidation`.
*   **Future Rules:** If `CacheHitEvent` gains new properties that need checking, add the rules to `Validate`. `IsValid` and `EnsureValid` are built on top of it and pick up new rules automatically.
