#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Services;

/// <summary>
/// Cache service wrapper that compresses large cache entries to reduce memory usage
/// Automatically decompresses on retrieval with transparent compression detection
/// </summary>
public class CompressedCacheService : ICacheService
{
    private readonly ICacheService _innerCache;
    private readonly ILogger<CompressedCacheService> _logger;
    private readonly int _compressionThresholdBytes;
    private const string CompressionMarker = "GZIP::";

    public CompressedCacheService(ICacheService innerCache, ILogger<CompressedCacheService> logger, int compressionThresholdBytes = 1024)
    {
        _innerCache = innerCache;
        _logger = logger;
        _compressionThresholdBytes = compressionThresholdBytes;
    }

    public async Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan? expiration = null)
    {
        return await _innerCache.GetOrLoadAsync(key, loadFn, expiration).ConfigureAwait(false);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _innerCache.GetAsync<string>(key).ConfigureAwait(false);
        if (value == null) return default;

        if (value.StartsWith(CompressionMarker))
        {
            return Decompress<T>(value[CompressionMarker.Length..]);
        }

        return JsonSerializer.Deserialize<T>(value);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var json = JsonSerializer.Serialize(value);
        var compressed = CompressIfNeeded(json);

        await _innerCache.SetAsync(key, compressed, expiration).ConfigureAwait(false);

        if (compressed.StartsWith(CompressionMarker))
        {
            _logger.LogDebug("Cached value compressed: {Key} | Original: {OriginalSize}B | Compressed: {CompressedSize}B",
                key, json.Length, compressed.Length);
        }
    }

    public async Task<T> WriteAsync<T>(string key, T value, Func<Task<T>> persistFn, TimeSpan? expiration = null)
    {
        return await _innerCache.WriteAsync(key, value, persistFn, expiration).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string key)
    {
        await _innerCache.RemoveAsync(key).ConfigureAwait(false);
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        await _innerCache.RemoveByPatternAsync(pattern).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _innerCache.ExistsAsync(key).ConfigureAwait(false);
    }

    public async Task<TimeSpan?> GetExpirationAsync(string key)
    {
        return await _innerCache.GetExpirationAsync(key).ConfigureAwait(false);
    }

    public async Task<bool> AcquireLockAsync(string lockKey, string lockValue, TimeSpan duration)
    {
        return await _innerCache.AcquireLockAsync(lockKey, lockValue, duration).ConfigureAwait(false);
    }

    public async Task<bool> ReleaseLockAsync(string lockKey, string lockValue)
    {
        return await _innerCache.ReleaseLockAsync(lockKey, lockValue).ConfigureAwait(false);
    }

    public async Task<bool> RenewLockAsync(string lockKey, string lockValue, TimeSpan newDuration)
    {
        return await _innerCache.RenewLockAsync(lockKey, lockValue, newDuration).ConfigureAwait(false);
    }

    public async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern)
    {
        return await _innerCache.GetKeysByPatternAsync(pattern).ConfigureAwait(false);
    }

    public async Task FlushAsync()
    {
        await _innerCache.FlushAsync().ConfigureAwait(false);
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        return await _innerCache.GetStatisticsAsync().ConfigureAwait(false);
    }

    public ValueTask SetPolicyAsync(Domain.CachePolicy policy) =>
        _innerCache.SetPolicyAsync(policy);

    public ValueTask<Domain.CachePolicy?> GetPolicyAsync(string key) =>
        _innerCache.GetPolicyAsync(key);

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
