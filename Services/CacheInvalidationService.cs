#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using RedisCachePatterns.Events;

namespace RedisCachePatterns.Services;

/// <summary>
/// Service for managing cache invalidation strategies and patterns
/// Supports tag-based invalidation, pattern matching, and smart dependency tracking
/// </summary>
public class CacheInvalidationService
{
    private readonly ICacheService _cacheService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CacheInvalidationService> _logger;
    private readonly Dictionary<string, HashSet<string>> _tagIndex = new();
    private readonly Lock _tagLock = new();

    public CacheInvalidationService(
        ICacheService cacheService,
        IEventPublisher eventPublisher,
        ILogger<CacheInvalidationService> logger)
    {
        _cacheService = cacheService;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <summary>
    /// Registers a cache key with one or more tags for group invalidation.
    /// Thread-safe: uses lock to protect the in-memory tag index.
    /// </summary>
    public void RegisterKeyWithTags(string cacheKey, params string[] tags)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            throw new ArgumentException("Cache key cannot be null or whitespace", nameof(cacheKey));
        if (tags is null || tags.Length == 0)
            return;

        lock (_tagLock)
        {
            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;

                if (!_tagIndex.TryGetValue(tag, out var keys))
                {
                    keys = new HashSet<string>();
                    _tagIndex[tag] = keys;
                }

                keys.Add(cacheKey);
            }
        }

        _logger.LogDebug("Cache key registered with tags: {Key} | Tags: {Tags}", cacheKey, string.Join(", ", tags));
    }

    /// <summary>
    /// Invalidates all cache keys associated with a tag
    /// </summary>
    public async Task InvalidateByTagAsync(string tag)
    {
        List<string> keysToRemove;
        lock (_tagLock)
        {
            if (!_tagIndex.TryGetValue(tag, out var keys))
            {
                _logger.LogDebug("No keys found for tag: {Tag}", tag);
                return;
            }

            keysToRemove = keys.ToList();
            _tagIndex.Remove(tag);
        }

        var removedCount = 0;

        foreach (var key in keysToRemove)
        {
            try
            {
                await _cacheService.RemoveAsync(key).ConfigureAwait(false);
                removedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate key: {Key}", key);
            }
        }

        // Publish invalidation event
        await _eventPublisher.PublishAsync(new CacheInvalidatedEvent
        {
            Source = nameof(CacheInvalidationService),
            CacheKeyPattern = $"tag:{tag}",
            KeysAffected = removedCount
        });

        _logger.LogInformation("Cache invalidated by tag: {Tag} | KeysRemoved: {Count}", tag, removedCount);
    }

    /// <summary>
    /// Invalidates cache by key pattern
    /// </summary>
    public async Task InvalidateByPatternAsync(string pattern)
    {
        var keys = await _cacheService.GetKeysByPatternAsync(pattern).ConfigureAwait(false);
        var keyList = keys.ToList();

        foreach (var key in keyList)
        {
            await _cacheService.RemoveAsync(key).ConfigureAwait(false);
        }

        // Publish invalidation event
        await _eventPublisher.PublishAsync(new CacheInvalidatedEvent
        {
            Source = nameof(CacheInvalidationService),
            CacheKeyPattern = pattern,
            KeysAffected = keyList.Count
        });

        _logger.LogInformation("Cache invalidated by pattern: {Pattern} | KeysRemoved: {Count}", pattern, keyList.Count);
    }

    /// <summary>
    /// Invalidates cache entry and related dependencies
    /// </summary>
    public async Task InvalidateWithDependenciesAsync(string cacheKey)
    {
        await _cacheService.RemoveAsync(cacheKey).ConfigureAwait(false);

        // Find and remove dependent keys (in production would maintain explicit dependency graph)
        var relatedKeys = _tagIndex.Values
            .Where(keys => keys.Contains(cacheKey))
            .SelectMany(keys => keys)
            .Distinct()
            .ToList();

        foreach (var relatedKey in relatedKeys)
        {
            if (relatedKey != cacheKey)
            {
                await _cacheService.RemoveAsync(relatedKey).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Cache invalidated with dependencies: {Key} | RelatedKeysRemoved: {Count}",
            cacheKey, relatedKeys.Count);
    }

    /// <summary>
    /// Gets all keys associated with a tag
    /// </summary>
    public IEnumerable<string> GetKeysByTag(string tag)
    {
        lock (_tagLock)
        {
            return _tagIndex.TryGetValue(tag, out var keys)
                ? keys.ToList() // Return a snapshot to avoid mutation during enumeration
                : Enumerable.Empty<string>();
        }
    }
}
