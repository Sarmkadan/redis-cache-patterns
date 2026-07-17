#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;

namespace RedisCachePatterns.Monitoring;

/// <summary>
/// Provides useful extension methods for <see cref="KeyAccessStats"/> to enable
/// richer analytics and filtering operations on cache key access statistics.
/// </summary>
public static class KeyAccessStatsExtensions
{
    /// <summary>
    /// Calculates the absolute number of cache misses for this key.
    /// </summary>
    /// <param name="stats">The key access statistics.</param>
    /// <returns>The number of cache misses recorded for this key.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stats"/> is <see langword="null"/></exception>
    public static long GetMisses(this KeyAccessStats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);
        return stats.Misses;
    }

    /// <summary>
    /// Calculates the absolute number of cache hits for this key.
    /// </summary>
    /// <param name="stats">The key access statistics.</param>
    /// <returns>The number of cache hits recorded for this key.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stats"/> is <see langword="null"/></exception>
    public static long GetHits(this KeyAccessStats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);
        return stats.Hits;
    }

    /// <summary>
    /// Determines whether this key is considered "hot" based on access patterns.
    /// A key is hot if it has been accessed more than <paramref name="minAccessThreshold"/> times.
    /// </summary>
    /// <param name="stats">The key access statistics.</param>
    /// <param name="minAccessThreshold">Minimum number of accesses to be considered hot. Defaults to 100.</param>
    /// <returns><see langword="true"/> if the key is hot; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stats"/> is <see langword="null"/></exception>
    public static bool IsHotKey(this KeyAccessStats stats, int minAccessThreshold = 100)
    {
        ArgumentNullException.ThrowIfNull(stats);
        return stats.TotalAccesses >= minAccessThreshold;
    }

    /// <summary>
    /// Determines whether this key is considered "cold" based on recency of access.
    /// A key is cold if it hasn't been accessed within the specified time window.
    /// </summary>
    /// <param name="stats">The key access statistics.</param>
    /// <param name="coldThreshold">Time span after which a key is considered cold. Defaults to 1 hour.</param>
    /// <returns><see langword="true"/> if the key is cold; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stats"/> is <see langword="null"/></exception>
    public static bool IsColdKey(this KeyAccessStats stats, TimeSpan? coldThreshold = null)
    {
        ArgumentNullException.ThrowIfNull(stats);
        var threshold = coldThreshold ?? TimeSpan.FromHours(1);
        var cutoff = DateTime.UtcNow - threshold;
        return stats.LastAccessedAt < cutoff;
    }

    /// <summary>
    /// Determines whether this key has poor cache efficiency based on its hit rate.
    /// A key has poor efficiency if its hit rate is below the specified threshold.
    /// </summary>
    /// <param name="stats">The key access statistics.</param>
    /// <param name="minHitRate">Minimum acceptable hit rate (0-1). Defaults to 0.5 (50%).</param>
    /// <returns><see langword="true"/> if the key has poor cache efficiency; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stats"/> is <see langword="null"/></exception>
    public static bool HasPoorEfficiency(this KeyAccessStats stats, double minHitRate = 0.5)
    {
        ArgumentNullException.ThrowIfNull(stats);
        return stats.HitRate < minHitRate;
    }

    /// <summary>
    /// Gets the age of this key in the cache, measured from when it was first seen.
    /// </summary>
    /// <param name="stats">The key access statistics.</param>
    /// <returns>A <see cref="TimeSpan"/> representing how long the key has been tracked.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stats"/> is <see langword="null"/></exception>
    public static TimeSpan GetAge(this KeyAccessStats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);
        return DateTime.UtcNow - stats.FirstSeenAt;
    }

    /// <summary>
    /// Formats the key access statistics as a compact, machine-readable string.
    /// Useful for logging, telemetry, or serialization scenarios.
    /// </summary>
    /// <param name="stats">The key access statistics.</param>
    /// <returns>A formatted string containing key statistics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stats"/> is <see langword="null"/></exception>
    public static string ToMachineString(this KeyAccessStats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);
        return string.Create(CultureInfo.InvariantCulture,
            $"{stats.Key}|hits={stats.Hits}|misses={stats.Misses}|hitRate={stats.HitRate:F4}|age={GetAge(stats).TotalMinutes:F0}m");
    }

    /// <summary>
    /// Determines whether this key should be considered for eviction based on multiple factors:
    /// low hit rate, cold age, and low total access count.
    /// </summary>
    /// <param name="stats">The key access statistics.</param>
    /// <param name="evictionThreshold">Hit rate threshold below which keys are candidates for eviction. Defaults to 0.3.</param>
    /// <param name="minAccesses">Minimum number of accesses required to avoid eviction. Defaults to 10.</param>
    /// <returns><see langword="true"/> if the key should be considered for eviction; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stats"/> is <see langword="null"/></exception>
    public static bool ShouldEvict(this KeyAccessStats stats, double evictionThreshold = 0.3, int minAccesses = 10)
    {
        ArgumentNullException.ThrowIfNull(stats);

        // Keys with very low access counts are not worth keeping around
        if (stats.TotalAccesses < minAccesses)
            return true;

        // Keys with poor hit rates are poor cache candidates
        if (stats.HasPoorEfficiency(evictionThreshold))
            return true;

        // Cold keys that haven't been accessed recently
        if (stats.IsColdKey(TimeSpan.FromHours(24)))
            return true;

        return false;
    }

    /// <summary>
    /// Gets a human-readable summary of the key's access pattern.
    /// </summary>
    /// <param name="stats">The key access statistics.</param>
    /// <returns>A formatted string suitable for display in dashboards or logs.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stats"/> is <see langword="null"/></exception>
    public static string ToSummaryString(this KeyAccessStats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);

        var age = GetAge(stats);
        var efficiencyClass = stats.HitRate switch
        {
            >= 0.9 => "Excellent",
            >= 0.8 => "Good",
            >= 0.6 => "Fair",
            >= 0.4 => "Poor",
            _ => "Very Poor"
        };

        return $"Key: {stats.Key}\n" +
               $" Age: {age.TotalDays:F1} days\n" +
               $" Accesses: {stats.TotalAccesses:N0} (hits: {stats.Hits:N0}, misses: {stats.Misses:N0})\n" +
               $" Hit Rate: {stats.HitRate:P1} ({efficiencyClass})\n" +
               $" Last Accessed: {stats.LastAccessedAt:yyyy-MM-dd HH:mm:ss} UTC";
    }
}