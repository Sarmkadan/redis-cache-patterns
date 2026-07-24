#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Infrastructure.Cache;
using RedisCachePatterns.Services;
using StackExchange.Redis;
using Xunit;

namespace RedisCachePatterns.Tests.Services;

/// <summary>
/// Unit tests for Lua-script-based distributed lock release and renewal operations.
/// These tests verify that the atomic Lua scripts prevent race conditions:
/// - Lock release only succeeds when the current holder matches the expected value
/// - Lock renewal only succeeds when the current holder matches the expected value
/// - Concurrent operations maintain atomicity and prevent accidental lock manipulation
/// </summary>
public class RedisCacheServiceLockTests
{
    private readonly Mock<IRedisConnection> _mockRedisConnection = new();
    private readonly Mock<IDatabase> _mockDatabase = new();
    private readonly Mock<ILogger<RedisCacheService>> _mockLogger = new();
    private readonly RedisCacheService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisCacheServiceLockTests"/> class,
    /// setting up mocks for Redis connection, database, and logger.
    /// </summary>
    public RedisCacheServiceLockTests()
    {
        // Setup Redis connection to return mocked database
        _mockRedisConnection.Setup(c => c.GetDatabase(It.IsAny<int>()))
            .Returns(_mockDatabase.Object);

        _sut = new RedisCacheService(_mockRedisConnection.Object, _mockLogger.Object);
    }

    #region ReleaseLockAsync Tests

    /// <summary>
    /// Verifies that ReleaseLockAsync succeeds when releasing a lock you own.
    /// The Lua script should return 1 (success) and delete the key.
    /// </summary>
    [Fact]
    public async Task ReleaseLockAsync_WhenReleasingOwnedLock_SucceedsAndRemovesKey()
    {
        // Arrange
        const string lockKey = "distributed:lock:resource1";
        const string lockValue = "owner-token-123";

        // Mock the Lua script evaluation to return RedisResult with value 1 (success)
        var successResult = RedisResult.Create(1);
        _mockDatabase.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<LuaScript>(),
                It.IsAny<object>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(successResult); // Lua script returns 1 for successful deletion

        // Act
        var result = await _sut.ReleaseLockAsync(lockKey, lockValue);

        // Assert
        result.Should().BeTrue("Lock release should succeed when releasing owned lock");
        _mockDatabase.Verify(db => db.ScriptEvaluateAsync(
            It.IsAny<LuaScript>(),
            It.Is<object>(args =>
                ((RedisKey)args.GetType().GetProperty("key")!.GetValue(args)!).ToString() == lockKey &&
                args.GetType().GetProperty("value")!.GetValue(args)!.ToString() == lockValue),
            It.IsAny<CommandFlags>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that ReleaseLockAsync fails when trying to release a lock owned by a different token.
    /// The Lua script should return 0 (no-op) and NOT delete the key.
    /// This is the critical race condition prevention test.
    /// </summary>
    [Fact]
    public async Task ReleaseLockAsync_WhenReleasingLockOwnedByOtherToken_FailsAndDoesNotDeleteKey()
    {
        // Arrange
        const string lockKey = "distributed:lock:resource1";
        const string myLockValue = "my-token-123";

        // Mock the Lua script evaluation to return RedisResult with value 0 (failure)
        var failureResult = RedisResult.Create(0);
        _mockDatabase.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<LuaScript>(),
                It.IsAny<object>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(failureResult); // Lua script returns 0 when value doesn't match

        // Act
        var result = await _sut.ReleaseLockAsync(lockKey, myLockValue);

        // Assert
        result.Should().BeFalse("Lock release should fail when trying to release lock owned by another token");

        // Verify the Lua script was called with correct parameters
        _mockDatabase.Verify(db => db.ScriptEvaluateAsync(
            It.IsAny<LuaScript>(),
            It.Is<object>(args =>
                ((RedisKey)args.GetType().GetProperty("key")!.GetValue(args)!).ToString() == lockKey &&
                args.GetType().GetProperty("value")!.GetValue(args)!.ToString() == myLockValue),
            It.IsAny<CommandFlags>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that ReleaseLockAsync fails when the lock has already expired.
    /// The Lua script should return 0 (no-op) since the key doesn't exist.
    /// </summary>
    [Fact]
    public async Task ReleaseLockAsync_WhenLockHasExpired_FailsGracefully()
    {
        // Arrange
        const string lockKey = "distributed:lock:expired";
        const string lockValue = "expired-token-123";

        // Mock the Lua script evaluation to return RedisResult with value 0 (failure)
        var expiredResult = RedisResult.Create(0);
        _mockDatabase.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<LuaScript>(),
                It.IsAny<object>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(expiredResult); // Lua script returns 0 when key doesn't exist

        // Act
        var result = await _sut.ReleaseLockAsync(lockKey, lockValue);

        // Assert
        result.Should().BeFalse("Lock release should fail gracefully when lock has expired");
    }

    /// <summary>
    /// Verifies that ReleaseLockAsync throws ArgumentNullException for null lock key.
    /// </summary>
    [Fact]
    public async Task ReleaseLockAsync_WithNullLockKey_ThrowsArgumentNullException()
    {
        // Arrange
        const string lockValue = "token-123";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.ReleaseLockAsync(null!, lockValue));
    }

    /// <summary>
    /// Verifies that ReleaseLockAsync throws ArgumentNullException for null lock value.
    /// </summary>
    [Fact]
    public async Task ReleaseLockAsync_WithNullLockValue_ThrowsArgumentNullException()
    {
        // Arrange
        const string lockKey = "distributed:lock:resource1";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.ReleaseLockAsync(lockKey, null!));
    }

    /// <summary>
    /// Verifies that ReleaseLockAsync throws ArgumentNullException for empty lock key.
    /// </summary>
    [Fact]
    public async Task ReleaseLockAsync_WithEmptyLockKey_ThrowsArgumentNullException()
    {
        // Arrange
        const string lockValue = "token-123";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.ReleaseLockAsync("", lockValue));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.ReleaseLockAsync(" ", lockValue));
    }

    /// <summary>
    /// Verifies that ReleaseLockAsync throws ArgumentNullException for empty lock value.
    /// </summary>
    [Fact]
    public async Task ReleaseLockAsync_WithEmptyLockValue_ThrowsArgumentNullException()
    {
        // Arrange
        const string lockKey = "distributed:lock:resource1";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.ReleaseLockAsync(lockKey, ""));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.ReleaseLockAsync(lockKey, " "));
    }

