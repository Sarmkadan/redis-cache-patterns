# SerializationHelperTests
The `SerializationHelperTests` class is designed to test the functionality of serialization and deserialization operations, ensuring that data is correctly converted between its original form and a serialized representation, such as JSON. This class contains a set of test methods that cover various scenarios, including simple objects, null properties, pretty formatting, complex nested objects, and error handling.

## API
* `public int Id`: A property representing a unique identifier.
* `public string? Name`: A property representing a name, which can be null.
* `public decimal Price`: A property representing a price value.
* `public bool IsActive`: A property indicating whether the object is active.
* `public TestObject? Inner`: A property representing a nested test object, which can be null.
* `public List<TestObject>? Items`: A property representing a list of test objects, which can be null.
* `public void Serialize_WithSimpleObject_ReturnsValidJson()`: Tests serialization of a simple object.
* `public void Serialize_WithNullProperties_OmitsNullValues()`: Tests serialization of an object with null properties.
* `public void Serialize_WithPrettyTrue_FormatsWithIndentation()`: Tests serialization with pretty formatting.
* `public void Serialize_WithComplexObject_SerializesAllNestedData()`: Tests serialization of a complex object with nested data.
* `public void Deserialize_WithValidJson_ReturnsObject()`: Tests deserialization of valid JSON.
* `public void Deserialize_WithCamelCaseJson_MapsToProperties()`: Tests deserialization of camelCase JSON.
* `public void Deserialize_WithMissingNullableProperty_StoresNull()`: Tests deserialization of JSON with a missing nullable property.
* `public void Deserialize_WithNestedObject_DeserializesFullStructure()`: Tests deserialization of a nested object.
* `public void Deserialize_WithInvalidJson_ThrowsInvalidOperationException()`: Tests deserialization of invalid JSON, expecting an InvalidOperationException.
* `public void Deserialize_WithWrongType_ThrowsInvalidOperationException()`: Tests deserialization with the wrong type, expecting an InvalidOperationException.
* `public void Serialize_WithEmptyString_ReturnsQuotedEmptyString()`: Tests serialization of an empty string.
* `public void Serialize_WithSpecialCharacters_EscapesCorrectly()`: Tests serialization of special characters.
* `public void Serialize_WithLargeDecimal_PreservesValue()`: Tests serialization of a large decimal value.

## Usage
```csharp
// Example 1: Serializing a simple object
var testObject = new SerializationHelperTests { Id = 1, Name = "Test Object" };
testObject.Serialize_WithSimpleObject_ReturnsValidJson();

// Example 2: Deserializing a complex object
var json = "{\"Id\":1,\"Name\":\"Test Object\",\"Inner\":{\"Id\":2,\"Name\":\"Nested Object\"}}";
var deserializedObject = new SerializationHelperTests();
deserializedObject.Deserialize_WithValidJson_ReturnsObject();
```

## Notes
When using the `SerializationHelperTests` class, be aware of the following edge cases:
* Null properties are omitted during serialization.
* Invalid JSON input will throw an InvalidOperationException during deserialization.
* Deserialization of JSON with missing nullable properties will result in null values for those properties.
* The class is designed for testing purposes and may not be suitable for production use.
* Thread-safety is not explicitly guaranteed, as the class is intended for testing and not for concurrent access.
