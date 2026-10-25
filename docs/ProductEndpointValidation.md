# ProductEndpointValidation

The `ProductEndpointValidation` class provides a set of static extension methods for validating `ProductEndpoint` instances within the `redis-cache-patterns` library. It follows the same `Validate` / `IsValid` / `EnsureValid` shape used by other validation helpers, so callers can check an endpoint's state in a consistent way.

`ProductEndpoint` is a stateless service class that delegates all validation to its method parameters. It has no instance fields to validate, so the validation methods currently always report the endpoint as valid.

## API

### `Validate`

Validates a `ProductEndpoint` instance and returns a list of human-readable validation problems.

- **Parameters**:
  - `value` (ProductEndpoint): The endpoint instance to validate.
- **Return value**: `IReadOnlyList<string>` containing the validation problems. The list is always empty in the current implementation.
- **Exceptions**: Throws `ArgumentNullException` if `value` is `null`.

### `IsValid`

Checks whether a `ProductEndpoint` instance is valid.

- **Parameters**:
  - `value` (ProductEndpoint): The endpoint instance to check.
- **Return value**: `bool` - `true` if `Validate` returns no problems; otherwise, `false`.
- **Exceptions**: Throws `ArgumentNullException` if `value` is `null`, because it calls `Validate` internally.

### `EnsureValid`

Ensures that a `ProductEndpoint` instance is valid, throwing an exception if it is not.

- **Parameters**:
  - `value` (ProductEndpoint): The endpoint instance to validate.
- **Return value**: `void`.
- **Exceptions**:
  - Throws `ArgumentNullException` if `value` is `null`.
  - Throws `ArgumentException` if `Validate` returns one or more problems. The message lists each problem on its own line, prefixed with `ProductEndpoint validation failed:`.

## Usage

### Example 1: Checking Validity

```csharp
var endpoint = new ProductEndpoint(logger, cacheService, repository);

if (endpoint.IsValid())
{
    Console.WriteLine("ProductEndpoint is ready to use.");
}
```

### Example 2: Collecting Validation Problems

```csharp
var problems = endpoint.Validate();

foreach (var problem in problems)
{
    Console.WriteLine($"Validation problem: {problem}");
}
```

### Example 3: Failing Fast

```csharp
var endpoint = new ProductEndpoint(logger, cacheService, repository);

// Throws ArgumentException if validation reports any problems.
endpoint.EnsureValid();
```

## Notes

*   **Extension Methods:** All members are extension methods on `ProductEndpoint`, so they are called on an instance (for example, `endpoint.IsValid()`) rather than on the class.
*   **Future Rules:** If `ProductEndpoint` gains configuration or dependencies that need checking, add the rules to `Validate`. `IsValid` and `EnsureValid` are built on top of it and pick up new rules automatically.
*   **Null Handling:** Passing a `null` instance is treated as a programming error and throws `ArgumentNullException` rather than returning `false`.
