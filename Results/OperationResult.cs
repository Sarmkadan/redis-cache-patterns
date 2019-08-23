// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Results;

/// <summary>
/// Standard result wrapper for operations
/// </summary>
public class OperationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static OperationResult Ok(string? message = null)
    {
        return new OperationResult
        {
            Success = true,
            Message = message ?? "Operation completed successfully"
        };
    }

    public static OperationResult Fail(string message, string? errorCode = null)
    {
        return new OperationResult
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode ?? "OPERATION_FAILED"
        };
    }
}

/// <summary>
/// Result wrapper with data payload
/// </summary>
public class OperationResult<T> : OperationResult
{
    public T? Data { get; set; }

    public static OperationResult<T> Ok(T data, string? message = null)
    {
        return new OperationResult<T>
        {
            Success = true,
            Data = data,
            Message = message ?? "Operation completed successfully"
        };
    }

    public new static OperationResult<T> Fail(string message, string? errorCode = null)
    {
        return new OperationResult<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode ?? "OPERATION_FAILED",
            Data = default
        };
    }
}

/// <summary>
/// Paginated result wrapper
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;

    public static PagedResult<T> Create(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
    {
        return new PagedResult<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}

/// <summary>
/// Represents a cache operation result with timing information
/// </summary>
public class CacheOperationResult<T>
{
    public T? Data { get; set; }
    public bool CacheHit { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public string Source { get; set; } = "unknown"; // "cache" or "database"
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    public static CacheOperationResult<T> FromCache(T data, long elapsedMs)
    {
        return new CacheOperationResult<T>
        {
            Data = data,
            CacheHit = true,
            ElapsedMilliseconds = elapsedMs,
            Source = "cache"
        };
    }

    public static CacheOperationResult<T> FromDatabase(T data, long elapsedMs)
    {
        return new CacheOperationResult<T>
        {
            Data = data,
            CacheHit = false,
            ElapsedMilliseconds = elapsedMs,
            Source = "database"
        };
    }
}
