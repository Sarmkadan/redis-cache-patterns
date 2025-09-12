// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Exceptions;

/// <summary>
/// Base exception for cache-related errors
/// </summary>
public class CacheException : Exception
{
    public string? ErrorCode { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public CacheException(string message) : base(message)
    {
    }

    public CacheException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public CacheException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public CacheException(string message, string errorCode, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception thrown when a cache connection fails
/// </summary>
public class CacheConnectionException : CacheException
{
    public CacheConnectionException(string message) : base(message, "CACHE_CONNECTION_FAILED")
    {
    }

    public CacheConnectionException(string message, Exception innerException) : base(message, "CACHE_CONNECTION_FAILED", innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a cache operation times out
/// </summary>
public class CacheTimeoutException : CacheException
{
    public TimeSpan Timeout { get; set; }

    public CacheTimeoutException(string message, TimeSpan timeout) : base(message, "CACHE_TIMEOUT")
    {
        Timeout = timeout;
    }

    public CacheTimeoutException(string message, TimeSpan timeout, Exception innerException) : base(message, "CACHE_TIMEOUT", innerException)
    {
        Timeout = timeout;
    }
}

/// <summary>
/// Exception thrown when a cache key is not found
/// </summary>
public class CacheKeyNotFoundException : CacheException
{
    public string CacheKey { get; set; } = string.Empty;

    public CacheKeyNotFoundException(string cacheKey) : base($"Cache key '{cacheKey}' not found", "CACHE_KEY_NOT_FOUND")
    {
        CacheKey = cacheKey;
    }
}

/// <summary>
/// Exception thrown when serialization/deserialization fails
/// </summary>
public class CacheSerializationException : CacheException
{
    public CacheSerializationException(string message) : base(message, "CACHE_SERIALIZATION_FAILED")
    {
    }

    public CacheSerializationException(string message, Exception innerException) : base(message, "CACHE_SERIALIZATION_FAILED", innerException)
    {
    }
}
