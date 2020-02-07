#nullable enable
using FluentAssertions;
using Moq;
using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;
using Xunit;

namespace RedisCachePatterns.Tests.Utilities;

/// <summary>
/// Contains unit tests for the <see cref="DistributedLockHelper"/> class,
/// verifying its lock acquisition, release, execution flow, and disposal behavior.
/// </summary>
public class DistributedLockHelperTests
{
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly string _lockKey = "test-lock";
    private readonly string _lockValue = Guid.NewGuid().ToString();
    private readonly TimeSpan _lockDuration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Verifies that <see cref="DistributedLockHelper.AcquireAsync"/> returns <c>true</c>
    /// when the underlying cache service reports that the lock could be acquired,
    /// and that the helper reports it is locked.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="DistributedLockHelper.AcquireAsync"/> returns <c>false</c>
    /// when the underlying cache service cannot acquire the lock,
    /// and that the helper reports it is not locked.
    /// </summary>
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

    /// <summary>
    /// Ensures that when a lock is held, calling <see cref="DistributedLockHelper.ReleaseAsync"/>
    /// invokes <see cref="ICacheService.ReleaseLockAsync"/> and returns <c>true</c>.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="DistributedLockHelper.ReleaseAsync"/> returns <c>false</c>
    /// when no lock is currently held and does not call the cache service.
    /// </summary>
    [Fact]
    public async Task ReleaseAsync_WhenLockIsNotHeld_ReturnsFalse()
    {
        var lockHelper = new DistributedLockHelper(_mockCache.Object, _lockKey, _lockValue, _lockDuration);
        var result = await lockHelper.ReleaseAsync();

        result.Should().BeFalse();
        _mockCache.Verify(c => c.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Tests that <see cref="DistributedLockHelper.ExecuteAsync(Func{Task})"/> acquires the lock,
    /// executes the provided action, and releases the lock afterwards.
    /// </summary>
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

    /// <summary>
    /// Ensures that <see cref="DistributedLockHelper.ExecuteAsync(Func{Task})"/> returns <c>false</c>
    /// when the lock cannot be acquired.
    /// </summary>
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

    /// <summary>
    /// Verifies that the generic <see cref="DistributedLockHelper.ExecuteAsync{TResult}(Func{Task{TResult}})"/>
    /// returns the result of the supplied action when the lock is successfully acquired.
    /// </summary>
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

    /// <summary>
    /// Confirms that the generic <see cref="DistributedLockHelper.ExecuteAsync{TResult}(Func{Task{TResult}})"/>
    /// throws <see cref="InvalidOperationException"/> when the lock cannot be acquired.
    /// </summary>
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

    /// <summary>
    /// Ensures that if the action passed to <see cref="DistributedLockHelper.ExecuteAsync(Func{Task})"/>
    /// throws an exception, the lock is still released and the exception propagates.
    /// </summary>
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

    /// <summary>
    /// Verifies that disposing the helper while a lock is held releases the lock via the cache service.
    /// </summary>
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

    /// <summary>
    /// Checks that the <see cref="DistributedLockHelper.LockValue"/> property returns the value
    /// supplied to the constructor.
    /// </summary>
    [Fact]
    public void LockValue_ReturnsCorrectValue()
    {
        var lockHelper = new DistributedLockHelper(_mockCache.Object, _lockKey, _lockValue, _lockDuration);
        lockHelper.LockValue.Should().Be(_lockValue);
    }

    /// <summary>
    /// Confirms that when the constructor is called without an explicit lock value,
    /// a new GUID string is generated for <see cref="DistributedLockHelper.LockValue"/>.
    /// </summary>
    [Fact]
    public void Constructor_WithoutExplicitLockValue_GeneratesGuid()
    {
        var lockHelper = new DistributedLockHelper(_mockCache.Object, _lockKey);
        lockHelper.LockValue.Should().NotBeNullOrEmpty();
        Guid.TryParse(lockHelper.LockValue, out _).Should().BeTrue();
    }

    /// <summary>
    /// Validates that constructing a <see cref="DistributedLockHelper"/> with the default
    /// duration does not mark the helper as locked.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaultDuration_UsesDefaultTimespan()
    {
        var lockHelper = new DistributedLockHelper(_mockCache.Object, _lockKey);
        lockHelper.IsLocked.Should().BeFalse();
    }
}
