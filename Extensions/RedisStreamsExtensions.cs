#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Infrastructure.Cache;
using RedisCachePatterns.Services;
using StackExchange.Redis;

namespace RedisCachePatterns.Extensions;

/// <summary>
/// Configuration options for <see cref="RedisStreamCacheInvalidationService"/>.
/// </summary>
public sealed class RedisStreamOptions
{
    /// <summary>
    /// Gets or sets the Redis stream key that holds invalidation events.
    /// Defaults to <c>cache:invalidation:stream</c>.
    /// </summary>
    public string StreamKey { get; set; } = "cache:invalidation:stream";

    /// <summary>
    /// Gets or sets the consumer group name shared by all instances of this service.
    /// Each instance competes for messages, so every invalidation event is processed exactly once.
    /// Defaults to <c>cache-invalidation-group</c>.
    /// </summary>
    public string ConsumerGroup { get; set; } = "cache-invalidation-group";

    /// <summary>
    /// Gets or sets the unique consumer name for this service instance within the group.
    /// Defaults to a combination of the machine name and a random suffix to avoid collisions
    /// in horizontally-scaled deployments.
    /// </summary>
    public string ConsumerName { get; set; } = $"{Environment.MachineName}-{Guid.NewGuid():N}";

    /// <summary>
    /// Gets or sets the maximum number of stream messages to read in a single XREADGROUP call.
    /// Defaults to <c>50</c>.
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the approximate maximum number of entries the stream will retain (MAXLEN).
    /// Older entries are trimmed automatically by Redis. Defaults to <c>10 000</c>.
    /// </summary>
    public int MaxStreamLength { get; set; } = 10_000;

    /// <summary>
    /// Gets or sets how long the consumer waits before polling again when no messages are available.
    /// Defaults to <c>250 ms</c>.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Gets or sets how long the consumer waits before retrying after an unhandled error.
    /// Defaults to <c>5 s</c>.
    /// </summary>
    public TimeSpan ErrorRetryDelay { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Extension methods for working with Redis Streams.
/// </summary>
public static class RedisStreamsExtensions
{
    /// <summary>
    /// Creates a new <see cref="NameValueEntry"/> array for a Redis Stream message.
    /// </summary>
    /// <param name="eventId">The unique identifier for the event.</param>
    /// <param name="cacheKey">The cache key to invalidate, or <see langword="null"/>.</param>
    /// <param name="keyPattern">The key pattern to invalidate, or <see langword="null"/>.</param>
    /// <param name="reason">The reason for invalidation.</param>
    /// <param name="source">The source service name for tracing.</param>
    /// <returns>An array of <see cref="NameValueEntry"/> ready for <see cref="IDatabase.StreamAddAsync"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when both <paramref name="cacheKey"/> and <paramref name="keyPattern"/> are <see langword="null"/> or whitespace.</exception>
    public static NameValueEntry[] CreateStreamMessage(
        this string eventId,
        string? cacheKey = null,
        string? keyPattern = null,
        InvalidationReason reason = InvalidationReason.DataUpdate,
        string source = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);

        if (string.IsNullOrWhiteSpace(cacheKey) && string.IsNullOrWhiteSpace(keyPattern))
        {
            throw new ArgumentException("Either cacheKey or keyPattern must be provided.");
        }

        return new[]
        {
            new NameValueEntry("eventId", eventId),
            new NameValueEntry("cacheKey", cacheKey ?? string.Empty),
            new NameValueEntry("keyPattern", keyPattern ?? string.Empty),
            new NameValueEntry("reason", reason.ToString()),
            new NameValueEntry("source", source),
            new NameValueEntry("occurredAt", DateTime.UtcNow.ToString("O"))
        };
    }

    /// <summary>
    /// Parses a Redis Stream message into a dictionary of field names and values.
    /// </summary>
    /// <param name="message">The Redis Stream message to parse.</param>
    /// <returns>A dictionary containing the message fields and their values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <see langword="null"/>.</exception>
    public static Dictionary<string, string> ParseStreamMessage(this StreamEntry message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in message.Values)
        {
            result[entry.Name.ToString()] = entry.Value.ToString();
        }
        return result;
    }

