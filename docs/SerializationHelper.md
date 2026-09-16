# SerializationHelper

`SerializationHelper` is a static utility that provides consistent JSON serialization and deserialization through `System.Text.Json`. It applies camel-case property names, ignores null properties when writing, matches property names case-insensitively when reading, and translates JSON processing failures into contextual `InvalidOperationException` instances.

## API

### `Serialize<T>`

```csharp
public static string Serialize<T>(T value, bool pretty = false)
```

Serializes `value` to JSON using the helper's shared options.

- **Parameters**:
  - `value` — the value to serialize.
  - `pretty` — `true` to indent the output; otherwise, `false` to produce compact JSON. The default is `false`.
- **Returns**: The serialized JSON string.
- **Throws**: `InvalidOperationException` when `System.Text.Json` throws a `JsonException`. The original exception is available through `InnerException`. Other exceptions raised by the serializer are not wrapped.

### `Deserialize<T>`

```csharp
public static T? Deserialize<T>(string json)
```

Deserializes JSON into the requested compile-time type using the default, compact option set. JSON property-name matching is case-insensitive.

- **Parameters**: `json` — the JSON string to deserialize.
- **Returns**: The deserialized value, or `null` when the JSON value is `null` or the serializer otherwise produces no value.
- **Throws**: `InvalidOperationException` when `System.Text.Json` throws a `JsonException`, such as for malformed JSON or an incompatible JSON value. The original exception is available through `InnerException`. Argument-related and other non-JSON exceptions are not wrapped.

### `Deserialize`

```csharp
public static object? Deserialize(string json, Type type)
```

Deserializes JSON when the target type is selected at runtime.

- **Parameters**:
  - `json` — the JSON string to deserialize.
  - `type` — the runtime type to create.
- **Returns**: The deserialized object, or `null` when the JSON value is `null` or the serializer otherwise produces no value.
- **Throws**: `InvalidOperationException` when `System.Text.Json` throws a `JsonException`. The message identifies the requested type, and the original exception is available through `InnerException`. Argument-related and other non-JSON exceptions are not wrapped.

## Usage

### Serialize a cache value

```csharp
using RedisCachePatterns.Utilities;

var product = new
{
    Id = 42,
    DisplayName = "Mechanical Keyboard",
    Description = (string?)null
};

string json = SerializationHelper.Serialize(product);
// {"id":42,"displayName":"Mechanical Keyboard"}
```

### Produce indented JSON

```csharp
string readableJson = SerializationHelper.Serialize(product, pretty: true);
```

Pretty output uses the same naming and null-handling rules as compact output; only indentation changes.

### Deserialize to a generic type

```csharp
string json = """
    {"id":42,"displayName":"Mechanical Keyboard"}
    """;

Product? product = SerializationHelper.Deserialize<Product>(json);
```

### Deserialize to a runtime type

```csharp
Type targetType = typeof(Product);
object? value = SerializationHelper.Deserialize(json, targetType);

if (value is Product product)
{
    Console.WriteLine(product.DisplayName);
}
```

## Serialization Options

Both compact and pretty serialization use these settings:

- Property names are written in camel case.
- Properties whose values are `null` are omitted.
- Property-name matching during deserialization is case-insensitive.
- No custom converters are registered by the helper.

Compact serialization uses `WriteIndented = false`; pretty serialization uses `WriteIndented = true`. Deserialization always uses the compact option set, although the indentation setting does not affect reading JSON.

## Notes

- **Consistent failures**: JSON-specific failures are wrapped in `InvalidOperationException` with the target type name in the message. Inspect `InnerException` for the original `JsonException`.
- **No fallback value on invalid JSON**: Unlike a try-style API, invalid or incompatible JSON throws instead of returning `null` or a default value.
- **Null values**: Serializing a null root value produces the JSON literal `null`. Null object properties are omitted. Deserializing the JSON literal `null` can return `null`.
- **Thread safety**: The helper reuses private serializer option instances without modifying them after initialization, so its methods can be called concurrently.
- **Type metadata**: The runtime-type overload uses the supplied `Type` directly and does not embed or resolve polymorphic type metadata on its own.
