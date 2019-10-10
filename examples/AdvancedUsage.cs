using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;
using RedisCachePatterns.Domain;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Examples;

/// <summary>
/// Demonstrates advanced features: distributed locking and error handling.
/// </summary>
public class AdvancedUsage
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<AdvancedUsage> _logger;

    public AdvancedUsage(ICacheService cacheService, ILogger<AdvancedUsage> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task RunExampleAsync(int productId)
    {
        var key = CacheKeyBuilder.BuildProductKey(productId);
        var lockKey = $"lock:{key}";

        try
        {
            // Acquire a distributed lock for 5 seconds to prevent cache stampede
            if (await _cacheService.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(5)))
            {
                try
                {
                    var product = await _cacheService.GetAsync<Product>(key);
                    // Perform operations while lock is held
                }
                finally
                {
                    // Ensure lock is released
                    await _cacheService.ReleaseLockAsync(lockKey);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during advanced cache operation.");
        }
    }
}
