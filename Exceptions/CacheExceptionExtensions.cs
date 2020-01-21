#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text;
using RedisCachePatterns.Exceptions;

namespace RedisCachePatterns.Exceptions;

/// <summary>
/// Extension methods for cache-related exceptions providing additional functionality
/// </summary>
public static class CacheExceptionExtensions
{
    /// <summary>
    /// Creates a detailed error message that includes the exception type, error code,
    /// occurred timestamp, and the original exception message.
    /// </summary>
    /// <param name="exception">The cache exception</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    /// <returns>Formatted error message string</returns>
    public static string GetDetailedErrorMessage(this CacheException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var builder = new StringBuilder();
        builder.AppendLine("Cache Exception Details:");
        builder.AppendLine($" Type: {exception.GetType().Name}");

        if (!string.IsNullOrEmpty(exception.ErrorCode))
        {
            builder.AppendLine($" Error Code: {exception.ErrorCode}");
        }

        builder.AppendLine($" Occurred At: {exception.OccurredAt:yyyy-MM-dd HH:mm:ss.fff}");
        builder.AppendLine($" Message: {exception.Message}");

        if (exception is CacheTimeoutException { Timeout: var timeout } timeoutEx && timeout != default)
        {
            builder.AppendLine($" Timeout: {timeout.TotalMilliseconds}ms");
        }

        if (exception is CacheKeyNotFoundException { CacheKey: var cacheKey } keyEx && !string.IsNullOrEmpty(cacheKey))
        {
            builder.AppendLine($" Cache Key: {cacheKey}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Determines whether the exception represents a transient cache error that might
    /// succeed on retry (connection issues, timeouts).
    /// </summary>
    /// <param name="exception">The cache exception to check</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    /// <returns>True if the exception is transient; otherwise false</returns>
    public static bool IsTransient(this CacheException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            CacheConnectionException => true,
            CacheTimeoutException => true,
            CacheSerializationException => false,
            CacheKeyNotFoundException => false,
            _ => false
        };
    }

    /// <summary>
    /// Creates a new exception with the same properties but with an updated error code.
    /// Useful for wrapping exceptions while preserving the original error context.
    /// </summary>
    /// <param name="exception">The original exception</param>
    /// <param name="newErrorCode">The new error code to apply</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="newErrorCode"/> is null or whitespace.</exception>
    /// <returns>A new exception with updated error code</returns>
    public static CacheException WithErrorCode(this CacheException exception, string newErrorCode)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(newErrorCode);

        return exception switch
        {
            CacheConnectionException connEx => new CacheConnectionException($"{exception.Message} [Code: {connEx.ErrorCode}]", connEx),
            CacheTimeoutException timeoutEx => new CacheTimeoutException($"{exception.Message} [Code: {timeoutEx.ErrorCode}]", timeoutEx.Timeout, timeoutEx),
            CacheKeyNotFoundException keyEx => new CacheKeyNotFoundException(keyEx.CacheKey)
            {
                ErrorCode = newErrorCode,
                OccurredAt = exception.OccurredAt
            },
            CacheSerializationException serEx => new CacheSerializationException($"{serEx.Message} [Code: {serEx.ErrorCode}]", serEx),
            _ => new CacheException($"{exception.Message} [Code: {exception.ErrorCode}]", newErrorCode, exception)
            {
                OccurredAt = exception.OccurredAt
            }
        };
    }

    /// <summary>
    /// Creates a user-friendly error message suitable for logging or displaying to users.
    /// Includes only essential information without technical details.
    /// </summary>
    /// <param name="exception">The cache exception</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    /// <returns>User-friendly error message</returns>
    public static string GetUserFriendlyMessage(this CacheException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            CacheConnectionException => "Unable to connect to the cache server. Please try again later.",
            CacheTimeoutException { Timeout: var timeout } timeoutEx => $"The operation timed out after {timeout.TotalSeconds} seconds. Please try again with a longer timeout.",
            CacheKeyNotFoundException { CacheKey: var cacheKey } keyEx => $"The requested item '{cacheKey}' was not found in cache.",
            CacheSerializationException => "Failed to process cached data. Please try again or contact support.",
            _ => "An unexpected cache error occurred. Please try again."
        };
    }
}