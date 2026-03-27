// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Domain;
using RedisCachePatterns.Services;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Monitor for tracking cache performance and health
/// </summary>
public class CacheMonitor
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheMonitor> _logger;
    private readonly List<CacheEntry> _entries = new();
    private DateTime _lastSnapshot = DateTime.UtcNow;

    public CacheMonitor(ICacheService cacheService, ILogger<CacheMonitor> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        return await _cacheService.GetStatisticsAsync();
    }

    public async Task PrintStatisticsAsync()
    {
        var stats = await GetStatisticsAsync();

        _logger.LogInformation("=== Cache Statistics ===");
        _logger.LogInformation("Total Keys: {Keys}", stats.TotalKeys);
        _logger.LogInformation("Memory Used: {Memory}MB", stats.MemoryUsedBytes / (1024 * 1024));
        _logger.LogInformation("Hits: {Hits}", stats.Hits);
        _logger.LogInformation("Misses: {Misses}", stats.Misses);
        _logger.LogInformation("Hit Rate: {HitRate}%", stats.HitRate.ToString("F2"));
        _logger.LogInformation("Captured At: {Time}", stats.CapturedAt);
    }

    public void TrackEntry(CacheEntry entry)
    {
        _entries.Add(entry);
    }

    public IEnumerable<CacheEntry> GetTrackedEntries() => _entries.ToList();

    public void ClearTracking()
    {
        _entries.Clear();
    }

    public async Task<double> GetAverageHitRateAsync()
    {
        if (!_entries.Any())
            return 0;

        return _entries.Average(e => e.HitRate);
    }

    public long GetTotalCacheSize()
    {
        return _entries.Sum(e => e.SizeInBytes);
    }

    public IEnumerable<CacheEntry> GetEntriesByHitRate(double minHitRate)
    {
        return _entries.Where(e => e.HitRate >= minHitRate).OrderByDescending(e => e.HitRate);
    }

    public IEnumerable<CacheEntry> GetColdEntries(TimeSpan inactiveDuration)
    {
        var threshold = DateTime.UtcNow - inactiveDuration;
        return _entries.Where(e => e.LastAccessedAt < threshold).OrderBy(e => e.LastAccessedAt);
    }
}
