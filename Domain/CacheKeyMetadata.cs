#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Domain;

/// <summary>
/// Per-key cache usage statistics stored alongside cached values.
/// Enables identifying hot keys, cold keys, and access patterns at the
/// individual entry level — complementing the aggregate <see cref="CacheStatistics"/>.
/// </summary>
public class CacheKeyMetadata
{
    /// <summary>The cache key this metadata belongs to.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Total number of cache hits recorded for this key.</summary>
    public long HitCount { get; set; }

    /// <summary>UTC timestamp of the most recent cache hit, or null if never accessed.</summary>
    public DateTime? LastAccessed { get; set; }

    /// <summary>UTC timestamp when the entry was first written to cache.</summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>Size of the serialized (and possibly compressed) cached value in bytes.</summary>
    public long SizeBytes { get; set; }
}
