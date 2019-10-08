#nullable enable
using FluentAssertions;
using Moq;
using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;
using Xunit;

namespace RedisCachePatterns.Tests.Utilities;

public class DistributedLockHelperTests
{
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly string _lockKey = "test-lock";
    private readonly string _lockValue = Guid.NewGuid().ToString();
    private readonly TimeSpan _lockDuration = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task AcquireAsync_WhenLockCanBeAcquired_ReturnsTrue()
    {
        _mockCache.Setup(c => c.AcquireLockAsync(
            _lockKey, It.IsAny<string>(), _lockDuration))
            .ReturnsAsync(true);

        var lockHelper = new DistributedLockHelper(_mockCache.Object, _lockKey, _lockValue, _lockDuration);
        var result = await lockHelper.AcquireAsync();

        result.Should().BeTrue();
        lockHelper.IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task AcquireAsync_WhenLockCannotBeAcquired_ReturnsFalse()
    {
        _mockCache.Setup(c => c.AcquireLockAsync(
            _lockKey, It.IsAny<string>(), _lockDuration))
            .ReturnsAsync(false);

        var lockHelper = new DistributedLockHelper(_mockCache.Object, _lockKey, _lockValue, _lockDuration);
        var result = await lockHelper.AcquireAsync();

        result.Should().BeFalse();
        lockHelper.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseAsync_WhenLockIsHeld_CallsReleaseLockAsync()
    {
        _mockCache.Setup(c => c.AcquireLockAsync(
            _lockKey, It.IsAny<string>(), _lockDuration))
            .ReturnsAsync(true);
        _mockCache.Setup(c => c.ReleaseLockAsync(_lockKey, _lockValue))
            .ReturnsAsync(true);

        var lockHelper = new DistributedLockHelper(_mockCache.Object, _lockKey, _lockValue, _lockDuration);
        await lockHelper.AcquireAsync();
        var result = await lockHelper.ReleaseAsync();

        result.Should().BeTrue();
        _mockCache.Verify(c => c.ReleaseLockAsync(_lockKey, _lockValue), Times.Once);
    }

    [Fact]
    public async Task ReleaseAsync_WhenLockIsNotHeld_ReturnsFalse()
    {
        var lockHelper = new DistributedLockHelper(_mockCache.Object, _lockKey, _lockValue, _lockDuration);
        var result = await lockHelper.ReleaseAsync();

        result.Should().BeFalse();
        _mockCache.Verify(c => c.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithAction_AcquiresLockExecutesActionAndReleases()
    {
        var actionExecuted = false;
        _mockCache.Setup(c => c.AcquireLockAsync(
            _lockKey, It.IsAny<string>(), _lockDuration))
            .ReturnsAsync(true);
        _mockCache.Setup(c => c.ReleaseLockAsync(_lockKey, _lockValue))
            .ReturnsAsync(true);

        var lockHelper = new DistributedLockHelper(_mockCache.Object, _lockKey, _lockValue, _lockDuration);
        var result = await lockHelper.ExecuteAsync(async () =>
        {
            actionExecuted = true;
            await Task.CompletedTask;
        });

        result.Should().BeTrue();
        actionExecuted.Should().BeTrue();
        _mockCache.Verify(c => c.AcquireLockAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
        _mockCache.Verify(c => c.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLockCannotBeAcquired_ReturnsFalse()
    {
        _mockCache.Setup(c => c.AcquireLockAsync(
            _lockKey, It.IsAny<string>(), _lockDuration))
            .ReturnsAsync(false);

        var lockHelper = new DistributedLockHelper(_mockCache.Object, _lockKey, _lockValue, _lockDuration);
        var result = await lockHelper.ExecuteAsync(async () =>
        {
            await Task.CompletedTask;
        });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsyncGeneric_ReturnsActionResult()
    {
        _mockCache.Setup(c => c.AcquireLockAsync(
            _lockKey, It.IsAny<string>(), _lockDuration))
            .ReturnsAsync(true);
        _mockCache.Setup(c => c.ReleaseLockAsync(_lockKey, _lockValue))
            .ReturnsAsync(true);

        var lockHelper = new DistributedLockHelper(_mockCache.Object, _lockKey, _lockValue, _lockDuration);
        var result = await lockHelper.ExecuteAsync(async () =>
        {
            await Task.Delay(10);
            return 42;
        });

        result.Should().Be(42);
        _mockCache.Verify(c => c.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsyncGeneric_WhenLockCannotBeAcquired_ThrowsInvalidOperationException()
    {
        _mockCache.Setup(c => c.AcquireLockAsync(
            _lockKey, It.IsAny<string>(), _lockDuration))
            .ReturnsAsync(false);

        var lockHelper = new DistributedLockHelper(_mockCache.Object, _lockKey, _lockValue, _lockDuration);

        Func<Task> act = () => lockHelper.ExecuteAsync(async () =>
        {
            await Task.CompletedTask;
            return 42;
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{_lockKey}*");
    }

    [Fact]
    public async Task ExecuteAsync_WhenActionThrows_ReleasesLockAndThrows()
    {
        _mockCache.Setup(c => c.AcquireLockAsync(
            _lockKey, It.IsAny<string>(), _lockDuration))
            .ReturnsAsync(true);
        _mockCache.Setup(c => c.ReleaseLockAsync(_lockKey, _lockValue))
            .ReturnsAsync(true);

        var lockHelper = new DistributedLockHelper(_mockCache.Object, _lockKey, _lockValue, _lockDuration);

        Func<Task> act = () => lockHelper.ExecuteAsync(async () =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Test exception");
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
        _mockCache.Verify(c => c.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_WhenLocked_ReleasesLock()
    {
        _mockCache.Setup(c => c.AcquireLockAsync(
            _lockKey, It.IsAny<string>(), _lockDuration))
            .ReturnsAsync(true);
        _mockCache.Setup(c => c.ReleaseLockAsync(_lockKey, _lockValue))
            .ReturnsAsync(true);

        var lockHelper = new DistributedLockHelper(_mockCache.Object, _lockKey, _lockValue, _lockDuration);
        await lockHelper.AcquireAsync();
        await lockHelper.DisposeAsync();

        _mockCache.Verify(c => c.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void LockValue_ReturnsCorrectValue()
    {
        var lockHelper = new DistributedLockHelper(_mockCache.Object, _lockKey, _lockValue, _lockDuration);
        lockHelper.LockValue.Should().Be(_lockValue);
    }

    [Fact]
    public void Constructor_WithoutExplicitLockValue_GeneratesGuid()
    {
        var lockHelper = new DistributedLockHelper(_mockCache.Object, _lockKey);
        lockHelper.LockValue.Should().NotBeNullOrEmpty();
        Guid.TryParse(lockHelper.LockValue, out _).Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithDefaultDuration_UsesDefaultTimespan()
    {
        var lockHelper = new DistributedLockHelper(_mockCache.Object, _lockKey);
        lockHelper.IsLocked.Should().BeFalse();
    }
}
