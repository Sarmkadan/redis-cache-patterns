#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Exceptions;

namespace RedisCachePatterns.Services;

/// <summary>
/// Provides consistent validation for cache keys across all cache service implementations.
/// This helper ensures that all cache operations use the same validation rules and exception types
/// for null and empty keys, providing a unified error contract.
/// </summary>
public static class CacheKeyValidation
{
    /// <summary>
    /// Validates that a cache key is not null or whitespace.
    /// </summary>
    /// <param name="key">The cache key to validate.</param>
    /// <param name="paramName">The name of the parameter for exception reporting.</param>
    /// <exception cref="ArgumentNullException">Thrown when key is null or whitespace.</exception>
    /// <exception cref="CacheKeyNotFoundException">Thrown when key is empty string (for operations that require non-empty keys).</exception>
    public static void ValidateKey(string key, string paramName = "key")
    {
        if (key == null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(paramName, "Cache key cannot be null or whitespace.");
        }

        // For operations that specifically require non-empty keys (not just whitespace),
        // check if the key is empty after trimming
        if (key.Trim().Length == 0)
        {
            throw new ArgumentException("Cache key cannot be empty or whitespace.", paramName);
        }
    }

    /// <summary>
    /// Validates that a cache key is not null or whitespace, and throws a consistent exception type.
    /// </summary>
    /// <param name="key">The cache key to validate.</param>
    /// <param name="paramName">The name of the parameter for exception reporting.</param>
    /// <exception cref="CacheKeyNotFoundException">Thrown when key is null, empty, or whitespace.</exception>
    public static void ValidateKeyWithCacheException(string key, string paramName = "key")
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new CacheKeyNotFoundException(paramName);
        }
    }

    /// <summary>
    /// Validates that a collection of cache keys is not null and contains no null or whitespace keys.
    /// </summary>
    /// <param name="keys">The collection of cache keys to validate.</param>
    /// <param name="paramName">The name of the parameter for exception reporting.</param>
    /// <exception cref="ArgumentNullException">Thrown when keys collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when any key in the collection is null or whitespace.</exception>
    public static void ValidateKeyCollection(IEnumerable<string> keys, string paramName = "keys")
    {
        ArgumentNullException.ThrowIfNull(keys);

        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Cache key collection cannot contain null or whitespace keys.", paramName);
            }
        }
    }

    /// <summary>
    /// Validates that a cache pattern is not null or whitespace.
    /// </summary>
    /// <param name="pattern">The cache pattern to validate.</param>
    /// <param name="paramName">The name of the parameter for exception reporting.</param>
    /// <exception cref="ArgumentNullException">Thrown when pattern is null or whitespace.</exception>
    public static void ValidatePattern(string pattern, string paramName = "pattern")
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentNullException(paramName, "Cache pattern cannot be null or whitespace.");
        }
    }
}