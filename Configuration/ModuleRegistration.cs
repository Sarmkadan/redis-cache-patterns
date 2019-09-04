#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using RedisCachePatterns.BackgroundWorkers;

namespace RedisCachePatterns.Configuration;

/// <summary>
/// Modular registration patterns for starting background workers and services
/// Provides lifecycle management for long-running operations
/// </summary>
public class ModuleRegistration
{
    private readonly IServiceProvider _serviceProvider;
    private readonly List<IDisposable> _activeWorkers = new();

    public ModuleRegistration(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Starts all registered background workers
    /// </summary>
    public void StartBackgroundWorkers()
    {
        var cleanupWorker = _serviceProvider.GetService<CacheCleanupWorker>();
        if (cleanupWorker != null)
        {
            cleanupWorker.Start();
            _activeWorkers.Add(cleanupWorker);
        }

        var rebalanceWorker = _serviceProvider.GetService<InventoryRebalanceWorker>();
        if (rebalanceWorker != null)
        {
            rebalanceWorker.Start();
            _activeWorkers.Add(rebalanceWorker);
        }

        var warmerWorker = _serviceProvider.GetService<CacheWarmerWorker>();
        if (warmerWorker != null)
        {
            warmerWorker.Start();
            _activeWorkers.Add(warmerWorker);
        }
    }

    /// <summary>
    /// Stops all running background workers
    /// </summary>
    public void StopBackgroundWorkers()
    {
        foreach (var worker in _activeWorkers)
        {
            worker?.Dispose();
        }
        _activeWorkers.Clear();
    }

    /// <summary>
    /// Explicitly starts a specific worker
    /// </summary>
    public void StartWorker<T>() where T : class
    {
        var worker = _serviceProvider.GetService<T>();
        if (worker is CacheCleanupWorker cleanupWorker)
            cleanupWorker.Start();
        else if (worker is InventoryRebalanceWorker rebalanceWorker)
            rebalanceWorker.Start();
        else if (worker is CacheWarmerWorker warmerWorker)
            warmerWorker.Start();
    }

    public void Dispose()
    {
        StopBackgroundWorkers();
    }
}
