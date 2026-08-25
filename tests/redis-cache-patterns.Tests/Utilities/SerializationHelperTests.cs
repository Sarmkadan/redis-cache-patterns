using FluentAssertions;
using RedisCachePatterns.Utilities;
using Xunit;
using System;

namespace RedisCachePatterns.Tests.Utilities;

/// <summary>
/// Tests for the SerializationHelper class.
/// </summary>
public class SerializationHelperTests
{
    /// <summary>
    /// Verifies that Serialize method returns valid JSON for a simple object.
    /// </summary>
    [Fact]
    public void Serialize_WithSimpleObject_ReturnsValidJson()
    {
        var obj = new TestObject { Id = 1, Name = "Test", Price = 99.99m, IsActive = true };
        var json = SerializationHelper.Serialize(obj);
        json.Should().Contain("\"id\":1");
        json.Should().Contain("\"name\":\"Test\"");
        json.Should().Contain("\"price\":99.99");
        json.Should().Contain("\"isActive\":true");
    }

    /// <summary>
    /// Verifies that Deserialize method returns an object from valid JSON.
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

    private class TestObject
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }

        public override string ToString() => $"SerializationHelperTests {{ Id = {Id}, Name = {Name}, Price = {Price}, IsActive = {IsActive} }}";
    }
}
