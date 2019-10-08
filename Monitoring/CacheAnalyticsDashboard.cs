#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Services;

namespace RedisCachePatterns.Monitoring;

// ─── Supporting models ────────────────────────────────────────────────────────

/// <summary>
/// Per-key access statistics tracked by <see cref="CacheAnalyticsDashboard"/>.
/// All counters use <see cref="long"/> to prevent overflow on high-traffic keys.
/// </summary>
public sealed class KeyAccessStats
{
    /// <summary>The Redis cache key these stats belong to.</summary>
    public string Key { get; init; } = string.Empty;

    // Backing fields so Interlocked operations work correctly.
    private long _hits;
    private long _misses;

    /// <summary>Number of times the key was found in cache (cache hit).</summary>
    public long Hits
    {
        get => Interlocked.Read(ref _hits);
        set => Interlocked.Exchange(ref _hits, value);
    }

    /// <summary>Number of times the key was not found in cache (cache miss).</summary>
    public long Misses
    {
        get => Interlocked.Read(ref _misses);
        set => Interlocked.Exchange(ref _misses, value);
    }

    internal void IncrementHits() => Interlocked.Increment(ref _hits);
    internal void IncrementMisses() => Interlocked.Increment(ref _misses);

    /// <summary>UTC timestamp of the most recent access (hit or miss).</summary>
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp when this stats entry was first created.</summary>
    public DateTime FirstSeenAt { get; init; } = DateTime.UtcNow;

    /// <summary>Ratio of hits to total accesses. Returns 0 when no accesses recorded.</summary>
    public double HitRate => (Hits + Misses) > 0 ? (double)Hits / (Hits + Misses) : 0;

    /// <summary>Total number of times this key was accessed.</summary>
    public long TotalAccesses => Hits + Misses;
}

/// <summary>
/// Aggregate analytics snapshot produced by <see cref="CacheAnalyticsDashboard.GetSnapshot"/>.
/// </summary>
public sealed class AnalyticsSnapshot
{
    /// <summary>UTC timestamp when the snapshot was captured.</summary>
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Overall cache hit rate across all tracked keys (0–1).</summary>
    public double OverallHitRate { get; init; }

    /// <summary>Total number of cache hits recorded since the last reset.</summary>
    public long TotalHits { get; init; }

    /// <summary>Total number of cache misses recorded since the last reset.</summary>
    public long TotalMisses { get; init; }

    /// <summary>Number of distinct cache keys that have been accessed.</summary>
    public int UniqueKeysTracked { get; init; }

    /// <summary>
    /// The top N most-accessed keys, sorted by total accesses descending.
    /// </summary>
    public IReadOnlyList<KeyAccessStats> HotKeys { get; init; } = Array.Empty<KeyAccessStats>();

    /// <summary>
    /// Keys that have been accessed only once or not accessed recently — candidates for eviction.
    /// </summary>
    public IReadOnlyList<KeyAccessStats> ColdKeys { get; init; } = Array.Empty<KeyAccessStats>();

    /// <summary>
    /// Keys whose hit rate is below the configured <c>lowHitRateThreshold</c>.
    /// These keys are frequently requested but seldom found in cache.
    /// </summary>
    public IReadOnlyList<KeyAccessStats> LowHitRateKeys { get; init; } = Array.Empty<KeyAccessStats>();
}

// ─── Dashboard ────────────────────────────────────────────────────────────────

/// <summary>
/// Tracks per-key cache access patterns and generates analytics snapshots and
/// text-based dashboard reports.
///
/// <para>
/// <b>Usage:</b> call <see cref="RecordHit"/> / <see cref="RecordMiss"/> from within the
/// cache layer (or decorate <see cref="ICacheService"/>), then periodically call
/// <see cref="GetSnapshot"/> or <see cref="RenderReport"/> to inspect cache behaviour.
/// </para>
///
/// <para>
/// <b>Thread safety:</b> uses a <see cref="ConcurrentDictionary{TKey, TValue}"/> for per-key counters
/// and <see cref="Interlocked"/> for aggregate totals so all public methods are safe to
/// call from multiple threads simultaneously.
/// </para>
/// </summary>
public sealed class CacheAnalyticsDashboard
{
    private readonly ILogger<CacheAnalyticsDashboard> _logger;
    private readonly ConcurrentDictionary<string, KeyAccessStats> _keyStats = new();
    private long _totalHits;
    private long _totalMisses;
    private DateTime _resetAt = DateTime.UtcNow;

