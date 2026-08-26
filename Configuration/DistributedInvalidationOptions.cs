#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace RedisCachePatterns.Configuration;

/// <summary>
/// Options that control the behaviour of DistributedInvalidationBroadcaster.
/// </summary>
public sealed class DistributedInvalidationOptions
{
    /// <summary>
    /// Redis Pub/Sub channel name used for immediate cross-node notifications.
    /// Defaults to <c>cache:invalidation:broadcast</c>.
    /// </summary>
    public string PubSubChannel { get; set; } = "cache:invalidation:broadcast";

    /// <summary>Maximum number of history entries retained in memory. Oldest entries are dropped first.</summary>
    public int MaxHistorySize { get; set; } = 500;

    /// <summary>Maximum allowed length for a cache key in bytes. Prevents oversized keys from causing memory issues.</summary>
    public int MaxKeyLength { get; set; } = 1024; // 1KB

    /// <summary>Maximum allowed length for a key pattern in bytes. Prevents oversized patterns from causing memory issues.</summary>
    public int MaxKeyPatternLength { get; set; } = 1024; // 1KB

    /// <summary>Maximum number of keys that can be invalidated via a single pattern. Prevents DoS via bulk invalidation.</summary>
    public int MaxPatternInvalidationBatchSize { get; set; } = 1000; // Maximum keys to remove when using a pattern

    ///
    /// When <c>true</c> the broadcaster also publishes events to the Redis Stream via
    /// IRedisStreamInvalidationService for reliable at-least-once delivery.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool UseStreamFallback { get; set; } = true;

    public override string ToString()
    {
        return $"DistributedInvalidationOptions {{ PubSubChannel = {PubSubChannel}, MaxHistorySize = {MaxHistorySize}, UseStreamFallback = {UseStreamFallback} }}";
    }
}
