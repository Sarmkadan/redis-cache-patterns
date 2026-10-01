#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace RedisCachePatterns.Services;

/// <summary>
/// Handles Redis Pub/Sub messaging for cache invalidation and event distribution
/// </summary>
public class PubSubHandler : IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<PubSubHandler> _logger;
    private readonly ConcurrentDictionary<string, Action<RedisChannel, RedisValue>> _handlers = new();
    private ISubscriber? _subscriber;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PubSubHandler"/> class
    /// </summary>
    /// <param name="redis">Redis connection multiplexer</param>
    /// <param name="logger">Logger instance</param>
    public PubSubHandler(IConnectionMultiplexer redis, ILogger<PubSubHandler> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _subscriber = _redis.GetSubscriber();
    }

    /// <summary>
    /// Publishes a message to a Redis channel
    /// </summary>
    /// <param name="channel">Channel name</param>
    /// <param name="message">Message payload</param>
    /// <returns>Number of subscribers that received the message</returns>
    public async Task<long> PublishAsync(string channel, string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(channel);
        ArgumentException.ThrowIfNullOrEmpty(message);

        var sub = GetSubscriber();
        var receivers = await sub.PublishAsync(RedisChannel.Literal(channel), message);
        _logger.LogDebug("Published to {Channel}, received by {Count} subscribers", channel, receivers);
        return receivers;
    }

    /// <summary>
    /// Subscribes to a Redis channel with a message handler
    /// </summary>
    /// <param name="channel">Channel name or pattern</param>
    /// <param name="handler">Action invoked for each received message</param>
    /// <param name="isPattern">True to subscribe using a glob-style pattern</param>
    public async Task SubscribeAsync(string channel, Action<string, string> handler, bool isPattern = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(channel);
        ArgumentNullException.ThrowIfNull(handler);

        var sub = GetSubscriber();
        var redisChannel = isPattern
            ? RedisChannel.Pattern(channel)
            : RedisChannel.Literal(channel);

        Action<RedisChannel, RedisValue> wrappedHandler = (ch, val) =>
        {
            try
            {
                handler(ch!, val!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling message on channel {Channel}", ch);
            }
        };

        _handlers[channel] = wrappedHandler;
        await sub.SubscribeAsync(redisChannel, wrappedHandler);
        _logger.LogInformation("Subscribed to channel {Channel} (pattern: {IsPattern})", channel, isPattern);
    }

    /// <summary>
    /// Unsubscribes from a previously subscribed channel
    /// </summary>
    /// <param name="channel">Channel name to unsubscribe from</param>
    public async Task UnsubscribeAsync(string channel)
    {
        ArgumentException.ThrowIfNullOrEmpty(channel);

        var sub = GetSubscriber();
        await sub.UnsubscribeAsync(RedisChannel.Literal(channel));
        _handlers.TryRemove(channel, out _);
        _logger.LogInformation("Unsubscribed from channel {Channel}", channel);
    }

    /// <summary>
    /// Publishes a cache invalidation event for the specified key
    /// </summary>
    /// <param name="cacheKey">The cache key that was invalidated</param>
    /// <param name="reason">Reason for invalidation</param>
    public async Task PublishInvalidationAsync(string cacheKey, string reason = "manual")
    {
        var message = $"{{\"key\":\"{cacheKey}\",\"reason\":\"{reason}\",\"timestamp\":\"{DateTime.UtcNow:O}\"}}";
        await PublishAsync("cache:invalidation", message);
    }

    /// <summary>
    /// Returns the count of active subscriptions
    /// </summary>
    public int ActiveSubscriptionCount => _handlers.Count;

    private ISubscriber GetSubscriber()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _subscriber ?? throw new InvalidOperationException("Subscriber not initialized");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _subscriber?.UnsubscribeAll();
        _handlers.Clear();
        _subscriber = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
