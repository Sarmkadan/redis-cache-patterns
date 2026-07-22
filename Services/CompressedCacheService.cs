#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Monitoring;

namespace RedisCachePatterns.Services;

/// <summary>
/// Cache service wrapper that compresses large cache entries to reduce memory usage
/// Automatically decompresses on retrieval with transparent compression detection
/// </summary>
public class CompressedCacheService : ICacheService
{
    private readonly ICacheService _innerCache;
    private readonly ILogger<CompressedCacheService> _logger;
    private readonly CacheStatisticsAggregator _statsAggregator = CacheStatisticsAggregator.Instance;
    private readonly int _compressionThresholdBytes;
    private const string CompressionMarker = "GZIP::";

    public CompressedCacheService(ICacheService innerCache, ILogger<CompressedCacheService> logger, int compressionThresholdBytes = 2048)
    {
        _innerCache = innerCache;
        _logger = logger;
        _compressionThresholdBytes = compressionThresholdBytes;
    }

    public async Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan? expiration = null)
    {
        // Use this class's GetAsync so that decompression is applied consistently.
        var cached = await GetAsync<T>(key);
        if (cached != null) return cached;

        // Cache miss — load the value, then store it via SetAsync which applies compression.
        var loaded = await loadFn();
        if (loaded != null)
            await SetAsync(key, loaded, expiration);

        _statsAggregator.IncrementMisses();
        return loaded;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _innerCache.GetAsync<string>(key);
        _statsAggregator.IncrementHits();
        if (value == null) return default;

        if (value.StartsWith(CompressionMarker))
        {
            return Decompress<T>(value[CompressionMarker.Length..]);
        }

        return JsonSerializer.Deserialize<T>(value);
    }

/// <summary>
/// Retrieves a cached value by key and refreshes its TTL on successful read (sliding expiration).
/// </summary>
/// <typeparam name="T">The type of the cached value.</typeparam>
/// <param name="key">The cache key to look up.</param>
/// <param name="slidingExpiration">The TTL to apply on every successful read.</param>
/// <returns>The deserialized value if found; otherwise <c>default</c>.</returns>
public async Task<T?> GetWithSlidingExpirationAsync<T>(string key, TimeSpan slidingExpiration)
{
    // Use this class's GetAsync so that decompression is applied consistently.
    var cached = await GetAsync<T>(key);
    if (cached != null)
    {
        // Reset TTL on the inner cache entry
        await _innerCache.GetWithSlidingExpirationAsync<T>(key, slidingExpiration);
        return cached;
    }
    return default;
}

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var json = JsonSerializer.Serialize(value);
        var compressed = CompressIfNeeded(json);

        await _innerCache.SetAsync(key, compressed, expiration);

        if (compressed.StartsWith(CompressionMarker))
        {
            _logger.LogDebug("Cached value compressed: {Key} | Original: {OriginalSize}B | Compressed: {CompressedSize}B",
                key, json.Length, compressed.Length);
        }
    }

    public async Task<T> WriteAsync<T>(string key, T value, Func<Task<T>> persistFn, TimeSpan? expiration = null)
    {
        return await _innerCache.WriteAsync(key, value, persistFn, expiration);
    }

    public async Task<T?> GetOrLoadWithSlidingExpirationAsync<T>(
        string key, Func<Task<T>> loadFn, TimeSpan slidingExpiration)
    {
        return await _innerCache.GetOrLoadWithSlidingExpirationAsync(key, loadFn, slidingExpiration);
    }

    public async Task RemoveAsync(string key)
    {
        await _innerCache.RemoveAsync(key);
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        await _innerCache.RemoveByPatternAsync(pattern);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _innerCache.ExistsAsync(key);
    }

    public async Task<TimeSpan?> GetExpirationAsync(string key)
    {
        return await _innerCache.GetExpirationAsync(key);
    }

    public async Task<bool> AcquireLockAsync(string lockKey, string lockValue, TimeSpan duration)
    {
        return await _innerCache.AcquireLockAsync(lockKey, lockValue, duration);
    }

    public async Task<bool> ReleaseLockAsync(string lockKey, string lockValue)
    {
        return await _innerCache.ReleaseLockAsync(lockKey, lockValue);
    }

    public async Task<bool> RenewLockAsync(string lockKey, string lockValue, TimeSpan newDuration)
    {
        return await _innerCache.RenewLockAsync(lockKey, lockValue, newDuration);
    }

    public async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern)
    {
        return await _innerCache.GetKeysByPatternAsync(pattern);
    }

