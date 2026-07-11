#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;
using RedisCachePatterns.Services;

namespace RedisCachePatterns.API;

/// <summary>
/// Extension methods for <see cref="CacheEndpoint"/> providing additional cache management functionality
/// </summary>
public static class CacheEndpointExtensions
{
    /// <summary>
    /// Invalidates all cache entries that match the specified pattern with optional key prefix
    /// </summary>
    /// <param name="endpoint">The cache endpoint instance</param>
    /// <param name="pattern">The pattern to match keys against</param>
    /// <param name="prefix">Optional key prefix to prepend to the pattern</param>
    /// <returns>ApiResponse indicating success or failure</returns>
    /// <exception cref="ArgumentNullException">Thrown when pattern is null</exception>
    public static async Task<ApiResponse<bool>> InvalidateByPatternWithPrefixAsync(
        this CacheEndpoint endpoint,
        string pattern,
        string? prefix = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(pattern);

        var fullPattern = prefix == null
            ? pattern
            : $"{prefix}:{pattern}";

        return await endpoint.InvalidateByPatternAsync(fullPattern);
    }

    /// <summary>
    /// Gets cache statistics with additional computed metrics including hit rate and memory usage
    /// </summary>
    /// <param name="endpoint">The cache endpoint instance</param>
    /// <returns>ApiResponse containing enhanced cache statistics</returns>
    public static async Task<ApiResponse<EnhancedCacheStatistics>> GetEnhancedStatisticsAsync(
        this CacheEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var statsResponse = await endpoint.GetStatisticsAsync();

        if (!statsResponse.IsSuccess)
        {
            return ApiResponse<EnhancedCacheStatistics>.Failure(
                statsResponse.Error ?? "Failed to retrieve cache statistics",
                statsResponse.StatusCode);
        }

        var baseStats = statsResponse.Data ?? new CacheStatistics();

        // Calculate derived metrics
        var totalRequests = baseStats.Hits + baseStats.Misses;
        var hitRate = totalRequests > 0
            ? (double)baseStats.Hits / totalRequests
            : 0.0;

        var memoryUsedMB = baseStats.MemoryUsedBytes / (1024.0 * 1024.0);

        var enhancedStats = new EnhancedCacheStatistics
        {
            TotalKeys = baseStats.TotalKeys,
            Hits = baseStats.Hits,
            Misses = baseStats.Misses,
            HitRate = hitRate,
            MemoryUsedBytes = baseStats.MemoryUsedBytes,
            MemoryUsedMB = memoryUsedMB,
            CapturedAt = baseStats.CapturedAt
        };

        return ApiResponse<EnhancedCacheStatistics>.Success(enhancedStats);
    }

    /// <summary>
    /// Gets all cache keys matching the specified pattern with pagination support
    /// </summary>
    /// <param name="endpoint">The cache endpoint instance</param>
    /// <param name="pattern">The pattern to match keys against</param>
    /// <param name="page">The page number (1-based)</param>
    /// <param name="pageSize">The number of keys per page</param>
    /// <returns>ApiResponse containing paginated cache keys</returns>
    /// <exception cref="ArgumentNullException">Thrown when pattern is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when page or pageSize are invalid</exception>
    public static async Task<ApiResponse<PaginatedResult<string>>> GetKeysByPatternPagedAsync(
        this CacheEndpoint endpoint,
        string pattern,
        int page = 1,
        int pageSize = 50)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var response = await endpoint.GetKeysByPatternAsync(pattern);

        if (!response.IsSuccess)
        {
            return ApiResponse<PaginatedResult<string>>.Failure(
                response.Error ?? "Failed to retrieve cache keys",
                response.StatusCode);
        }

        var allKeys = response.Data?.ToList() ?? [];
        var totalCount = allKeys.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var paginatedKeys = allKeys
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new PaginatedResult<string>
        {
            Items = paginatedKeys,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };

        return ApiResponse<PaginatedResult<string>>.Success(result);
    }

    /// <summary>
    /// Flushes the cache and returns detailed operation statistics including timing information
    /// </summary>
    /// <param name="endpoint">The cache endpoint instance</param>
    /// <returns>ApiResponse containing flush operation statistics</returns>
    public static async Task<ApiResponse<FlushOperationResult>> FlushWithStatisticsAsync(
        this CacheEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var startKeys = await endpoint.GetKeysByPatternAsync("*");
        var startCount = startKeys.IsSuccess && startKeys.Data != null
            ? startKeys.Data.Count()
            : 0;

        var flushResponse = await endpoint.FlushAsync();

        if (!flushResponse.IsSuccess)
        {
            return ApiResponse<FlushOperationResult>.Failure(
                flushResponse.Error ?? "Failed to flush cache",
                flushResponse.StatusCode);
        }

        stopwatch.Stop();

        var endKeys = await endpoint.GetKeysByPatternAsync("*");
        var endCount = endKeys.IsSuccess && endKeys.Data != null
            ? endKeys.Data.Count()
            : 0;

        var result = new FlushOperationResult
        {
            Success = true,
            KeysBefore = startCount,
            KeysAfter = endCount,
            KeysFlushed = startCount - endCount,
            DurationMilliseconds = stopwatch.ElapsedMilliseconds,
            Duration = stopwatch.Elapsed,
            Timestamp = DateTime.UtcNow
        };

        return ApiResponse<FlushOperationResult>.Success(result);
    }
}

/// <summary>
/// Paginated result wrapper
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public record PaginatedResult<T>
{
    /// <summary>Items for the current page</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>Total number of items across all pages</summary>
    public int TotalCount { get; init; }

    /// <summary>Current page number (1-based)</summary>
    public int Page { get; init; }

    /// <summary>Number of items per page</summary>
    public int PageSize { get; init; }

    /// <summary>Total number of pages</summary>
    public int TotalPages { get; init; }
}

/// <summary>
/// Enhanced cache statistics with computed metrics
/// </summary>
public record EnhancedCacheStatistics
{
    /// <summary>Total number of keys in cache</summary>
    public int TotalKeys { get; init; }

    /// <summary>Number of cache hits</summary>
    public int Hits { get; init; }

    /// <summary>Number of cache misses</summary>
    public int Misses { get; init; }

    /// <summary>Cache hit rate (0.0 to 1.0)</summary>
    public double HitRate { get; init; }

    /// <summary>Memory used in bytes</summary>
    public long MemoryUsedBytes { get; init; }

    /// <summary>Memory used in megabytes</summary>
    public double MemoryUsedMB { get; init; }

    /// <summary>Timestamp when statistics were captured</summary>
    public DateTime CapturedAt { get; init; }
}

/// <summary>
/// Result of a flush operation with detailed statistics
/// </summary>
public record FlushOperationResult
{
    /// <summary>Whether the flush operation succeeded</summary>
    public bool Success { get; init; }

    /// <summary>Number of keys before flush</summary>
    public int KeysBefore { get; init; }

    /// <summary>Number of keys after flush</summary>
    public int KeysAfter { get; init; }

    /// <summary>Number of keys that were flushed</summary>
    public int KeysFlushed { get; init; }

    /// <summary>Duration of flush operation in milliseconds</summary>
    public long DurationMilliseconds { get; init; }

    /// <summary>Duration of flush operation</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Timestamp when flush completed</summary>
    public DateTime Timestamp { get; init; }
}