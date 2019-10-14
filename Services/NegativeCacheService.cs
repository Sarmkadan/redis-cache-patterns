using System;
using System.Threading.Tasks;

namespace RedisCachePatterns.Services;

/// <summary>
/// Cache-aside with negative caching: null loader results are stored as a string sentinel
/// ("__NEGATIVE__") under the same key with a short TTL, so repeated misses for nonexistent
/// entities do not repeatedly hit the data source (cache penetration protection).
/// </summary>
public sealed class NegativeCacheService
{
    /// <summary>Sentinel stored in Redis to mark a known-missing value.</summary>
    public const string NegativeSentinel = "__NEGATIVE__";

    private readonly ICacheService _cache;
    /// <summary>TTL applied to negative (sentinel) entries.</summary>
    public TimeSpan NegativeTtl { get; }
    /// <summary>Count of lookups answered by a cached negative entry.</summary>
    public long NegativeHits { get; private set; }

    public NegativeCacheService(ICacheService cache, TimeSpan? negativeTtl = null)
    {
        _cache = cache;
        NegativeTtl = negativeTtl ?? TimeSpan.FromSeconds(60);
    }

    /// <summary>
    /// Cache-aside for reference types: checks for the sentinel first (via GetAsync<string>),
    /// then the typed value, then calls loadFn. Null results are cached as the sentinel with NegativeTtl.
    /// </summary>
    public async Task<T?> GetOrLoadWithNegativeCachingAsync<T>(string key, Func<Task<T?>> loadFn, TimeSpan? expiration = null) where T : class
    {
        // Check if the key has a negative sentinel
        var cachedSentinel = await _cache.GetAsync<string>(key);
        if (cachedSentinel == NegativeSentinel)
        {
            NegativeHits++;
            return null;
        }

        // Try to get the actual value
        var cachedValue = await _cache.GetAsync<T>(key);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        // Load from source
        var loadedValue = await loadFn();

        if (loadedValue == null)
        {
            // Cache the negative sentinel
            await _cache.SetAsync(key, NegativeSentinel, NegativeTtl);
            return null;
        }

        // Cache the actual value with the provided expiration or default
        await _cache.SetAsync(key, loadedValue, expiration);
        return loadedValue;
    }

    /// <summary>True if the key currently holds a negative sentinel.</summary>
    public async Task<bool> IsNegativelyCachedAsync(string key)
    {
        var value = await _cache.GetAsync<string>(key);
        return value == NegativeSentinel;
    }

    /// <summary>Explicitly mark a key as known-missing for NegativeTtl.</summary>
    public Task MarkNegativeAsync(string key)
    {
        return _cache.SetAsync(key, NegativeSentinel, NegativeTtl);
    }

    /// <summary>Removes a negative entry so the next lookup retries the loader. Returns true if one existed.</summary>
    public async Task<bool> ClearNegativeAsync(string key)
    {
        var wasNegative = await IsNegativelyCachedAsync(key);
        if (wasNegative)
        {
            await _cache.RemoveAsync(key);
        }
        return wasNegative;
    }
}