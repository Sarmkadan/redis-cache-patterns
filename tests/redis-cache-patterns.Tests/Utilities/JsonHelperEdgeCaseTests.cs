#nullable enable
using FluentAssertions;
using RedisCachePatterns.Utilities;
using Xunit;

namespace RedisCachePatterns.Tests.Utilities;

public sealed class JsonHelperEdgeCaseTests
{
    [Fact]
    public void Serialize_NullObject_ReturnsNullJson()
    {
        var result = JsonHelper.Serialize<TestDto>(null);
        result.Should().Be("null");
    }

    [Fact]
    public void Serialize_ValidObject_ReturnsJson()
    {
        var dto = new TestDto { Name = "test", Value = 42 };
        var result = JsonHelper.Serialize(dto);
        result.Should().Contain("test");
        result.Should().Contain("42");
    }

    [Fact]
    public void Serialize_WithIndent_ReturnsFormattedJson()
    {
        var dto = new TestDto { Name = "test", Value = 42 };
        var result = JsonHelper.Serialize(dto, indent: true);
        result.Should().Contain("\n");
    }

    [Fact]
    public void Deserialize_ValidJson_ReturnsObject()
    {
        var json = "{\"name\":\"test\",\"value\":42}";
        var result = JsonHelper.Deserialize<TestDto>(json);
        result.Should().NotBeNull();
        result!.Name.Should().Be("test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsInvalidOperation()
    {
        var act = () => JsonHelper.Deserialize<TestDto>("not json");
        act.Should().Throw<InvalidOperationException>().WithMessage("*deserialization failed*");
    }

    [Fact]
    public void Deserialize_EmptyJson_ThrowsInvalidOperation()
    {
        var act = () => JsonHelper.Deserialize<TestDto>("");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DeserializeSafe_InvalidJson_ReturnsDefault()
    {
        var fallback = new TestDto { Name = "fallback" };
        var result = JsonHelper.DeserializeSafe("bad json", fallback);
        result.Should().BeSameAs(fallback);
    }

    [Fact]
    public void DeserializeSafe_ValidJson_ReturnsDeserialized()
    {
        var result = JsonHelper.DeserializeSafe<TestDto>("{\"name\":\"ok\",\"value\":1}");
        result.Should().NotBeNull();
        result!.Name.Should().Be("ok");
    }

    [Fact]
    public void Serialize_NullProperty_OmitsProperty()
    {
        var dto = new TestDto { Name = null!, Value = 42 };
        var result = JsonHelper.Serialize(dto);
        result.Should().NotContain("name");
    }

    private sealed class TestDto
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }
}
