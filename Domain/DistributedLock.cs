#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Domain;

/// <summary>
/// Represents a distributed lock for coordinating access across multiple instances
/// </summary>
public class DistributedLock
{
    public string Key { get; set; } = string.Empty;
    public string LockValue { get; set; } = string.Empty;
    public DateTime AcquiredAt { get; set; }
    public TimeSpan Duration { get; set; }
    public string HolderIdentifier { get; set; } = string.Empty;
    public int RetryCount { get; set; } = 0;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public bool IsAcquired { get; set; } = false;

    public DistributedLock()
    {
    }

    public DistributedLock(string key, string holderIdentifier, TimeSpan duration)
    {
        Key = key;
        HolderIdentifier = holderIdentifier;
        Duration = duration;
        LockValue = Guid.NewGuid().ToString();
    }

    public DateTime ExpiresAt => AcquiredAt.Add(Duration);

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public TimeSpan TimeRemaining => ExpiresAt - DateTime.UtcNow;

    public void Acquire()
    {
        AcquiredAt = DateTime.UtcNow;
        IsAcquired = true;
    }

    public void Release()
    {
        IsAcquired = false;
    }

    public bool CanRenew(string lockValue)
    {
        return IsAcquired && LockValue == lockValue && !IsExpired;
    }

    public void RenewLock(TimeSpan newDuration)
    {
        if (!IsAcquired)
            throw new InvalidOperationException("Cannot renew a lock that is not acquired");
        AcquiredAt = DateTime.UtcNow;
        Duration = newDuration;
    }

    public override string ToString() => $"Lock [{Key}] held by {HolderIdentifier}, expires in {TimeRemaining.TotalSeconds:F1}s";
}