    /// <summary>
    /// Attempts to extract the cache key from a Redis Stream message.
    /// </summary>
    /// <param name="message">The Redis Stream message to parse.</param>
    /// <param name="cacheKey">Receives the cache key if present.</param>
    /// <returns><see langword="true"/> if a cache key was found; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <see langword="null"/>.</exception>
    public static bool TryGetCacheKey(this StreamEntry message, out string? cacheKey)
    {
        ArgumentNullException.ThrowIfNull(message);
        cacheKey = null;

        var fields = message.ParseStreamMessage();
        if (fields.TryGetValue("cacheKey", out var value) && !string.IsNullOrWhiteSpace(value))
        {
            cacheKey = value;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to extract the key pattern from a Redis Stream message.
    /// </summary>
    /// <param name="message">The Redis Stream message to parse.</param>
    /// <param name="keyPattern">Receives the key pattern if present.</param>
    /// <returns><see langword="true"/> if a key pattern was found; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <see langword="null"/>.</exception>
    public static bool TryGetKeyPattern(this StreamEntry message, out string? keyPattern)
    {
        ArgumentNullException.ThrowIfNull(message);
        keyPattern = null;

        var fields = message.ParseStreamMessage();
        if (fields.TryGetValue("keyPattern", out var value) && !string.IsNullOrWhiteSpace(value))
        {
            keyPattern = value;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to extract the invalidation reason from a Redis Stream message.
    /// </summary>
    /// <param name="message">The Redis Stream message to parse.</param>
    /// <param name="reason">Receives the invalidation reason.</param>
    /// <returns><see langword="true"/> if a valid reason was found; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <see langword="null"/>.</exception>
    public static bool TryGetInvalidationReason(this StreamEntry message, out InvalidationReason reason)
    {
        ArgumentNullException.ThrowIfNull(message);
        reason = InvalidationReason.DataUpdate;

        var fields = message.ParseStreamMessage();
        if (fields.TryGetValue("reason", out var value) && Enum.TryParse<InvalidationReason>(value, out var parsedReason))
        {
            reason = parsedReason;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Registers <see cref="RedisStreamCacheInvalidationService"/> as both a long-running
    /// <see cref="Microsoft.Extensions.Hosting.IHostedService"/> (consumer) and an
    /// <see cref="IRedisStreamInvalidationService"/> (producer), so any service in the
    /// application can publish cross-instance invalidation events with a single injection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="configure">
    /// Optional delegate to customise <see cref="RedisStreamOptions"/>
    /// (stream key, consumer group name, batch size, etc.).
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// builder.Services.AddRedisCachePatterns();
    /// builder.Services.AddRedisStreamInvalidation(opts =>
    /// {
    ///     opts.StreamKey = "myapp:cache:events";
    ///     opts.ConsumerGroup = "myapp-cache-group";
    ///     opts.BatchSize = 100;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddRedisStreamInvalidation(
        this IServiceCollection services,
        Action<RedisStreamOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new RedisStreamOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        // Register the concrete service as a singleton so both the producer interface
        // and the hosted-service registration share the same instance.
        services.AddSingleton<RedisStreamCacheInvalidationService>(sp =>
            new RedisStreamCacheInvalidationService(
                sp.GetRequiredService<IRedisConnection>(),
                sp.GetRequiredService<ILogger<RedisStreamCacheInvalidationService>>(),
                sp.GetRequiredService<RedisStreamOptions>()));

        services.AddSingleton<IRedisStreamInvalidationService>(sp =>
            sp.GetRequiredService<RedisStreamCacheInvalidationService>());

        services.AddHostedService(sp =>
            sp.GetRequiredService<RedisStreamCacheInvalidationService>());

        return services;
    }
}