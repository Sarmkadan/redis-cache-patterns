using FluentAssertions;
using RedisCachePatterns.API;
using Xunit;

namespace RedisCachePatterns.Tests.API;

public class AnalyticsEndpointExtensionsTests
{
    [Fact]
    public void FormatTopN_ShouldFormatCorrectly()
    {
        var items = new List<string> { "a", "b", "c" };
        var result = AnalyticsEndpointExtensions.FormatTopN(items, 2, s => s.ToUpper());
        
        result.Should().Be("1. A\n2. B\n");
    }

    [Fact]
    public void CalculatePercentage_ShouldReturnCorrectValue()
    {
        AnalyticsEndpointExtensions.CalculatePercentage(50, 100).Should().Be(50);
        AnalyticsEndpointExtensions.CalculatePercentage(1, 4).Should().Be(25);
        AnalyticsEndpointExtensions.CalculatePercentage(0, 100).Should().Be(0);
        AnalyticsEndpointExtensions.CalculatePercentage(100, 0).Should().Be(0);
    }
}
