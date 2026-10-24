#nullable enable
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
/// Exception thrown when a compressed cache payload is malformed, truncated or otherwise not valid GZIP data
/// </summary>
public class CacheDecompressionException : CacheException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CacheDecompressionException"/> class
    /// </summary>
    /// <param name="message">Description of the decompression failure</param>
    public CacheDecompressionException(string message) : base(message, "CACHE_DECOMPRESSION_FAILED")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheDecompressionException"/> class
    /// </summary>
    /// <param name="message">Description of the decompression failure</param>
    /// <param name="innerException">Underlying exception raised by the decoder</param>
    public CacheDecompressionException(string message, Exception innerException) : base(message, "CACHE_DECOMPRESSION_FAILED", innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a compressed cache payload, or its decompressed output, exceeds the configured size limit
/// </summary>
public class CacheSizeLimitExceededException : CacheException
{
    /// <summary>
    /// Gets the configured limit in bytes that was exceeded
    /// </summary>
    public long LimitBytes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheSizeLimitExceededException"/> class
    /// </summary>
    /// <param name="message">Description of the exceeded limit</param>
    /// <param name="limitBytes">Configured limit in bytes</param>
    public CacheSizeLimitExceededException(string message, long limitBytes) : base(message, "CACHE_SIZE_LIMIT_EXCEEDED")
    {
        LimitBytes = limitBytes;
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
