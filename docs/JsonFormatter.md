# JSON Formatter

Documentation for the `JsonFormatter` class in the `RedisCachePatterns.Formatters` namespace. It implements `IOutputFormatter` and serializes single objects or generic sequences with `System.Text.Json`.

## Class: `JsonFormatter`

`JsonFormatter` exposes `application/json` as its content type. Each instance creates and retains its own `JsonSerializerOptions`, configured through the constructor.

### Constructor

```csharp
public JsonFormatter(
    bool indent = true,
    JsonUnknownTypeHandling unknownHandling = JsonUnknownTypeHandling.JsonElement)
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `indent` | `true` | Controls whether the generated JSON is indented for readability. |
| `unknownHandling` | `JsonUnknownTypeHandling.JsonElement` | Sets `JsonSerializerOptions.UnknownTypeHandling` on the formatter's options. |

### Property: `ContentType`

```csharp
public string ContentType => "application/json";
```

Returns the MIME type `application/json`.

## Serialization Options

The formatter configures `System.Text.Json` with the following behavior:

- Property names are converted to camel case.
- Properties whose values are `null` are omitted.
- Enum values are written as strings, with camel-case names used by the first configured enum converter.
- JSON is indented by default; pass `false` for compact output.
- The constructor assigns its `unknownHandling` argument to `JsonSerializerOptions.UnknownTypeHandling`. This setting governs deserialization of values declared as `object`; the formatter itself only performs serialization.

These options are fixed for the lifetime of the formatter and are not exposed for later modification.

## Methods

### `Format(object data)`

```csharp
public string Format(object data)
```

Serializes one value using its runtime type.

**Behavior:**

- Produces a JSON object, array, primitive, or `null`, depending on the supplied value.
- Applies the formatter's naming, null-value, enum, and indentation options.
- Catches exceptions raised by serialization and returns a JSON error object instead of propagating the original exception.

### `Format<T>(IEnumerable<T> data)`

```csharp
public string Format<T>(IEnumerable<T> data)
```

Serializes a sequence as a JSON array.

**Behavior:**

- Materializes the sequence with `ToList()` before serialization.
- Preserves the sequence's enumeration order.
- Returns `[]` for an empty sequence.
- Applies the same serializer options as the single-object overload.
- Catches exceptions raised while materializing or serializing the sequence and returns a JSON error object.

## Error Output

When either overload catches an exception, it serializes an anonymous error object with this shape:

```json
{
  "error": "Serialization failed",
  "message": "Exception message"
}
```

The error output follows the formatter's indentation setting. Because both properties are non-null strings, neither is omitted by the null-value policy. Exception messages may contain implementation details, so callers should consider whether the output is appropriate to expose outside a trusted environment.

## Usage Example

```csharp
var formatter = new JsonFormatter();
var products = new[]
{
    new { Id = 1, Name = "Coffee", Description = (string?)null },
    new { Id = 2, Name = "Tea", Description = "Green tea" }
};

string json = formatter.Format(products);
```

The generated text is equivalent to:

```json
[
  {
    "id": 1,
    "name": "Coffee"
  },
  {
    "id": 2,
    "name": "Tea",
    "description": "Green tea"
  }
]
```

For compact output:

```csharp
var formatter = new JsonFormatter(indent: false);
string json = formatter.Format(products);
```

## Notes

- The formatter produces strings only; it does not write to a stream or perform file I/O.
- The sequence overload eagerly enumerates its input and therefore does not stream JSON incrementally.
- A null value passed to the single-object overload is serialized as the JSON literal `null` at runtime.
- A null sequence passed to the generic overload is handled as a serialization failure and produces the error object.
