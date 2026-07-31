#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Moq;
using RedisCachePatterns.Exceptions;
using RedisCachePatterns.Services;
using Xunit;

namespace RedisCachePatterns.Tests.Services;

public class CacheCircuitBreakerServiceExtensionsTests
{
    private readonly Mock<ICacheService> _mockInnerCache = new();
    private readonly CacheCircuitBreakerService _sut;

    public CacheCircuitBreakerServiceExtensionsTests()
    {
        _sut = new CacheCircuitBreakerService(_mockInnerCache.Object, failureThreshold: 1, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task IsOpen_ReturnsTrueWhenOpen()
    {
        // Arrange - Force open
        _mockInnerCache
            .Setup(c => c.GetAsync<int>(It.IsAny<string>()))
            .ThrowsAsync(new CacheException("fail"));
        
        await _sut.Invoking(s => s.GetAsync<int>("key")).Should().ThrowAsync<CacheException>();
        
        _sut.State.Should().Be(CacheCircuitState.Open);

        // Act
        _sut.IsOpen().Should().BeTrue();
    }

    [Fact]
    public void IsOpen_ReturnsFalseWhenClosed()
    {
        _sut.State.Should().Be(CacheCircuitState.Closed);
        _sut.IsOpen().Should().BeFalse();
    }

    [Fact]
    public async Task TimeUntilHalfOpen_ReturnsRemainingTimeWhenOpen()
    {
        // Arrange - Force open
        _mockInnerCache
            .Setup(c => c.GetAsync<int>(It.IsAny<string>()))
            .ThrowsAsync(new CacheException("fail"));
        
        await _sut.Invoking(s => s.GetAsync<int>("key")).Should().ThrowAsync<CacheException>();

        // Act
        var remaining = _sut.TimeUntilHalfOpen();

        // Assert
        remaining.Should().BeGreaterThan(TimeSpan.Zero).And.BeLessThanOrEqualTo(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void TimeUntilHalfOpen_ReturnsZeroWhenClosed()
    {
        _sut.State.Should().Be(CacheCircuitState.Closed);
        _sut.TimeUntilHalfOpen().Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task ToStatusString_ReturnsDetailsWhenOpen()
    {
        // Arrange - Force open
        _mockInnerCache
            .Setup(c => c.GetAsync<int>(It.IsAny<string>()))
            .ThrowsAsync(new CacheException("fail"));
        
        await _sut.Invoking(s => s.GetAsync<int>("key")).Should().ThrowAsync<CacheException>();

        // Act
        var status = _sut.ToStatusString();

        // Assert
        status.Should().StartWith("Open");
        status.Should().Contain("Remaining:");
    }

    [Fact]
    public void ToStatusString_ReturnsStateWhenClosed()
    {
        _sut.State.Should().Be(CacheCircuitState.Closed);
        _sut.ToStatusString().Should().Be("Closed");
    }
}
