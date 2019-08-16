// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Infrastructure.Cache;
using RedisCachePatterns.Services;

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
/// <see cref="IServiceCollection"/> extension methods for registering Redis Streams
/// event-driven cache invalidation.
/// </summary>
public static class RedisStreamsExtensions
{
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
    ///     opts.StreamKey     = "myapp:cache:events";
    ///     opts.ConsumerGroup = "myapp-cache-group";
    ///     opts.BatchSize     = 100;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddRedisStreamInvalidation(
        this IServiceCollection services,
        Action<RedisStreamOptions>? configure = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

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
