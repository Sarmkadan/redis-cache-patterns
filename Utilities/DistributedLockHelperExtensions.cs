#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Extension methods for <see cref="DistributedLockHelper"/> that provide additional
/// functionality for working with distributed locks in common scenarios.
/// </summary>
public static class DistributedLockHelperExtensions
{
    /// <summary>
    /// Attempts to acquire the lock with a timeout. If the lock cannot be acquired
    /// within the specified timeout period, the method returns <c>false</c>.
    /// </summary>
    /// <param name="helper">The distributed lock helper instance.</param>
    /// <param name="timeout">The maximum time to wait for the lock.</param>
    /// <returns>
    /// <c>true</c> if the lock was acquired; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="timeout"/> is not a positive time span.
    /// </exception>
    public static async Task<bool> AcquireAsync(this DistributedLockHelper helper, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

        var startTime = DateTime.UtcNow;
        var remaining = timeout;

        while (remaining > TimeSpan.Zero)
        {
            if (await helper.AcquireAsync())
            {
                return true;
            }

            // Calculate delay without exceeding remaining time
            var delay = TimeSpan.FromMilliseconds(Math.Min(
                100,
                remaining.TotalMilliseconds
            ));

            await Task.Delay(delay, CancellationToken.None);
            remaining = timeout - (DateTime.UtcNow - startTime);
        }

        return false;
    }

    /// <summary>
    /// Executes an action with the lock, retrying if the lock is initially unavailable.
    /// </summary>
    /// <param name="helper">The distributed lock helper instance.</param>
    /// <param name="action">The action to execute while holding the lock.</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3).</param>
    /// <param name="retryDelay">Delay between retries (default: 100ms).</param>
    /// <returns>
    /// <c>true</c> if the action was executed successfully; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxRetries"/> is negative or <paramref name="retryDelay"/> is not positive.
    /// </exception>
    public static async Task<bool> ExecuteWithRetryAsync(
        this DistributedLockHelper helper,
        Func<Task> action,
        int maxRetries = 3,
        TimeSpan? retryDelay = null)
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            retryDelay ?? TimeSpan.FromMilliseconds(100),
            TimeSpan.Zero
        );

        for (var i = 0; i <= maxRetries; i++)
        {
            if (await helper.ExecuteAsync(action))
            {
                return true;
            }

            if (i < maxRetries)
            {
                await Task.Delay(retryDelay ?? TimeSpan.FromMilliseconds(100), CancellationToken.None);
            }
        }

        return false;
    }

    /// <summary>
    /// Executes a function with the lock, retrying if the lock is initially unavailable.
    /// </summary>
    /// <typeparam name="TResult">The result type of the function.</typeparam>
    /// <param name="helper">The distributed lock helper instance.</param>
    /// <param name="func">The function to execute while holding the lock.</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3).</param>
    /// <param name="retryDelay">Delay between retries (default: 100ms).</param>
    /// <returns>
    /// The result of the function, or <c>default</c> if all retries failed.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="func"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxRetries"/> is negative or <paramref name="retryDelay"/> is not positive.
    /// </exception>
    public static async Task<TResult?> ExecuteWithRetryAsync<TResult>(
        this DistributedLockHelper helper,
        Func<Task<TResult>> func,
        int maxRetries = 3,
        TimeSpan? retryDelay = null)
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentNullException.ThrowIfNull(func);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            retryDelay ?? TimeSpan.FromMilliseconds(100),
            TimeSpan.Zero
        );

        for (var i = 0; i <= maxRetries; i++)
        {
            try
            {
                return await helper.ExecuteAsync(func);
            }
            catch (InvalidOperationException)
            {
                if (i < maxRetries)
                {
                    await Task.Delay(retryDelay ?? TimeSpan.FromMilliseconds(100), CancellationToken.None);
                }
            }
        }

        return default;
    }

    /// <summary>
    /// Executes multiple actions sequentially, each protected by the same lock.
    /// </summary>
    /// <param name="helper">The distributed lock helper instance.</param>
    /// <param name="actions">The actions to execute while holding the lock.</param>
    /// <returns>
    /// <c>true</c> if all actions were executed successfully; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="helper"/> or <paramref name="actions"/> is null,
    /// or when any element in <paramref name="actions"/> is null.
    /// </exception>
    /// <remarks>
    /// This method acquires the lock once and executes all actions sequentially
    /// without releasing the lock between them.
    /// </remarks>
    public static async Task<bool> ExecuteBatchAsync(
        this DistributedLockHelper helper,
        IReadOnlyList<Func<Task>> actions)
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentNullException.ThrowIfNull(actions);

        if (actions.Count == 0)
        {
            return true;
        }

        return await helper.ExecuteAsync(async () =>
        {
            var tasks = new List<Task>(actions.Count);
            foreach (var action in actions)
            {
                ArgumentNullException.ThrowIfNull(action);
                tasks.Add(action());
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Gets a value indicating whether the lock is currently held.
    /// </summary>
    /// <param name="helper">The distributed lock helper instance.</param>
    /// <returns><c>true</c> if the lock is held; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="helper"/> is null.</exception>
    public static bool IsHeld([NotNullWhen(true)] this DistributedLockHelper helper)
    {
        ArgumentNullException.ThrowIfNull(helper);
        return helper.IsLocked;
    }

    /// <summary>
    /// Gets the lock value as a hexadecimal string representation.
    /// Useful for logging and debugging purposes.
    /// </summary>
    /// <param name="helper">The distributed lock helper instance.</param>
    /// <returns>
    /// A hexadecimal string representation of the lock value,
    /// or <c>null</c> if the lock value is not a valid hexadecimal string or Guid.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="helper"/> is null.</exception>
    /// <remarks>
    /// This method attempts to parse the lock value as a hexadecimal string or Guid.
    /// </remarks>
    public static string? GetLockValueHex(this DistributedLockHelper helper)
    {
        ArgumentNullException.ThrowIfNull(helper);

        if (string.IsNullOrEmpty(helper.LockValue))
        {
            return null;
        }

        // Try to parse as Guid first (common case)
        if (Guid.TryParse(helper.LockValue, out var guid))
        {
            return guid.ToString("N");
        }

        // Try to parse as hexadecimal
        // Check if all characters are valid hex digits
        if (IsHexString(helper.LockValue))
        {
            return helper.LockValue;
        }

        return null;
    }

    private static bool IsHexString(string s)
    {
        foreach (var c in s)
        {
            if (!Uri.IsHexDigit(c))
            {
                return false;
            }
        }
        return true;
    }
}

