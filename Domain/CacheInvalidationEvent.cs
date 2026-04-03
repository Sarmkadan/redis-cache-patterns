#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace RedisCachePatterns.Domain;

/// <summary>
/// Represents a cache invalidation event that is published to or consumed from a Redis Stream.
/// Either <see cref="CacheKey"/> or <see cref="KeyPattern"/> must be set — not both.
/// </summary>
public sealed class CacheInvalidationEvent
{
    /// <summary>Gets or sets the globally-unique identifier for this event.</summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the exact Redis key to invalidate.
    /// Mutually exclusive with <see cref="KeyPattern"/>.
    /// </summary>
    public string? CacheKey { get; set; }

    /// <summary>
    /// Gets or sets a glob-style pattern used to invalidate multiple matching keys.
    /// Mutually exclusive with <see cref="CacheKey"/>.
    /// </summary>
    public string? KeyPattern { get; set; }

    /// <summary>Gets or sets the reason this invalidation was triggered.</summary>
    public InvalidationReason Reason { get; set; } = InvalidationReason.DataUpdate;

    /// <summary>Gets or sets the UTC timestamp when the invalidation was requested.</summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the name of the service or component that originated this event.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets optional key-value metadata attached to this event.</summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>Describes why a cache invalidation event was triggered.</summary>
public enum InvalidationReason
{
    /// <summary>The underlying data was modified.</summary>
    DataUpdate = 0,

    /// <summary>The underlying data was removed.</summary>
    DataDelete = 1,

    /// <summary>An operator explicitly requested a cache purge.</summary>
    ManualPurge = 2,

    /// <summary>A value that the cached entry depends on changed.</summary>
    DependencyChange = 3,

    /// <summary>A configuration change rendered the cached entry stale.</summary>
    ConfigurationChange = 4
}
