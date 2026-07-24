#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.IO;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Services;

namespace RedisCachePatterns.CLI;

/// <summary>
/// Implements cache-related CLI commands for management and diagnostics
/// Provides operations like flush, stats, key inspection, policy management, and cache warming
/// </summary>
public class CacheCommand
{
    private readonly ICacheService _cacheService;
    private readonly CacheWarmingService? _warmingService;
    private readonly ILogger<CacheCommand> _logger;

    public CacheCommand(ICacheService cacheService, ILogger<CacheCommand> logger, CacheWarmingService? warmingService = null)
    {
        _cacheService = cacheService;
        _warmingService = warmingService;
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
            "warm" => await WarmAsync(options),
            "warm-aside" => await WarmAsideAsync(options),
            _ => InvalidCommand(subcommand)
        };
    }

    private async Task<int> StatsAsync(Dictionary<string, string> options)
    {
        try
        {
            var stats = await _cacheService.GetStatisticsAsync();
            Console.WriteLine("=== Cache Statistics ===");
            Console.WriteLine($"Total Keys: {stats.TotalKeys}");
            Console.WriteLine($"Memory Used: {stats.MemoryUsedBytes / 1024.0:F2} KB");
            Console.WriteLine($"Hit Rate: {stats.HitRate:F2}%");
            Console.WriteLine($"Captured At: {stats.CapturedAt:O}");
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
                Console.WriteLine($" - {key}");
            }

            if (keyList.Count > 100)
                Console.WriteLine($" ... and {keyList.Count - 100} more keys");

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
                ? (TimeSpan?)TimeSpan.FromSeconds(seconds)
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

    private async Task<int> WarmAsync(Dictionary<string, string> options)
    {
        if (_warmingService is null)
        {
            Console.Error.WriteLine("Cache warming service is not configured.");
            return 1;
        }

        try
        {
            Console.WriteLine("Starting cache warming...");
            var result = await _warmingService.WarmAsync();
            Console.WriteLine($"Cache warming complete:");
            Console.WriteLine($" Items warmed: {result.TotalItemsWarmed}");
            Console.WriteLine($" Strategies succeeded: {result.SuccessfulStrategies}");
            Console.WriteLine($" Strategies failed: {result.FailedStrategies}");
            Console.WriteLine($" Duration: {result.DurationMs} ms");

            if (result.Errors.Count > 0)
            {
                Console.WriteLine(" Errors:");
                foreach (var err in result.Errors)
                    Console.WriteLine($" - {err}");
            }

            return result.FailedStrategies > 0 && result.SuccessfulStrategies == 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute cache warming");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> WarmAsideAsync(Dictionary<string, string> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.TryGetValue("keys", out var keysValue) &&
            !options.TryGetValue("file", out _) &&
            !options.TryGetValue("pattern", out _))
        {
            Console.Error.WriteLine("One of --keys (comma-separated), --file (path to key file), or --pattern (key pattern) parameter is required");
            return 1;
        }

        try
        {
            var keys = new List<string>();
            var sourceDescription = string.Empty;

            // Handle file input
            if (options.TryGetValue("file", out var filePath))
            {
                sourceDescription = $"file '{filePath}'";
                keys = await LoadKeysFromFileAsync(filePath);
            }
            // Handle pattern input
            else if (options.TryGetValue("pattern", out var pattern))
            {
                sourceDescription = $"pattern '{pattern}'";
                var limit = options.TryGetValue("limit", out var limitStr) && int.TryParse(limitStr, out var parsedLimit) && parsedLimit > 0
                    ? parsedLimit
                    : 1000; // Default limit for patterns

                keys = await LoadKeysByPatternAsync(pattern, limit);
            }
            // Handle direct keys input
            else if (!string.IsNullOrEmpty(keysValue))
            {
                sourceDescription = $"{keysValue.Split(',').Length} keys";
                keys = keysValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(k => k.Trim())
                    .Where(k => !string.IsNullOrEmpty(k))
                    .ToList();
            }

            if (keys.Count == 0)
            {
                Console.Error.WriteLine("No valid keys provided");
                return 1;
            }

            Console.WriteLine($"Starting cache-aside preloading for {keys.Count} keys from {sourceDescription}...");
            var startedAt = DateTime.UtcNow;
            var warmedCount = 0;
            var errors = new List<string>();

            foreach (var key in keys)
            {
                try
                {
                    var value = await _cacheService.GetOrLoadAsync<object>(
                        key,
                        async () =>
                        {
                            return new { Preloaded = true, Key = key, Timestamp = DateTime.UtcNow };
                        },
                        TimeSpan.FromHours(1)
                    );

                    if (value != null)
                    {
                        warmedCount++;
                        Console.WriteLine($"✓ Preloaded key: {key}");
                    }
                    else
                    {
                        errors.Add($"Key '{key}' returned null from load function");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to preload key '{key}': {ex.Message}");
                    _logger.LogError(ex, "Failed to preload key through cache-aside: {Key}", key);
                }
            }

            var duration = DateTime.UtcNow - startedAt;
            Console.WriteLine($"\nCache-aside preloading complete:");
            Console.WriteLine($" Keys preloaded: {warmedCount}/{keys.Count}");
            Console.WriteLine($" Duration: {duration.TotalMilliseconds:F0} ms");

            if (errors.Count > 0)
            {
                Console.WriteLine("\nErrors:");
                foreach (var error in errors)
                {
                    Console.WriteLine($" - {error}");
                }
                return 1;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute cache-aside preloading");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Loads keys from a file with path validation to prevent directory traversal attacks
    /// </summary>
    /// <param name="filePath">Path to the file containing keys</param>
    /// <returns>List of keys loaded from the file</returns>
    /// <exception cref="ArgumentException">Thrown when file path is invalid or contains path traversal</exception>
    private async Task<List<string>> LoadKeysFromFileAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        // Canonicalize the path to resolve any relative segments
        var canonicalPath = Path.GetFullPath(filePath);

        // Define allowed base directory (current working directory)
        var allowedBase = Path.GetFullPath(Directory.GetCurrentDirectory());
        var fileDirectory = Path.GetDirectoryName(canonicalPath) ?? string.Empty;
        var canonicalBase = Path.GetFullPath(fileDirectory);

        // Validate that the file is within the allowed base directory
        if (!canonicalBase.StartsWith(allowedBase, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"File path '{filePath}' resolves to '{canonicalPath}' which is outside the allowed directory '{allowedBase}'. " +
                "Path traversal is not permitted.");
        }

        // Check if file exists
        if (!File.Exists(canonicalPath))
        {
            throw new FileNotFoundException("Key file not found", filePath);
        }

        // Read keys from file (one key per line)
        var keys = new List<string>();

        using var reader = new StreamReader(canonicalPath);
        while (reader.ReadLine() is string line)
        {
            var key = line.Trim();
            if (!string.IsNullOrEmpty(key))
            {
                keys.Add(key);
            }
        }

        _logger.LogInformation("Loaded {KeyCount} keys from file '{FilePath}'", keys.Count, canonicalPath);
        return keys;
    }

    /// <summary>
    /// Loads keys by pattern with a maximum limit to prevent resource exhaustion
    /// </summary>
    /// <param name="pattern">Key pattern to match</param>
    /// <param name="limit">Maximum number of keys to return</param>
    /// <returns>List of keys matching the pattern (up to limit)</returns>
    private async Task<List<string>> LoadKeysByPatternAsync(string pattern, int limit)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than 0");
        }

        if (_cacheService is null)
        {
            throw new InvalidOperationException("Cache service is not available");
        }

        try
        {
            var keys = await _cacheService.GetKeysByPatternAsync(pattern);
            var keyList = keys.Take(limit).ToList();

            _logger.LogInformation("Matched {MatchedCount} keys with pattern '{Pattern}', returning first {ReturnedCount} (limit: {Limit}",
                keys.Count(), pattern, keyList.Count, limit);

            return keyList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get keys by pattern '{Pattern}'", pattern);
            throw new InvalidOperationException($"Failed to match pattern '{pattern}': {ex.Message}", ex);
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
        Console.Error.WriteLine("Available subcommands: stats, flush, keys, get, set, delete, ttl, warm, warm-aside");
        return 1;
    }
}