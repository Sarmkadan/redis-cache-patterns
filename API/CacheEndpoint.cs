#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;

namespace RedisCachePatterns.API;

/// <summary>
/// API endpoint for cache management operations
/// </summary>
public class CacheEndpoint : ApiEndpointBase
{
    private readonly ICacheService _cacheService;
    private readonly CacheInvalidationService _invalidationService;
    private readonly Monitoring.CacheMetricsCollector? _metricsCollector;

    public CacheEndpoint(
        ICacheService cacheService,
        CacheInvalidationService invalidationService,
        ILogger<CacheEndpoint> logger,
        PerformanceMonitor performanceMonitor,
        Monitoring.CacheMetricsCollector? metricsCollector = null)
        : base(logger, performanceMonitor)
    {
        _cacheService = cacheService;
        _invalidationService = invalidationService;
        _metricsCollector = metricsCollector;
    }

    public async Task<ApiResponse<CacheStatistics>> GetStatisticsAsync()
    {
        return await ExecuteAsync(
            () => _cacheService.GetStatisticsAsync(),
            "GetCacheStatistics");
    }

    public async Task<ApiResponse<bool>> InvalidateByPatternAsync(string pattern)
    {
        ValidateRequired(pattern, nameof(pattern));

        return await ExecuteAsync(
            async () =>
            {
                await _invalidationService.InvalidateByPatternAsync(pattern);
                return true;
            },
            $"InvalidateByPattern({pattern})");
    }

    public async Task<ApiResponse<bool>> FlushAsync()
    {
        return await ExecuteAsync(
            async () =>
            {
                await _cacheService.FlushAsync();
                return true;
            },
            "FlushCache");
    }

    public async Task<ApiResponse<BulkGetResponse<T>>> BulkGetAsync<T>(List<string> keys, bool returnNullForMissing = false)
    {
        ValidateRequired(keys, nameof(keys));

        return await ExecuteAsync(async () =>
        {
            var results = new List<BulkGetResult<T>>();
            var retrievedCount = 0;
            var notFoundCount = 0;
            var failedCount = 0;

            foreach (var key in keys)
            {
                try
                {
                    var value = await _cacheService.GetAsync<T>(key);
                    if (value != null)
                    {
                        retrievedCount++;
                        results.Add(new BulkGetResult<T> { Key = key, Value = value, Found = true });
                    }
                    else
                    {
                        notFoundCount++;
                        if (returnNullForMissing)
                        {
                            results.Add(new BulkGetResult<T> { Key = key, Value = default, Found = false });
                        }
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    results.Add(new BulkGetResult<T> { Key = key, Value = default, Found = false, Error = ex.Message });
                }
            }

            return new BulkGetResponse<T>
            {
                Success = true,
                Results = results,
                TotalKeys = keys.Count,
                RetrievedCount = retrievedCount,
                NotFoundCount = notFoundCount,
                FailedCount = failedCount
            };
        }, "BulkGet");
    }

    public async Task<ApiResponse<BulkSetResponse>> BulkSetAsync<T>(List<CacheEntry> entries, TimeSpan? defaultExpiration = null)
    {
        ValidateRequired(entries, nameof(entries));

        return await ExecuteAsync(async () =>
        {
            var results = new List<BulkSetResult>();
            var successCount = 0;
            var failedCount = 0;
            var totalSizeBytes = 0L;

            var ttl = defaultExpiration ?? TimeSpan.FromMinutes(30);

            foreach (var entry in entries)
            {
                try
                {
                    await _cacheService.SetAsync(entry.Key, entry, ttl);
                    successCount++;
                    totalSizeBytes += entry.SizeInBytes;

                    results.Add(new BulkSetResult
                    {
                        Key = entry.Key,
                        Success = true,
                        SizeBytes = entry.SizeInBytes
                    });
                }
                catch (Exception ex)
                {
                    failedCount++;
                    results.Add(new BulkSetResult
                    {
                        Key = entry.Key,
                        Success = false,
                        Error = ex.Message
                    });
                }
            }

            return new BulkSetResponse
            {
                Success = failedCount == 0,
                Results = results,
                TotalEntries = entries.Count,
                SuccessCount = successCount,
                FailedCount = failedCount,
                TotalSizeBytes = totalSizeBytes
            };
        }, "BulkSet");
    }

    public async Task<ApiResponse<IEnumerable<string>>> GetKeysByPatternAsync(string pattern)
    {
        ValidateRequired(pattern, nameof(pattern));

        return await ExecuteAsync(
            async () =>
            {
                var keys = await _cacheService.GetKeysByPatternAsync(pattern);
                return keys.Take(100); // Limit to 100 for API response
            },
            $"GetKeysByPattern({pattern})");
    }

    public ApiResponse<object> GetMetrics()
    {
        if (_metricsCollector == null)
            return ApiResponse<object>.Failure("Metrics collection not enabled", 503);

        return ExecuteAsync(
            () => Task.FromResult((object)_metricsCollector.GetMetrics()),
            "GetCacheMetrics").GetAwaiter().GetResult();
    }

public async Task<ApiResponse<T>> GetWithSlidingExpirationAsync<T>(string key, TimeSpan slidingExpiration)
{
    ValidateRequired(key, nameof(key));
    return await ExecuteAsync( () => _cacheService.GetWithSlidingExpirationAsync<T>(key, slidingExpiration),
        $"GetWithSlidingExpiration({key}, {slidingExpiration.TotalSeconds}s)");
}
}
