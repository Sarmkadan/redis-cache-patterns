#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;

namespace RedisCachePatterns.API;

/// <summary>
/// Request model for triggering a key-level distributed cache invalidation.
/// </summary>
public sealed class InvalidateKeyRequest
{
    /// <summary>The exact Redis key to invalidate across all connected nodes.</summary>
    public string CacheKey { get; set; } = string.Empty;

    /// <summary>Why the invalidation is being triggered.</summary>
    public InvalidationReason Reason { get; set; } = InvalidationReason.DataUpdate;

    /// <summary>Name of the requesting service used for audit tracing.</summary>
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// Request model for triggering a pattern-based distributed cache invalidation.
/// </summary>
public sealed class InvalidatePatternRequest
{
    /// <summary>Glob-style pattern (e.g. <c>product:*</c>) applied on each node to remove matching keys.</summary>
    public string KeyPattern { get; set; } = string.Empty;

    /// <summary>Why the invalidation is being triggered.</summary>
    public InvalidationReason Reason { get; set; } = InvalidationReason.DataUpdate;

    /// <summary>Name of the requesting service used for audit tracing.</summary>
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// Response returned by invalidation endpoints, summarising the broadcast result.
/// </summary>
public sealed class InvalidationBroadcastResult
{
    /// <summary>Whether the broadcast completed without error.</summary>
    public bool Success { get; set; }

    /// <summary>Number of nodes that received the pub/sub notification. <c>-1</c> if unavailable.</summary>
    public long NodesNotified { get; set; }

    /// <summary>The invalidation event identifier for tracing.</summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the broadcast.</summary>
    public DateTime BroadcastAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// API endpoint for triggering distributed cache invalidation and inspecting
/// the invalidation history on this node.
/// </summary>
public sealed class DistributedInvalidationEndpoint : ApiEndpointBase
{
    private readonly IDistributedInvalidationBroadcaster _broadcaster;

    /// <param name="broadcaster">Broadcaster that delivers invalidation events to all nodes.</param>
    /// <param name="logger">Logger for operational diagnostics.</param>
    /// <param name="performanceMonitor">Shared performance monitor.</param>
    public DistributedInvalidationEndpoint(
        IDistributedInvalidationBroadcaster broadcaster,
        ILogger<DistributedInvalidationEndpoint> logger,
        PerformanceMonitor performanceMonitor)
        : base(logger, performanceMonitor)
    {
        _broadcaster = broadcaster ?? throw new ArgumentNullException(nameof(broadcaster));
    }

    /// <summary>
    /// Broadcasts an invalidation event for a single exact cache key.
    /// Returns the number of nodes notified and the event identifier.
    /// </summary>
    /// <param name="request">Invalidation request specifying the key and metadata.</param>
    public Task<ApiResponse<InvalidationBroadcastResult>> InvalidateKeyAsync(InvalidateKeyRequest request)
    {
        ValidateRequired(request, nameof(request));
        ValidateRequired(request.CacheKey, nameof(request.CacheKey));

        return ExecuteAsync(async () =>
        {
            await _broadcaster.BroadcastAsync(request.CacheKey, request.Reason, request.Source);
            var history = _broadcaster.GetHistory();
            var latest = history.FirstOrDefault();

            return new InvalidationBroadcastResult
            {
                Success       = true,
                NodesNotified = latest?.NodesNotified ?? 0,
                EventId       = latest?.EventId ?? string.Empty,
                BroadcastAt   = DateTime.UtcNow
            };
        }, $"InvalidateKey({request.CacheKey})");
    }

    /// <summary>
    /// Broadcasts an invalidation event targeting all keys matching a glob pattern.
    /// </summary>
    /// <param name="request">Invalidation request specifying the pattern and metadata.</param>
    public Task<ApiResponse<InvalidationBroadcastResult>> InvalidatePatternAsync(InvalidatePatternRequest request)
    {
        ValidateRequired(request, nameof(request));
        ValidateRequired(request.KeyPattern, nameof(request.KeyPattern));

        return ExecuteAsync(async () =>
        {
            await _broadcaster.BroadcastPatternAsync(request.KeyPattern, request.Reason, request.Source);
            var history = _broadcaster.GetHistory();
            var latest = history.FirstOrDefault();

            return new InvalidationBroadcastResult
            {
                Success       = true,
                NodesNotified = latest?.NodesNotified ?? 0,
                EventId       = latest?.EventId ?? string.Empty,
                BroadcastAt   = DateTime.UtcNow
            };
        }, $"InvalidatePattern({request.KeyPattern})");
    }

    /// <summary>
    /// Returns the invalidation history recorded on this node, newest first.
    /// </summary>
    public Task<ApiResponse<IReadOnlyList<InvalidationHistoryEntry>>> GetHistoryAsync()
    {
        return ExecuteAsync(
            () => Task.FromResult(_broadcaster.GetHistory()),
            "GetInvalidationHistory");
    }
}
