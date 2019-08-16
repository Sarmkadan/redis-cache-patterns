// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    public static IServiceCollection AddRedisCachePatterns(
        this IServiceCollection services,
        string redisConnectionString = "localhost:6379")
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

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
    public static IServiceCollection AddRedisCache(
        this IServiceCollection services,
        CacheConfiguration? configuration = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        var cacheConfig = configuration ?? CacheConfiguration.FromEnvironment();

        services.AddSingleton<IRedisConnection>(sp =>
            new RedisConnection(cacheConfig.ConnectionString, sp.GetRequiredService<ILogger<RedisConnection>>()));

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }

    /// <summary>
    /// Validates Redis connection on startup
    /// </summary>
    public static async Task ValidateRedisConnectionAsync(this IServiceProvider serviceProvider)
    {
        var redisConnection = serviceProvider.GetRequiredService<IRedisConnection>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

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
        }
    }
}
