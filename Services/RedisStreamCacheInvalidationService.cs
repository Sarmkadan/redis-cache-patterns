// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Extensions;
using RedisCachePatterns.Infrastructure.Cache;
using StackExchange.Redis;

namespace RedisCachePatterns.Services;

/// <summary>
/// Defines the producer side of the Redis Streams cache invalidation pipeline.
/// Implementations publish <see cref="CacheInvalidationEvent"/> records to a Redis Stream
/// so that all service instances in the cluster can react and remove stale entries.
/// </summary>
public interface IRedisStreamInvalidationService
{
    /// <summary>Publishes a pre-built <see cref="CacheInvalidationEvent"/> to the stream.</summary>
    /// <param name="invalidationEvent">The event to publish. Must have either <c>CacheKey</c> or <c>KeyPattern</c> set.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task PublishAsync(CacheInvalidationEvent invalidationEvent, CancellationToken cancellationToken = default);

    /// <summary>Publishes an invalidation event targeting a single, exact cache key.</summary>
    /// <param name="cacheKey">The Redis key to remove.</param>
    /// <param name="reason">Why the invalidation is occurring.</param>
    /// <param name="source">Originating service name, used for audit/tracing.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task PublishAsync(string cacheKey, InvalidationReason reason = InvalidationReason.DataUpdate, string source = "", CancellationToken cancellationToken = default);

    /// <summary>Publishes an invalidation event targeting all keys matching a glob pattern.</summary>
    /// <param name="keyPattern">Glob-style pattern (e.g. <c>product:*</c>) passed to Redis SCAN.</param>
    /// <param name="reason">Why the invalidation is occurring.</param>
    /// <param name="source">Originating service name, used for audit/tracing.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task PublishPatternAsync(string keyPattern, InvalidationReason reason = InvalidationReason.DataUpdate, string source = "", CancellationToken cancellationToken = default);
}

/// <summary>
/// Listens to a Redis Stream and invalidates cache entries in response to published events.
/// Runs as a <see cref="BackgroundService"/> (consumer) and also exposes
/// <see cref="IRedisStreamInvalidationService"/> (producer) so any service can trigger
/// cross-instance invalidation with a single call.
/// </summary>
public sealed class RedisStreamCacheInvalidationService : BackgroundService, IRedisStreamInvalidationService
{
    private readonly IRedisConnection _redisConnection;
    private readonly ILogger<RedisStreamCacheInvalidationService> _logger;
    private readonly RedisStreamOptions _options;

    private const string FieldEventId    = "eventId";
    private const string FieldCacheKey   = "cacheKey";
    private const string FieldKeyPattern = "keyPattern";
    private const string FieldReason     = "reason";
    private const string FieldSource     = "source";
    private const string FieldOccurredAt = "occurredAt";

    /// <summary>
    /// Initialises a new instance of <see cref="RedisStreamCacheInvalidationService"/>.
    /// </summary>
    /// <param name="redisConnection">Active Redis connection.</param>
    /// <param name="logger">Logger for structured diagnostics.</param>
    /// <param name="options">Stream configuration (key name, group, batch size, …).</param>
    public RedisStreamCacheInvalidationService(
        IRedisConnection redisConnection,
        ILogger<RedisStreamCacheInvalidationService> logger,
        RedisStreamOptions options)
    {
        _redisConnection = redisConnection ?? throw new ArgumentNullException(nameof(redisConnection));
        _logger          = logger          ?? throw new ArgumentNullException(nameof(logger));
        _options         = options         ?? new RedisStreamOptions();
    }