    // Configurable thresholds
    private readonly int _topNHotKeys;
    private readonly double _lowHitRateThreshold;
    private readonly TimeSpan _coldKeyAge;

    /// <param name="logger">Logger for administrative events (reset, report).</param>
    /// <param name="topNHotKeys">
    /// How many hot keys to include in <see cref="AnalyticsSnapshot.HotKeys"/>.
    /// Defaults to 10.
    /// </param>
    /// <param name="lowHitRateThreshold">
    /// Hit-rate below which a key is classified as "low hit rate".
    /// Defaults to <c>0.3</c> (30 %).
    /// </param>
    /// <param name="coldKeyAge">
    /// A key that has not been accessed for longer than this duration is classified as cold.
    /// Defaults to 1 hour.
    /// </param>
    public CacheAnalyticsDashboard(
        ILogger<CacheAnalyticsDashboard> logger,
        int topNHotKeys = 10,
        double lowHitRateThreshold = 0.3,
        TimeSpan? coldKeyAge = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _topNHotKeys = topNHotKeys;
        _lowHitRateThreshold = lowHitRateThreshold;
        _coldKeyAge = coldKeyAge ?? TimeSpan.FromHours(1);
    }

    // ─── Recording ────────────────────────────────────────────────────────────

    /// <summary>
    /// Records a cache hit for the specified key.
    /// Increments the per-key hit counter and the aggregate hit total.
    /// </summary>
    /// <param name="key">The Redis cache key that was found.</param>
    public void RecordHit(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        var stats = _keyStats.GetOrAdd(key, k => new KeyAccessStats { Key = k });
        stats.IncrementHits();
        stats.LastAccessedAt = DateTime.UtcNow;
        Interlocked.Increment(ref _totalHits);
    }

    /// <summary>
    /// Records a cache miss for the specified key.
    /// Increments the per-key miss counter and the aggregate miss total.
    /// </summary>
    /// <param name="key">The Redis cache key that was not found.</param>
    public void RecordMiss(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        var stats = _keyStats.GetOrAdd(key, k => new KeyAccessStats { Key = k });
        stats.IncrementMisses();
        stats.LastAccessedAt = DateTime.UtcNow;
        Interlocked.Increment(ref _totalMisses);
    }

    // ─── Querying ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all tracked statistics for a specific key, or <c>null</c> if the key
    /// has not been accessed since the last reset.
    /// </summary>
    public KeyAccessStats? GetKeyStats(string key) =>
        _keyStats.TryGetValue(key, out var s) ? s : null;

    /// <summary>
    /// Builds and returns a point-in-time <see cref="AnalyticsSnapshot"/> of all
    /// tracked access patterns.
    /// </summary>
    public AnalyticsSnapshot GetSnapshot()
    {
        var hits = Interlocked.Read(ref _totalHits);
        var misses = Interlocked.Read(ref _totalMisses);
        var total = hits + misses;
        var allStats = _keyStats.Values.ToList();
        var cutoff = DateTime.UtcNow - _coldKeyAge;

        return new AnalyticsSnapshot
        {
            CapturedAt = DateTime.UtcNow,
            TotalHits = hits,
            TotalMisses = misses,
            OverallHitRate = total > 0 ? (double)hits / total : 0,
            UniqueKeysTracked = allStats.Count,
            HotKeys = allStats
                .OrderByDescending(s => s.TotalAccesses)
                .Take(_topNHotKeys)
                .ToList(),
            ColdKeys = allStats
                .Where(s => s.LastAccessedAt < cutoff)
                .OrderBy(s => s.LastAccessedAt)
                .Take(_topNHotKeys)
                .ToList(),
            LowHitRateKeys = allStats
                .Where(s => s.TotalAccesses >= 5 && s.HitRate < _lowHitRateThreshold)
                .OrderBy(s => s.HitRate)
                .Take(_topNHotKeys)
                .ToList()
        };
    }

