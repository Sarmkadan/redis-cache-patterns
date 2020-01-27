#nullable enable

namespace RedisCachePatterns.Domain;

/// <summary>
/// Extension methods for <see cref="CacheEntry"/> providing additional cache management functionality
/// </summary>
public static class CacheEntryExtensions
{
    /// <summary>
    /// Determines if the cache entry is currently active and not expired
    /// </summary>
    /// <param name="entry">The cache entry to check</param>
    /// <returns>True if the entry is active and not expired; otherwise false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null</exception>
    public static bool IsActive(this CacheEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return entry.Status.Equals("active", StringComparison.OrdinalIgnoreCase) && !entry.IsExpired;
    }

    /// <summary>
    /// Determines if the cache entry is stale (not accessed for a significant period)
    /// </summary>
    /// <param name="entry">The cache entry to check</param>
    /// <param name="staleThreshold">Maximum time since last access to consider it stale (default: 24 hours)</param>
    /// <returns>True if the entry hasn't been accessed for longer than the threshold; otherwise false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null</exception>
    public static bool IsStale(this CacheEntry entry, TimeSpan? staleThreshold = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var threshold = staleThreshold ?? TimeSpan.FromHours(24);
        return DateTime.UtcNow - entry.LastAccessedAt > threshold;
    }

    /// <summary>
    /// Gets the remaining time until the cache entry expires
    /// </summary>
    /// <param name="entry">The cache entry to check</param>
    /// <returns>A formatted string representing the time to expiry, or "Never" if not set</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null</exception>
    public static string GetTimeToExpiryFormatted(this CacheEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.TimeToExpiry.HasValue)
        {
            var timeSpan = entry.TimeToExpiry.Value;
            if (timeSpan.TotalDays >= 1) return $"{timeSpan.TotalDays:F1} days";
            if (timeSpan.TotalHours >= 1) return $"{timeSpan.TotalHours:F1} hours";
            if (timeSpan.TotalMinutes >= 1) return $"{timeSpan.TotalMinutes:F1} minutes";
            return $"{timeSpan.TotalSeconds:F1} seconds";
        }
        return "Never";
    }

    /// <summary>
    /// Gets a detailed status string for the cache entry
    /// </summary>
    /// <param name="entry">The cache entry to get status for</param>
    /// <returns>A formatted status string with key metrics</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null</exception>
    public static string GetDetailedStatus(this CacheEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var statusParts = new List<string>();

        statusParts.Add($"Status: {entry.Status}");
        statusParts.Add($"Type: {entry.DataType}");
        statusParts.Add($"Size: {FormatSize(entry.SizeInBytes)}");

        if (entry.IsExpired)
            statusParts.Add("State: EXPIRED");
        else if (entry.Status.Equals("invalidated", StringComparison.OrdinalIgnoreCase))
            statusParts.Add("State: INVALIDATED");
        else if (entry.IsStale())
            statusParts.Add("State: STALE");
        else
            statusParts.Add("State: ACTIVE");

        statusParts.Add($"Hit Rate: {entry.HitRate:F1}%");
        statusParts.Add($"Accesses: {entry.AccessCount}");
        statusParts.Add($"Last Accessed: {entry.LastAccessedAt:yyyy-MM-dd HH:mm:ss}");

        if (!string.IsNullOrEmpty(entry.Tags))
            statusParts.Add($"Tags: {entry.Tags}");

        return string.Join(" | ", statusParts);
    }

    /// <summary>
    /// Formats the size in bytes to a human-readable format
    /// </summary>
    /// <param name="bytes">The size in bytes</param>
    /// <returns>A human-readable size string (e.g., "1.2 KB", "5.4 MB")</returns>
    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double len = bytes;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:F1} {sizes[order]}";
    }

    /// <summary>
    /// Determines if the cache entry matches all specified tags
    /// </summary>
    /// <param name="entry">The cache entry to check</param>
    /// <param name="requiredTags">Tags that must all be present</param>
    /// <returns>True if all required tags are present; otherwise false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null</exception>
    public static bool HasAllTags(this CacheEntry entry, params string[] requiredTags)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (requiredTags == null || requiredTags.Length == 0)
            return true;

        if (string.IsNullOrEmpty(entry.Tags))
            return false;

        var entryTags = entry.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return requiredTags.All(tag => entryTags.Contains(tag, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines if the cache entry matches any of the specified tags
    /// </summary>
    /// <param name="entry">The cache entry to check</param>
    /// <param name="anyTags">Tags to check for presence of any</param>
    /// <returns>True if any of the tags are present; otherwise false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null</exception>
    public static bool HasAnyTag(this CacheEntry entry, params string[] anyTags)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (anyTags == null || anyTags.Length == 0)
            return false;

        if (string.IsNullOrEmpty(entry.Tags))
            return false;

        var entryTags = entry.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return anyTags.Any(tag => entryTags.Contains(tag, StringComparer.OrdinalIgnoreCase));
    }
}