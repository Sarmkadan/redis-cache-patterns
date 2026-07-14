#nullable enable
using FluentAssertions;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Utilities;
using Xunit;

/// <summary>
/// Tests for the SerializationHelper class.
/// </summary>
namespace RedisCachePatterns.Tests.Utilities;

public class SerializationHelperTests
{
    private class TestObject
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }

    private class NestedObject
    {
        public int Id { get; set; }
        public TestObject? Inner { get; set; }
        public List<TestObject>? Items { get; set; }
    }

    /// <summary>
    /// Tests that the Serialize method returns valid JSON for a simple object.
    /// </summary>
    [Fact]
    public void Serialize_WithSimpleObject_ReturnsValidJson()
    {
        var obj = new TestObject { Id = 1, Name = "Test", Price = 99.99m, IsActive = true };

        var json = SerializationHelper.Serialize(obj);

        json.Should().Contain("\"id\"");
        json.Should().Contain("\"name\"");
        json.Should().Contain("Test");
        json.Should().NotContain("\n");
    }

    /// <summary>
    /// Tests that the Serialize method omits null values for a simple object.
    /// </summary>
    [Fact]
    public void Serialize_WithNullProperties_OmitsNullValues()
    {
        var obj = new TestObject { Id = 1, Name = null, Price = 50m, IsActive = false };

        var json = SerializationHelper.Serialize(obj);

        json.Should().NotContain("\"name\"");
        json.Should().Contain("\"id\"");
    }

    /// <summary>
    /// Tests that the Serialize method formats the JSON with indentation when pretty is true.
    /// </summary>
    [Fact]
    public void Serialize_WithPrettyTrue_FormatsWithIndentation()
    {
        var obj = new TestObject { Id = 1, Name = "Pretty", Price = 10m, IsActive = true };

        var json = SerializationHelper.Serialize(obj, pretty: true);

        json.Should().Contain("\n");
        json.Should().Contain("  ");
    }

    /// <summary>
    /// Tests that the Serialize method serializes all nested data for a complex object.
    /// </summary>
    [Fact]
    public void Serialize_WithComplexObject_SerializesAllNestedData()
    {
        var obj = new NestedObject
        {
            Id = 1,
            Inner = new TestObject { Id = 10, Name = "Inner", Price = 100m, IsActive = true },
            Items = new List<TestObject>
            {
                new() { Id = 20, Name = "Item1", Price = 20m, IsActive = true },
                new() { Id = 21, Name = "Item2", Price = 21m, IsActive = false }
            }
        };

        var json = SerializationHelper.Serialize(obj);

        json.Should().Contain("\"inner\"");
        json.Should().Contain("\"items\"");
        json.Should().Contain("Item1");
        json.Should().Contain("Item2");
    }

    /// <summary>
    /// Tests that the Deserialize method returns an object from valid JSON.
    /// </summary>
    [Fact]
    public void Deserialize_WithValidJson_ReturnsObject()
    {
        var json = "{\"id\":1,\"name\":\"test\",\"price\":99.99,\"isActive\":true}";

        var obj = SerializationHelper.Deserialize<TestObject>(json);

        obj.Should().NotBeNull();
        obj?.Id.Should().Be(1);
        obj?.Name.Should().Be("test");
        obj?.Price.Should().Be(99.99m);
        obj?.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Tests that the Deserialize method maps camel case JSON properties to properties.
    /// </summary>
    [Fact]
    public void Deserialize_WithCamelCaseJson_MapsToProperties()
    {
        var json = "{\"id\":42,\"name\":\"CamelCase\",\"price\":50.00,\"isActive\":false}";

        var obj = SerializationHelper.Deserialize<TestObject>(json);

        obj.Should().NotBeNull();
        obj?.Id.Should().Be(42);
        obj?.Name.Should().Be("CamelCase");
        obj?.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Tests that the Deserialize method stores null for missing nullable properties.
    /// </summary>
    [Fact]
    public void Deserialize_WithMissingNullableProperty_StoresNull()
    {
        var json = "{\"id\":1,\"price\":99.99,\"isActive\":true}";

        var obj = SerializationHelper.Deserialize<TestObject>(json);

        obj.Should().NotBeNull();
        obj?.Name.Should().BeNull();
    }

    /// <summary>
    /// Tests that the Deserialize method deserializes a nested object.
    /// </summary>
    [Fact]
    public void Deserialize_WithNestedObject_DeserializesFullStructure()
    {
        var json = "{\"id\":1,\"inner\":{\"id\":10,\"name\":\"nested\",\"price\":100.00,\"isActive\":true}," +
                   "\"items\":[{\"id\":20,\"name\":\"item1\",\"price\":20.00,\"isActive\":true}]}";

        var obj = SerializationHelper.Deserialize<NestedObject>(json);

        obj.Should().NotBeNull();
        obj?.Id.Should().Be(1);
        obj?.Inner?.Id.Should().Be(10);
        obj?.Inner?.Name.Should().Be("nested");
        obj?.Items.Should().HaveCount(1);
        obj?.Items?[0].Id.Should().Be(20);
    }

    /// <summary>
    /// Tests that the Deserialize method throws an InvalidOperationException for invalid JSON.
    /// </summary>
    [Fact]
    public void Deserialize_WithInvalidJson_ThrowsInvalidOperationException()
    {
        var invalidJson = "{not valid json}";

        Action act = () => SerializationHelper.Deserialize<TestObject>(invalidJson);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Deserialization failed*");
    }

    /// <summary>
    /// Tests that the Deserialize method throws an InvalidOperationException for wrong type.
    /// </summary>
    [Fact]
    public void Deserialize_WithWrongType_ThrowsInvalidOperationException()
    {
        var json = "{\"id\":1,\"name\":\"test\",\"price\":\"not-a-number\",\"isActive\":true}";

        Action act = () => SerializationHelper.Deserialize<TestObject>(json);

        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// Tests that the Serialize method returns a quoted empty string for an empty string property.
    /// </summary>
    [Fact]
    public void Serialize_WithEmptyString_ReturnsQuotedEmptyString()
    {
        var obj = new TestObject { Id = 1, Name = "", Price = 10m, IsActive = true };

        var json = SerializationHelper.Serialize(obj);

        json.Should().Contain("\"name\"");
    }

    /// <summary>
    /// Tests that the Serialize method escapes special characters correctly.
    /// </summary>
    [Fact]
    public void Serialize_WithSpecialCharacters_EscapesCorrectly()
    {
        var obj = new TestObject { Id = 1, Name = "Test\"Quote'", Price = 10m, IsActive = true };

        var json = SerializationHelper.Serialize(obj);

        var deserialized = SerializationHelper.Deserialize<TestObject>(json);

        deserialized?.Name.Should().Be("Test\"Quote'");
    }

    /// <summary>
    /// Tests that the Serialize method preserves large decimal values.
    /// </summary>
    [Fact]
    public void Serialize_WithLargeDecimal_PreservesValue()
    {
        var obj = new TestObject { Id = 1, Name = "Expensive", Price = 99999.9999m, IsActive = true };

        var json = SerializationHelper.Serialize(obj);
        var deserialized = SerializationHelper.Deserialize<TestObject>(json);

        deserialized?.Price.Should().Be(99999.9999m);
    }

    /// <summary>
    /// Tests that the Deserialize method returns an object of the correct type.
    /// </summary>
    [Fact]
    public void Deserialize_WithType_ReturnsObjectOfCorrectType()
    {
        var json = "{\"id\":1,\"name\":\"test\",\"price\":99.99,\"isActive\":true}";

        var obj = SerializationHelper.Deserialize(json, typeof(TestObject));

        obj.Should().BeOfType<TestObject>();
        ((TestObject?)obj)?.Id.Should().Be(1);
    }

    /// <summary>
    /// Tests that the Serialize and Deserialize methods preserve data in a round trip.
    /// </summary>
    [Fact]
    public void RoundTrip_Serialize_ThenDeserialize_PreservesData()
    {
        var original = new NestedObject
        {
            Id = 1,
            Inner = new TestObject { Id = 10, Name = "Inner", Price = 100m, IsActive = true },
            Items = new List<TestObject>
            {
                new() { Id = 20, Name = "Item1", Price = 20m, IsActive = false }
            }
        };

        var json = SerializationHelper.Serialize(original);
        var deserialized = SerializationHelper.Deserialize<NestedObject>(json);

        deserialized.Should().NotBeNull();
        deserialized?.Id.Should().Be(original.Id);
        deserialized?.Inner?.Name.Should().Be(original.Inner?.Name);
        deserialized?.Items?.Count.Should().Be(original.Items?.Count);
    }
}
