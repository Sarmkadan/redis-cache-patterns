#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using RedisCachePatterns.Monitoring;
using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;

namespace RedisCachePatterns.API;

/// <summary>
/// API endpoint that exposes aggregated cache statistics from <see cref="CacheStatisticsAggregator"/>.
/// </summary>
public sealed class CacheStatisticsEndpoint : ApiEndpointBase
{
    private readonly CacheStatisticsAggregator _statsAggregator;

    /// <param name="statsAggregator">Statistics aggregator to query.</param>
    /// <param name="logger">Logger for operational diagnostics.</param>
    /// <param name="performanceMonitor">Performance monitor shared with all endpoints.</param>
    public CacheStatisticsEndpoint(
        CacheStatisticsAggregator statsAggregator,
        ILogger<CacheStatisticsEndpoint> logger,
        PerformanceMonitor performanceMonitor)
        : base(logger, performanceMonitor)
    {
        _statsAggregator = statsAggregator ?? throw new ArgumentNullException(nameof(statsAggregator));
    }

    /// <summary>
    /// Returns aggregated cache statistics including hits, misses, hit ratio, and error count.
    /// </summary>
    public Task<ApiResponse<CacheStatistics>> GetStatisticsAsync()
    {
        return ExecuteAsync(() =>
        {
            var stats = _statsAggregator.GetStatistics();
            return Task.FromResult(stats);
        }, "GetCacheStatistics");
    }

    /// <summary>
    /// Resets all cache statistics counters to zero.
    /// Useful after a cache flush or deployment boundary.
    /// </summary>
    public Task<ApiResponse<bool>> ResetAsync()
    {
        return ExecuteAsync(() =>
        {
            _statsAggregator.Reset();
            return Task.FromResult(true);
        }, "ResetCacheStatistics");
    }
}
