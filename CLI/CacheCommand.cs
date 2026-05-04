// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using RedisCachePatterns.Services;

namespace RedisCachePatterns.CLI;

/// <summary>
/// Implements cache-related CLI commands for management and diagnostics
/// Provides operations like flush, stats, key inspection, policy management
/// </summary>
public class CacheCommand
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheCommand> _logger;

    public CacheCommand(ICacheService cacheService, ILogger<CacheCommand> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(Dictionary<string, string> options)
    {
        if (!options.TryGetValue("subcommand", out var subcommand))
        {
            subcommand = "stats";
        }

        return subcommand.ToLower() switch
        {
            "stats" => await StatsAsync(options),
            "flush" => await FlushAsync(options),
            "keys" => await ListKeysAsync(options),
            "get" => await GetKeyAsync(options),
            "set" => await SetKeyAsync(options),
            "delete" => await DeleteKeyAsync(options),
            "ttl" => await GetTtlAsync(options),
            _ => InvalidCommand(subcommand)
        };
    }

    private async Task<int> StatsAsync(Dictionary<string, string> options)
    {
        try
        {
            var stats = await _cacheService.GetStatisticsAsync();
            Console.WriteLine("=== Cache Statistics ===");
            Console.WriteLine($"Total Keys:      {stats.TotalKeys}");
            Console.WriteLine($"Memory Used:     {stats.MemoryUsedBytes / 1024.0:F2} KB");
            Console.WriteLine($"Hit Rate:        {stats.HitRate:F2}%");
            Console.WriteLine($"Captured At:     {stats.CapturedAt:O}");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve cache statistics");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> FlushAsync(Dictionary<string, string> options)
    {
        if (!options.ContainsKey("force") && !ConfirmAction("Flush entire cache"))
            return 0;

        try
        {
            await _cacheService.FlushAsync();
            Console.WriteLine("Cache flushed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush cache");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> ListKeysAsync(Dictionary<string, string> options)
    {
        try
        {
            var pattern = options.TryGetValue("pattern", out var p) ? p : "*";
            var keys = await _cacheService.GetKeysByPatternAsync(pattern);
            var keyList = keys.ToList();

            Console.WriteLine($"Keys matching pattern '{pattern}': {keyList.Count}");
            foreach (var key in keyList.Take(100))
            {
                Console.WriteLine($"  - {key}");
            }

            if (keyList.Count > 100)
                Console.WriteLine($"  ... and {keyList.Count - 100} more keys");

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list cache keys");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> GetKeyAsync(Dictionary<string, string> options)
    {
        if (!options.TryGetValue("key", out var key))
        {
            Console.Error.WriteLine("--key parameter required");
            return 1;
        }

        try
        {
            var value = await _cacheService.GetAsync<object>(key);
            if (value != null)
            {
                Console.WriteLine($"Key: {key}");
                Console.WriteLine($"Value: {value}");
            }
            else
            {
                Console.WriteLine($"Key '{key}' not found in cache");
            }
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get key from cache");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> SetKeyAsync(Dictionary<string, string> options)
    {
        if (!options.TryGetValue("key", out var key) || !options.TryGetValue("value", out var value))
        {
            Console.Error.WriteLine("--key and --value parameters required");
            return 1;
        }

        try
        {
            var ttl = options.TryGetValue("ttl", out var ttlStr) && int.TryParse(ttlStr, out var seconds)
                ? TimeSpan.FromSeconds(seconds)
                : null;

            await _cacheService.SetAsync(key, value, ttl);
            Console.WriteLine($"Key '{key}' set successfully");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set key in cache");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> DeleteKeyAsync(Dictionary<string, string> options)
    {
        if (!options.TryGetValue("key", out var key))
        {
            Console.Error.WriteLine("--key parameter required");
            return 1;
        }

        try
        {
            await _cacheService.RemoveAsync(key);
            Console.WriteLine($"Key '{key}' deleted");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete key from cache");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> GetTtlAsync(Dictionary<string, string> options)
    {
        if (!options.TryGetValue("key", out var key))
        {
            Console.Error.WriteLine("--key parameter required");
            return 1;
        }

        try
        {
            var ttl = await _cacheService.GetExpirationAsync(key);
            if (ttl.HasValue)
                Console.WriteLine($"Key '{key}' TTL: {ttl.Value.TotalSeconds:F0} seconds");
            else
                Console.WriteLine($"Key '{key}' has no expiration");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get TTL for key");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private bool ConfirmAction(string action)
    {
        Console.Write($"{action}? (y/n): ");
        var response = Console.ReadLine()?.ToLower();
        return response == "y" || response == "yes";
    }

    private int InvalidCommand(string command)
    {
        Console.Error.WriteLine($"Unknown cache subcommand: {command}");
        return 1;
    }
}
