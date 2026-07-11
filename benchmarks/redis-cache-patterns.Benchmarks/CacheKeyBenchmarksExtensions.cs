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
    /// <param name="benchmarks">The benchmarks instance. Cannot be <see langword="null"/></param>
    /// <param name="ttlMinutes">Time-to-live in minutes. Must be non-negative.</param>
    /// <returns>Cache key with TTL suffix.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
    public static string WithTtl(this CacheKeyBenchmarks benchmarks, int ttlMinutes)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentOutOfRangeException.ThrowIfNegative(ttlMinutes);

        var baseKey = benchmarks.GenericBuildKey();
        return $"{baseKey}:ttl-{ttlMinutes}";
    }

    /// <summary>
    /// Creates a composite cache key by combining multiple keys with a separator.
    /// Useful for caching relationships between entities.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance. Cannot be <see langword="null"/></param>
    /// <param name="keys">Array of cache keys to combine. Cannot be <see langword="null"/> or empty.</param>
    /// <returns>Combined cache key.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> or <paramref name="keys"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="keys"/> is empty.</exception>
    public static string CombineKeys(this CacheKeyBenchmarks benchmarks, params string[] keys)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(keys);
        if (keys.Length == 0)
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
    /// <param name="benchmarks">The benchmarks instance. Cannot be <see langword="null"/></param>
    /// <param name="version">Version number. Must be non-negative.</param>
    /// <returns>Versioned cache key.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is negative.</exception>
    public static string WithVersion(this CacheKeyBenchmarks benchmarks, int version)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentOutOfRangeException.ThrowIfNegative(version);

        var baseKey = benchmarks.EntityKey;
        return $"{baseKey}:v{version}";
    }

    /// <summary>
    /// Creates a cache key with a hash suffix for data consistency validation.
    /// Useful for detecting when cached data has changed.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance. Cannot be <see langword="null"/></param>
    /// <param name="hash">Consistency hash value. Cannot be <see langword="null"/>, empty, or whitespace-only.</param>
    /// <returns>Cache key with hash suffix.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="hash"/> is null, empty, or whitespace-only.</exception>
    public static string WithHash(this CacheKeyBenchmarks benchmarks, string hash)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentException.ThrowIfNullOrEmpty(hash, nameof(hash));

        return $"{benchmarks.PatternKey}:hash-{hash}";
    }
}