#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Infrastructure.Cache;
using RedisCachePatterns.Infrastructure.Repositories;
using RedisCachePatterns.Services;

namespace RedisCachePatterns.Configuration;

/// <summary>
/// Extension methods for setting up dependency injection
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers all Redis caching and repository services
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="redisConnectionString">Redis connection string. Defaults to "localhost:6379".</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddRedisCachePatterns(
        this IServiceCollection services,
        string redisConnectionString = "localhost:6379")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(redisConnectionString, nameof(redisConnectionString));

        // Register Redis connection
        services.AddSingleton<IRedisConnection>(sp =>
            new RedisConnection(redisConnectionString, sp.GetRequiredService<ILogger<RedisConnection>>()));

        // Register cache service
        services.AddSingleton<ICacheService, RedisCacheService>();

        // Register repositories
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IProductRepository, ProductRepository>();
        services.AddSingleton<IOrderRepository, OrderRepository>();
        services.AddSingleton<IInventoryRepository, InventoryRepository>();

        // Register business services
        services.AddSingleton<UserService>();
        services.AddSingleton<ProductService>();
        services.AddSingleton<OrderService>();
        services.AddSingleton<InventoryService>();

        return services;
    }

    /// <summary>
    /// Registers only the cache service without repositories (for consumption of caching patterns)
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">Optional cache configuration. If null, reads from environment variables.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddRedisCache(
        this IServiceCollection services,
        CacheConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var cacheConfig = configuration ?? CacheConfiguration.FromEnvironment();

        services.AddSingleton<IRedisConnection>(sp =>
            new RedisConnection(cacheConfig.ConnectionString, sp.GetRequiredService<ILogger<RedisConnection>>()));

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }

    /// <summary>
    /// Validates Redis connection on startup
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to resolve services from.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/> is null.</exception>
    public static async Task ValidateRedisConnectionAsync(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var redisConnection = serviceProvider.GetRequiredService<IRedisConnection>();
        var logger = serviceProvider.GetRequiredService<ILogger>();

        try
        {
            var isConnected = await redisConnection.IsConnectedAsync();
            if (isConnected)
            {
                logger.LogInformation("Redis connection verified successfully");
            }
            else
            {
                logger.LogWarning("Redis connection could not be verified. Using fallback behavior.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate Redis connection");
            throw; // Re-throw to surface connection failures
        }
    }
}