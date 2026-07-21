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

/// <summary>
/// Contains unit tests for the <see cref="CacheCircuitBreakerService"/> class.
/// </summary>
public class CacheCircuitBreakerServiceTests
{
    private readonly Mock<ICacheService> _mockInnerCache = new();
    private readonly CacheCircuitBreakerService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheCircuitBreakerServiceTests"/> class,
    /// setting up mocks for the inner cache service.
    /// </summary>
    public CacheCircuitBreakerServiceTests()
    {
        _sut = new CacheCircuitBreakerService(_mockInnerCache.Object, failureThreshold: 3, TimeSpan.FromMilliseconds(50));
    }

    /// <summary>
    /// Verifies that the circuit breaker opens after reaching the failure threshold.
    /// </summary>
    [Fact]
    public async Task GetOrLoadAsync_AfterThresholdFailures_CircuitOpens()
    {
        // Arrange
        _mockInnerCache
            .Setup(c => c.GetOrLoadAsync<int>(It.IsAny<string>(), It.IsAny<Func<Task<int>>>(), It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new CacheException("Cache failure"));

        // Act & Assert - First 2 failures should not open circuit
        for (int i = 0; i < 2; i++)
        {
            await _sut.Invoking(s => s.GetOrLoadAsync<int>("key", () => Task.FromResult(42)))
                .Should().ThrowAsync<CacheException>();

            _sut.State.Should().Be(CacheCircuitState.Closed, $"After {i + 1} failures");
            _sut.ConsecutiveFailures.Should().Be(i + 1);
        }

        // Third failure should open the circuit
        await _sut.Invoking(s => s.GetOrLoadAsync<int>("key", () => Task.FromResult(42)))
            .Should().ThrowAsync<CacheException>();

        _sut.State.Should().Be(CacheCircuitState.Open);
        _sut.ConsecutiveFailures.Should().Be(3);
    }

    /// <summary>
    /// Verifies that GetOrLoadAsync bypasses the inner cache and calls loadFn directly when circuit is open.
    /// </summary>
    [Fact]
    public async Task GetOrLoadAsync_WhenCircuitOpen_CallsLoadFnDirectly()
    {
        // Arrange - Force circuit to open
        _mockInnerCache
            .Setup(c => c.GetOrLoadAsync<int>(It.IsAny<string>(), It.IsAny<Func<Task<int>>>(), It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new CacheException("Cache failure"));

        // Cause 3 failures to open circuit
        for (int i = 0; i < 3; i++)
        {
            await _sut.Invoking(s => s.GetOrLoadAsync<int>("key", () => Task.FromResult(42)))
                .Should().ThrowAsync<CacheException>();
        }

        _sut.State.Should().Be(CacheCircuitState.Open);

        // Reset mock to verify loadFn is called
        _mockInnerCache.Reset();

        // Act - This should bypass inner cache and call loadFn directly
        var result = await _sut.GetOrLoadAsync<int>("test-key", () => Task.FromResult(99));

        // Assert
        result.Should().Be(99);
        _mockInnerCache.Verify(c => c.GetOrLoadAsync<int>(It.IsAny<string>(), It.IsAny<Func<Task<int>>>(), It.IsAny<TimeSpan?>()), Times.Never);
    }

    /// <summary>
    /// Verifies that GetAsync returns default(T) when circuit is open.
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenCircuitOpen_ReturnsDefault()
    {
        // Arrange - Force circuit to open
        _mockInnerCache
            .Setup(c => c.GetAsync<int>(It.IsAny<string>()))
            .ThrowsAsync(new CacheException("Cache failure"));

        // Cause 3 failures to open circuit
        for (int i = 0; i < 3; i++)
        {
            await _sut.Invoking(s => s.GetAsync<int>("key"))
                .Should().ThrowAsync<CacheException>();
        }

        _sut.State.Should().Be(CacheCircuitState.Open);

        // Reset mock to verify inner cache is not called
        _mockInnerCache.Reset();

        // Act
        var result = await _sut.GetAsync<int>("test-key");

        // Assert
        result.Should().Be(default(int)); // Should return 0 for int
        _mockInnerCache.Verify(c => c.GetAsync<int>(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Verifies that SetAsync is a no-op when circuit is open.
    /// </summary>
    [Fact]
    public async Task SetAsync_WhenCircuitOpen_DoesNotCallInnerCache()
    {
        // Arrange - Force circuit to open
        _mockInnerCache
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new CacheException("Cache failure"));

        // Cause 3 failures to open circuit
        for (int i = 0; i < 3; i++)
        {
            await _sut.Invoking(s => s.SetAsync<int>("key", 42))
                .Should().ThrowAsync<CacheException>();
        }

        _sut.State.Should().Be(CacheCircuitState.Open);

        // Reset mock to verify inner cache is not called
        _mockInnerCache.Reset();

        // Act
        await _sut.SetAsync<int>("test-key", 99);

        // Assert
        _mockInnerCache.Verify(c => c.SetAsync<int>(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan?>()), Times.Never);
    }

    /// <summary>
    /// Verifies that RemoveAsync is a no-op when circuit is open.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_WhenCircuitOpen_DoesNotCallInnerCache()
    {
        // Arrange - Force circuit to open
        _mockInnerCache
            .Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .ThrowsAsync(new CacheException("Cache failure"));

        // Cause 3 failures to open circuit
        for (int i = 0; i < 3; i++)
        {
            await _sut.Invoking(s => s.RemoveAsync("key"))
                .Should().ThrowAsync<CacheException>();
        }

        _sut.State.Should().Be(CacheCircuitState.Open);

        // Reset mock to verify inner cache is not called
        _mockInnerCache.Reset();

        // Act
        await _sut.RemoveAsync("test-key");

        // Assert
        _mockInnerCache.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Verifies that RecordSuccess resets failures when closed.
    /// </summary>
    [Fact]
    public void RecordSuccess_WhenClosed_ResetsFailures()
    {
        // Arrange - Set some failures while closed
        for (int i = 0; i < 3; i++)
        {
            _sut.RecordFailure();
        }

        _sut.State.Should().Be(CacheCircuitState.Closed);
        _sut.ConsecutiveFailures.Should().Be(3);

        // Act
        _sut.RecordSuccess();

        // Assert
        _sut.State.Should().Be(CacheCircuitState.Closed);
        _sut.ConsecutiveFailures.Should().Be(0);
        _sut.OpenedAtUtc.Should().BeNull();
    }

    /// <summary>
    /// Verifies that RecordFailure increments failures and opens circuit at threshold.
    /// </summary>
    [Fact]
    public void RecordFailure_IncrementAndOpenAtThreshold()
    {
        // Arrange
        _sut.State.Should().Be(CacheCircuitState.Closed);
        _sut.ConsecutiveFailures.Should().Be(0);

        // Act & Assert - First 2 failures should increment but not open
        for (int i = 0; i < 2; i++)
        {
            _sut.RecordFailure();
            _sut.State.Should().Be(CacheCircuitState.Closed, $"After {i + 1} failures");
            _sut.ConsecutiveFailures.Should().Be(i + 1);
        }

        // Third failure should open circuit
        _sut.RecordFailure();
        _sut.State.Should().Be(CacheCircuitState.Open);
        _sut.ConsecutiveFailures.Should().Be(3);
        _sut.OpenedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that Reset manually closes circuit and resets failures.
    /// </summary>
    [Fact]
    public void Reset_ClosesCircuitAndResetsFailures()
    {
        // Arrange - Open the circuit
        _mockInnerCache
            .Setup(c => c.GetOrLoadAsync<int>(It.IsAny<string>(), It.IsAny<Func<Task<int>>>(), It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new CacheException("Cache failure"));

        // Cause 3 failures to open circuit
        for (int i = 0; i < 3; i++)
        {
            _sut.RecordFailure();
        }

        _sut.State.Should().Be(CacheCircuitState.Open);
        _sut.ConsecutiveFailures.Should().Be(3);
        _sut.OpenedAtUtc.Should().NotBeNull();

        // Act
        _sut.Reset();

        // Assert
        _sut.State.Should().Be(CacheCircuitState.Closed);
        _sut.ConsecutiveFailures.Should().Be(0);
        _sut.OpenedAtUtc.Should().BeNull();
    }
}
