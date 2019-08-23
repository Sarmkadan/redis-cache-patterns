// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Domain;

/// <summary>
/// Defines caching behavior and expiration policies for cached data
/// </summary>
public class CachePolicy
{
    public string Key { get; set; } = string.Empty;
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public CachePattern Pattern { get; set; } = CachePattern.CacheAside;
    public bool UseCompression { get; set; } = false;
    public int MaxSize { get; set; } = 1024 * 1024; // 1MB default
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? Description { get; set; }

    public CachePolicy()
    {
    }

    public CachePolicy(string key, TimeSpan expiration, CachePattern pattern = CachePattern.CacheAside)
    {
        Key = key;
        DefaultExpiration = expiration;
        Pattern = pattern;
    }

    public void UpdateExpiration(TimeSpan newExpiration)
    {
        if (newExpiration <= TimeSpan.Zero)
            throw new ArgumentException("Expiration must be positive");
        DefaultExpiration = newExpiration;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPattern(CachePattern pattern)
    {
        Pattern = pattern;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EnableCompression()
    {
        UseCompression = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DisableCompression()
    {
        UseCompression = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Enable()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public override string ToString() => $"Policy [{Key}] - Pattern: {Pattern}, TTL: {DefaultExpiration.TotalSeconds}s";
}

public enum CachePattern
{
    CacheAside = 0,      // Check cache, miss -> load from DB, store in cache
    WriteThrough = 1,    // Write to cache and DB synchronously
    WriteAround = 2,     // Write to DB only, invalidate cache
    RefreshAhead = 3     // Proactively refresh cache before expiration
}
