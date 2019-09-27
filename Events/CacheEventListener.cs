// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Events;

/// <summary>
/// Domain events related to cache operations
/// </summary>
public class CacheHitEvent : DomainEvent
{
    public string CacheKey { get; set; } = string.Empty;
    public long DataSize { get; set; }
}

public class CacheMissEvent : DomainEvent
{
    public string CacheKey { get; set; } = string.Empty;
}

public class CacheInvalidatedEvent : DomainEvent
{
    public string CacheKeyPattern { get; set; } = string.Empty;
    public int KeysAffected { get; set; }
}

public class CacheFlushEvent : DomainEvent
{
    public int KeysRemoved { get; set; }
}

/// <summary>
/// Listens to cache events and maintains statistics
/// </summary>
public class CacheEventListener
{
    private readonly ILogger<CacheEventListener> _logger;
    private int _hits;
    private int _misses;

    public CacheEventListener(ILogger<CacheEventListener> logger)
    {
        _logger = logger;
    }

    public Task OnCacheHitAsync(CacheHitEvent @event)
    {
        _hits++;
        _logger.LogDebug("Cache hit recorded: {Key} | Total hits: {Hits}", @event.CacheKey, _hits);
        return Task.CompletedTask;
    }

    public Task OnCacheMissAsync(CacheMissEvent @event)
    {
        _misses++;
        _logger.LogDebug("Cache miss recorded: {Key} | Total misses: {Misses}", @event.CacheKey, _misses);
        return Task.CompletedTask;
    }

    public Task OnCacheInvalidatedAsync(CacheInvalidatedEvent @event)
    {
        _logger.LogInformation("Cache invalidated: Pattern={Pattern} | KeysAffected={Count}",
            @event.CacheKeyPattern, @event.KeysAffected);
        return Task.CompletedTask;
    }

    public Task OnCacheFlushedAsync(CacheFlushEvent @event)
    {
        _logger.LogWarning("Cache flushed: {KeysRemoved} keys removed", @event.KeysRemoved);
        _hits = 0;
        _misses = 0;
        return Task.CompletedTask;
    }

    public double GetHitRate() => (_hits + _misses) > 0 ? (double)_hits / (_hits + _misses) * 100 : 0;
    public int GetTotalHits() => _hits;
    public int GetTotalMisses() => _misses;
}
