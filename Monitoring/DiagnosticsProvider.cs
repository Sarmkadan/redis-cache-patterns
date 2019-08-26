// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Services;

namespace RedisCachePatterns.Monitoring;

/// <summary>
/// Provides comprehensive diagnostics information for troubleshooting and analysis
/// Collects system metrics, cache stats, and performance data
/// </summary>
public class DiagnosticsProvider
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<DiagnosticsProvider> _logger;
    private readonly DateTime _startupTime = DateTime.UtcNow;

    public DiagnosticsProvider(ICacheService cacheService, ILogger<DiagnosticsProvider> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<DiagnosticReport> GenerateReportAsync()
    {
        try
        {
            var report = new DiagnosticReport { GeneratedAt = DateTime.UtcNow };

            // Application info
            report.ApplicationInfo.Add("Uptime", GetUptime());
            report.ApplicationInfo.Add("RunningOn", $".NET {GetNetVersion()}");

            // System info
            var process = Process.GetCurrentProcess();
            report.SystemInfo.Add("WorkingMemoryMB", (process.WorkingSet64 / 1024 / 1024).ToString());
            report.SystemInfo.Add("ProcessorCount", Environment.ProcessorCount.ToString());

            // Cache info
            try
            {
                var stats = await _cacheService.GetStatisticsAsync();
                report.CacheInfo.Add("TotalKeys", stats.TotalKeys.ToString());
                report.CacheInfo.Add("MemoryUsedKB", (stats.MemoryUsedBytes / 1024).ToString());
                report.CacheInfo.Add("HitRate", $"{stats.HitRate:F2}%");
            }
            catch (Exception ex)
            {
                report.Warnings.Add($"Cache diagnostics failed: {ex.Message}");
                _logger.LogWarning(ex, "Error collecting cache diagnostics");
            }

            _logger.LogInformation("Diagnostics report generated successfully");
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating diagnostics report");
            throw;
        }
    }

    public async Task<string> GenerateHtmlReportAsync()
    {
        var report = await GenerateReportAsync();
        return GenerateHtml(report);
    }

    private string GetUptime()
    {
        var uptime = DateTime.UtcNow - _startupTime;
        return uptime.Days > 0
            ? $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m"
            : $"{uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
    }

    private string GetNetVersion()
    {
        return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
    }

    private string GenerateHtml(DiagnosticReport report)
    {
        var html = new System.Text.StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head><title>Cache Diagnostics</title></head><body>");
        html.AppendLine($"<h1>Cache Diagnostics Report - {report.GeneratedAt:O}</h1>");

        html.AppendLine("<h2>Application Info</h2><ul>");
        foreach (var (key, value) in report.ApplicationInfo)
            html.AppendLine($"<li>{key}: {value}</li>");
        html.AppendLine("</ul>");

        html.AppendLine("<h2>System Info</h2><ul>");
        foreach (var (key, value) in report.SystemInfo)
            html.AppendLine($"<li>{key}: {value}</li>");
        html.AppendLine("</ul>");

        html.AppendLine("<h2>Cache Info</h2><ul>");
        foreach (var (key, value) in report.CacheInfo)
            html.AppendLine($"<li>{key}: {value}</li>");
        html.AppendLine("</ul>");

        if (report.Warnings.Count > 0)
        {
            html.AppendLine("<h2>Warnings</h2><ul>");
            foreach (var warning in report.Warnings)
                html.AppendLine($"<li>{warning}</li>");
            html.AppendLine("</ul>");
        }

        html.AppendLine("</body></html>");
        return html.ToString();
    }
}

/// <summary>
/// Diagnostic report containing system and cache metrics
/// </summary>
public class DiagnosticReport
{
    public DateTime GeneratedAt { get; set; }
    public Dictionary<string, string> ApplicationInfo { get; set; } = new();
    public Dictionary<string, string> SystemInfo { get; set; } = new();
    public Dictionary<string, string> CacheInfo { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
