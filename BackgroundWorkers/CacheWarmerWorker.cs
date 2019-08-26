// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using RedisCachePatterns.Services;

namespace RedisCachePatterns.BackgroundWorkers;

/// <summary>
/// Background worker that pre-warms cache with frequently accessed data
/// Improves performance by loading hot data during off-peak hours
/// </summary>
public class CacheWarmerWorker : IDisposable
{
    private readonly ProductService _productService;
    private readonly UserService _userService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheWarmerWorker> _logger;
    private readonly TimeSpan _interval;
    private Timer? _timer;
    private bool _isRunning;

    public CacheWarmerWorker(
        ProductService productService,
        UserService userService,
        ICacheService cacheService,
        ILogger<CacheWarmerWorker> logger,
        TimeSpan? interval = null)
    {
        _productService = productService;
        _userService = userService;
        _cacheService = cacheService;
        _logger = logger;
        _interval = interval ?? TimeSpan.FromHours(6);
    }

    public void Start()
    {
        if (_isRunning)
        {
            _logger.LogWarning("Cache warmer worker is already running");
            return;
        }

        _isRunning = true;
        _timer = new Timer(ExecuteWarming, null, TimeSpan.Zero, _interval);
        _logger.LogInformation("Cache warmer worker started with interval: {IntervalSeconds}s", _interval.TotalSeconds);
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _timer?.Dispose();
        _timer = null;
        _logger.LogInformation("Cache warmer worker stopped");
    }

    private async void ExecuteWarming(object? state)
    {
        try
        {
            _logger.LogInformation("Starting cache warming");

            // Warm product cache
            var warmStarted = DateTime.UtcNow;
            var productIds = new[] { 1, 2, 3, 4, 5 };

            foreach (var productId in productIds)
            {
                try
                {
                    await _productService.GetProductByIdAsync(productId);
                    _logger.LogDebug("Warmed cache for product: {ProductId}", productId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warm cache for product: {ProductId}", productId);
                }
            }

            // Warm user cache (example)
            var userIds = new[] { 1, 2 };
            foreach (var userId in userIds)
            {
                try
                {
                    await _userService.GetUserByIdAsync(userId);
                    _logger.LogDebug("Warmed cache for user: {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warm cache for user: {UserId}", userId);
                }
            }

            var duration = DateTime.UtcNow - warmStarted;
            _logger.LogInformation("Cache warming completed in {DurationMs}ms", duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache warming");
        }
    }

    public void Dispose()
    {
        Stop();
        _timer?.Dispose();
    }
}