public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys)
{
// Use this class's GetAsync so that decompression is applied consistently
var innerResults = await _innerCache.GetManyAsync<string>(keys);

var result = new Dictionary<string, T?>();
foreach (var kvp in innerResults)
{
var key = kvp.Key;
var stringValue = kvp.Value;

if (stringValue == null)
{
result[key] = default;
continue;
}

if (stringValue.StartsWith("GZIP::"))
{
try
{
var decompressed = Decompress<T>(stringValue["GZIP::".Length..]);
result[key] = decompressed;
}
catch (Exception ex)
{
_logger.LogError(ex, "Decompression failed for key: {Key}", key);
result[key] = default;
}
}
else
{
try
{
var deserialized = JsonSerializer.Deserialize<T>(stringValue);
result[key] = deserialized;
}
catch (Exception ex)
{
_logger.LogError(ex, "Deserialization failed for key: {Key}", key);
result[key] = default;
}
}
}

return result;
}


    public async Task FlushAsync()
    {
        await _innerCache.FlushAsync();
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        return await _innerCache.GetStatisticsAsync();
    }

    public ValueTask SetPolicyAsync(Domain.CachePolicy policy) =>
        _innerCache.SetPolicyAsync(policy);

    public ValueTask<Domain.CachePolicy?> GetPolicyAsync(string key) =>
        _innerCache.GetPolicyAsync(key);

    public async Task<T?> GetOrLoadWithEarlyExpirationAsync<T>(
        string key, Func<Task<T>> loadFn, TimeSpan expiration, double beta = 1.0)
    {
        // Load via inner cache XFetch, then ensure compressed storage on miss/refresh
        // by wrapping the loadFn to route through this class's SetAsync.
        T? result = default;
        var loaded = false;

        result = await _innerCache.GetOrLoadWithEarlyExpirationAsync<T>(key, async () =>
        {
            var value = await loadFn();
            loaded = true;
            return value;
        }, expiration, beta);

        // If a fresh value was loaded, re-store it compressed (the inner cache stored it
        // without compression; overwrite so GetAsync returns a compressed value).
        if (loaded && result != null)
            await SetAsync(key, result, expiration);

        return result;
    }

    public async Task<Domain.CacheKeyMetadata?> GetKeyMetadataAsync(string key) =>
        await _innerCache.GetKeyMetadataAsync(key);

    private string CompressIfNeeded(string data)
    {
        if (data.Length <= _compressionThresholdBytes)
            return data;

        try
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(data);
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionMode.Compress))
                {
                    gzip.Write(bytes, 0, bytes.Length);
                }
                var compressed = output.ToArray();
                return CompressionMarker + Convert.ToBase64String(compressed);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Compression failed, using uncompressed data");
            return data;
        }
    }

    private T? Decompress<T>(string compressedData)
    {
        try
        {
            var bytes = Convert.FromBase64String(compressedData);
            using (var input = new MemoryStream(bytes))
            {
                using (var gzip = new GZipStream(input, CompressionMode.Decompress))
                {
                    using (var output = new MemoryStream())
                    {
                        gzip.CopyTo(output);
                        var decompressed = System.Text.Encoding.UTF8.GetString(output.ToArray());
                        return JsonSerializer.Deserialize<T>(decompressed);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decompression failed");
            throw;
        }
    }
}
