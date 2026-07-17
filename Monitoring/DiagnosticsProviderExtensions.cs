#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.ObjectModel;

namespace RedisCachePatterns.Monitoring;

/// <summary>
/// Extension methods for <see cref="DiagnosticsProvider"/> that provide additional diagnostic capabilities
/// </summary>
public static class DiagnosticsProviderExtensions
{
    /// <summary>
    /// Filters warnings by severity or keyword from the diagnostic report
    /// </summary>
    /// <param name="provider">The diagnostics provider instance</param>
    /// <param name="predicate">Filter predicate to match warnings</param>
    /// <returns>Filtered collection of warnings matching the predicate</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider"/> or <paramref name="predicate"/> is null</exception>
    public static async Task<IReadOnlyList<string>> FilterWarningsAsync(
        this DiagnosticsProvider provider,
        Func<string, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(predicate);

        var report = await provider.GenerateReportAsync();
        return report.Warnings.Where(predicate).ToList();
    }

    /// <summary>
    /// Gets cache statistics summary as a formatted string
    /// </summary>
    /// <param name="provider">The diagnostics provider instance</param>
    /// <returns>Formatted cache statistics string</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider"/> is null</exception>
    public static async Task<string> GetCacheStatsSummaryAsync(this DiagnosticsProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var report = await provider.GenerateReportAsync();

        if (report.CacheInfo.TryGetValue("TotalKeys", out var totalKeys) &&
            report.CacheInfo.TryGetValue("MemoryUsedKB", out var memoryUsedKB))
        {
            return $"Total Keys: {totalKeys}, Memory Used: {memoryUsedKB} KB";
        }

        return "Cache statistics unavailable";
    }

    /// <summary>
    /// Gets application information as a read-only dictionary
    /// </summary>
    /// <param name="provider">The diagnostics provider instance</param>
    /// <param name="key">The key to retrieve from application info</param>
    /// <returns>The value associated with the key, or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or empty</exception>
    public static async Task<string?> GetApplicationInfoAsync(
        this DiagnosticsProvider provider,
        string key)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var report = await provider.GenerateReportAsync();
        return report.ApplicationInfo.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Gets system information as a read-only dictionary
    /// </summary>
    /// <param name="provider">The diagnostics provider instance</param>
    /// <param name="key">The key to retrieve from system info</param>
    /// <returns>The value associated with the key, or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or empty</exception>
    public static async Task<string?> GetSystemInfoAsync(
        this DiagnosticsProvider provider,
        string key)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var report = await provider.GenerateReportAsync();
        return report.SystemInfo.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Checks if any warnings were generated in the diagnostic report
    /// </summary>
    /// <param name="provider">The diagnostics provider instance</param>
    /// <returns>True if warnings exist, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider"/> is null</exception>
    public static async Task<bool> HasWarningsAsync(this DiagnosticsProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var report = await provider.GenerateReportAsync();
        return report.Warnings.Count > 0;
    }

    /// <summary>
    /// Gets all diagnostic information as a flattened dictionary
    /// </summary>
    /// <param name="provider">The diagnostics provider instance</param>
    /// <returns>Dictionary containing all diagnostic information with prefixed keys</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider"/> is null</exception>
    public static async Task<IReadOnlyDictionary<string, string>> GetAllDiagnosticsAsync(
        this DiagnosticsProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var report = await provider.GenerateReportAsync();
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (key, value) in report.ApplicationInfo)
        {
            result[$"app.{key}"] = value;
        }

        foreach (var (key, value) in report.SystemInfo)
        {
            result[$"system.{key}"] = value;
        }

        foreach (var (key, value) in report.CacheInfo)
        {
            result[$"cache.{key}"] = value;
        }

        return result;
    }
}