    // ─── Producer ────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task PublishAsync(CacheInvalidationEvent invalidationEvent, CancellationToken cancellationToken = default)
    {
        if (invalidationEvent == null) throw new ArgumentNullException(nameof(invalidationEvent));

        try
        {
            var db      = _redisConnection.GetDatabase();
            var entries = BuildEntries(invalidationEvent.EventId, invalidationEvent.CacheKey, invalidationEvent.KeyPattern, invalidationEvent.Reason, invalidationEvent.Source);
            await db.StreamAddAsync(_options.StreamKey, entries, maxLength: _options.MaxStreamLength);
            _logger.LogDebug("Published invalidation event {EventId} | Key: {Key} | Pattern: {Pattern}", invalidationEvent.EventId, invalidationEvent.CacheKey, invalidationEvent.KeyPattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish invalidation event {EventId}", invalidationEvent.EventId);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task PublishAsync(string cacheKey, InvalidationReason reason = InvalidationReason.DataUpdate, string source = "", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cacheKey)) throw new ArgumentException("Cache key must not be empty.", nameof(cacheKey));
        return PublishAsync(new CacheInvalidationEvent { CacheKey = cacheKey, Reason = reason, Source = source }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task PublishPatternAsync(string keyPattern, InvalidationReason reason = InvalidationReason.DataUpdate, string source = "", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyPattern)) throw new ArgumentException("Key pattern must not be empty.", nameof(keyPattern));
        return PublishAsync(new CacheInvalidationEvent { KeyPattern = keyPattern, Reason = reason, Source = source }, cancellationToken);
    }

    // ─── Consumer (BackgroundService) ────────────────────────────────────────

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureConsumerGroupAsync();
        _logger.LogInformation("Stream invalidation consumer started. Stream: {Stream} | Group: {Group} | Consumer: {Consumer}", _options.StreamKey, _options.ConsumerGroup, _options.ConsumerName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await ReadMessagesAsync();
                if (messages.Length > 0)
                    await ProcessBatchAsync(messages, stoppingToken);
                else
                    await Task.Delay(_options.PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in stream consumer loop");
                await Task.Delay(_options.ErrorRetryDelay, stoppingToken);
            }
        }

        _logger.LogInformation("Stream invalidation consumer stopped.");
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    private async Task EnsureConsumerGroupAsync()
    {
        try
        {
            var db = _redisConnection.GetDatabase();
            await db.StreamCreateConsumerGroupAsync(_options.StreamKey, _options.ConsumerGroup, StreamPosition.NewMessages, createStream: true);
            _logger.LogInformation("Consumer group '{Group}' ready on stream '{Stream}'", _options.ConsumerGroup, _options.StreamKey);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Group already exists — normal on restart, nothing to do.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create consumer group '{Group}' on stream '{Stream}'", _options.ConsumerGroup, _options.StreamKey);
            throw;
        }
    }

    private async Task<StreamEntry[]> ReadMessagesAsync()
    {
        var db = _redisConnection.GetDatabase();
        return await db.StreamReadGroupAsync(
            _options.StreamKey,
            _options.ConsumerGroup,
            _options.ConsumerName,
            StreamPosition.NewMessages,
            count: _options.BatchSize);
    }

    private async Task ProcessBatchAsync(StreamEntry[] messages, CancellationToken cancellationToken)
    {
        var db    = _redisConnection.GetDatabase();
        var toAck = new List<RedisValue>(messages.Length);

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                await DispatchMessageAsync(message, cancellationToken);
                toAck.Add(message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process stream message {MessageId} — will not acknowledge", message.Id);
            }
        }

        if (toAck.Count > 0)
            await db.StreamAcknowledgeAsync(_options.StreamKey, _options.ConsumerGroup, toAck.ToArray());
    }

    private async Task DispatchMessageAsync(StreamEntry message, CancellationToken cancellationToken)
    {
        var fields = message.Values.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());

        fields.TryGetValue(FieldCacheKey,   out var cacheKey);
        fields.TryGetValue(FieldKeyPattern, out var keyPattern);
        fields.TryGetValue(FieldReason,     out var reasonStr);
        fields.TryGetValue(FieldSource,     out var source);

        var reason = Enum.TryParse<InvalidationReason>(reasonStr, out var r) ? r : InvalidationReason.DataUpdate;

        if (!string.IsNullOrWhiteSpace(cacheKey))
        {
            await _redisConnection.GetDatabase().KeyDeleteAsync(new RedisKey(cacheKey!));
            _logger.LogInformation("Invalidated key: {Key} | Reason: {Reason} | Source: {Source}", cacheKey, reason, source);
            return;
        }

        if (!string.IsNullOrWhiteSpace(keyPattern))
        {
            var deleted = await InvalidateByPatternAsync(keyPattern!, cancellationToken);
            _logger.LogInformation("Invalidated {Count} key(s) matching pattern: {Pattern} | Reason: {Reason} | Source: {Source}", deleted, keyPattern, reason, source);
        }
    }

    private async Task<int> InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken)
    {
        var connection = _redisConnection.GetConnection();
        var deleted    = 0;

        foreach (var endpoint in connection.GetEndPoints())
        {
            var server = connection.GetServer(endpoint);
            if (server.IsReplica) continue;

            var keys = server.Keys(pattern: pattern).ToArray();
            if (keys.Length == 0) continue;

            deleted += (int)await _redisConnection.GetDatabase().KeyDeleteAsync(keys);
        }

        return deleted;
    }

    private static NameValueEntry[] BuildEntries(string eventId, string? cacheKey, string? keyPattern, InvalidationReason reason, string source) =>
        new[]
        {
            new NameValueEntry(FieldEventId,    eventId),
            new NameValueEntry(FieldCacheKey,   cacheKey    ?? string.Empty),
            new NameValueEntry(FieldKeyPattern, keyPattern  ?? string.Empty),
            new NameValueEntry(FieldReason,     reason.ToString()),
            new NameValueEntry(FieldSource,     source),
            new NameValueEntry(FieldOccurredAt, DateTime.UtcNow.ToString("O"))
        };
}
