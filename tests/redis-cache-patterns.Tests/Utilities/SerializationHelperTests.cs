#nullable enable
using FluentAssertions;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Utilities;
using Xunit;

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

    [Fact]
    public void Serialize_WithNullProperties_OmitsNullValues()
    {
        var obj = new TestObject { Id = 1, Name = null, Price = 50m, IsActive = false };

        var json = SerializationHelper.Serialize(obj);

        json.Should().NotContain("\"name\"");
        json.Should().Contain("\"id\"");
    }

    [Fact]
    public void Serialize_WithPrettyTrue_FormatsWithIndentation()
    {
        var obj = new TestObject { Id = 1, Name = "Pretty", Price = 10m, IsActive = true };

        var json = SerializationHelper.Serialize(obj, pretty: true);

        json.Should().Contain("\n");
        json.Should().Contain("  ");
    }

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

    [Fact]
    public void Deserialize_WithMissingNullableProperty_StoresNull()
    {
        var json = "{\"id\":1,\"price\":99.99,\"isActive\":true}";

        var obj = SerializationHelper.Deserialize<TestObject>(json);

        obj.Should().NotBeNull();
        obj?.Name.Should().BeNull();
    }

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

    [Fact]
    public void Deserialize_WithInvalidJson_ThrowsInvalidOperationException()
    {
        var invalidJson = "{not valid json}";

        Action act = () => SerializationHelper.Deserialize<TestObject>(invalidJson);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Deserialization failed*");
    }

    [Fact]
    public void Deserialize_WithWrongType_ThrowsInvalidOperationException()
    {
        var json = "{\"id\":1,\"name\":\"test\",\"price\":\"not-a-number\",\"isActive\":true}";

        Action act = () => SerializationHelper.Deserialize<TestObject>(json);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Serialize_WithEmptyString_ReturnsQuotedEmptyString()
    {
        var obj = new TestObject { Id = 1, Name = "", Price = 10m, IsActive = true };

        var json = SerializationHelper.Serialize(obj);

        json.Should().Contain("\"name\"");
    }

    [Fact]
    public void Serialize_WithSpecialCharacters_EscapesCorrectly()
    {
        var obj = new TestObject { Id = 1, Name = "Test\"Quote'", Price = 10m, IsActive = true };

        var json = SerializationHelper.Serialize(obj);

        var deserialized = SerializationHelper.Deserialize<TestObject>(json);

        deserialized?.Name.Should().Be("Test\"Quote'");
    }

    [Fact]
    public void Serialize_WithLargeDecimal_PreservesValue()
    {
        var obj = new TestObject { Id = 1, Name = "Expensive", Price = 99999.9999m, IsActive = true };

        var json = SerializationHelper.Serialize(obj);
        var deserialized = SerializationHelper.Deserialize<TestObject>(json);

        deserialized?.Price.Should().Be(99999.9999m);
    }

    [Fact]
    public void Deserialize_WithType_ReturnsObjectOfCorrectType()
    {
        var json = "{\"id\":1,\"name\":\"test\",\"price\":99.99,\"isActive\":true}";

        var obj = SerializationHelper.Deserialize(json, typeof(TestObject));

        obj.Should().BeOfType<TestObject>();
        ((TestObject?)obj)?.Id.Should().Be(1);
    }

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
