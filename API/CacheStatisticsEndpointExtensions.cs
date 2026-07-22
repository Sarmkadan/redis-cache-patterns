#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Monitoring;
using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;

namespace RedisCachePatterns.API;

/// <summary>
/// Extension methods for registering and configuring <see cref="CacheStatisticsEndpoint"/> with the DI container.
/// </summary>
public static class CacheStatisticsEndpointExtensions
{
    /// <summary>
    /// Adds the <see cref="CacheStatisticsEndpoint"/> and its dependencies to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddCacheStatisticsEndpoint(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Register the singleton statistics aggregator
        services.AddSingleton<CacheStatisticsAggregator>();

        // Register the endpoint
        services.AddSingleton<CacheStatisticsEndpoint>();

        return services;
    }

    /// <summary>
    /// Adds the <see cref="CacheStatisticsEndpoint"/> and its dependencies to the service collection
    /// with a specific <see cref="CacheStatisticsAggregator"/> instance.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="statsAggregator">The pre-configured statistics aggregator instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddCacheStatisticsEndpoint(
        this IServiceCollection services,
        CacheStatisticsAggregator statsAggregator)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (statsAggregator == null)
            throw new ArgumentNullException(nameof(statsAggregator));

        // Register the endpoint with the provided aggregator
        services.AddSingleton<CacheStatisticsEndpoint>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<CacheStatisticsEndpoint>>();
            var perfMonitor = provider.GetRequiredService<PerformanceMonitor>();
            return new CacheStatisticsEndpoint(statsAggregator, logger, perfMonitor);
        });

        return services;
    }

    /// <summary>
    /// Adds the <see cref="CacheStatisticsEndpoint"/> and its dependencies to the service collection
    /// with the specified cache services to aggregate statistics from.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="cacheServices">Collection of cache services to aggregate statistics from.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddCacheStatisticsEndpoint(
        this IServiceCollection services,
        IEnumerable<ICacheService> cacheServices)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Create a shared aggregator
        var aggregator = CacheStatisticsAggregator.Instance;

        // Register the endpoint
        services.AddSingleton<CacheStatisticsEndpoint>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<CacheStatisticsEndpoint>>();
            var perfMonitor = provider.GetRequiredService<PerformanceMonitor>();
            return new CacheStatisticsEndpoint(aggregator, logger, perfMonitor);
        });

        return services;
    }
}
