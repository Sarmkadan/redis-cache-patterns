using FluentAssertions;
using RedisCachePatterns.Utilities;
using Xunit;

namespace RedisCachePatterns.Tests.Utilities;

public class CoreFunctionalityTests
{
    [Fact]
    public void BuildKey_ShouldReturnCorrectFormattedKey()
    {
        // Act
        var result = CacheKeyHelper.BuildKey("user", 123, "profile");

        // Assert
        result.Should().Be("user:123:profile");
    }

    [Fact]
    public void BuildKey_ShouldIgnoreNullParameters()
    {
        // Act
        var result = CacheKeyHelper.BuildKey("user", 123, null, "profile");

        // Assert
        result.Should().Be("user:123:profile");
    }

    [Fact]
    public void BuildPattern_ShouldReturnWildcardPattern()
    {
        // Act
        var result = CacheKeyHelper.BuildPattern("user", 123);

        // Assert
        result.Should().Be("user:123:*");
    }

    [Theory]
    [InlineData("valid:key", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("key\nwith\rnewline", false)]
    public void IsValidKey_ShouldReturnCorrectValidationResult(string key, bool expected)
    {
        // Act
        var result = CacheKeyHelper.IsValidKey(key);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void NormalizeKey_ShouldReturnLowercaseAndTrimmedKey()
    {
        // Act
        var result = CacheKeyHelper.NormalizeKey("  USER:KEY:123  ");

        // Assert
        result.Should().Be("user:key:123");
    }

    [Fact]
    public void ParseKey_ShouldReturnCorrectParts()
    {
        // Act
        var result = CacheKeyHelper.ParseKey("user:123:profile");

        // Assert
        result.Should().HaveCount(3);
        result.Should().ContainInOrder("user", "123", "profile");
    }
}