    #endregion

    #region RenewLockAsync Tests

    /// <summary>
    /// Verifies that RenewLockAsync succeeds when renewing a lock you own.
    /// The Lua script should return 1 (success) and extend the TTL.
    /// </summary>
    [Fact]
    public async Task RenewLockAsync_WhenRenewingOwnedLock_SucceedsAndExtendsTTL()
    {
        // Arrange
        const string lockKey = "distributed:lock:resource2";
        const string lockValue = "owner-token-456";
        var newDuration = TimeSpan.FromSeconds(30);
        var ttlMs = (long)newDuration.TotalMilliseconds;

        // Mock the Lua script evaluation to return RedisResult with value 1 (success)
        var successResult = RedisResult.Create(1);
        _mockDatabase.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<LuaScript>(),
                It.IsAny<object>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(successResult); // Lua script returns 1 for successful TTL extension

        // Act
        var result = await _sut.RenewLockAsync(lockKey, lockValue, newDuration);

        // Assert
        result.Should().BeTrue("Lock renewal should succeed when renewing owned lock");

        // Verify the Lua script was called with correct parameters including TTL
        _mockDatabase.Verify(db => db.ScriptEvaluateAsync(
            It.IsAny<LuaScript>(),
            It.Is<object>(args =>
                ((RedisKey)args.GetType().GetProperty("key")!.GetValue(args)!).ToString() == lockKey &&
                args.GetType().GetProperty("value")!.GetValue(args)!.ToString() == lockValue &&
                (long)args.GetType().GetProperty("ttl")!.GetValue(args)! == ttlMs),
            It.IsAny<CommandFlags>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that RenewLockAsync fails when trying to renew a lock owned by a different token.
    /// The Lua script should return 0 (no-op) and NOT extend the TTL.
    /// This prevents the race condition where you might extend someone else's lock.
    /// </summary>
    [Fact]
    public async Task RenewLockAsync_WhenRenewingLockOwnedByOtherToken_FailsAndDoesNotExtendTTL()
    {
        // Arrange
        const string lockKey = "distributed:lock:resource2";
        const string myLockValue = "my-token-789";
        var newDuration = TimeSpan.FromSeconds(30);

        // Mock the Lua script evaluation to return RedisResult with value 0 (failure)
        var failureResult = RedisResult.Create(0);
        _mockDatabase.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<LuaScript>(),
                It.IsAny<object>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(failureResult); // Lua script returns 0 when value doesn't match

        // Act
        var result = await _sut.RenewLockAsync(lockKey, myLockValue, newDuration);

        // Assert
        result.Should().BeFalse("Lock renewal should fail when trying to renew lock owned by another token");
    }

    /// <summary>
    /// Verifies that RenewLockAsync fails when the lock has already expired.
    /// The Lua script should return 0 (no-op) since the key doesn't exist.
    /// </summary>
    [Fact]
    public async Task RenewLockAsync_WhenLockHasExpired_FailsGracefully()
    {
        // Arrange
        const string lockKey = "distributed:lock:expired2";
        const string lockValue = "expired-token-789";
        var newDuration = TimeSpan.FromSeconds(30);

        // Mock the Lua script evaluation to return RedisResult with value 0 (failure)
        var expiredResult = RedisResult.Create(0);
        _mockDatabase.Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<LuaScript>(),
                It.IsAny<object>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(expiredResult); // Lua script returns 0 when key doesn't exist

        // Act
        var result = await _sut.RenewLockAsync(lockKey, lockValue, newDuration);

        // Assert
        result.Should().BeFalse("Lock renewal should fail gracefully when lock has expired");
    }

    /// <summary>
    /// Verifies that RenewLockAsync throws ArgumentNullException for null lock key.
    /// </summary>
    [Fact]
    public async Task RenewLockAsync_WithNullLockKey_ThrowsArgumentNullException()
    {
        // Arrange
        const string lockValue = "token-123";
        var duration = TimeSpan.FromSeconds(30);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.RenewLockAsync(null!, lockValue, duration));
    }

    /// <summary>
    /// Verifies that RenewLockAsync throws ArgumentNullException for null lock value.
    /// </summary>
    [Fact]
    public async Task RenewLockAsync_WithNullLockValue_ThrowsArgumentNullException()
    {
        // Arrange
        const string lockKey = "distributed:lock:resource2";
        var duration = TimeSpan.FromSeconds(30);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.RenewLockAsync(lockKey, null!, duration));
    }

    /// <summary>
    /// Verifies that RenewLockAsync throws ArgumentOutOfRangeException for zero or negative duration.
    /// </summary>
    [Fact]
    public async Task RenewLockAsync_WithZeroOrNegativeDuration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const string lockKey = "distributed:lock:resource2";
        const string lockValue = "token-123";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.RenewLockAsync(lockKey, lockValue, TimeSpan.Zero));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.RenewLockAsync(lockKey, lockValue, TimeSpan.FromSeconds(-1)));
    }

    /// <summary>
    /// Verifies that RenewLockAsync throws ArgumentNullException for empty lock key.
    /// </summary>
    [Fact]
    public async Task RenewLockAsync_WithEmptyLockKey_ThrowsArgumentNullException()
    {
        // Arrange
        const string lockValue = "token-123";
        var duration = TimeSpan.FromSeconds(30);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.RenewLockAsync("", lockValue, duration));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.RenewLockAsync(" ", lockValue, duration));
    }

    /// <summary>
    /// Verifies that RenewLockAsync throws ArgumentNullException for empty lock value.
    /// </summary>
    [Fact]
    public async Task RenewLockAsync_WithEmptyLockValue_ThrowsArgumentNullException()
    {
        // Arrange
        const string lockKey = "distributed:lock:resource2";
        var duration = TimeSpan.FromSeconds(30);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.RenewLockAsync(lockKey, "", duration));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.RenewLockAsync(lockKey, " ", duration));
    }

    #endregion

    #region Integration-style Race Condition Tests

    /// <summary>
    /// Simulates the race condition that the Lua scripts prevent:
    /// Two different clients trying to release the same lock.
    /// Only the client with the matching lock value should succeed.
    /// </summary>
    [Fact]
    public async Task ReleaseLockAsync_RaceCondition_TwoClientsWithDifferentTokens_OnlyMatchingTokenSucceeds()
    {
        // Arrange
        const string lockKey = "race:test:lock";
        const string client1Token = "client1-token";
        const string client2Token = "client2-token";

        // Simulate scenario where client1 owns the lock
        var client1SuccessResult = RedisResult.Create(1);
        var client2FailureResult = RedisResult.Create(0);

        _mockDatabase.SetupSequence(db => db.ScriptEvaluateAsync(
                It.IsAny<LuaScript>(),
                It.Is<object>(args =>
                    ((RedisKey)args.GetType().GetProperty("key")!.GetValue(args)!).ToString() == lockKey &&
                    args.GetType().GetProperty("value")!.GetValue(args)!.ToString() == client1Token),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(client1SuccessResult); // Client1's release succeeds

        _mockDatabase.SetupSequence(db => db.ScriptEvaluateAsync(
                It.IsAny<LuaScript>(),
                It.Is<object>(args =>
                    ((RedisKey)args.GetType().GetProperty("key")!.GetValue(args)!).ToString() == lockKey &&
                    args.GetType().GetProperty("value")!.GetValue(args)!.ToString() == client2Token),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(client2FailureResult); // Client2's release fails (value mismatch)

        // Act - Client1 releases lock
        var client1Result = await _sut.ReleaseLockAsync(lockKey, client1Token);

        // Act - Client2 tries to release the same lock (should fail)
        var client2Result = await _sut.ReleaseLockAsync(lockKey, client2Token);

        // Assert
        client1Result.Should().BeTrue("Client1 should successfully release their own lock");
        client2Result.Should().BeFalse("Client2 should fail to release Client1's lock");
    }

    /// <summary>
    /// Simulates the race condition that the Lua scripts prevent:
    /// Two different clients trying to renew the same lock.
    /// Only the client with the matching lock value should succeed.
    /// </summary>
    [Fact]
    public async Task RenewLockAsync_RaceCondition_TwoClientsWithDifferentTokens_OnlyMatchingTokenSucceeds()
    {
        // Arrange
        const string lockKey = "race:test:renew";
        const string client1Token = "client1-renew-token";
        const string client2Token = "client2-renew-token";
        var newDuration = TimeSpan.FromSeconds(30);
        var ttlMs = (long)newDuration.TotalMilliseconds;

        // Simulate scenario where client1 owns the lock
        var client1SuccessResult = RedisResult.Create(1);
        var client2FailureResult = RedisResult.Create(0);

        _mockDatabase.SetupSequence(db => db.ScriptEvaluateAsync(
                It.IsAny<LuaScript>(),
                It.Is<object>(args =>
                    ((RedisKey)args.GetType().GetProperty("key")!.GetValue(args)!).ToString() == lockKey &&
                    args.GetType().GetProperty("value")!.GetValue(args)!.ToString() == client1Token &&
                    (long)args.GetType().GetProperty("ttl")!.GetValue(args)! == ttlMs),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(client1SuccessResult); // Client1's renewal succeeds

        _mockDatabase.SetupSequence(db => db.ScriptEvaluateAsync(
                It.IsAny<LuaScript>(),
                It.Is<object>(args =>
                    ((RedisKey)args.GetType().GetProperty("key")!.GetValue(args)!).ToString() == lockKey &&
                    args.GetType().GetProperty("value")!.GetValue(args)!.ToString() == client2Token &&
                    (long)args.GetType().GetProperty("ttl")!.GetValue(args)! == ttlMs),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(client2FailureResult); // Client2's renewal fails (value mismatch)

        // Act - Client1 renews lock
        var client1Result = await _sut.RenewLockAsync(lockKey, client1Token, newDuration);

        // Act - Client2 tries to renew the same lock (should fail)
        var client2Result = await _sut.RenewLockAsync(lockKey, client2Token, newDuration);

        // Assert
        client1Result.Should().BeTrue("Client1 should successfully renew their own lock");
        client2Result.Should().BeFalse("Client2 should fail to renew Client1's lock");
    }

    #endregion
}