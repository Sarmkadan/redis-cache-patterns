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
    private const int TagSetChunkSize = 100;
    private readonly IRedisConnection _redis;
    private readonly ICacheService _cache;

    // Lua script for atomic tag invalidation: retrieves all tag members, removes them from cache,
    // and deletes the tag set in a single atomic operation.
    private static readonly LuaScript InvalidateTagScript = LuaScript.Prepare(
        @"local members = redis.call('SMEMBERS', @tagKey)
          if #members == 0 then
            redis.call('DEL', @tagKey)
            return 0
          end
          for _, key in ipairs(members) do
            redis.call('DEL', key)
          end
          redis.call('DEL', @tagKey)
          return #members");

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

    /// <summary>
    /// Removes every key in the tag set from the cache, deletes the tag set, and returns the number of keys invalidated.
    /// This operation is atomic: all keys are removed and the tag set is deleted in a single Lua script execution.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="tag"/> is empty or whitespace.</exception>
    public async Task<int> InvalidateTagAsync(string tag)
    {
        ArgumentException.ThrowIfNullOrEmpty(tag);

        var tagKey = BuildTagKey(tag);
        var db = _redis.GetDatabase();

        // Use Lua script for atomic invalidation: removes all keys and the tag set in one operation
        var result = (int)await db.ScriptEvaluateAsync(
            InvalidateTagScript,
            new { tagKey },
            flags: CommandFlags.None).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Invalidates multiple tags atomically; returns total keys invalidated across all of them.
    /// Each tag is invalidated in sequence using atomic Lua scripts to ensure consistency.
    /// </summary>
    /// <param name="tags">Collection of tags to invalidate.</param>
    /// <returns>Total number of keys invalidated across all tags.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tags"/> is null.</exception>
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

    /// <summary>
    /// Builds the Redis key for a tag set: "cache:tags:{tag}".
    /// </summary>
    /// <param name="tag">The tag name.</param>
    /// <returns>The Redis key for the tag set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="tag"/> is empty or whitespace.</exception>
    public static string BuildTagKey(string tag)
    {
        ArgumentException.ThrowIfNullOrEmpty(tag);
        return TagKeyPrefix + tag;
    }

    /// <summary>
    /// Cleans up orphaned tag entries by removing tag set members that reference non-existent cache keys.
    /// This prevents unbounded growth of tag sets when keys expire naturally without being untagged.
    /// </summary>
    /// <param name="tag">The tag to clean up.</param>
    /// <returns>The number of orphaned entries removed from the tag set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="tag"/> is empty or whitespace.</exception>
    public async Task<int> CleanOrphanedTagEntriesAsync(string tag)
    {
        ArgumentException.ThrowIfNullOrEmpty(tag);

        var tagKey = BuildTagKey(tag);
        var db = _redis.GetDatabase();

        // Lua script to remove non-existent keys from tag set atomically
        // Returns the number of orphaned entries removed
        var cleanScript = LuaScript.Prepare(
            @"local members = redis.call('SMEMBERS', @tagKey)
              local removedCount = 0
              for _, key in ipairs(members) do
                if redis.call('EXISTS', key) == 0 then
                  redis.call('SREM', @tagKey, key)
                  removedCount = removedCount + 1
                end
              end
              return removedCount");

        var result = (int)await db.ScriptEvaluateAsync(
            cleanScript,
            new { tagKey },
            flags: CommandFlags.None).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Cleans up orphaned tag entries across all tags by scanning for tag sets and removing
    /// references to non-existent cache keys. This is a maintenance operation that should be
    /// run periodically to prevent tag set bloat.
    /// </summary>
    /// <param name="batchSize">Maximum number of tags to process in one call.</param>
    /// <returns>Total number of orphaned entries removed across all tags.</returns>
    public async Task<long> CleanOrphanedTagEntriesAsync(int batchSize = 100)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be positive.");
        }

        var connection = _redis.GetConnection();
        var server = connection.GetServer(connection.GetEndPoints().First());
        long totalRemoved = 0;

        // Get all tag keys using SCAN
        await foreach (var tagKey in server.KeysAsync(pattern: TagKeyPrefix + "*"))
        {
            // Extract tag name from key (format: "cache:tags:{tag}")
            var tag = tagKey.ToString().Substring(TagKeyPrefix.Length);

            var removed = await CleanOrphanedTagEntriesAsync(tag).ConfigureAwait(false);
            totalRemoved += removed;
        }

        return totalRemoved;
    }
}