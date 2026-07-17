#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;
using RedisCachePatterns.Domain;

namespace RedisCachePatterns.Configuration;

/// <summary>
/// Extension methods for <see cref="CacheConfigurationBuilder"/> that provide additional
/// configuration capabilities for cache service setup and policy management.
/// </summary>
public static class CacheConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds a policy with a time-based key pattern using a human-readable format.
    /// </summary>
    /// <param name="builder">The configuration builder instance.</param>
    /// <param name="keyPattern">The key pattern with placeholders (e.g., "user:{0}:profile").</param>
    /// <param name="timeSpan">The expiration timespan in a human-readable format (e.g., "2h 30m").</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="keyPattern"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="keyPattern"/> is empty or <paramref name="timeSpan"/> is null or empty.</exception>
    /// <exception cref="FormatException">Thrown when the timespan format is invalid.</exception>
    public static CacheConfigurationBuilder AddPolicy(
        this CacheConfigurationBuilder builder,
        string keyPattern,
        string timeSpan)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(keyPattern);
        ArgumentException.ThrowIfNullOrEmpty(timeSpan);

        var expiration = ParseTimeSpan(timeSpan);
        builder.AddPolicy(keyPattern, expiration);
        return builder;
    }

    /// <summary>
    /// Adds a policy with a time-based key pattern using days, hours, minutes, and seconds.
    /// </summary>
    /// <param name="builder">The configuration builder instance.</param>
    /// <param name="keyPattern">The key pattern with placeholders (e.g., "user:{0}:profile").</param>
    /// <param name="days">The expiration in days.</param>
    /// <param name="hours">Additional hours beyond days.</param>
    /// <param name="minutes">Additional minutes beyond hours.</param>
    /// <param name="seconds">Additional seconds beyond minutes.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="keyPattern"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="keyPattern"/> is empty.</exception>
    public static CacheConfigurationBuilder AddPolicy(
        this CacheConfigurationBuilder builder,
        string keyPattern,
        int days = 0,
        int hours = 0,
        int minutes = 0,
        int seconds = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(keyPattern);

        var expiration = new TimeSpan(days, hours, minutes, seconds);
        builder.AddPolicy(keyPattern, expiration);
        return builder;
    }

    /// <summary>
    /// Adds multiple policies from a collection of key-expiration pairs.
    /// </summary>
    /// <param name="builder">The configuration builder instance.</param>
    /// <param name="policies">Collection of key patterns and their expiration timespans.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="policies"/> is null.</exception>
    public static CacheConfigurationBuilder AddPolicies(
        this CacheConfigurationBuilder builder,
        IEnumerable<(string KeyPattern, TimeSpan Expiration)> policies)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(policies);

        foreach (var (keyPattern, expiration) in policies)
        {
            builder.AddPolicy(keyPattern, expiration);
        }
        return builder;
    }

    /// <summary>
    /// Enables compression with a custom threshold in bytes.
    /// </summary>
    /// <param name="builder">The configuration builder instance.</param>
    /// <param name="thresholdBytes">The compression threshold in bytes.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="thresholdBytes"/> is negative.</exception>
    public static CacheConfigurationBuilder EnableCompression(
        this CacheConfigurationBuilder builder,
        int thresholdBytes)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfNegative(thresholdBytes);

        builder.EnableCompression(thresholdBytes);
        return builder;
    }

    /// <summary>
    /// Parses a human-readable timespan string into a TimeSpan.
    /// Supports formats like "2h 30m", "1d", "30s", "1.5h", etc.
    /// </summary>
    /// <param name="timeSpan">The timespan string to parse.</param>
    /// <returns>The parsed TimeSpan.</returns>
    /// <exception cref="FormatException">Thrown when the format is invalid.</exception>
    private static TimeSpan ParseTimeSpan(string timeSpan)
    {
        ArgumentException.ThrowIfNullOrEmpty(timeSpan);

        var totalSeconds = 0d;
        var parts = timeSpan.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            if (part.EndsWith("d", StringComparison.OrdinalIgnoreCase))
            {
                var days = double.Parse(part.AsSpan(0, part.Length - 1), CultureInfo.InvariantCulture);
                totalSeconds += days * 24 * 60 * 60;
            }
            else if (part.EndsWith("h", StringComparison.OrdinalIgnoreCase))
            {
                var hours = double.Parse(part.AsSpan(0, part.Length - 1), CultureInfo.InvariantCulture);
                totalSeconds += hours * 60 * 60;
            }
            else if (part.EndsWith("m", StringComparison.OrdinalIgnoreCase))
            {
                var minutes = double.Parse(part.AsSpan(0, part.Length - 1), CultureInfo.InvariantCulture);
                totalSeconds += minutes * 60;
            }
            else if (part.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                var seconds = double.Parse(part.AsSpan(0, part.Length - 1), CultureInfo.InvariantCulture);
                totalSeconds += seconds;
            }
            else
            {
                throw new FormatException($"Invalid timespan format: '{timeSpan}'. Expected format like '2h 30m' or '1.5d'");
            }
        }

        return TimeSpan.FromSeconds(totalSeconds);
    }
}