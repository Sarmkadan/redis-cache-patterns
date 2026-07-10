#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace RedisCachePatterns.Configuration;

/// <summary>
/// Extension methods for <see cref="RedisCachePatternsOptions"/> providing convenient
/// operations for cache configuration and validation.
/// </summary>
public static class RedisCachePatternsOptionsExtensions
{
    /// <summary>
    /// Validates that the Redis cache configuration is valid for connection.
    /// </summary>
    /// <param name="options">The Redis cache options to validate</param>
    /// <returns>True if the configuration is valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
    public static bool IsValid(this RedisCachePatternsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return !string.IsNullOrWhiteSpace(options.ConnectionString) &&
               options.DatabaseId >= 0 &&
               options.ConnectTimeoutMs is >= 100 and <= 30000 &&
               options.SyncTimeoutMs is >= 100 and <= 30000 &&
               !string.IsNullOrWhiteSpace(options.EvictionPolicy);
    }

    /// <summary>
    /// Gets the effective timeout in milliseconds, using the stricter of the two timeout values.
    /// </summary>
    /// <param name="options">The Redis cache options</param>
    /// <returns>The effective timeout in milliseconds</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
    public static int GetEffectiveTimeoutMs(this RedisCachePatternsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Math.Min(options.ConnectTimeoutMs, options.SyncTimeoutMs);
    }

    /// <summary>
    /// Gets the cache size limit in megabytes.
    /// </summary>
    /// <param name="options">The Redis cache options</param>
    /// <returns>The cache size limit in megabytes</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
    public static double GetCacheSizeMb(this RedisCachePatternsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Math.Round(options.MaxCacheSizeBytes / (1024.0 * 1024.0), 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Gets the connection string with sensitive data redacted for logging purposes.
    /// </summary>
    /// <param name="options">The Redis cache options</param>
    /// <returns>A redacted connection string suitable for logging</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
    public static string GetRedactedConnectionString(this RedisCachePatternsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return string.Empty;
        }

        // Simple redaction: replace password portion with ***
        var parts = options.ConnectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].StartsWith("password=", StringComparison.OrdinalIgnoreCase) && parts[i].Length > 9)
            {
                parts[i] = "password=***";
            }
        }

        return string.Join(";", parts);
    }
}