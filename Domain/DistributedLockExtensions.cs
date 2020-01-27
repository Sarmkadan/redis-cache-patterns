#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Domain;

/// <summary>
/// Extension methods for <see cref="DistributedLock"/> to provide additional functionality
/// for working with distributed locks in common scenarios.
/// </summary>
public static class DistributedLockExtensions
{
    /// <summary>
    /// Attempts to acquire the lock with automatic retry logic based on the lock's RetryCount and RetryDelay properties.
    /// </summary>
    /// <param name="distributedLock">The distributed lock instance</param>
    /// <param name="acquireAction">Action that attempts to acquire the lock</param>
    /// <returns>True if lock was acquired, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="distributedLock"/> or <paramref name="acquireAction"/> is null</exception>
    /// <exception cref="InvalidOperationException">Thrown if lock is already acquired</exception>
    public static bool TryAcquireWithRetry(this DistributedLock distributedLock, Func<bool> acquireAction)
    {
        ArgumentNullException.ThrowIfNull(distributedLock);
        ArgumentNullException.ThrowIfNull(acquireAction);

        if (distributedLock.IsAcquired)
            throw new InvalidOperationException("Lock is already acquired");

        int attempts = 0;
        while (attempts < distributedLock.RetryCount)
        {
            if (acquireAction())
            {
                distributedLock.Acquire();
                return true;
            }

            attempts++;
            if (attempts < distributedLock.RetryCount)
            {
                Thread.Sleep(distributedLock.RetryDelay);
            }
        }

        return false;
    }

    /// <summary>
    /// Safely releases the lock if it's currently acquired and matches the provided lock value.
    /// </summary>
    /// <param name="distributedLock">The distributed lock instance</param>
    /// <param name="lockValue">Expected lock value to verify before release</param>
    /// <returns>True if lock was released, false if lock wasn't acquired or values didn't match</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="distributedLock"/> is null</exception>
    public static bool SafeRelease(this DistributedLock distributedLock, string lockValue)
    {
        ArgumentNullException.ThrowIfNull(distributedLock);
        ArgumentException.ThrowIfNullOrEmpty(lockValue);

        if (distributedLock.IsAcquired && distributedLock.LockValue == lockValue)
        {
            distributedLock.Release();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the lock is about to expire within the specified threshold.
    /// </summary>
    /// <param name="distributedLock">The distributed lock instance</param>
    /// <param name="threshold">Time threshold before expiration to consider as "about to expire"</param>
    /// <returns>True if lock will expire within the threshold, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="distributedLock"/> is null</exception>
    public static bool IsAboutToExpire(this DistributedLock distributedLock, TimeSpan threshold)
    {
        ArgumentNullException.ThrowIfNull(distributedLock);
        return distributedLock.TimeRemaining <= threshold;
    }

    /// <summary>
    /// Creates a new lock instance with the same key and holder identifier but with an extended duration.
    /// </summary>
    /// <param name="distributedLock">The distributed lock instance to extend</param>
    /// <param name="additionalDuration">Additional time to add to the current duration</param>
    /// <returns>A new DistributedLock instance with extended duration</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="distributedLock"/> is null</exception>
    public static DistributedLock WithExtendedDuration(this DistributedLock distributedLock, TimeSpan additionalDuration)
    {
        ArgumentNullException.ThrowIfNull(distributedLock);

        return new DistributedLock
        {
            Key = distributedLock.Key,
            LockValue = distributedLock.LockValue,
            HolderIdentifier = distributedLock.HolderIdentifier,
            Duration = distributedLock.Duration + additionalDuration,
            AcquiredAt = distributedLock.AcquiredAt,
            RetryCount = distributedLock.RetryCount,
            RetryDelay = distributedLock.RetryDelay,
            IsAcquired = distributedLock.IsAcquired
        };
    }
}