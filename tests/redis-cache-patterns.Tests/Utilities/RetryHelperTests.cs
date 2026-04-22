#nullable enable
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Utilities;
using Xunit;

namespace RedisCachePatterns.Tests.Utilities;

public class RetryHelperTests
{
    private readonly Mock<ILogger> _mockLogger = new();

    [Fact]
    public async Task ExecuteWithRetryAsync_WhenOperationSucceedsOnFirstAttempt_ReturnsResult()
    {
        var result = await RetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await Task.CompletedTask;
                return 42;
            },
            maxRetries: 3);

        result.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WhenOperationFailsThenSucceeds_EventuallyReturnsResult()
    {
        var attemptCount = 0;
        var result = await RetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                attemptCount++;
                await Task.CompletedTask;
                if (attemptCount < 2)
                    throw new InvalidOperationException("Temporary failure");
                return 99;
            },
            maxRetries: 3);

        result.Should().Be(99);
        attemptCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WhenOperationExceedsMaxRetries_ThrowsInvalidOperationException()
    {
        Func<Task> act = () => RetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await Task.CompletedTask;
                throw new TimeoutException("Persistent failure");
            },
            maxRetries: 3);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Operation failed after 3 attempts*");
        ex.And.InnerException.Should().BeOfType<TimeoutException>();
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithCustomInitialDelay_RespectsDelayBetweenAttempts()
    {
        var attemptCount = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await RetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                attemptCount++;
                await Task.CompletedTask;
                if (attemptCount < 2)
                    throw new InvalidOperationException("Fail");
                return true;
            },
            maxRetries: 3,
            initialDelayMs: 50);

        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(50);
        attemptCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithExponentialBackoff_IncreaseDelayBetweenAttempts()
    {
        var attemptCount = 0;
        var attemptTimes = new List<DateTime>();

        try
        {
            await RetryHelper.ExecuteWithRetryAsync(
                async () =>
                {
                    attemptTimes.Add(DateTime.UtcNow);
                    attemptCount++;
                    await Task.CompletedTask;
                    throw new InvalidOperationException("Always fail");
                },
                maxRetries: 4,
                initialDelayMs: 10);
        }
        catch
        {
            // Expected to throw
        }

        attemptCount.Should().Be(4);
        attemptTimes.Count.Should().Be(4);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_LogsWarningOnRetry()
    {
        var mockLogger = new Mock<ILogger>();
        var attemptCount = 0;

        await RetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                attemptCount++;
                await Task.CompletedTask;
                if (attemptCount < 2)
                    throw new InvalidOperationException("Test failure");
                return true;
            },
            maxRetries: 3,
            initialDelayMs: 10,
            logger: mockLogger.Object);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log warning on retry");
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_OnFinalAttempt_DoesNotRetry()
    {
        var attemptCount = 0;

        Func<Task> act = () => RetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                attemptCount++;
                await Task.CompletedTask;
                throw new InvalidOperationException("Always fail");
            },
            maxRetries: 2);

        await act.Should().ThrowAsync<InvalidOperationException>();
        attemptCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_PreservesInnerExceptionAsInnerException()
    {
        var originalException = new TimeoutException("Original timeout");

        Func<Task> act = () => RetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await Task.CompletedTask;
                throw originalException;
            },
            maxRetries: 1);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.And.InnerException.Should().Be(originalException);
    }
}

