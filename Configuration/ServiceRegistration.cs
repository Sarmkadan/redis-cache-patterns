// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.BackgroundWorkers;
using RedisCachePatterns.Events;
using RedisCachePatterns.Formatters;
using RedisCachePatterns.Infrastructure.Cache;
using RedisCachePatterns.Integration;
using RedisCachePatterns.Monitoring;
using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;

namespace RedisCachePatterns.Configuration;

/// <summary>
/// Extension methods for registering Redis cache patterns services in dependency injection
/// Provides convenient setup for full feature set with sensible defaults
/// </summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddRedisCachePatterns(
        this IServiceCollection services,
        string redisConnectionString,
        Action<CacheConfigurationBuilder>? configureCache = null)
    {
        // Build cache configuration
        var configBuilder = new CacheConfigurationBuilder();
        configureCache?.Invoke(configBuilder);
        var config = configBuilder.Build();

        // Register core cache services
        services.AddSingleton<IRedisConnection>(sp =>
            new RedisConnection(redisConnectionString, sp.GetRequiredService<ILogger<RedisConnection>>()));

        services.AddSingleton<ICacheService, RedisCacheService>();

        // Register cache wrapping services if enabled
        if (config.CompressionEnabled)
        {
            services.Decorate<ICacheService>((inner, sp) =>
                new CompressedCacheService(inner, sp.GetRequiredService<ILogger<CompressedCacheService>>(), config.CompressionThresholdBytes));
        }

        // Register cache invalidation and warming services
        services.AddSingleton<CacheInvalidationService>();
        services.AddSingleton<CacheWarmingService>();

        // Register monitoring services
        if (config.MonitoringEnabled)
        {
            services.AddSingleton<CacheMetricsCollector>();
            services.AddSingleton<HealthCheckService>();
            services.AddSingleton<DiagnosticsProvider>();
        }

        // Register utilities
        services.AddSingleton<PerformanceMonitor>();

        // Register event system
        services.AddSingleton<IEventPublisher, EventPublisher>();
        services.AddSingleton<CacheEventListener>();
        services.AddSingleton<OrderEventHandler>();

        // Register formatters
        services.AddSingleton<FormatterRegistry>(sp =>
        {
            var registry = new FormatterRegistry();
            registry.RegisterFormatter("json", new JsonFormatter());
            registry.RegisterFormatter("csv", new CsvFormatter());
            registry.RegisterFormatter("xml", new XmlFormatter());
            return registry;
        });

        // Register HTTP integration
        services.AddSingleton<HttpClientFactory>();
        services.AddScoped<ExternalApiClient>();
        services.AddSingleton<WebhookHandler>();

        return services;
    }

    public static IServiceCollection AddBackgroundWorkers(this IServiceCollection services)
    {
        services.AddSingleton<CacheCleanupWorker>();
        services.AddSingleton<InventoryRebalanceWorker>();
        services.AddSingleton<CacheWarmerWorker>();
        return services;
    }

    /// <summary>
    /// Decorator pattern helper for wrapping services
    /// </summary>
    private static IServiceCollection Decorate<TInterface>(
        this IServiceCollection services,
        Func<TInterface, IServiceProvider, TInterface> decorator) where TInterface : class
    {
        var wrappedDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(TInterface));
        if (wrappedDescriptor == null)
            throw new InvalidOperationException($"{typeof(TInterface).Name} is not registered");

        var objectFactory = ActivatorUtilities.CreateFactory(wrappedDescriptor.ImplementationType!, new[] { typeof(TInterface) });

        services.Replace(ServiceDescriptor.Describe(
            typeof(TInterface),
            provider => (TInterface)objectFactory(provider, new[] { provider.CreateInstance(wrappedDescriptor) })!,
            wrappedDescriptor.Lifetime));

        return services;
    }

    private static object CreateInstance(this IServiceProvider provider, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance != null)
            return descriptor.ImplementationInstance;

        if (descriptor.ImplementationFactory != null)
            return descriptor.ImplementationFactory(provider);

        return ActivatorUtilities.GetServiceOrCreateInstance(provider, descriptor.ImplementationType!);
    }
}
