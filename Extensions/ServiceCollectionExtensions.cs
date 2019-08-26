// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;

namespace RedisCachePatterns.Extensions;

/// <summary>
/// Extension methods for configuring and registering utility services
/// Provides convenience methods for common service registration patterns
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuditing(this IServiceCollection services)
    {
        services.AddSingleton<AuditingService>();
        return services;
    }

    public static IServiceCollection AddBatchProcessing<T>(
        this IServiceCollection services,
        Func<List<T>, Task> processFn,
        int batchSize = 100,
        TimeSpan? flushInterval = null)
    {
        services.AddSingleton(sp =>
            new BatchProcessingService<T>(processFn, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BatchProcessingService<T>>>(), batchSize, flushInterval));
        return services;
    }

    public static IServiceCollection AddIdempotency(this IServiceCollection services, TimeSpan? recordRetention = null)
    {
        services.AddSingleton(new IdempotencyHelper(recordRetention));
        return services;
    }

    public static IServiceCollection AddPerformanceMonitoring(this IServiceCollection services)
    {
        services.AddSingleton<PerformanceMonitor>();
        return services;
    }
}
