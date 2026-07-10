#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;
using System.Text;
using System.Text.Json;
using RedisCachePatterns.Monitoring;

namespace RedisCachePatterns.API;

/// <summary>
/// Provides extension methods for <see cref="AnalyticsEndpoint"/> to enable
/// additional analytics operations and convenience methods for common scenarios.
/// </summary>
public static class AnalyticsEndpointExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Gets a snapshot of analytics with hot keys filtered by a minimum access threshold.
    /// </summary>
    /// <param name="endpoint">The analytics endpoint to query.</param>
    /// <param name="minAccessThreshold">Minimum number of accesses to be considered hot. Defaults to 100.</param>
    /// <param name="includeReport">When <c>true</c>, includes the pre-rendered text dashboard.</param>
    /// <returns>An API response containing filtered analytics data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="endpoint"/> is <see langword="null"/></exception>
    public static async Task<ApiResponse<AnalyticsDashboardResponse>> GetHotKeysSnapshotAsync(
        this AnalyticsEndpoint endpoint,
        int minAccessThreshold = 100,
        bool includeReport = false)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var response = await endpoint.GetSnapshotAsync(includeReport);

        if (response.IsSuccess && response.Data is not null)
        {
            var filtered = FilterHotKeys(response.Data, minAccessThreshold);
            var result = new AnalyticsDashboardResponse
            {
                CapturedAt = response.Data.CapturedAt,
                OverallHitRate = response.Data.OverallHitRate,
                TotalHits = response.Data.TotalHits,
                TotalMisses = response.Data.TotalMisses,
                UniqueKeysTracked = response.Data.UniqueKeysTracked,
                HotKeys = filtered,
                LowHitRateKeys = response.Data.LowHitRateKeys,
                ColdKeys = response.Data.ColdKeys
            };

            return ApiResponse<AnalyticsDashboardResponse>.Success(result);
        }

        return response;
    }

    /// <summary>
    /// Gets a snapshot of analytics with cold keys filtered by a maximum age threshold.
    /// </summary>
    /// <param name="endpoint">The analytics endpoint to query.</param>
    /// <param name="maxAgeHours">Maximum age in hours for a key to be considered cold. Defaults to 1 hour.</param>
    /// <param name="includeReport">When <c>true</c>, includes the pre-rendered text dashboard.</param>
    /// <returns>An API response containing filtered analytics data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="endpoint"/> is <see langword="null"/></exception>
    public static async Task<ApiResponse<AnalyticsDashboardResponse>> GetColdKeysSnapshotAsync(
        this AnalyticsEndpoint endpoint,
        double maxAgeHours = 1,
        bool includeReport = false)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var response = await endpoint.GetSnapshotAsync(includeReport);

        if (response.IsSuccess && response.Data is not null)
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromHours(maxAgeHours);
            var filtered = response.Data.ColdKeys
                .Where(k => k.LastAccessedAt < cutoff)
                .ToList()
                .AsReadOnly();

            var result = new AnalyticsDashboardResponse
            {
                CapturedAt = response.Data.CapturedAt,
                OverallHitRate = response.Data.OverallHitRate,
                TotalHits = response.Data.TotalHits,
                TotalMisses = response.Data.TotalMisses,
                UniqueKeysTracked = response.Data.UniqueKeysTracked,
                HotKeys = response.Data.HotKeys,
                LowHitRateKeys = response.Data.LowHitRateKeys,
                ColdKeys = filtered
            };

            return ApiResponse<AnalyticsDashboardResponse>.Success(result);
        }

        return response;
    }

    /// <summary>
    /// Gets a snapshot of analytics with keys having poor cache efficiency filtered by hit rate threshold.
    /// </summary>
    /// <param name="endpoint">The analytics endpoint to query.</param>
    /// <param name="minHitRate">Minimum acceptable hit rate (0-1). Defaults to 0.5 (50%).</param>
    /// <param name="includeReport">When <c>true</c>, includes the pre-rendered text dashboard.</param>
    /// <returns>An API response containing filtered analytics data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="endpoint"/> is <see langword="null"/></exception>
    public static async Task<ApiResponse<AnalyticsDashboardResponse>> GetPoorEfficiencyKeysAsync(
        this AnalyticsEndpoint endpoint,
        double minHitRate = 0.5,
        bool includeReport = false)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var response = await endpoint.GetSnapshotAsync(includeReport);

        if (response.IsSuccess && response.Data is not null)
        {
            var filtered = response.Data.LowHitRateKeys
                .Where(k => k.HitRate < minHitRate)
                .ToList()
                .AsReadOnly();

            var result = new AnalyticsDashboardResponse
            {
                CapturedAt = response.Data.CapturedAt,
                OverallHitRate = response.Data.OverallHitRate,
                TotalHits = response.Data.TotalHits,
                TotalMisses = response.Data.TotalMisses,
                UniqueKeysTracked = response.Data.UniqueKeysTracked,
                HotKeys = response.Data.HotKeys,
                LowHitRateKeys = filtered,
                ColdKeys = response.Data.ColdKeys
            };

            return ApiResponse<AnalyticsDashboardResponse>.Success(result);
        }

        return response;
    }

    /// <summary>
    /// Gets statistics for a specific cache key with enhanced formatting options.
    /// </summary>
    /// <param name="endpoint">The analytics endpoint to query.</param>
    /// <param name="key">The exact cache key to look up.</param>
    /// <param name="format">Output format for the statistics. Defaults to machine-readable format.</param>
    /// <returns>An API response containing formatted key statistics.</returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="endpoint"/> is <see langword="null"/>
    ///   <paramref name="key"/> is <see langword="null"/> or empty
    /// </exception>
    public static async Task<ApiResponse<string>> GetFormattedKeyStatsAsync(
        this AnalyticsEndpoint endpoint,
        string key,
        KeyStatsFormat format = KeyStatsFormat.MachineReadable)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var response = await endpoint.GetKeyStatsAsync(key);

        if (response.IsSuccess && response.Data is not null)
        {
            return format switch
            {
                KeyStatsFormat.MachineReadable => ApiResponse<string>.Success(response.Data.ToMachineString()),
                KeyStatsFormat.HumanReadable => ApiResponse<string>.Success(response.Data.ToSummaryString()),
                KeyStatsFormat.Json => ApiResponse<string>.Success(JsonSerializer.Serialize(response.Data, _jsonOptions)),
                _ => ApiResponse<string>.Success(response.Data.ToMachineString())
            };
        }

        return ApiResponse<string>.Failure(response.Error ?? "Unknown error", response.StatusCode);
    }

    /// <summary>
    /// Resets analytics counters and returns a confirmation message.
    /// </summary>
    /// <param name="endpoint">The analytics endpoint to reset.</param>
    /// <param name="reason">Optional reason for resetting analytics.</param>
    /// <returns>An API response containing a confirmation message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="endpoint"/> is <see langword="null"/></exception>
    public static async Task<ApiResponse<string>> ResetWithConfirmationAsync(
        this AnalyticsEndpoint endpoint,
        string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var response = await endpoint.ResetAsync();

        if (response.IsSuccess)
        {
            var message = reason is null
                ? "Analytics counters have been reset successfully."
                : $"Analytics counters have been reset successfully. Reason: {reason}";

            return ApiResponse<string>.Success(message);
        }

        return ApiResponse<string>.Failure(response.Error ?? "Unknown error", response.StatusCode);
    }

    /// <summary>
    /// Gets a summary report of cache efficiency across all tracked keys.
    /// </summary>
    /// <param name="endpoint">The analytics endpoint to query.</param>
    /// <returns>An API response containing a cache efficiency summary.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="endpoint"/> is <see langword="null"/></exception>
    public static async Task<ApiResponse<string>> GetEfficiencySummaryAsync(this AnalyticsEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var snapshotResponse = await endpoint.GetSnapshotAsync();

        if (!snapshotResponse.IsSuccess || snapshotResponse.Data is null)
        {
            return ApiResponse<string>.Failure(
                snapshotResponse.Error ?? "Failed to retrieve analytics snapshot",
                snapshotResponse.StatusCode);
        }

        var snapshot = snapshotResponse.Data;
        var efficiencySummary = new StringBuilder();

        efficiencySummary.AppendLine("=== Cache Efficiency Summary ===");
        efficiencySummary.AppendLine($"Captured At: {snapshot.CapturedAt:yyyy-MM-dd HH:mm:ss} UTC");
        efficiencySummary.AppendLine($"Overall Hit Rate: {snapshot.OverallHitRate:P1}");
        efficiencySummary.AppendLine($"Total Hits: {snapshot.TotalHits:N0}");
        efficiencySummary.AppendLine($"Total Misses: {snapshot.TotalMisses:N0}");
        efficiencySummary.AppendLine($"Unique Keys Tracked: {snapshot.UniqueKeysTracked:N0}");
        efficiencySummary.AppendLine();

        efficiencySummary.AppendLine("=== Key Categories ===");
        efficiencySummary.AppendLine($"Hot Keys (>= 100 accesses): {snapshot.HotKeys.Count:N0}");
        efficiencySummary.AppendLine($"Cold Keys: {snapshot.ColdKeys.Count:N0}");
        efficiencySummary.AppendLine($"Low Hit Rate Keys (< 50%): {snapshot.LowHitRateKeys.Count:N0}");
        efficiencySummary.AppendLine();

        if (snapshot.HotKeys.Count > 0)
        {
            var topHotKey = snapshot.HotKeys[0];
            efficiencySummary.AppendLine("=== Top Hot Key ===");
            efficiencySummary.AppendLine(topHotKey.ToSummaryString());
        }

        if (snapshot.ColdKeys.Count > 0)
        {
            var oldestColdKey = snapshot.ColdKeys
                .OrderBy(k => k.LastAccessedAt)
                .FirstOrDefault();

            if (oldestColdKey is not null)
            {
                efficiencySummary.AppendLine("=== Oldest Cold Key ===");
                efficiencySummary.AppendLine(oldestColdKey.ToSummaryString());
            }
        }

        return ApiResponse<string>.Success(efficiencySummary.ToString());
    }

    /// <summary>
    /// Filters hot keys based on minimum access threshold.
    /// </summary>
    private static IReadOnlyList<KeyAccessStats> FilterHotKeys(
        AnalyticsDashboardResponse response,
        int minAccessThreshold)
    {
        return response.HotKeys
            .Where(k => k.TotalAccesses >= minAccessThreshold)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Format options for key statistics output.
    /// </summary>
    public enum KeyStatsFormat
    {
        /// <summary>Machine-readable format suitable for logging and telemetry.</summary>
        MachineReadable,
        /// <summary>Human-readable format suitable for display in dashboards.</summary>
        HumanReadable,
        /// <summary>JSON format for programmatic consumption.</summary>
        Json
    }
}