    /// <summary>
    /// Renders a formatted text-based dashboard report suitable for console output or logging.
    /// </summary>
    /// <returns>A multi-line string report.</returns>
    public string RenderReport()
    {
        var snap = GetSnapshot();
        var sb = new StringBuilder();

        sb.AppendLine("╔══════════════════════════════════════════════════╗");
        sb.AppendLine("║          Cache Analytics Dashboard                ║");
        sb.AppendLine("╚══════════════════════════════════════════════════╝");
        sb.AppendLine($"  Report generated : {snap.CapturedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"  Tracking since   : {_resetAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("── Overview ──────────────────────────────────────────");
        sb.AppendLine($"  Total hits       : {snap.TotalHits:N0}");
        sb.AppendLine($"  Total misses     : {snap.TotalMisses:N0}");
        sb.AppendLine($"  Overall hit rate : {snap.OverallHitRate:P1}");
        sb.AppendLine($"  Unique keys      : {snap.UniqueKeysTracked:N0}");
        sb.AppendLine();

        AppendKeySection(sb, "── Hot Keys (most accessed) ──────────────────────────", snap.HotKeys,
            s => $"  {s.Key,-40} hits={s.Hits,6} misses={s.Misses,6} hit%={s.HitRate:P0}");

        AppendKeySection(sb, "── Low Hit-Rate Keys (frequent misses) ───────────────", snap.LowHitRateKeys,
            s => $"  {s.Key,-40} hits={s.Hits,6} misses={s.Misses,6} hit%={s.HitRate:P0}");

        AppendKeySection(sb, "── Cold Keys (stale / not recently accessed) ──────────", snap.ColdKeys,
            s => $"  {s.Key,-40} last={s.LastAccessedAt:HH:mm:ss} accesses={s.TotalAccesses,6}");

        _logger.LogDebug("Cache analytics report rendered. UniqueKeys={Keys} HitRate={Rate:P1}",
            snap.UniqueKeysTracked, snap.OverallHitRate);

        return sb.ToString();
    }

    /// <summary>
    /// Resets all counters and clears per-key statistics.
    /// </summary>
    public void Reset()
    {
        _keyStats.Clear();
        Interlocked.Exchange(ref _totalHits, 0);
        Interlocked.Exchange(ref _totalMisses, 0);
        _resetAt = DateTime.UtcNow;
        _logger.LogInformation("Cache analytics dashboard reset.");
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    private static void AppendKeySection(
        StringBuilder sb,
        string header,
        IReadOnlyList<KeyAccessStats> keys,
        Func<KeyAccessStats, string> formatter)
    {
        if (keys.Count == 0) return;

        sb.AppendLine(header);
        foreach (var k in keys)
            sb.AppendLine(formatter(k));
        sb.AppendLine();
    }
}

// ─── Analytics API endpoint ───────────────────────────────────────────────────

/// <summary>
/// Response model returned by the analytics API endpoint.
/// </summary>
public sealed class AnalyticsDashboardResponse
{
    /// <summary>UTC timestamp when the data was collected.</summary>
    public DateTime CapturedAt { get; set; }

    /// <summary>Overall cache hit rate (0–1).</summary>
    public double OverallHitRate { get; set; }

    /// <summary>Total hits since last reset.</summary>
    public long TotalHits { get; set; }

    /// <summary>Total misses since last reset.</summary>
    public long TotalMisses { get; set; }

    /// <summary>Number of distinct keys tracked.</summary>
    public int UniqueKeysTracked { get; set; }

    /// <summary>Top N most-accessed keys.</summary>
    public IReadOnlyList<KeyAccessStats> HotKeys { get; set; } = Array.Empty<KeyAccessStats>();

    /// <summary>Keys with a hit rate below the configured threshold.</summary>
    public IReadOnlyList<KeyAccessStats> LowHitRateKeys { get; set; } = Array.Empty<KeyAccessStats>();

    /// <summary>Keys not accessed recently.</summary>
    public IReadOnlyList<KeyAccessStats> ColdKeys { get; set; } = Array.Empty<KeyAccessStats>();

    /// <summary>Pre-rendered text report.</summary>
    public string? TextReport { get; set; }

    internal static AnalyticsDashboardResponse FromSnapshot(AnalyticsSnapshot snap, string? textReport = null) =>
        new()
        {
            CapturedAt = snap.CapturedAt,
            OverallHitRate = snap.OverallHitRate,
            TotalHits = snap.TotalHits,
            TotalMisses = snap.TotalMisses,
            UniqueKeysTracked = snap.UniqueKeysTracked,
            HotKeys = snap.HotKeys,
            LowHitRateKeys = snap.LowHitRateKeys,
            ColdKeys = snap.ColdKeys,
            TextReport = textReport
        };
}
