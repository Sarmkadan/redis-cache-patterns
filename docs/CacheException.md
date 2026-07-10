# CacheException

The `CacheException` class and its derived types represent errors occurring within the `redis-cache-patterns` library during cache operations. These exceptions provide contextual information about cache failures, including error codes, timestamps, and specific details related to connection issues, timeouts, missing keys, or serialization failures.

## API

### CacheException
The base class for all exceptions in the library.

*   `public string? ErrorCode`: A machine-readable string identifier for the error type.
*   `public DateTime OccurredAt`: The timestamp indicating when the exception was instantiated.
*   `public CacheException(string message) : base`: Initializes a new instance with a specific error message.
*   `public CacheException(string message, string errorCode) : base`: Initializes a new instance with a message and a specific error code.
*   `public CacheException(string message, Exception innerException) : base`: Initializes a new instance with a message and an underlying exception.
*   `public CacheException(string message, string errorCode, Exception innerException) : base`: Initializes a new instance with a message, error code, and inner exception.

### CacheConnectionException
Thrown when the library cannot establish or maintain a connection to the Redis server.

*   `public CacheConnectionException(string message) : base`: Initializes a new instance with a specific message.
*   `public CacheConnectionException(string message, Exception innerException) : base`: Initializes a new instance with a message and an inner exception.

### CacheTimeoutException
Thrown when a cache operation exceeds the configured timeout duration.

*   `public TimeSpan Timeout`: The timeout duration that was exceeded.
*   `public CacheTimeoutException(string message, TimeSpan timeout) : base`: Initializes a new instance with a message and the timeout threshold.
*   `public CacheTimeoutException(string message, TimeSpan timeout, Exception innerException) : base`: Initializes a new instance with a message, timeout, and inner exception.

### CacheKeyNotFoundException
Thrown when an operation expects a key to exist in the cache but it is not found.

*   `public string CacheKey`: The key that was requested but not found.
*   `public CacheKeyNotFoundException(string cacheKey) : base(...)`: Initializes a new instance for a missing key.

### CacheSerializationException
Thrown when an error occurs during the serialization or deserialization of cache values.

*   `public CacheSerializationException(string message) : base`: Initializes a new instance with a specific message.
*   `public CacheSerializationException(string message, Exception innerException) : base`: Initializes a new instance with a message and an inner exception.

## Usage

```csharp
// Example 1: Catching the base CacheException to log diagnostic data
try
{
    await _cache.SetAsync("user:123", userData);
}
catch (CacheException ex)
{
    _logger.LogError(ex, "Cache error occurred at {Timestamp}. Code: {Code}", 
                     ex.OccurredAt, ex.ErrorCode);
}
```

```csharp
// Example 2: Handling specific derived exceptions for targeted recovery
try
{
    var data = await _cache.GetAsync("critical-config");
}
catch (CacheTimeoutException ex)
{
    _logger.LogWarning("Operation timed out after {Time}ms.", ex.Timeout.TotalMilliseconds);
    // Logic to switch to a fallback data source
}
catch (CacheKeyNotFoundException ex)
{
    _logger.LogInformation("Required key '{Key}' not found in cache.", ex.CacheKey);
}
```

## Notes

*   **Thread Safety:** As standard .NET exception types, these classes are immutable regarding their state after construction and are inherently thread-safe to throw and catch across threads.
*   **Timestamps:** The `OccurredAt` property is populated at the time of the exception instantiation. If an exception is re-thrown, this value remains unchanged, representing the time of the initial failure.
*   **Inner Exceptions:** When dealing with `CacheConnectionException` or `CacheSerializationException`, always inspect the `InnerException` property to determine the root cause, such as a `SocketException` or a `JsonException`.
*   **ErrorCode usage:** The `ErrorCode` property is intended for programmatic handling of error scenarios (e.g., distinguishing between a transient network blip and a permanent authentication failure) rather than being displayed directly to end users.
