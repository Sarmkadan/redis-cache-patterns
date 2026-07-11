using FluentAssertions;
using RedisCachePatterns.Utilities;
using Xunit;

/// <summary>
/// Tests for the CoreFunctionality of the RedisCachePatterns.Utilities namespace.
/// </summary>
namespace RedisCachePatterns.Tests.Utilities;

public class CoreFunctionalityTests
{
    /// <summary>
    /// Tests the BuildKey method to ensure it returns a correctly formatted key.
    /// </summary>
    [Fact]
    public void BuildKey_ShouldReturnCorrectFormattedKey()
    {
        // Act
        var result = CacheKeyHelper.BuildKey("user", 123, "profile");

        // Assert
        result.Should().Be("user:123:profile");
    }

    /// <summary>
    /// Tests the BuildKey method to ensure it ignores null parameters.
    /// </summary>
    [Fact]
    public void BuildKey_ShouldIgnoreNullParameters()
    {
        // Act
        var result = CacheKeyHelper.BuildKey("user", 123, null, "profile");

        // Assert
        result.Should().Be("user:123:profile");
    }

    /// <summary>
    /// Tests the BuildPattern method to ensure it returns a wildcard pattern.
    /// </summary>
    [Fact]
    public void BuildPattern_ShouldReturnWildcardPattern()
    {
        // Act
        var result = CacheKeyHelper.BuildPattern("user", 123);

        // Assert
        result.Should().Be("user:123:*");
    }

    /// <summary>
    /// Tests the IsValidKey method to ensure it returns the correct validation result for various key inputs.
    /// </summary>
    /// <param name="key">The key to validate.</param>
    /// <param name="expected">The expected validation result.</param>
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

    /// <summary>
    /// Tests the NormalizeKey method to ensure it returns a lowercase and trimmed key.
    /// </summary>
    [Fact]
    public void NormalizeKey_ShouldReturnLowercaseAndTrimmedKey()
    {
        // Act
        var result = CacheKeyHelper.NormalizeKey("  USER:KEY:123  ");

        // Assert
        result.Should().Be("user:key:123");
    }

    /// <summary>
    /// Tests the ParseKey method to ensure it returns the correct key parts.
    /// </summary>
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