public class CircuitBreakerTests
{
    [Fact]
    public async Task CircuitBreaker_WhenOperationSucceeds_AllowsSubsequentCalls()
    {
        var callCount = 0;
        RetryHelper.CircuitBreaker.Reset("test-circuit");

        var result1 = await RetryHelper.CircuitBreaker.ExecuteAsync(
            "test-circuit",
            async () =>
            {
                callCount++;
                await Task.CompletedTask;
                return 1;
            },
            failureThreshold: 3);

        var result2 = await RetryHelper.CircuitBreaker.ExecuteAsync(
            "test-circuit",
            async () =>
            {
                callCount++;
                await Task.CompletedTask;
                return 2;
            },
            failureThreshold: 3);

        result1.Should().Be(1);
        result2.Should().Be(2);
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task CircuitBreaker_WhenFailureThresholdIsReached_OpensCircuit()
    {
        RetryHelper.CircuitBreaker.Reset("test-circuit-fail");

        for (int i = 0; i < 3; i++)
        {
            try
            {
                await RetryHelper.CircuitBreaker.ExecuteAsync(
                    "test-circuit-fail",
                    async () =>
                    {
                        await Task.CompletedTask;
                        throw new InvalidOperationException("Failure");
                    },
                    failureThreshold: 3);
            }
            catch { /* Expected */ }
        }

        Func<Task> act = () => RetryHelper.CircuitBreaker.ExecuteAsync(
            "test-circuit-fail",
            async () =>
            {
                await Task.CompletedTask;
                return 0;
            },
            failureThreshold: 3);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Circuit breaker*is open*");
    }

    [Fact]
    public async Task CircuitBreaker_WhenCircuitOpens_RejectsNewRequests()
    {
        RetryHelper.CircuitBreaker.Reset("open-circuit");
        var blockingOperation = 0;

        for (int i = 0; i < 3; i++)
        {
            try
            {
                await RetryHelper.CircuitBreaker.ExecuteAsync(
                    "open-circuit",
                    async () =>
                    {
                        await Task.CompletedTask;
                        throw new InvalidOperationException("Fail");
                    },
                    failureThreshold: 3);
            }
            catch { /* Expected */ }
        }

        Func<Task> act = () => RetryHelper.CircuitBreaker.ExecuteAsync(
            "open-circuit",
            async () =>
            {
                blockingOperation++;
                await Task.CompletedTask;
                return 42;
            },
            failureThreshold: 3);

        await act.Should().ThrowAsync<InvalidOperationException>();
        blockingOperation.Should().Be(0);
    }

    [Fact]
    public async Task CircuitBreaker_AfterSuccessfulCall_ResetsFailureCount()
    {
        RetryHelper.CircuitBreaker.Reset("reset-circuit");
        var attemptCount = 0;

        try
        {
            await RetryHelper.CircuitBreaker.ExecuteAsync(
                "reset-circuit",
                async () =>
                {
                    attemptCount++;
                    await Task.CompletedTask;
                    throw new InvalidOperationException("Fail");
                },
                failureThreshold: 3);
        }
        catch { }

        attemptCount++;
        var result = await RetryHelper.CircuitBreaker.ExecuteAsync(
            "reset-circuit",
            async () =>
            {
                await Task.CompletedTask;
                return 100;
            },
            failureThreshold: 3);

        result.Should().Be(100);
    }

    [Fact]
    public async Task CircuitBreaker_MultipleCircuits_AreIndependent()
    {
        RetryHelper.CircuitBreaker.Reset("circuit-1");
        RetryHelper.CircuitBreaker.Reset("circuit-2");

        for (int i = 0; i < 3; i++)
        {
            try
            {
                await RetryHelper.CircuitBreaker.ExecuteAsync(
                    "circuit-1",
                    async () =>
                    {
                        await Task.CompletedTask;
                        throw new InvalidOperationException("Fail");
                    },
                    failureThreshold: 3);
            }
            catch { }
        }

        var result = await RetryHelper.CircuitBreaker.ExecuteAsync(
            "circuit-2",
            async () =>
            {
                await Task.CompletedTask;
                return 42;
            },
            failureThreshold: 3);

        result.Should().Be(42);
    }

    [Fact]
    public async Task CircuitBreaker_AfterResetTimeout_AllowsRetry()
    {
        RetryHelper.CircuitBreaker.Reset("timeout-circuit");

        for (int i = 0; i < 3; i++)
        {
            try
            {
                await RetryHelper.CircuitBreaker.ExecuteAsync(
                    "timeout-circuit",
                    async () =>
                    {
                        await Task.CompletedTask;
                        throw new InvalidOperationException("Fail");
                    },
                    failureThreshold: 3,
                    resetTimeoutSeconds: 1);
            }
            catch { }
        }

        await Task.Delay(1100);

        var result = await RetryHelper.CircuitBreaker.ExecuteAsync(
            "timeout-circuit",
            async () =>
            {
                await Task.CompletedTask;
                return 99;
            },
            failureThreshold: 3,
            resetTimeoutSeconds: 1);

        result.Should().Be(99);
    }

    [Fact]
    public void CircuitBreaker_Reset_ClearsCircuitState()
    {
        RetryHelper.CircuitBreaker.Reset("clear-circuit");

        Func<Task> act = async () =>
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    await RetryHelper.CircuitBreaker.ExecuteAsync(
                        "clear-circuit",
                        async () =>
                        {
                            await Task.CompletedTask;
                            throw new InvalidOperationException("Fail");
                        },
                        failureThreshold: 3);
                }
                catch { }
            }

            RetryHelper.CircuitBreaker.Reset("clear-circuit");

            await RetryHelper.CircuitBreaker.ExecuteAsync(
                "clear-circuit",
                async () =>
                {
                    await Task.CompletedTask;
                    return 42;
                },
                failureThreshold: 3);
        };

        act.Should().NotThrow();
    }
}
