# OrderCreatedEventValidation

The `OrderCreatedEventValidation` class provides a set of static extension methods for validating `OrderCreatedEvent` instances within the `redis-cache-patterns` library. It follows the same `Validate` / `IsValid` / `EnsureValid` shape used by other validation helpers, so callers can check an event's state in a consistent way.

The checks cover both the inherited `DomainEvent` properties (`EventId`, `OccurredAt`, `Source`) and the `OrderCreatedEvent` properties (`OrderId`, `UserId`, `TotalAmount`). Validation only reads the event; it does not change any of its properties.

`OrderCreatedEvent` is declared in `Events/OrderEventHandler.cs`. This validation class is in `Events/OrderCreatedEventValidation.cs`.

## API

### `Validate`

Validates an `OrderCreatedEvent` instance and returns a list of human-readable validation problems.

- **Parameters**:
  - `value` (OrderCreatedEvent?): The event instance to validate.
- **Return value**: `IReadOnlyList<string>` containing one message per failed rule, in the order the rules are checked. The list is empty when the event is valid. Each call returns a new read-only list.
- **Exceptions**: Throws `ArgumentNullException` if `value` is `null`.

The rules are checked in this order:

| Property | Rule | Problem message |
|---|---|---|
| `OrderId` | Must be greater than `0` | `OrderId must be a positive integer, but was {value}.` |
| `UserId` | Must be greater than `0` | `UserId must be a positive integer, but was {value}.` |
| `TotalAmount` | Must be greater than `0` | `TotalAmount must be a positive decimal, but was {value:F2}.` |
| `TotalAmount` | Must not be greater than `1,000,000.00` | `TotalAmount exceeds maximum allowed value of 1,000,000.00, but was {value:F2}.` |
| `EventId` | Must not be null, empty, or whitespace | `EventId must not be null or whitespace.` |
| `EventId` | Must parse as a GUID (`Guid.TryParse`) | `EventId must be a valid GUID, but was '{value}'.` |
| `OccurredAt` | Must not be more than 5 seconds after the current UTC time | `OccurredAt must not be in the future, but was {value:O}.` |
| `Source` | If set, must be 200 characters or less | `Source must be 200 characters or less, but was {length} characters.` |

`TotalAmount` is formatted with the invariant culture, so the message always uses `.` as the decimal separator.

The `OccurredAt` check is evaluated against `DateTime.UtcNow` at the moment of the call. The 5-second allowance absorbs small clock differences between the producer and the validating process. For `EventId`, only one problem is reported: the GUID check runs only when the value is not empty. For `TotalAmount`, the maximum check runs only when the amount is positive, so a negative amount reports only the positive-amount problem.

Unlike `CacheHitEventValidation`, there is no "too old" rule for `OccurredAt`. An event that is far in the past passes.

### `IsValid`

Checks whether an `OrderCreatedEvent` instance is valid.

- **Parameters**:
  - `value` (OrderCreatedEvent?): The event instance to check.
- **Return value**: `bool` - `true` if the event is not `null` and `Validate` returns no problems; otherwise, `false`.
- **Exceptions**: Does not throw. A `null` instance returns `false`.

### `EnsureValid`

Ensures that an `OrderCreatedEvent` instance is valid, throwing an exception if it is not.

- **Parameters**:
  - `value` (OrderCreatedEvent?): The event instance to validate.
- **Return value**: `void`.
- **Exceptions**:
  - Throws `ArgumentNullException` if `value` is `null`.
  - Throws `ArgumentException` if `Validate` returns one or more problems. The message starts with `OrderCreatedEvent is invalid. Validation errors:` and then lists each problem on its own line, prefixed with `- `.

## Defaults and Common Failures

A newly constructed `OrderCreatedEvent` is not valid until the caller sets the required fields:

- `EventId` defaults to a new GUID string and `OccurredAt` defaults to `DateTime.UtcNow`, so both pass.
- `Source` defaults to an empty string, which passes because it is optional.
- `OrderId`, `UserId`, and `TotalAmount` default to `0`, so all three fail until they are set.

This means `new OrderCreatedEvent().IsValid()` returns `false`.

## Usage

### Example 1: Checking Validity

```csharp
var orderEvent = new OrderCreatedEvent
{
    OrderId = 42,
    UserId = 7,
    TotalAmount = 129.99m,
    Source = "OrderService"
};

if (orderEvent.IsValid())
{
    Console.WriteLine("OrderCreatedEvent is ready to publish.");
}
```

### Example 2: Collecting Validation Problems

```csharp
var orderEvent = new OrderCreatedEvent
{
    OrderId = 42,
    UserId = 0,
    TotalAmount = 2_000_000m
};

var problems = orderEvent.Validate();

foreach (var problem in problems)
{
    Console.WriteLine($"Validation problem: {problem}");
}
// Validation problem: UserId must be a positive integer, but was 0.
// Validation problem: TotalAmount exceeds maximum allowed value of 1,000,000.00, but was 2000000.00.
```

### Example 3: Failing Fast

```csharp
var orderEvent = new OrderCreatedEvent
{
    OrderId = 42,
    UserId = 7,
    TotalAmount = 0m
};

// Throws ArgumentException:
// "OrderCreatedEvent is invalid. Validation errors:
// - TotalAmount must be a positive decimal, but was 0.00."
orderEvent.EnsureValid();
```

## Notes

*   **Extension Methods:** All members are extension methods on `OrderCreatedEvent?`. They are called on an instance (for example, `orderEvent.IsValid()`) rather than on the class. Because the receiver is nullable, `IsValid` can be called on a `null` reference, while `Validate` and `EnsureValid` throw in that case.
*   **Identifier Checks:** `OrderId` and `UserId` are only checked to be positive. The class does not confirm that the order or user exists.
*   **Exception Format:** `EnsureValid` puts each problem on its own line. `CacheHitEventValidation` joins its problems with a single space instead, so the two formats differ.
*   **GUID Format:** `Guid.TryParse` accepts several GUID formats, including values with or without hyphens and with braces. The check verifies that the value parses, not that it matches the default `D` format that `DomainEvent` produces.
*   **Time-Based Rules:** The `OccurredAt` window is relative to the local clock of the process running the check. Events created on machines with clocks more than 5 seconds ahead of the validating process will fail the future check.
*   **Future Rules:** If `OrderCreatedEvent` gains new properties that need checking, add the rules to `Validate`. `IsValid` and `EnsureValid` are built on top of it and pick up new rules automatically.
