#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Services;

/// <summary>
/// Tag-based grouping and invalidation of cache keys. Consumers depend on this abstraction
/// so tag operations can be substituted in tests and decorated without touching the Redis implementation.
/// </summary>
public interface ICacheTagService
{
    /// <summary>Writes the value via <see cref="ICacheService"/> and adds the key to every tag set.</summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key to write.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="tags">Tags the key belongs to.</param>
    /// <param name="expiration">Optional expiration for the cached entry.</param>
    Task SetWithTagsAsync<T>(string key, T value, IEnumerable<string> tags, TimeSpan? expiration = null);

    /// <summary>Adds an existing cache key to a tag set without rewriting the value.</summary>
    /// <param name="key">The cache key to tag.</param>
    /// <param name="tag">The tag name.</param>
    Task TagKeyAsync(string key, string tag);

    /// <summary>Removes a key from a tag set. Returns true if the key was a member.</summary>
    /// <param name="key">The cache key to untag.</param>
    /// <param name="tag">The tag name.</param>
    Task<bool> UntagKeyAsync(string key, string tag);

    /// <summary>Returns all cache keys currently associated with the tag.</summary>
    /// <param name="tag">The tag name.</param>
    Task<IReadOnlyList<string>> GetKeysByTagAsync(string tag);

    /// <summary>Removes every key in the tag set from the cache and returns the number of keys invalidated.</summary>
    /// <param name="tag">The tag name.</param>
    Task<int> InvalidateTagAsync(string tag);

    /// <summary>Invalidates multiple tags; returns the total number of keys invalidated across all of them.</summary>
    /// <param name="tags">Collection of tags to invalidate.</param>
    Task<int> InvalidateTagsAsync(IEnumerable<string> tags);

    /// <summary>Removes tag set members that reference non-existent cache keys for one tag.</summary>
    /// <param name="tag">The tag to clean up.</param>
    /// <returns>The number of orphaned entries removed.</returns>
    Task<int> CleanOrphanedTagEntriesAsync(string tag);

    /// <summary>Removes orphaned tag set members across all tags.</summary>
    /// <param name="batchSize">Maximum number of tags to process in one call.</param>
    /// <returns>Total number of orphaned entries removed across all tags.</returns>
    Task<long> CleanOrphanedTagEntriesAsync(int batchSize = 100);
}
