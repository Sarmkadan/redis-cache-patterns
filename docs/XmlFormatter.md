# XML Formatter

Documentation for the `XmlFormatter` class in the `RedisCachePatterns.Formatters` namespace. It implements `IOutputFormatter` and serializes single objects or generic sequences with `System.Xml.Serialization.XmlSerializer`.

## Class: `XmlFormatter`

`XmlFormatter` exposes `application/xml` as its content type. Constructor options control whether an XML declaration is included and whether single-object output is indented.

### Constructor

```csharp
public XmlFormatter(bool includeDeclaration = true, bool indent = true)
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `includeDeclaration` | `true` | Includes an XML declaration in successful output and error output. |
| `indent` | `true` | Enables `XmlWriter` indentation for single-object output and prefixes each serialized collection item with two spaces. |

### Property: `ContentType`

```csharp
public string ContentType => "application/xml";
```

Returns the MIME type `application/xml`.

## Methods

### `Format(object data)`

```csharp
public string Format(object data)
```

Serializes one value using its runtime type.

**Behavior:**

- Creates an `XmlSerializer` for `data.GetType()`.
- Writes UTF-8 XML to an in-memory stream.
- Includes or omits the XML declaration according to `includeDeclaration`.
- Applies the configured `indent` value to `XmlWriterSettings.Indent`.
- Uses the element names, public members, and XML serialization attributes defined by the value's type.
- Catches failures, including a null `data` value or an unsupported type, and returns error XML instead of propagating the exception.

### `Format<T>(IEnumerable<T> data)`

```csharp
public string Format<T>(IEnumerable<T> data)
```

Serializes a sequence inside a generated collection element.

**Behavior:**

- Materializes the sequence with `ToList()` before producing output.
- Names the root element `<TCollection>`, where `T` is `typeof(T).Name`. For example, a sequence of `Product` values uses `<ProductCollection>`.
- Serializes each item separately with an `XmlSerializer` created for `typeof(T)`.
- Omits the XML declaration from each item fragment.
- Preserves the sequence's enumeration order.
- Produces an empty collection root for an empty sequence.
- When `indent` is `true`, prefixes each item fragment with two spaces. The option does not otherwise configure indentation inside item fragments.
- Catches failures while materializing or serializing the sequence and returns error XML. If an item fails, no partial collection output is returned.

The generated collection wrapper is assembled as text rather than serialized by `XmlSerializer`. Consequently, it has no namespace declarations of its own, while individual item elements normally contain the serializer's default namespace declarations unless their type configuration changes that behavior.

## Error Output

When either overload catches an exception, it returns XML with this shape:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Error>
  <Message>Serialization failed: Exception message</Message>
  <Timestamp>2026-01-15T12:34:56.7890000Z</Timestamp>
</Error>
```

Collection failures use the prefix `Collection serialization failed:` instead. The declaration is omitted when `includeDeclaration` is `false`. Error elements retain their fixed two-space indentation regardless of the `indent` constructor option.

The message escapes `&`, `<`, `>`, double quotes, and apostrophes. The timestamp is generated with `DateTime.UtcNow` and the round-trip (`O`) format. Exception messages may contain implementation details, so callers should consider whether the output is appropriate to expose outside a trusted environment.

## Usage Example

Given an XML-serializable type:

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

format a sequence as follows:

```csharp
var formatter = new XmlFormatter();
var products = new[]
{
    new Product { Id = 1, Name = "Coffee" },
    new Product { Id = 2, Name = "Tea" }
};

string xml = formatter.Format(products);
```

The output has a `ProductCollection` wrapper containing one serialized `Product` element per item. To omit the declaration and the two-space item prefixes:

```csharp
var compactFormatter = new XmlFormatter(
    includeDeclaration: false,
    indent: false);

string xml = compactFormatter.Format(products);
```

## Notes

- Types must satisfy `XmlSerializer` requirements, such as exposing a public parameterless constructor where required.
- The sequence overload eagerly enumerates its input and does not stream XML incrementally.
- A null value passed to the single-object overload and a null sequence passed to the generic overload produce error XML.
- Output line endings added by `StringBuilder.AppendLine()` follow the current platform.
- The formatter returns strings only; it does not write to a file or HTTP response.
