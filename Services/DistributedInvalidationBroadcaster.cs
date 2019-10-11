#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Configuration;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Infrastructure.Cache;
using StackExchange.Redis;

namespace RedisCachePatterns.Services;

// ─── Supporting models ────────────────────────────────────────────────────────

/// <summary>
/// Represents a single entry in the distributed invalidation history log.
/// </summary>
public sealed class InvalidationHistoryEntry
{
    /// <summary>Globally-unique identifier for this invalidation event.</summary>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>Exact key invalidated, or <c>null</c> when a pattern was used.</summary>
    public string? CacheKey { get; init; }

    /// <summary>Glob pattern used for bulk invalidation, or <c>null</c> when an exact key was targeted.</summary>
    public string? KeyPattern { get; init; }

    /// <summary>Why the invalidation was triggered.</summary>
    public InvalidationReason Reason { get; init; }

    /// <summary>Service or component that requested the invalidation.</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>UTC timestamp when the broadcast was initiated.</summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>How many nodes acknowledged the pub/sub notification. <c>-1</c> if pub/sub was unavailable.</summary>
    public long NodesNotified { get; set; }
}


// ─── Broadcaster interface ────────────────────────────────────────────────────

/// <summary>
/// Provides broadcast-style distributed cache invalidation using Redis Pub/Sub for
/// immediate cross-node delivery, optionally backed by a Redis Stream for reliable
/// at-least-once delivery to nodes that are temporarily offline.
/// </summary>
public interface IDistributedInvalidationBroadcaster
{
    /// <summary>
    /// Broadcasts an invalidation event for a single cache key to all connected nodes
    /// and, when configured, to the reliable Redis Stream.
    /// </summary>
    /// <param name="cacheKey">The exact Redis key to invalidate across all nodes.</param>
    /// <param name="reason">Why the invalidation is occurring.</param>
    /// <param name="source">Originating service name used for audit tracing.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task BroadcastAsync(
        string cacheKey,
        InvalidationReason reason = InvalidationReason.DataUpdate,
        string source = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts an invalidation event targeting all keys matching a glob pattern.
    /// </summary>
    /// <param name="keyPattern">Glob-style pattern (e.g. <c>product:*</c>).</param>
    /// <param name="reason">Why the invalidation is occurring.</param>
    /// <param name="source">Originating service name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task BroadcastPatternAsync(
        string keyPattern,
        InvalidationReason reason = InvalidationReason.DataUpdate,
        string source = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes this node to the invalidation broadcast channel so that events
    /// published by other nodes cause immediate local cache removal.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the subscription setup.</param>
    Task SubscribeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the most recent invalidation events recorded by this node,
    /// newest first, capped at <see cref="DistributedInvalidationOptions.MaxHistorySize"/>.
    /// </summary>
    IReadOnlyList<InvalidationHistoryEntry> GetHistory();
}

// ─── Implementation ───────────────────────────────────────────────────────────

/// <summary>
/// Default implementation of <see cref="IDistributedInvalidationBroadcaster"/>.
///
/// <para>
/// <b>Delivery model:</b>
/// <list type="bullet">
///   <item>
///     <term>Pub/Sub (fire-and-forget)</term>
///     <description>
///       Every broadcast publishes a JSON message on a Redis channel. All subscribed
///       nodes receive it immediately and remove the affected key(s) from their local
///       Redis connection. Suitable for low-latency invalidation when brief inconsistency
///       on a restarting node is acceptable.
///     </description>
///   </item>
///   <item>
///     <term>Stream (reliable fallback, optional)</term>
///     <description>
///       When <see cref="DistributedInvalidationOptions.UseStreamFallback"/> is <c>true</c>
///       the event is also written to the Redis Stream consumed by
///       <see cref="RedisStreamCacheInvalidationService"/>. Nodes that were offline during
///       the pub/sub broadcast will process the event from the stream on reconnect.
///     </description>
///   </item>
/// </list>
/// </para>
/// </summary>
public sealed class DistributedInvalidationBroadcaster : IDistributedInvalidationBroadcaster
{
    private readonly IRedisConnection _redisConnection;
    private readonly ICacheService _cacheService;
    private readonly IRedisStreamInvalidationService? _streamService;
    private readonly ILogger<DistributedInvalidationBroadcaster> _logger;
    private readonly DistributedInvalidationOptions _options;

    // Thread-safe bounded history log: items are prepended and trimmed when MaxHistorySize is reached.
    private readonly ConcurrentQueue<InvalidationHistoryEntry> _history = new();

