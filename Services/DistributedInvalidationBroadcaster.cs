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
    /// <exception cref="ArgumentException">Thrown when <paramref name="cacheKey"/> is null or empty.</exception>
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
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="keyPattern"/> is null or empty.</exception>
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

/// <summary>
/// Default implementation of <see cref="IDistributedInvalidationBroadcaster"/>.
/// </summary>
public sealed class DistributedInvalidationBroadcaster : IDistributedInvalidationBroadcaster
{
    private readonly IRedisConnection _redisConnection;
    private readonly ICacheService _cacheService;
    private readonly IRedisStreamInvalidationService? _streamService;
    private readonly ILogger<DistributedInvalidationBroadcaster> _logger;
    private readonly DistributedInvalidationOptions _options;

    // Unique identifier for this instance to avoid self‑invalidation.
    private readonly string _instanceId = Guid.NewGuid().ToString();

    // Generation/epoch counter to detect missed invalidations during connection drops.
    // When connection is restored, this is incremented to force local cache clearance.
    private long _currentGeneration = 0;

    // Thread-safe bounded history log: items are prepended and trimmed when MaxHistorySize is reached.
    private readonly ConcurrentQueue<InvalidationHistoryEntry> _history = new();

    /// <summary>
    /// Initializes a new instance of <see cref="DistributedInvalidationBroadcaster"/>.
    /// </summary>
    /// <param name="redisConnection">Active Redis connection used for pub/sub.</param>
    /// <param name="cacheService">Local cache service to remove invalidated keys on the receiving side.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="options">Broadcaster configuration. Defaults applied when <c>null</c>.</param>
    /// <param name="streamService">
    /// Optional stream service for reliable fallback delivery.
    /// When <c>null</c> stream publishing is skipped even if
    /// <see cref="DistributedInvalidationOptions.UseStreamFallback"/> is <c>true</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when any required argument is null.</exception>
    public DistributedInvalidationBroadcaster(
        IRedisConnection redisConnection,
        ICacheService cacheService,
        ILogger<DistributedInvalidationBroadcaster> logger,
        DistributedInvalidationOptions? options = null,
        IRedisStreamInvalidationService? streamService = null)
    {
        ArgumentNullException.ThrowIfNull(redisConnection);
        ArgumentNullException.ThrowIfNull(cacheService);
        ArgumentNullException.ThrowIfNull(logger);

        _redisConnection = redisConnection;
        _cacheService = cacheService;
        _logger = logger;
        _options = options ?? new DistributedInvalidationOptions();
        _streamService = streamService;

        // Wire up connection restored event to handle reconnect gaps
        var connection = _redisConnection.GetConnection();
        connection.ConnectionRestored += OnConnectionRestored;
    }

    // ─── IDistributedInvalidationBroadcaster ─────────────────────────────────

    /// <inheritdoc/>
    public Task BroadcastAsync(
        string cacheKey,
        InvalidationReason reason = InvalidationReason.DataUpdate,
        string source = "",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(cacheKey, nameof(cacheKey));

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
        ArgumentException.ThrowIfNullOrEmpty(keyPattern, nameof(keyPattern));

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

    // ─── Connection event handlers ────────────────────────────────────────────────

    /// <summary>
    /// Handles Redis connection restored events.
    /// When connection is re-established after a drop, we need to clear local cache
    /// to prevent serving stale data that was cached during the downtime.
    /// </summary>
    private void OnConnectionRestored(object? sender, EventArgs e)
    {
        try
        {
            // Increment generation to signal that we may have missed invalidations
            Interlocked.Increment(ref _currentGeneration);

            _logger.LogWarning(
                "Redis connection restored. Incremented generation to {Generation} to clear potential stale cache",
                _currentGeneration);

            // Clear local cache to prevent serving stale data
            // This ensures we don't serve cached data that was cached during the connection drop
            _cacheService.FlushAsync().GetAwaiter().GetResult();

            _logger.LogInformation(
                "Local cache cleared after Redis connection restored (generation {Generation})",
                _currentGeneration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cache after Redis connection restored");
        }
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    private async Task BroadcastCoreAsync(CacheInvalidationEvent evt, CancellationToken cancellationToken)
    {
        var historyEntry = new InvalidationHistoryEntry
        {
            EventId = evt.EventId,
            CacheKey = evt.CacheKey,
            KeyPattern = evt.KeyPattern,
            Reason = evt.Reason,
            Source = evt.Source
        };

        // Wrap the event together with the originating instance identifier.
        var wrapper = new InvalidationMessage
        {
            Event = evt,
            OriginNodeId = _instanceId,
            Generation = _currentGeneration
        };

        try
        {
            // 1. Pub/Sub: immediate broadcast to all subscribed nodes.
            var payload = JsonSerializer.Serialize(wrapper);
            var subscriber = _redisConnection.GetConnection().GetSubscriber();
            var notified = await subscriber.PublishAsync(
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

            var wrapper = JsonSerializer.Deserialize<InvalidationMessage>(message.ToString());
            if (wrapper is null) return;

            // Skip messages that originated from this instance.
            if (wrapper.OriginNodeId == _instanceId) return;

            var evt = wrapper.Event;
            if (evt is null) return;

            // Check if this message is from an older generation (connection drop occurred)
            // If so, we should clear local cache to prevent stale data
            if (wrapper.Generation < _currentGeneration)
            {
                _logger.LogWarning(
                    "Received message from older generation {MessageGeneration} vs current {CurrentGeneration}. Clearing local cache.",
                    wrapper.Generation,
                    _currentGeneration);

                await _cacheService.FlushAsync();
                return; // Don't process the old message after clearing cache
            }

            if (!string.IsNullOrWhiteSpace(evt.CacheKey))
            {
                await _cacheService.RemoveAsync(evt.CacheKey);
                _logger.LogDebug(
                    "Local cache invalidated via pub/sub: Key={Key} EventId={EventId}",
                    evt.CacheKey,
                    evt.EventId);
            }
            else if (!string.IsNullOrWhiteSpace(evt.KeyPattern))
            {
                await _cacheService.RemoveByPatternAsync(evt.KeyPattern);
                _logger.LogDebug(
                    "Local cache invalidated via pub/sub: Pattern={Pattern} EventId={EventId}",
                    evt.KeyPattern,
                    evt.EventId);
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

    // ─── Message wrapper used to carry the originating node identifier ───────

    private sealed class InvalidationMessage
    {
        /// <summary>The actual invalidation event.</summary>
        public CacheInvalidationEvent Event { get; set; } = null!;

        /// <summary>Identifier of the node that originated the broadcast.</summary>
        public string OriginNodeId { get; set; } = string.Empty;

        /// <summary>
        /// Generation/epoch counter to detect messages from before a connection drop.
        /// When a connection is restored, this generation is incremented.
        /// </summary>
        public long Generation { get; set; }
    }
}