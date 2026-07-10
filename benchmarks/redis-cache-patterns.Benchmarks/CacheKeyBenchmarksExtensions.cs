#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Text;
using RedisCachePatterns.Utilities;

namespace RedisCachePatterns.Benchmarks;

/// <summary>
/// Extension methods for <see cref="CacheKeyBenchmarks"/> providing additional cache key utilities
/// and performance optimizations for real-world scenarios.
/// </summary>
public static class CacheKeyBenchmarksExtensions
{
    /// <summary>
    /// Creates a cache key with a TTL suffix for time-based invalidation.
    /// Useful for keys that should automatically expire after a certain period.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance</param>
    /// <param name="ttlMinutes">Time-to-live in minutes</param>
    /// <returns>Cache key with TTL suffix</returns>
    public static string WithTtl(this CacheKeyBenchmarks benchmarks, int ttlMinutes)
    {
        var baseKey = benchmarks.GenericBuildKey();
        return $"{baseKey}:ttl-{ttlMinutes}";
    }

    /// <summary>
    /// Creates a composite cache key by combining multiple keys with a separator.
    /// Useful for caching relationships between entities.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance</param>
    /// <param name="keys">Array of cache keys to combine</param>
    /// <returns>Combined cache key</returns>
    public static string CombineKeys(this CacheKeyBenchmarks benchmarks, params string[] keys)
    {
        if (keys == null || keys.Length == 0)
            throw new ArgumentException("At least one key must be provided", nameof(keys));

        var builder = new StringBuilder();
        builder.Append(keys[0]);

        for (int i = 1; i < keys.Length; i++)
        {
            builder.Append(':');
            builder.Append(keys[i]);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Creates a versioned cache key to support cache invalidation on schema changes.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance</param>
    /// <param name="version">Version number</param>
    /// <returns>Versioned cache key</returns>
    public static string WithVersion(this CacheKeyBenchmarks benchmarks, int version)
    {
        var baseKey = benchmarks.EntityKey;
        return $"{baseKey}:v{version}";
    }

    /// <summary>
    /// Creates a cache key with a hash suffix for data consistency validation.
    /// Useful for detecting when cached data has changed.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance</param>
    /// <param name="hash">Consistency hash value</param>
    /// <returns>Cache key with hash suffix</returns>
    public static string WithHash(this CacheKeyBenchmarks benchmarks, string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("Hash cannot be null or empty", nameof(hash));

        return $"{benchmarks.PatternKey}:hash-{hash}";
    }
}