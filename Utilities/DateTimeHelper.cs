#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Utilities;

/// <summary>
/// DateTime utility functions for formatting, parsing, and timezone conversions
/// Provides consistent date/time handling across the application
/// </summary>
public static class DateTimeHelper
{
    private static readonly string[] DateFormats =
    {
        "O", // ISO 8601 extended format
        "u", // Universal sortable format
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd"
    };

    /// <summary>
    /// Parses datetime string using multiple formats
    /// </summary>
    public static bool TryParseFlexible(string input, out DateTime result)
    {
        return DateTime.TryParseExact(input, DateFormats, null, System.Globalization.DateTimeStyles.None, out result);
    }

    /// <summary>
    /// Formats datetime in ISO 8601 extended format for consistency
    /// </summary>
    public static string FormatIso8601(DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("O");
    }

    /// <summary>
    /// Calculates human-readable time difference (e.g., "2 hours ago")
    /// </summary>
    public static string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime.ToUniversalTime();

        return timeSpan.TotalSeconds < 60
            ? $"{(int)timeSpan.TotalSeconds}s ago"
            : timeSpan.TotalMinutes < 60
                ? $"{(int)timeSpan.TotalMinutes}m ago"
                : timeSpan.TotalHours < 24
                    ? $"{(int)timeSpan.TotalHours}h ago"
                    : timeSpan.TotalDays < 7
                        ? $"{(int)timeSpan.TotalDays}d ago"
                        : $"{(int)(timeSpan.TotalDays / 7)}w ago";
    }

    /// <summary>
    /// Gets the start of the day in UTC
    /// </summary>
    public static DateTime GetDayStart(DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Gets the end of the day in UTC
    /// </summary>
    public static DateTime GetDayEnd(DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddTicks(-1);
    }

    /// <summary>
    /// Calculates expiration datetime for cache TTL
    /// </summary>
    public static DateTime CalculateExpiration(TimeSpan? ttl)
    {
        return ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : DateTime.MaxValue;
    }

    /// <summary>
    /// Checks if datetime is in the past
    /// </summary>
    public static bool IsExpired(DateTime expirationTime)
    {
        return expirationTime <= DateTime.UtcNow;
    }

    /// <summary>
    /// Gets remaining time until expiration
    /// </summary>
    public static TimeSpan? GetTimeRemaining(DateTime expirationTime)
    {
        var remaining = expirationTime - DateTime.UtcNow;
        return remaining.TotalSeconds > 0 ? remaining : null;
    }
}
