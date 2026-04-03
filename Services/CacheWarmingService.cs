#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Services;

/// <summary>
/// Service for proactively warming cache with frequently accessed data
/// Implements cache warming strategies to improve application startup performance
/// </summary>
public class CacheWarmingService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheWarmingService> _logger;
    private readonly List<CacheWarmingStrategy> _strategies = new();

    public CacheWarmingService(ICacheService cacheService, ILogger<CacheWarmingService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public CacheWarmingService AddStrategy(CacheWarmingStrategy strategy)
    {
        _strategies.Add(strategy);
        return this;
    }

    public async Task<CacheWarmingResult> WarmAsync()
    {
        var result = new CacheWarmingResult { StartedAt = DateTime.UtcNow };
        var startedAt = DateTime.UtcNow;

        _logger.LogInformation("Starting cache warming with {StrategyCount} strategies", _strategies.Count);

        foreach (var strategy in _strategies)
        {
            try
            {
                var itemsWarmed = await strategy.ExecuteAsync(_cacheService);
                result.TotalItemsWarmed += itemsWarmed;
                result.SuccessfulStrategies++;
                _logger.LogInformation("Strategy completed: {Strategy} | Items warmed: {Count}", strategy.Name, itemsWarmed);
            }
            catch (Exception ex)
            {
                result.FailedStrategies++;
                result.Errors.Add($"{strategy.Name}: {ex.Message}");
                _logger.LogError(ex, "Strategy failed: {Strategy}", strategy.Name);
            }
        }

        result.CompletedAt = DateTime.UtcNow;
        result.DurationMs = (long)(result.CompletedAt.Value - startedAt).TotalMilliseconds;

        _logger.LogInformation(
            "Cache warming completed: Items warmed={Items} | Duration={DurationMs}ms | Success={Success}",
            result.TotalItemsWarmed, result.DurationMs, result.SuccessfulStrategies);

        return result;
    }
}

/// <summary>
/// Base class for cache warming strategies
/// </summary>
public abstract class CacheWarmingStrategy
{
    public string Name { get; protected set; } = "Default";

    public abstract Task<int> ExecuteAsync(ICacheService cacheService);
}

/// <summary>
/// Simple cache warming strategy that loads predefined keys
/// </summary>
public class PredefinedKeyStrategy : CacheWarmingStrategy
{
    private readonly Dictionary<string, object> _dataToWarm;

    public PredefinedKeyStrategy(string name, Dictionary<string, object> dataToWarm)
    {
        Name = name;
        _dataToWarm = dataToWarm;
    }

    public override async Task<int> ExecuteAsync(ICacheService cacheService)
    {
        var count = 0;
        foreach (var (key, value) in _dataToWarm)
        {
            try
            {
                await cacheService.SetAsync(key, value, TimeSpan.FromHours(1));
                count++;
            }
            catch
            {
                // Continue with other items on failure
            }
        }
        return count;
    }
}

/// <summary>
/// Result of cache warming operation
/// </summary>
public class CacheWarmingResult
{
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long DurationMs { get; set; }
    public int TotalItemsWarmed { get; set; }
    public int SuccessfulStrategies { get; set; }
    public int FailedStrategies { get; set; }
    public List<string> Errors { get; set; } = new();

    public override string ToString() =>
        $"Warmed {TotalItemsWarmed} items in {DurationMs}ms ({SuccessfulStrategies} strategies succeeded, {FailedStrategies} failed)";
}
