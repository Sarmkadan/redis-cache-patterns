#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

namespace RedisCachePatterns.Domain;

/// <summary>
/// Request model for bulk get operations
/// </summary>
public class BulkGetRequest
{
    /// <summary>List of cache keys to retrieve</summary>
    public List<string> Keys { get; set; } = new List<string>();

    /// <summary>Whether to return null for missing keys or omit them from response</summary>
    public bool ReturnNullForMissing { get; set; } = false;
}

/// <summary>
/// Response model for individual bulk get operation result
/// </summary>
/// <typeparam name="T">Type of cached value</typeparam>
public class BulkGetResult<T>
{
    /// <summary>Cache key</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Retrieved value, or null if key not found</summary>
    public T? Value { get; set; }

    /// <summary>Whether the key was found in cache</summary>
    public bool Found { get; set; }

    /// <summary>Error message if operation failed for this key</summary>
    public string? Error { get; set; }
}

/// <summary>
/// Response model for bulk get operations
/// </summary>
/// <typeparam name="T">Type of cached values</typeparam>
public class BulkGetResponse<T>
{
    /// <summary>Whether the bulk operation succeeded</summary>
    public bool Success { get; set; } = true;

    /// <summary>Individual results for each requested key</summary>
    public List<BulkGetResult<T>> Results { get; set; } = new List<BulkGetResult<T>>();

    /// <summary>Total number of keys requested</summary>
    public int TotalKeys { get; set; }

    /// <summary>Number of keys successfully retrieved</summary>
    public int RetrievedCount { get; set; }

    /// <summary>Number of keys not found in cache</summary>
    public int NotFoundCount { get; set; }

    /// <summary>Number of keys that failed to retrieve</summary>
    public int FailedCount { get; set; }
}

/// <summary>
/// Request model for bulk set operations
/// </summary>
/// <typeparam name="T">Type of values to cache</typeparam>
public class BulkSetRequest<T>
{
    /// <summary>List of cache entries to set</summary>
    public List<CacheEntry> Entries { get; set; } = new List<CacheEntry>();

    /// <summary>Default TTL to apply if not specified in individual entries</summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
}

/// <summary>
/// Response model for individual bulk set operation result
/// </summary>
public class BulkSetResult
{
    /// <summary>Cache key that was set</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Whether the set operation succeeded</summary>
    public bool Success { get; set; }

    /// <summary>Error message if operation failed</summary>
    public string? Error { get; set; }

    /// <summary>Size of the cached value in bytes</summary>
    public long SizeBytes { get; set; }
}

/// <summary>
/// Response model for bulk set operations
/// </summary>
public class BulkSetResponse
{
    /// <summary>Whether the bulk operation succeeded</summary>
    public bool Success { get; set; } = true;

    /// <summary>Individual results for each entry</summary>
    public List<BulkSetResult> Results { get; set; } = new List<BulkSetResult>();

    /// <summary>Total number of entries attempted</summary>
    public int TotalEntries { get; set; }

    /// <summary>Number of entries successfully set</summary>
    public int SuccessCount { get; set; }

    /// <summary>Number of entries that failed to set</summary>
    public int FailedCount { get; set; }

    /// <summary>Total size of all successfully cached entries in bytes</summary>
    public long TotalSizeBytes { get; set; }
}