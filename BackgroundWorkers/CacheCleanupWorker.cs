// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

#nullable enable

using Microsoft.Extensions.Logging;
using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;

namespace RedisCachePatterns.BackgroundWorkers;

/// <summary>
/// Background worker that periodically cleans up expired cache entries
/// Removes stale data and monitors cache size growth
/// </summary>
public class CacheCleanupWorker : IDisposable
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheCleanupWorker> _logger;
    private readonly TimeSpan _interval;
    private Timer? _timer;
    private bool _isRunning;

    public CacheCleanupWorker(ICacheService cacheService, ILogger<CacheCleanupWorker> logger, TimeSpan? interval = null)
    {
        _cacheService = cacheService;
        _logger = logger;
        _interval = interval ?? TimeSpan.FromHours(1);
    }

    public void Start()
    {
        if (_isRunning)
        {
            _logger.LogWarning("Cache cleanup worker is already running");
            return;
        }

        _isRunning = true;
        _timer = new Timer(ExecuteCleanup, null, TimeSpan.Zero, _interval);
        _logger.LogInformation("Cache cleanup worker started with interval: {IntervalSeconds}s", _interval.TotalSeconds);
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _timer?.Dispose();
        _timer = null;
        _logger.LogInformation("Cache cleanup worker stopped");
    }

    private async void ExecuteCleanup(object? state)
    {
        try
        {
            _logger.LogInformation("Starting cache cleanup");

            var beforeStats = await _cacheService.GetStatisticsAsync();
            _logger.LogDebug("Cache status before cleanup: Keys={Keys}, Memory={MemoryKB}KB",
                beforeStats.TotalKeys, beforeStats.MemoryUsedBytes / 1024);

            // Clean up expired lock keys
            var lockKeys = await _cacheService.GetKeysByPatternAsync("lock:*");
            var expiredLocks = 0;
            foreach (var lockKey in lockKeys)
            {
                var ttl = await _cacheService.GetExpirationAsync(lockKey);
                if (!ttl.HasValue || ttl.Value.TotalSeconds <= 0)
                {
                    await _cacheService.RemoveAsync(lockKey);
                    expiredLocks++;
                }
            }

            if (expiredLocks > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired lock keys", expiredLocks);
            }

            var afterStats = await _cacheService.GetStatisticsAsync();
            _logger.LogInformation(
                "Cache cleanup completed: Removed={RemovedKeys} | Memory saved={SavedKB}KB",
                beforeStats.TotalKeys - afterStats.TotalKeys,
                (beforeStats.MemoryUsedBytes - afterStats.MemoryUsedBytes) / 1024);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache cleanup");
        }
    }

    public void Dispose()
    {
        Stop();
        _timer?.Dispose();
    }
}
