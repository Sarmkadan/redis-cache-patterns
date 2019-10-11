#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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

    /// <summary>
    /// When <c>true</c> the broadcaster also publishes events to the Redis Stream via
    /// IRedisStreamInvalidationService for reliable at-least-once delivery.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool UseStreamFallback { get; set; } = true;
}
