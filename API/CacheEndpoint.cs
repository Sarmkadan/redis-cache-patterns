#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
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
                await _invalidationService.InvalidateByPatternAsync(pattern).ConfigureAwait(false);
                return true;
            },
            $"InvalidateByPattern({pattern})");
    }

    public async Task<ApiResponse<bool>> FlushAsync()
    {
        return await ExecuteAsync(
            async () =>
            {
                await _cacheService.FlushAsync().ConfigureAwait(false);
                return true;
            },
            "FlushCache");
    }

    public async Task<ApiResponse<IEnumerable<string>>> GetKeysByPatternAsync(string pattern)
    {
        ValidateRequired(pattern, nameof(pattern));

        return await ExecuteAsync(
            async () =>
            {
                var keys = await _cacheService.GetKeysByPatternAsync(pattern).ConfigureAwait(false);
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
}
