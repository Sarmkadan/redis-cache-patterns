# IOutputFormatter

A formatter interface used by the Redis Cache Patterns library to serialize and deserialize data for output operations. It provides registry capabilities for formatters and exposes metadata about the formatted response.

## API

### `FormatterRegistry RegisterFormatter(IOutputFormatter formatter)`

Registers a new formatter in the registry. The formatter is added to the internal collection of available formatters.

- **Parameters**
  - `formatter`: The `IOutputFormatter` instance to register.
- **Return Value**
  - Returns the current `FormatterRegistry` instance to allow method chaining.
- **Exceptions**
  - Throws `ArgumentNullException` if `formatter` is `null`.

---

### `IOutputFormatter GetFormatter(string format)`

Retrieves a registered formatter by its format identifier.

- **Parameters**
  - `format`: The format identifier (e.g., "json", "xml") to look up.
- **Return Value**
  - Returns the `IOutputFormatter` instance associated with the given format, or `null` if not found.
- **Exceptions**
  - Throws `ArgumentNullException` if `format` is `null`.

---

### `bool HasFormatter(string format)`

Checks whether a formatter for the specified format is registered.

- **Parameters**
  - `format`: The format identifier to check.
- **Return Value**
  - Returns `true` if a formatter for the specified format exists; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `format` is `null`.

---
### `IEnumerable<string> GetAvailableFormats()`

Returns a sequence of all registered format identifiers.

- **Return Value**
  - An `IEnumerable<string>` containing all available format identifiers (e.g., "json", "xml").
- **Exceptions**
  - None.

---
### `T Data { get; }`

Gets the data to be formatted.

- **Type**
  - Generic type `T` representing the data payload.
- **Return Value**
  - The data object to be serialized.
- **Exceptions**
  - None.

---
### `string Format { get; }`

Gets the format identifier used for serialization.

- **Return Value**
  - The format identifier (e.g., "json", "xml").
- **Exceptions**
  - None.

---
### `DateTime GeneratedAt { get; }`

Gets the timestamp when the formatted response was generated.

- **Return Value**
  - A `DateTime` indicating when the formatting occurred.
- **Exceptions**
  - None.

---
### `FormattedResponse FormattedResponse { get; }`

Gets the result of the formatting operation.

- **Return Value**
  - A `FormattedResponse` object containing the serialized output and metadata.
- **Exceptions**
  - None.

---
### `override string ToString()`

Returns a string representation of the formatted response.

- **Return Value**
  - A string containing the formatted output.
- **Exceptions**
  - None.

## Usage

### Example 1: Registering and retrieving a formatter
```csharp
var registry = new FormatterRegistry();
var jsonFormatter = new JsonOutputFormatter<MyData>(myData, "json");
registry.RegisterFormatter(jsonFormatter);

// Retrieve the formatter
var formatter = registry.GetFormatter("json");
if (formatter != null)
{
    Console.WriteLine($"Formatter found: {formatter.Format}");
}
```

### Example 2: Formatting data and retrieving metadata
```csharp
var data = new MyData { Id = 1, Name = "Test" };
var formatter = new JsonOutputFormatter<MyData>(data, "json");
var formattedResponse = formatter.FormattedResponse;

Console.WriteLine($"Formatted at: {formatter.GeneratedAt}");
Console.WriteLine($"Output: {formatter.ToString()}");
```

## Notes

- **Thread Safety**: The `RegisterFormatter`, `GetFormatter`, and `HasFormatter` methods are not thread-safe by default. Concurrent access should be synchronized externally if multiple threads may register or retrieve formatters simultaneously.
- **Null Handling**: All methods accepting string parameters (`GetFormatter`, `HasFormatter`) throw `ArgumentNullException` if the input is `null`. Ensure format identifiers are validated before calling these methods.
- **Formatter Identity**: The `GetFormatter` method returns `null` if no formatter is registered for the given format. Always check the return value before use.
- **Timestamp Precision**: The `GeneratedAt` property reflects the time when the formatter instance was created, not when the data was originally produced.
