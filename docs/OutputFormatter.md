# Output Formatter

Documentation for the output-formatting contracts and supporting types in `Formatters/OutputFormatter.cs`, in the `RedisCachePatterns.Formatters` namespace.

## Interface: `IOutputFormatter`

Defines the strategy used to convert a single value or a sequence of values into an output string.

| Member | Description |
|--------|-------------|
| `string Format(object data)` | Formats a single value. The concrete formatter determines serialization and null-handling behavior. |
| `string Format<T>(IEnumerable<T> data)` | Formats a generic sequence. Enumeration and error behavior are determined by the concrete formatter. |
| `string ContentType { get; }` | Gets the MIME type produced by the formatter, such as `application/json` or `text/csv`. |

Implementations can be registered in a `FormatterRegistry` so callers can select a formatting strategy by name.

## Class: `FormatterRegistry`

Stores named `IOutputFormatter` implementations in an in-memory dictionary. Format names are normalized to lowercase when they are registered or looked up.

### `RegisterFormatter(string format, IOutputFormatter formatter)`

```csharp
public FormatterRegistry RegisterFormatter(
    string format,
    IOutputFormatter formatter)
```

Registers a formatter under the supplied format name and returns the same registry instance for fluent configuration.

**Parameters:**

- `format`: Name used to identify the formatter. The name is converted to lowercase before storage.
- `formatter`: Formatter associated with the name.

**Behavior:**

- Replaces the existing formatter when the normalized name is already registered.
- Returns the current `FormatterRegistry` instance.
- A null `format` causes `NullReferenceException` during lowercase conversion.
- The method does not explicitly reject a null `formatter`, despite the non-nullable parameter declaration.

### `GetFormatter(string format)`

```csharp
public IOutputFormatter GetFormatter(string format)
```

Returns the formatter registered for the supplied name. If that name is not present, the registry attempts to return the formatter registered as `json`.

**Fallback behavior:**

- If the requested name exists, its associated value is returned.
- If the requested name does not exist but `json` is registered, the JSON formatter is returned.
- If no `json` key exists, the dictionary indexer throws `KeyNotFoundException`.
- If the `json` key exists with a null value, `InvalidOperationException` is thrown with the message `No default JSON formatter registered`.
- A null `format` causes `NullReferenceException` during lowercase conversion.

### `HasFormatter(string format)`

```csharp
public bool HasFormatter(string format)
```

Returns `true` when the normalized format name is present in the registry; otherwise, returns `false`. A null `format` causes `NullReferenceException` during lowercase conversion.

### `GetAvailableFormats()`

```csharp
public IEnumerable<string> GetAvailableFormats()
```

Returns the registry's normalized format names. The returned enumerable is the dictionary's live key collection rather than a snapshot, so later registry changes are visible when it is enumerated.

### Normalization and Thread Safety

Format names are normalized with `string.ToLower()` using the current culture. The dictionary itself uses its default case-sensitive comparer; case-insensitive behavior comes from normalizing every supplied name.

`FormatterRegistry` is not thread-safe. Concurrent reads and registrations require external synchronization.

## Class: `FormattedResponse<T>`

Wraps a data value with its requested format and the UTC time at which the wrapper was created.

### Constructor

```csharp
public FormattedResponse(T data, string format = "json")
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `data` | None | Value assigned to `Data`. |
| `format` | `"json"` | Format label assigned to `Format`. |

The constructor sets `GeneratedAt` to `DateTime.UtcNow`. The class does not invoke a formatter or serialize `Data`; it only stores the value and metadata.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Data` | `T` | Gets or sets the wrapped data. |
| `Format` | `string` | Gets or sets the format label. |
| `GeneratedAt` | `DateTime` | Gets or sets the timestamp initialized to the current UTC time by the constructor. |

All three properties are mutable after construction.

### `ToString()`

```csharp
public override string ToString()
```

Returns a string in the form `[Format] Data`. String interpolation uses the current values of `Format` and `Data`; a null value is represented by an empty segment.

## Usage Example

```csharp
var registry = new FormatterRegistry()
    .RegisterFormatter("json", new JsonFormatter())
    .RegisterFormatter("csv", new CsvFormatter());

IOutputFormatter formatter = registry.GetFormatter("CSV");
string output = formatter.Format(new[]
{
    new { Id = 1, Name = "Coffee" },
    new { Id = 2, Name = "Tea" }
});

var response = new FormattedResponse<string>(output, "csv");
Console.WriteLine(response); // [csv] Id,Name ...
```

An unknown name uses the registered JSON formatter:

```csharp
IOutputFormatter fallback = registry.GetFormatter("xml");
Console.WriteLine(fallback.ContentType); // application/json
```

## Notes

- Register a `json` formatter before relying on `GetFormatter` fallback behavior.
- `GetAvailableFormats()` exposes names in their normalized lowercase form.
- Neither the registry nor `FormattedResponse<T>` performs output encoding, HTTP response construction, or file I/O.
