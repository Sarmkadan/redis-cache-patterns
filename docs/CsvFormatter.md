# CSV Formatter

Documentation for the `CsvFormatter` class in the `RedisCachePatterns.Formatters` namespace. It implements `IOutputFormatter` and generates CSV text from objects by inspecting their properties at runtime.

## Class: `CsvFormatter`

`CsvFormatter` exposes `text/csv` as its content type and supports formatting either a single object or a generic sequence. It uses reflection to obtain public properties, writes their names as the header, and converts each property value to text by calling `ToString()`.

### Constructor

```csharp
public CsvFormatter(string delimiter = ",")
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `delimiter` | `","` | Text inserted between columns. It is also used when deciding whether a value must be quoted. |

### Property: `ContentType`

```csharp
public string ContentType => "text/csv";
```

Returns the MIME type `text/csv`.

## Reflection-Based Generation

The formatter discovers properties from the runtime type for `Format(object)` and from `typeof(T)` for `Format<T>(IEnumerable<T>)`. Property names become column headers, and property values are read in the same reflection-provided order for every row.

Property values are converted with `ToString()`. A `null` property value is written as an empty field. Nested objects and collections are not flattened; their own `ToString()` result is used.

The formatter does not define a column sort order. Reflection property order is therefore the order returned by the runtime and should not be treated as a stable CSV schema across runtime or type changes.

## Methods

### `Format(object data)`

Formats one object as a header row followed by one data row.

**Behavior:**

- Returns an empty string when `data` is `null`.
- Reflects over the object's type to find public properties.
- If no properties are found, returns the escaped result of `data.ToString()` without adding a header or line ending.
- Otherwise, writes the property names as a header, appends a platform-specific line ending, and then writes the property values.
- Does not append a line ending after the data row.

### `Format<T>(IEnumerable<T> data)`

Formats a sequence as one header row followed by one row per item.

**Behavior:**

- Materializes the input with `ToList()` before producing output.
- Returns an empty string for an empty sequence.
- Uses the public properties declared by the generic type `T` as columns.
- Appends a platform-specific line ending after the header and after every data row, including the last row.

The sequence and its items are expected to be non-null. A null sequence fails while it is being materialized, and a null item fails when the formatter attempts to read its properties.

## Delimiter and Escaping

The delimiter defaults to a comma but can be replaced through the constructor, for example with `";"` for semicolon-separated output. The configured delimiter is used both between fields and as the delimiter text checked inside field values.

A non-empty value is enclosed in double quotes when it contains any of the following:

- The configured delimiter
- A double quote (`"`)
- A line-feed character (`\n`)

Inside a quoted value, each double quote is doubled. Empty strings and null property values produce empty, unquoted fields. A carriage return (`\r`) by itself does not trigger quoting.

For example, with the default delimiter, the value `Smith, "Sam"` becomes:

```text
"Smith, ""Sam"""
```

## Usage Example

```csharp
var formatter = new CsvFormatter();
var products = new[]
{
    new { Id = 1, Name = "Coffee, dark roast" },
    new { Id = 2, Name = "Tea" }
};

string csv = formatter.Format(products);
```

The generated text is equivalent to:

```text
Id,Name
1,"Coffee, dark roast"
2,Tea
```

To use a different delimiter:

```csharp
var formatter = new CsvFormatter(";");
string csv = formatter.Format(products);
```

## Notes

- CSV line endings are produced by `StringBuilder.AppendLine()` and therefore follow the current platform.
- Formatting is culture-sensitive because values are converted using their parameterless `ToString()` methods.
- The formatter does not prepend a byte-order mark or perform file I/O.
