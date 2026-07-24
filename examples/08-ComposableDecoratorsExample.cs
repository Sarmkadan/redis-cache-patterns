#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Configuration;
using RedisCachePatterns.Services;

namespace RedisCachePatterns.Examples;

/// <summary>
/// Demonstrates the decorator pattern for cache services - composing multiple decorators
/// (compression, circuit breaker, statistics) without combinatorial class explosion.
/// </summary>
public class ComposableDecoratorsExample
{
    private readonly ILogger<ComposableDecoratorsExample> _logger;

    public ComposableDecoratorsExample(ILogger<ComposableDecoratorsExample> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Shows how to manually compose decorators in code.
    /// </summary>
    public ICacheService CreateManuallyComposedCacheService()
    {
        // Create the base Redis cache service
        var redisConnectionString = "localhost:6379";
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddRedisCache(new CacheConfiguration { ConnectionString = redisConnectionString });

        var serviceProvider = services.BuildServiceProvider();
        var baseCache = serviceProvider.GetRequiredService<ICacheService>();

        // Manually compose decorators: Redis -> Compression -> Circuit Breaker
        var compressedCache = new CompressedCacheService(baseCache, _logger);
        var circuitBreakerCache = new CircuitBreakerCacheService(compressedCache, 5, TimeSpan.FromSeconds(30), _logger);

        _logger.LogInformation("Created manually composed cache pipeline: Redis -> Compression -> Circuit Breaker");
        return circuitBreakerCache;
    }

    /// <summary>
    /// Shows how to use the DI extension method to automatically compose decorators.
    /// </summary>
    public IServiceProvider CreateDiComposedCacheService()
    {
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());

        // Register base Redis cache
        services.AddRedisCache(new CacheConfiguration { ConnectionString = "localhost:6379" });

        // Add decorators using extension methods (order matters!)
        // Circuit breaker wraps the cache pipeline
        services.AddCircuitBreakerCache(failureThreshold: 5, breakDuration: TimeSpan.FromSeconds(30));

        // Note: Compression decorator would need to be added before circuit breaker
        // services.AddSingleton<ICacheService>(sp =>
        // {
        //     var inner = sp.GetRequiredService<ICacheService>();
        //     var logger = sp.GetService<ILogger<CompressedCacheService>>();
        //     return new CompressedCacheService(inner, logger);
        // });

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider;
    }

    /// <summary>
    /// Demonstrates the flexibility of the decorator pattern - you can compose
    /// decorators in different orders for different use cases.
    /// </summary>
    public void DemonstrateDecoratorFlexibility()
    {
        _logger.LogInformation("=== Decorator Pattern Benefits ===");
        _logger.LogInformation("1. No combinatorial explosion of classes");
        _logger.LogInformation("2. Decorators can be composed in any order");
        _logger.LogInformation("3. Each decorator adds a single concern");
        _logger.LogInformation("4. Easy to add/remove decorators without changing base implementation");
        _logger.LogInformation("");
        _logger.LogInformation("Example compositions:");
        _logger.LogInformation("- Redis -> Circuit Breaker (basic resilience)");
        _logger.LogInformation("- Redis -> Compression -> Circuit Breaker (resilience + memory efficiency)");
        _logger.LogInformation("- Redis -> Circuit Breaker -> Compression (circuit protects compression overhead)");
        _logger.LogInformation("- Redis -> Statistics -> Circuit Breaker -> Compression (monitoring + resilience + memory)");
    }

    /// <summary>
    /// Shows how to use the circuit breaker decorator to protect cache operations.
    /// </summary>
    public async Task DemonstrateCircuitBreakerUsage(ICacheService cacheService, string testKey = "test:decorator")
    {
        _logger.LogInformation("Testing circuit breaker decorator...");

        // Simulate a failing operation
        int failureCount = 0;
        Func<Task<string>> failingOperation = async () =>
        {
            failureCount++;
            _logger.LogWarning("Operation failed (attempt {Count})", failureCount);
            throw new Exception("Simulated failure");
        };

        try
        {
            // This will fail and increment failure counter
            await cacheService.GetOrLoadAsync(testKey, failingOperation);
        }
        catch { /* Expected */ }

        try
        {
            // This will also fail
            await cacheService.GetOrLoadAsync(testKey, failingOperation);
        }
        catch { /* Expected */ }

        _logger.LogInformation("After {Count} failures, circuit breaker state:", failureCount);

        if (cacheService is CircuitBreakerCacheService breakerService)
        {
            _logger.LogInformation("- State: {State}", breakerService.State);
            _logger.LogInformation("- Consecutive Failures: {Failures}", breakerService.ConsecutiveFailures);
            _logger.LogInformation("- Failure Threshold: {Threshold}", breakerService.FailureThreshold);
        }
    }
}