using RedisCachePatterns.Infrastructure.Cache;
using StackExchange.Redis;

namespace RedisCachePatterns.Services;

/// <summary>
/// Tag-based grouping and invalidation of cache keys. Each tag is a Redis SET at
/// "cache:tags:{tag}" containing the member keys. Invalidating a tag removes every
/// member key via ICacheService.RemoveAsync and then deletes the tag set.
/// </summary>
public sealed class CacheTagService
{
    private const string TagKeyPrefix = "cache:tags:";
    private readonly IRedisConnection _redis;
    private readonly ICacheService _cache;

    public CacheTagService(IRedisConnection redis, ICacheService cache)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(cache);

        _redis = redis;
        _cache = cache;
    }

    /// <summary>Writes the value via ICacheService.SetAsync and adds the key to every tag set (SADD).</summary>
    public async Task SetWithTagsAsync<T>(string key, T value, IEnumerable<string> tags, TimeSpan? expiration = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(tags);

        // Write the value to cache
        await _cache.SetAsync(key, value, expiration).ConfigureAwait(false);

        // Add the key to each tag set
        foreach (var tag in tags)
        {
            await TagKeyAsync(key, tag).ConfigureAwait(false);
        }
    }

    /// <summary>Adds an existing cache key to a tag set without rewriting the value.</summary>
    public async Task TagKeyAsync(string key, string tag)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(tag);

        var tagKey = BuildTagKey(tag);
        var db = _redis.GetDatabase();
        await db.SetAddAsync(tagKey, key).ConfigureAwait(false);
    }

    /// <summary>Removes a key from a tag set (SREM). Returns true if the key was a member.</summary>
    public async Task<bool> UntagKeyAsync(string key, string tag)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(tag);

        var tagKey = BuildTagKey(tag);
        var db = _redis.GetDatabase();
        return await db.SetRemoveAsync(tagKey, key).ConfigureAwait(false);
    }

    /// <summary>Returns all cache keys currently associated with the tag (SMEMBERS).</summary>
    public async Task<IReadOnlyList<string>> GetKeysByTagAsync(string tag)
    {
        ArgumentException.ThrowIfNullOrEmpty(tag);

        var tagKey = BuildTagKey(tag);
        var db = _redis.GetDatabase();
        var members = await db.SetMembersAsync(tagKey).ConfigureAwait(false);
        return members.Select(m => (string)m!).ToList().AsReadOnly();
    }

    /// <summary>Removes every key in the tag set from the cache, deletes the tag set, and returns the number of keys invalidated.</summary>
    public async Task<int> InvalidateTagAsync(string tag)
    {
        ArgumentException.ThrowIfNullOrEmpty(tag);

        var tagKey = BuildTagKey(tag);
        var db = _redis.GetDatabase();

        // Get all keys in the tag set
        var members = await db.SetMembersAsync(tagKey).ConfigureAwait(false);
        var keys = members.Select(m => (string)m!).ToList();

        // Remove each key from cache
        foreach (var key in keys)
        {
            await _cache.RemoveAsync(key).ConfigureAwait(false);
        }

        // Remove the tag set itself
        await db.KeyDeleteAsync(tagKey).ConfigureAwait(false);

        return keys.Count;
    }

    /// <summary>Invalidates multiple tags; returns total keys invalidated across all of them.</summary>
    public async Task<int> InvalidateTagsAsync(IEnumerable<string> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        int totalInvalidated = 0;
        foreach (var tag in tags)
        {
            totalInvalidated += await InvalidateTagAsync(tag).ConfigureAwait(false);
        }

        return totalInvalidated;
    }

    /// <summary>Builds the Redis key for a tag set: "cache:tags:{tag}".</summary>
    public static string BuildTagKey(string tag)
    {
        ArgumentException.ThrowIfNullOrEmpty(tag);
        return TagKeyPrefix + tag;
    }
}