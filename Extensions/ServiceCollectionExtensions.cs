#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;

namespace RedisCachePatterns.Extensions;

/// <summary>
/// Extension methods for configuring and registering utility services.
/// Provides convenience methods for common service registration patterns.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the auditing service to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddAuditing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<AuditingService>();
        return services;
    }

    /// <summary>
    /// Adds a batch processing service to the service collection.
    /// </summary>
    /// <typeparam name="T">The type of items to process.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
    /// <param name="processFn">The function to process batches of items.</param>
    /// <param name="batchSize">The maximum number of items to process in a single batch. Defaults to 100.</param>
    /// <param name="flushInterval">The time interval between automatic flushes. If <see langword="null"/>, automatic flushes are disabled.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is <see langword="null"/>.
    /// Thrown when <paramref name="processFn"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddBatchProcessing<T>(
        this IServiceCollection services,
        Func<List<T>, Task> processFn,
        int batchSize = 100,
        TimeSpan? flushInterval = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(processFn);

        services.AddSingleton(sp =>
            new BatchProcessingService<T>(
                processFn,
                sp.GetRequiredService<ILogger<BatchProcessingService<T>>>(),
                batchSize,
                flushInterval));

        return services;
    }

    /// <summary>
    /// Adds the idempotency helper to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
    /// <param name="recordRetention">The duration to retain idempotency records. If <see langword="null"/>, records are retained indefinitely.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddIdempotency(this IServiceCollection services, TimeSpan? recordRetention = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(new IdempotencyHelper(recordRetention));
        return services;
    }

    /// <summary>
    /// Adds the performance monitoring service to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddPerformanceMonitoring(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<PerformanceMonitor>();
        return services;
    }
}