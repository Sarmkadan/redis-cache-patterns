#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Retry utility providing exponential backoff and jitter for resilient operations
/// Used for transient failure handling in distributed systems
/// </summary>
public static class RetryHelper
{
    /// <summary>
    /// Executes operation with exponential backoff retry policy
    /// </summary>
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxRetries = 3,
        int initialDelayMs = 100,
        ILogger? logger = null)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < maxRetries - 1)
            {
                lastException = ex;
                var delayMs = initialDelayMs * (int)Math.Pow(2, attempt) + Random.Shared.Next(0, initialDelayMs);

                logger?.LogWarning(
                    "Retry attempt {Attempt} after {DelayMs}ms: {Error}",
                    attempt + 1, delayMs, ex.Message);

                await Task.Delay(delayMs);
            }
        }

        throw new InvalidOperationException($"Operation failed after {maxRetries} attempts", lastException);
    }

    /// <summary>
    /// Executes operation with circuit breaker pattern
    /// </summary>
    public static class CircuitBreaker
    {
        private static readonly Dictionary<string, CircuitState> States = new();
        private static readonly object StateLocker = new object();

        public static async Task<T> ExecuteAsync<T>(
            string circuitName,
            Func<Task<T>> operation,
            int failureThreshold = 5,
            int resetTimeoutSeconds = 60,
            ILogger? logger = null)
        {
            lock (StateLocker)
            {
                if (!States.ContainsKey(circuitName))
                {
                    States[circuitName] = new CircuitState();
                }

                var state = States[circuitName];

                // Check if circuit should reset
                if (state.IsOpen && (DateTime.UtcNow - state.LastFailureTime).TotalSeconds > resetTimeoutSeconds)
                {
                    state.IsOpen = false;
                    state.FailureCount = 0;
                    logger?.LogInformation("Circuit {CircuitName} reset", circuitName);
                }

                // Reject if circuit is open
                if (state.IsOpen)
                {
                    throw new InvalidOperationException($"Circuit breaker {circuitName} is open");
                }
            }

            try
            {
                var result = await operation();
                lock (StateLocker)
                {
                    States[circuitName].FailureCount = 0;
                }
                return result;
            }
            catch (Exception ex)
            {
                lock (StateLocker)
                {
                    var state = States[circuitName];
                    state.FailureCount++;
                    state.LastFailureTime = DateTime.UtcNow;

                    if (state.FailureCount >= failureThreshold)
                    {
                        state.IsOpen = true;
                        logger?.LogError("Circuit {CircuitName} opened after {Failures} failures",
                            circuitName, state.FailureCount);
                    }
                }

                throw;
            }
        }

        private class CircuitState
        {
            public bool IsOpen { get; set; }
            public int FailureCount { get; set; }
            public DateTime LastFailureTime { get; set; }
        }

        public static void Reset(string circuitName)
        {
            lock (StateLocker)
            {
                States.Remove(circuitName);
            }
        }
    }
}
