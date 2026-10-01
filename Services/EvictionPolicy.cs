#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Services;

/// <summary>
/// Implements cache eviction strategies (LRU, LFU, FIFO) for selecting entries to remove
/// </summary>
public class EvictionPolicy
{
    private readonly ILogger<EvictionPolicy> _logger;
    private readonly EvictionStrategy _strategy;
    private readonly ConcurrentDictionary<string, AccessRecord> _accessLog = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EvictionPolicy"/> class
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="strategy">Eviction strategy to use (default LRU)</param>
    public EvictionPolicy(ILogger<EvictionPolicy> logger, EvictionStrategy strategy = EvictionStrategy.LeastRecentlyUsed)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _strategy = strategy;
    }

    /// <summary>
    /// Records an access event for the given key, updating frequency and recency data
    /// </summary>
    /// <param name="key">Cache key that was accessed</param>
    public void RecordAccess(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        _accessLog.AddOrUpdate(
            key,
            _ => new AccessRecord { Key = key, LastAccess = DateTime.UtcNow, AccessCount = 1, CreatedAt = DateTime.UtcNow },
            (_, existing) =>
            {
                existing.LastAccess = DateTime.UtcNow;
                existing.AccessCount++;
                return existing;
            });
    }

    /// <summary>
    /// Removes a key from the access tracking log
    /// </summary>
    /// <param name="key">Cache key to stop tracking</param>
    public void Remove(string key)
    {
        _accessLog.TryRemove(key, out _);
    }

    /// <summary>
    /// Selects keys for eviction based on the configured strategy
    /// </summary>
    /// <param name="candidateKeys">Keys eligible for eviction</param>
    /// <param name="maxToKeep">Maximum number of keys to retain</param>
    /// <returns>Keys that should be evicted</returns>
    public IReadOnlyList<string> SelectForEviction(IEnumerable<string> candidateKeys, int maxToKeep)
    {
        var candidates = candidateKeys.ToList();
        if (candidates.Count <= maxToKeep)
            return Array.Empty<string>();

        var toEvictCount = candidates.Count - maxToKeep;

        var ordered = _strategy switch
        {
            EvictionStrategy.LeastRecentlyUsed => candidates
                .OrderBy(k => _accessLog.TryGetValue(k, out var r) ? r.LastAccess : DateTime.MinValue),
            EvictionStrategy.LeastFrequentlyUsed => candidates
                .OrderBy(k => _accessLog.TryGetValue(k, out var r) ? r.AccessCount : 0),
            EvictionStrategy.FirstInFirstOut => candidates
                .OrderBy(k => _accessLog.TryGetValue(k, out var r) ? r.CreatedAt : DateTime.MinValue),
            _ => candidates.OrderBy(_ => Guid.NewGuid()) // random fallback
        };

        var evicted = ordered.Take(toEvictCount).ToList();
        _logger.LogInformation(
            "Selected {Count} keys for eviction using {Strategy}",
            evicted.Count, _strategy);
        return evicted;
    }

    /// <summary>
    /// Returns the current eviction strategy name
    /// </summary>
    public string StrategyName => _strategy.ToString();

    /// <summary>
    /// Returns the number of keys currently being tracked
    /// </summary>
    public int TrackedKeyCount => _accessLog.Count;

    private class AccessRecord
    {
        public string Key { get; set; } = string.Empty;
        public DateTime LastAccess { get; set; }
        public DateTime CreatedAt { get; set; }
        public int AccessCount { get; set; }
    }
}

/// <summary>
/// Available cache eviction strategies
/// </summary>
public enum EvictionStrategy
{
    /// <summary>Evicts the least recently accessed entries first</summary>
    LeastRecentlyUsed,

    /// <summary>Evicts the least frequently accessed entries first</summary>
    LeastFrequentlyUsed,

    /// <summary>Evicts the oldest entries first</summary>
    FirstInFirstOut
}