    /// <param name="redisConnection">Active Redis connection used for pub/sub.</param>
    /// <param name="cacheService">Local cache service to remove invalidated keys on the receiving side.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="options">Broadcaster configuration. Defaults applied when <c>null</c>.</param>
    /// <param name="streamService">
    /// Optional stream service for reliable fallback delivery.
    /// When <c>null</c> stream publishing is skipped even if
    /// <see cref="DistributedInvalidationOptions.UseStreamFallback"/> is <c>true</c>.
    /// </param>
    public DistributedInvalidationBroadcaster(
        IRedisConnection redisConnection,
        ICacheService cacheService,
        ILogger<DistributedInvalidationBroadcaster> logger,
        DistributedInvalidationOptions? options = null,
        IRedisStreamInvalidationService? streamService = null)
    {
        _redisConnection = redisConnection ?? throw new ArgumentNullException(nameof(redisConnection));
        _cacheService    = cacheService    ?? throw new ArgumentNullException(nameof(cacheService));
        _logger          = logger          ?? throw new ArgumentNullException(nameof(logger));
        _options         = options         ?? new DistributedInvalidationOptions();
        _streamService   = streamService;
    }

    // ─── IDistributedInvalidationBroadcaster ─────────────────────────────────

    /// <inheritdoc/>
    public Task BroadcastAsync(
        string cacheKey,
        InvalidationReason reason = InvalidationReason.DataUpdate,
        string source = "",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            throw new ArgumentException("Cache key must not be empty.", nameof(cacheKey));

        return BroadcastCoreAsync(
            new CacheInvalidationEvent { CacheKey = cacheKey, Reason = reason, Source = source },
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task BroadcastPatternAsync(
        string keyPattern,
        InvalidationReason reason = InvalidationReason.DataUpdate,
        string source = "",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyPattern))
            throw new ArgumentException("Key pattern must not be empty.", nameof(keyPattern));

        return BroadcastCoreAsync(
            new CacheInvalidationEvent { KeyPattern = keyPattern, Reason = reason, Source = source },
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SubscribeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriber = _redisConnection.GetConnection().GetSubscriber();
            await subscriber.SubscribeAsync(
                RedisChannel.Literal(_options.PubSubChannel),
                OnMessageReceivedAsync);

            _logger.LogInformation(
                "Subscribed to distributed invalidation channel: {Channel}",
                _options.PubSubChannel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to invalidation channel: {Channel}", _options.PubSubChannel);
            throw;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<InvalidationHistoryEntry> GetHistory() => _history.ToList();

    // ─── Private helpers ─────────────────────────────────────────────────────

    private async Task BroadcastCoreAsync(CacheInvalidationEvent evt, CancellationToken cancellationToken)
    {
        var historyEntry = new InvalidationHistoryEntry
        {
            EventId    = evt.EventId,
            CacheKey   = evt.CacheKey,
            KeyPattern = evt.KeyPattern,
            Reason     = evt.Reason,
            Source     = evt.Source
        };

        try
        {
            // 1. Pub/Sub: immediate broadcast to all subscribed nodes.
            var payload    = JsonSerializer.Serialize(evt);
            var subscriber = _redisConnection.GetConnection().GetSubscriber();
            var notified   = await subscriber.PublishAsync(
                RedisChannel.Literal(_options.PubSubChannel),
                payload);

            historyEntry.NodesNotified = notified;

            _logger.LogInformation(
                "Broadcast invalidation event {EventId} | Key={Key} Pattern={Pattern} Nodes={Nodes}",
                evt.EventId, evt.CacheKey, evt.KeyPattern, notified);

            // 2. Stream fallback: reliable delivery to nodes that were offline.
            if (_options.UseStreamFallback && _streamService is not null)
            {
                await _streamService.PublishAsync(evt, cancellationToken);
                _logger.LogDebug("Stream fallback published for event {EventId}", evt.EventId);
            }
        }
        catch (Exception ex)
        {
            historyEntry.NodesNotified = -1;
            _logger.LogError(ex, "Failed to broadcast invalidation event {EventId}", evt.EventId);
            throw;
        }
        finally
        {
            AppendHistory(historyEntry);
        }
    }

    private async void OnMessageReceivedAsync(RedisChannel channel, RedisValue message)
    {
        try
        {
            if (message.IsNullOrEmpty) return;

            var evt = JsonSerializer.Deserialize<CacheInvalidationEvent>(message.ToString());
            if (evt is null) return;

            if (!string.IsNullOrWhiteSpace(evt.CacheKey))
            {
                await _cacheService.RemoveAsync(evt.CacheKey);
                _logger.LogDebug(
                    "Local cache invalidated via pub/sub: Key={Key} EventId={EventId}",
                    evt.CacheKey, evt.EventId);
            }
            else if (!string.IsNullOrWhiteSpace(evt.KeyPattern))
            {
                await _cacheService.RemoveByPatternAsync(evt.KeyPattern);
                _logger.LogDebug(
                    "Local cache invalidated via pub/sub: Pattern={Pattern} EventId={EventId}",
                    evt.KeyPattern, evt.EventId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing invalidation message on channel {Channel}", channel);
        }
    }

    private void AppendHistory(InvalidationHistoryEntry entry)
    {
        _history.Enqueue(entry);

        // Trim to MaxHistorySize — dequeue oldest entries.
        while (_history.Count > _options.MaxHistorySize)
            _history.TryDequeue(out _);
    }
}
