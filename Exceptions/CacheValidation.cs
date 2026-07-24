#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics.CodeAnalysis;

namespace RedisCachePatterns.Exceptions;

/// <summary>
/// Provides validation methods for cache operations to ensure consistent error handling
/// across all cache service implementations.
/// </summary>
public static class CacheValidation
{
    /// <summary>
    /// Validates a cache key and throws appropriate exceptions if invalid.
    /// </summary>
    /// <param name="key">The cache key to validate.</param>
    /// <param name="paramName">The name of the parameter being validated (for exception messages).</param>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    /// <exception cref="ArgumentException">Thrown when key is empty or whitespace.</exception>
    public static void ValidateKey([NotNull] string? key, string paramName = "key")
    {
        if (key is null)
        {
            throw new ArgumentNullException(paramName, "Cache key cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be empty or whitespace.", paramName);
        }
    }

    /// <summary>
    /// Validates a cache pattern and throws appropriate exceptions if invalid.
    /// </summary>
    /// <param name="pattern">The cache pattern to validate.</param>
    /// <param name="paramName">The name of the parameter being validated (for exception messages).</param>
    /// <exception cref="ArgumentNullException">Thrown when pattern is null.</exception>
    /// <exception cref="ArgumentException">Thrown when pattern is empty or whitespace.</exception>
    public static void ValidatePattern([NotNull] string? pattern, string paramName = "pattern")
    {
        if (pattern is null)
        {
            throw new ArgumentNullException(paramName, "Cache pattern cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Cache pattern cannot be empty or whitespace.", paramName);
        }
    }

    /// <summary>
    /// Validates a cache value and throws appropriate exceptions if invalid.
    /// </summary>
    /// <param name="value">The cache value to validate.</param>
    /// <param name="paramName">The name of the parameter being validated (for exception messages).</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public static void ValidateValue<T>([NotNull] T? value, string paramName = "value")
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName, "Cache value cannot be null.");
        }
    }

    /// <summary>
    /// Validates a load function and throws appropriate exceptions if invalid.
    /// </summary>
    /// <param name="loadFn">The load function to validate.</param>
    /// <param name="paramName">The name of the parameter being validated (for exception messages).</param>
    /// <exception cref="ArgumentNullException">Thrown when loadFn is null.</exception>
    public static void ValidateLoadFunction([NotNull] Delegate? loadFn, string paramName = "loadFn")
    {
        if (loadFn is null)
        {
            throw new ArgumentNullException(paramName, "Load function cannot be null.");
        }
    }

    /// <summary>
    /// Validates a persist function and throws appropriate exceptions if invalid.
    /// </summary>
    /// <param name="persistFn">The persist function to validate.</param>
    /// <param name="paramName">The name of the parameter being validated (for exception messages).</param>
    /// <exception cref="ArgumentNullException">Thrown when persistFn is null.</exception>
    public static void ValidatePersistFunction([NotNull] Delegate? persistFn, string paramName = "persistFn")
    {
        if (persistFn is null)
        {
            throw new ArgumentNullException(paramName, "Persist function cannot be null.");
        }
    }

    /// <summary>
    /// Validates expiration time and throws appropriate exceptions if invalid.
    /// </summary>
    /// <param name="expiration">The expiration time to validate.</param>
    /// <param name="paramName">The name of the parameter being validated (for exception messages).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when expiration is not positive.</exception>
    public static void ValidateExpiration(TimeSpan? expiration, string paramName = "expiration")
    {
        if (expiration.HasValue && expiration.Value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(paramName, "Expiration must be a positive duration.");
        }
    }

    /// <summary>
    /// Creates a standardized CacheException for cache operation failures.
    /// </summary>
    /// <param name="operation">The cache operation that failed.</param>
    /// <param name="innerException">The inner exception that caused the failure.</param>
    /// <returns>A CacheException instance with standardized message format.</returns>
    public static CacheException CreateOperationException(string operation, Exception innerException)
    {
        return new CacheException($"Cache {operation} failed: {innerException.Message}", innerException);
    }

    /// <summary>
    /// Creates a standardized CacheException for cache operation failures with custom message.
    /// </summary>
    /// <param name="operation">The cache operation that failed.</param>
    /// <param name="message">Custom error message.</param>
    /// <param name="innerException">The inner exception that caused the failure.</param>
    /// <returns>A CacheException instance with standardized message format.</returns>
    public static CacheException CreateOperationException(string operation, string message, Exception? innerException = null)
    {
        if (innerException is not null)
        {
            return new CacheException($"Cache {operation} failed: {message}", innerException);
        }

        return new CacheException($"Cache {operation} failed: {message}");
    }
}