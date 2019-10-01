#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Domain;

/// <summary>
/// Represents metadata about a cached entry for monitoring and management
/// </summary>
public class CacheEntry
{
    public string Key { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
    public int AccessCount { get; set; } = 0;
    public int HitCount { get; set; } = 0;
    public int MissCount { get; set; } = 0;
    public string Status { get; set; } = "active";
    public string? Tags { get; set; }

    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow >= ExpiresAt;

    public TimeSpan? TimeToExpiry => ExpiresAt.HasValue ? ExpiresAt.Value - DateTime.UtcNow : null;

    public double HitRate => AccessCount > 0 ? (double)HitCount / AccessCount * 100 : 0;

    public void RecordHit()
    {
        HitCount++;
        AccessCount++;
        LastAccessedAt = DateTime.UtcNow;
    }

    public void RecordMiss()
    {
        MissCount++;
        AccessCount++;
    }

    public void UpdateLastAccess()
    {
        LastAccessedAt = DateTime.UtcNow;
    }

    public void SetExpiration(DateTime expiryTime)
    {
        if (expiryTime <= DateTime.UtcNow)
            throw new ArgumentException("Expiry time must be in the future");
        ExpiresAt = expiryTime;
    }

    public void Invalidate()
    {
        Status = "invalidated";
        ExpiresAt = DateTime.UtcNow;
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrEmpty(Tags))
            Tags = tag;
        else if (!Tags.Contains(tag))
            Tags += $",{tag}";
    }

    public bool HasTag(string tag)
    {
        return !string.IsNullOrEmpty(Tags) && Tags.Split(',').Contains(tag);
    }

    public override string ToString() => $"Cache [{Key}] - Size: {SizeInBytes}B, Hit Rate: {HitRate:F1}%, Status: {Status}";
}
