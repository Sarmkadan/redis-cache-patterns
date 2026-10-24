#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Services;

/// <summary>
/// Per-call configuration for stampede protection on a cache miss, used by
/// <see cref="CacheAsideExtensions.GetOrSetAsync{T}"/>.
/// </summary>
public sealed record StampedeProtectionOptions
{
    /// <summary>
    /// Gets the default options: in-process single-flight only, no cross-node lock.
    /// </summary>
    public static StampedeProtectionOptions Default { get; } = new();

    /// <summary>
    /// Gets options with all stampede protection turned off: every miss invokes the factory.
    /// </summary>
    public static StampedeProtectionOptions Disabled { get; } = new() { CoalesceInProcess = false };

    /// <summary>
    /// Gets or sets a value indicating whether concurrent misses for the same key within this process
    /// share a single factory invocation. Defaults to <c>true</c>.
    /// </summary>
    public bool CoalesceInProcess { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the factory runs only while holding a distributed lock
    /// (<c>lock:{key}</c>) so that only one node in the fleet loads a missing key at a time.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool DistributedLock { get; init; }

    /// <summary>
    /// Gets or sets the lifetime of the distributed lock. The lock is renewed by a watchdog while held,
    /// so this only bounds how long a crashed holder can block other nodes. Defaults to 10 seconds.
    /// </summary>
    public TimeSpan LockDuration { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets how long a caller that did not acquire the distributed lock waits for the holder
    /// to publish the value before loading it directly. Defaults to 5 seconds.
    /// </summary>
    public TimeSpan LockWaitTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the delay between cache checks while waiting for the distributed lock holder.
    /// Defaults to 50 milliseconds.
    /// </summary>
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Validates the timing values.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <see cref="LockDuration"/> or <see cref="PollInterval"/> is not positive,
    /// or <see cref="LockWaitTimeout"/> is negative.
    /// </exception>
    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(LockDuration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(LockWaitTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(PollInterval, TimeSpan.Zero);
    }
}
