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
/// API endpoint that exposes cache analytics data gathered by
/// <see cref="CacheAnalyticsDashboard"/>.
/// </summary>
public sealed class AnalyticsEndpoint : ApiEndpointBase
{
    private readonly CacheAnalyticsDashboard _dashboard;

    /// <param name="dashboard">Analytics dashboard to query.</param>
    /// <param name="logger">Logger for operational diagnostics.</param>
    /// <param name="performanceMonitor">Performance monitor shared with all endpoints.</param>
    public AnalyticsEndpoint(
        CacheAnalyticsDashboard dashboard,
        ILogger<AnalyticsEndpoint> logger,
        PerformanceMonitor performanceMonitor)
        : base(logger, performanceMonitor)
    {
        _dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));
    }

    /// <summary>
    /// Returns a full analytics snapshot including hot keys, cold keys, and hit-rate data.
    /// Optionally includes a pre-rendered text report when <paramref name="includeReport"/> is <c>true</c>.
    /// </summary>
    /// <param name="includeReport">
    /// When <c>true</c> the response includes the pre-rendered text dashboard.
    /// </param>
    public Task<ApiResponse<AnalyticsDashboardResponse>> GetSnapshotAsync(bool includeReport = false)
    {
        return ExecuteAsync(() =>
        {
            var snap = _dashboard.GetSnapshot();
            var report = includeReport ? _dashboard.RenderReport() : null;
            return Task.FromResult(AnalyticsDashboardResponse.FromSnapshot(snap, report));
        }, "GetAnalyticsSnapshot");
    }

    /// <summary>
    /// Returns the rendered text-based dashboard report for quick console inspection.
    /// </summary>
    public Task<ApiResponse<string>> GetReportAsync()
    {
        return ExecuteAsync(
            () => Task.FromResult(_dashboard.RenderReport()),
            "GetAnalyticsReport");
    }

    /// <summary>
    /// Returns access statistics for a single cache key, or HTTP 404 when not tracked.
    /// </summary>
    /// <param name="key">The exact cache key to look up.</param>
    public Task<ApiResponse<KeyAccessStats>> GetKeyStatsAsync(string key)
    {
        ValidateRequired(key, nameof(key));

        return ExecuteAsync(() =>
        {
            var stats = _dashboard.GetKeyStats(key);
            if (stats is null)
                throw new KeyNotFoundException($"Key '{key}' is not tracked in the analytics dashboard.");

            return Task.FromResult(stats);
        }, $"GetKeyStats({key})");
    }

    /// <summary>
    /// Resets all analytics counters. Useful after a cache flush or deployment boundary.
    /// </summary>
    public Task<ApiResponse<bool>> ResetAsync()
    {
        return ExecuteAsync(() =>
        {
            _dashboard.Reset();
            return Task.FromResult(true);
        }, "ResetAnalytics");
    }
}
