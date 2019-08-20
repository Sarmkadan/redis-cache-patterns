// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Infrastructure.Cache;
using RedisCachePatterns.Infrastructure.Repositories;
using RedisCachePatterns.Services;

namespace RedisCachePatterns.Configuration;

/// <summary>
/// Extension methods for registering Redis Cluster services into an
/// <see cref="IServiceCollection"/>.
/// <para>
/// Use <see cref="AddRedisCluster(IServiceCollection, ClusterConfiguration?)"/> for the minimal
/// cache-only setup, or <see cref="AddRedisCluster(IServiceCollection, Action{ClusterConfiguration})"/>
/// for inline configuration. Both register:
/// <list type="bullet">
///   <item><see cref="ClusterConfiguration"/> as a singleton</item>
///   <item><see cref="IRedisClusterConnection"/> → <see cref="RedisClusterConnection"/></item>
///   <item><see cref="IRedisConnection"/> forwarded to the same singleton</item>
///   <item><see cref="ICacheService"/> → <see cref="RedisClusterCacheService"/></item>
/// </list>
/// </para>
/// </summary>
public static class ClusterDependencyInjectionExtensions
{
    /// <summary>
    /// Registers all Redis Cluster cache services using the supplied
    /// <paramref name="configuration"/>.
    /// When <paramref name="configuration"/> is <c>null</c>, settings are read from environment
    /// variables via <see cref="ClusterConfiguration.FromEnvironment"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">
    /// Optional pre-built configuration. Pass <c>null</c> to auto-detect from environment.
    /// </param>
    public static IServiceCollection AddRedisCluster(
        this IServiceCollection services,
        ClusterConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var config = configuration ?? ClusterConfiguration.FromEnvironment();

        services.AddSingleton(config);

        services.AddSingleton<IRedisClusterConnection>(sp =>
            new RedisClusterConnection(
                sp.GetRequiredService<ClusterConfiguration>(),
                sp.GetRequiredService<ILogger<RedisClusterConnection>>()));

        // Also expose the cluster connection as the base IRedisConnection so that any
        // existing code depending on IRedisConnection continues to work without change.
        services.AddSingleton<IRedisConnection>(
            sp => sp.GetRequiredService<IRedisClusterConnection>());

        services.AddSingleton<ICacheService, RedisClusterCacheService>();

        return services;
    }

    /// <summary>
    /// Registers Redis Cluster services with an inline configuration delegate.
    /// The delegate receives a pre-populated <see cref="ClusterConfiguration"/> sourced from
    /// environment variables so you only need to override the values that differ.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Delegate that modifies the default configuration.</param>
    public static IServiceCollection AddRedisCluster(
        this IServiceCollection services,
        Action<ClusterConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var config = ClusterConfiguration.FromEnvironment();
        configure(config);
        return services.AddRedisCluster(config);
    }

    /// <summary>
    /// Registers Redis Cluster services together with the full application stack:
    /// repositories, domain services, and background workers — mirroring
    /// <see cref="DependencyInjectionExtensions.AddRedisCachePatterns"/> but backed by a cluster.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">
    /// Optional cluster configuration. Defaults to environment variables when omitted.
    /// </param>
    public static IServiceCollection AddRedisClusterWithFullStack(
        this IServiceCollection services,
        ClusterConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRedisCluster(configuration);

        // Repositories and domain services are transport-agnostic; they depend on
        // ICacheService and IRedisConnection — both satisfied by the cluster registration above.
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IProductRepository, ProductRepository>();
        services.AddSingleton<IOrderRepository, OrderRepository>();
        services.AddSingleton<IInventoryRepository, InventoryRepository>();

        services.AddSingleton<UserService>();
        services.AddSingleton<ProductService>();
        services.AddSingleton<OrderService>();
        services.AddSingleton<InventoryService>();

        return services;
    }

    /// <summary>
    /// Validates the Redis Cluster connection and logs the current topology at startup.
    /// Should be called after <see cref="IServiceCollection"/> is built but before the
    /// application begins serving requests.
    /// </summary>
    /// <param name="serviceProvider">The built service provider.</param>
    /// <returns>
    /// A <see cref="ClusterInfo"/> snapshot when the cluster is reachable;
    /// <c>null</c> when the connection cannot be established.
    /// </returns>
    public static async Task<ClusterInfo?> ValidateClusterConnectionAsync(
        this IServiceProvider serviceProvider)
    {
        var cluster = serviceProvider.GetRequiredService<IRedisClusterConnection>();
        var logger = serviceProvider.GetRequiredService<ILogger<RedisClusterConnection>>();

        try
        {
            var isConnected = await cluster.IsConnectedAsync();
            if (!isConnected)
            {
                logger.LogWarning("Redis Cluster could not be reached. The application will use fallback behaviour.");
                return null;
            }

            var info = await cluster.GetClusterInfoAsync();
            if (info.IsHealthy)
            {
                logger.LogInformation(
                    "Redis Cluster verified: {Masters} master(s), {Replicas} replica(s), " +
                    "{Coverage:F1}% slot coverage",
                    info.MasterCount, info.ReplicaCount, info.SlotCoverage);
            }
            else
            {
                logger.LogWarning(
                    "Redis Cluster is DEGRADED: only {Covered}/{Total} slots covered. " +
                    "Some keys may be unavailable until resharding completes.",
                    info.CoveredSlots, info.TotalSlots);
            }

            return info;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Redis Cluster validation failed");
            return null;
        }
    }
}
