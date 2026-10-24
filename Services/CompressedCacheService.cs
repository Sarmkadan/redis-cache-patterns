#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Exceptions;
using RedisCachePatterns.Monitoring;

namespace RedisCachePatterns.Services;

/// <summary>
/// Cache service wrapper that compresses large cache entries to reduce memory usage
/// Automatically decompresses on retrieval with transparent compression detection
/// </summary>
/// <remarks>
/// Values smaller than <see cref="MinCompressSizeBytes"/> are stored uncompressed with a format marker.
/// This avoids CPU overhead and size inflation from compressing small payloads (less than ~1KB).
/// Compressed payloads read back from the cache are untrusted: their encoded size and their decompressed
/// output are both capped, so a small high-ratio GZIP payload cannot expand into unbounded memory.
/// </remarks>
public class CompressedCacheService : ICacheService
{
    /// <summary>
    /// Default maximum size in bytes of a compressed payload accepted for decompression (4 MiB).
    /// </summary>
    public const int DefaultMaxCompressedBytes = 4 * 1024 * 1024;

    /// <summary>
    /// Default maximum size in bytes of decompressed output produced from a cached payload (32 MiB).
    /// </summary>
    public const int DefaultMaxDecompressedBytes = 32 * 1024 * 1024;

    private readonly ICacheService _innerCache;
    private readonly ILogger<CompressedCacheService> _logger;
    private readonly CacheStatisticsAggregator _statsAggregator = CacheStatisticsAggregator.Instance;
    private readonly int _compressionThresholdBytes;
    private readonly int _maxCompressedBytes;
    private readonly int _maxDecompressedBytes;
    private const string CompressionMarker = "GZIP::";
    private const string UncompressedMarker = "RAW::";
    private const int DecompressionBufferBytes = 8192;
    // GZIP header (10 bytes) plus trailer (8 bytes); any valid stream is at least this long.
    private const int MinGzipLength = 18;
    private const int GzipTrailerLength = 8;
    private static readonly uint[] Crc32Table = BuildCrc32Table();

    /// <summary>
    /// Initializes a new instance of the <see cref="CompressedCacheService"/> class.
    /// </summary>
    /// <param name="innerCache">Cache service that stores the (possibly compressed) values</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="compressionThresholdBytes">Minimum size in bytes before compression is applied (default 2048)</param>
    /// <param name="maxCompressedBytes">Maximum accepted size in bytes of a compressed payload read from the cache</param>
    /// <param name="maxDecompressedBytes">Maximum size in bytes of decompressed output produced from a cached payload</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="innerCache"/> or <paramref name="logger"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when either size limit is zero or negative.</exception>
    public CompressedCacheService(
        ICacheService innerCache,
        ILogger<CompressedCacheService> logger,
        int compressionThresholdBytes = 2048,
        int maxCompressedBytes = DefaultMaxCompressedBytes,
        int maxDecompressedBytes = DefaultMaxDecompressedBytes)
    {
        ArgumentNullException.ThrowIfNull(innerCache);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCompressedBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxDecompressedBytes);

        _innerCache = innerCache;
        _logger = logger;
        _compressionThresholdBytes = compressionThresholdBytes;
        _maxCompressedBytes = maxCompressedBytes;
        _maxDecompressedBytes = maxDecompressedBytes;
    }

    /// <summary>
    /// Minimum size in bytes for compression to be applied.
    /// Values smaller than this threshold are stored uncompressed to avoid CPU overhead and size inflation.
    /// </summary>
    public int MinCompressSizeBytes => _compressionThresholdBytes;

    public async Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan? expiration = null)
    {
        // Use this class's GetAsync so that decompression is applied consistently.
        var cached = await GetAsync<T>(key);
        if (cached != null)
        {
            return cached;
        }

        // Cache miss — load the value, then store it via SetAsync which applies compression.
        var loaded = await loadFn();
        // The miss was already counted by GetAsync above; counting again here would
        // double-count every miss.
        if (loaded != null)
        {
            await SetAsync(key, loaded, expiration);
        }

        return loaded;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _innerCache.GetAsync<string>(key);
        if (value == null)
        {
            _statsAggregator.IncrementMisses();
            return default;
        }

        _statsAggregator.IncrementHits();

        if (value.StartsWith(CompressionMarker))
        {
            try
            {
                return Decompress<T>(value[CompressionMarker.Length..]);
            }
            catch (CacheException ex)
            {
                _logger.LogWarning(ex, "Rejected compressed cache value for key {Key}", key);
                throw;
            }
        }
        else if (value.StartsWith(UncompressedMarker))
        {
            return JsonSerializer.Deserialize<T>(value[UncompressedMarker.Length..]);
        }

        // Handle legacy uncompressed entries from RedisCacheService or other services
        // The value is a JSON string that needs to be deserialized to type T
        return JsonSerializer.Deserialize<T>(value);
    }

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
        else if (compressed.StartsWith(UncompressedMarker))
        {
            _logger.LogDebug("Cached value stored uncompressed (small): {Key} | Size: {Size}B",
                key, json.Length);
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

            if (stringValue.StartsWith(CompressionMarker))
            {
                // A rejected or corrupt entry is reported as a miss for that key only, so one bad value
                // does not fail the whole batch.
                try
                {
                    result[key] = Decompress<T>(stringValue[CompressionMarker.Length..]);
                }
                catch (Exception ex) when (ex is CacheException or JsonException)
                {
                    _logger.LogWarning(ex, "Rejected compressed cache value for key {Key}", key);
                    result[key] = default;
                }
            }
            else if (stringValue.StartsWith(UncompressedMarker))
            {
                try
                {
                    var deserialized = JsonSerializer.Deserialize<T>(stringValue[UncompressedMarker.Length..]);
                    result[key] = deserialized;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Deserialization failed for key: {Key}", key);
                    result[key] = default;
                }
            }
            else
            {
                // Handle legacy uncompressed entries
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
        {
            await SetAsync(key, result, expiration);
        }

        return result;
    }

    public async Task<Domain.CacheKeyMetadata?> GetKeyMetadataAsync(string key) =>
        await _innerCache.GetKeyMetadataAsync(key);

    private string CompressIfNeeded(string data)
    {
        // Calculate actual byte length to account for UTF-8 multi-byte characters
        int byteLength = System.Text.Encoding.UTF8.GetByteCount(data);
        if (byteLength <= _compressionThresholdBytes)
        {
            return UncompressedMarker + data;
        }

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

    /// <summary>
    /// Decodes, inflates and deserializes a GZIP payload read from the cache, enforcing the size limits.
    /// </summary>
    /// <exception cref="CacheSizeLimitExceededException">Thrown when the encoded or decompressed payload exceeds its limit.</exception>
    /// <exception cref="CacheDecompressionException">Thrown when the payload is not valid base64 or GZIP data, or is truncated.</exception>
    private T? Decompress<T>(string compressedData)
    {
        var compressed = DecodeBase64(compressedData);
        var json = Inflate(compressed);
        return JsonSerializer.Deserialize<T>(json);
    }

    /// <summary>
    /// Decodes the base64 payload. The encoded length is checked first so an oversized value is rejected
    /// before the decoded buffer is allocated.
    /// </summary>
    /// <exception cref="CacheSizeLimitExceededException">Thrown when the payload would decode to more than the compressed-size limit.</exception>
    /// <exception cref="CacheDecompressionException">Thrown when the payload is not valid base64.</exception>
    private byte[] DecodeBase64(string encoded)
    {
        // Base64 of n bytes is 4 * ceil(n / 3) characters, so this bound is exact for the byte limit.
        long maxEncodedLength = 4L * ((_maxCompressedBytes + 2) / 3);
        if (encoded.Length > maxEncodedLength)
        {
            throw new CacheSizeLimitExceededException(
                $"Compressed cache payload of {encoded.Length} characters exceeds the {_maxCompressedBytes}-byte limit",
                _maxCompressedBytes);
        }

        try
        {
            return Convert.FromBase64String(encoded);
        }
        catch (FormatException ex)
        {
            throw new CacheDecompressionException("Compressed cache payload is not valid base64", ex);
        }
    }

    /// <summary>
    /// Inflates GZIP data into UTF-8 JSON bytes, stopping as soon as the output would exceed the decompressed-size limit.
    /// The CRC-32 and ISIZE trailer is verified against the output, because <see cref="GZipStream"/> returns a
    /// partial result without error when the input is truncated.
    /// </summary>
    /// <exception cref="CacheSizeLimitExceededException">Thrown when the decompressed output exceeds the limit.</exception>
    /// <exception cref="CacheDecompressionException">Thrown when the data is not valid, is truncated or fails the trailer check.</exception>
    private byte[] Inflate(byte[] compressed)
    {
        if (compressed.Length < MinGzipLength)
        {
            throw new CacheDecompressionException("Compressed cache payload is too short to be a GZIP stream");
        }

        var trailer = compressed.AsSpan(compressed.Length - GzipTrailerLength);
        uint expectedCrc = BinaryPrimitives.ReadUInt32LittleEndian(trailer);
        uint expectedSize = BinaryPrimitives.ReadUInt32LittleEndian(trailer[4..]);

        try
        {
            using var input = new MemoryStream(compressed, writable: false);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();

            uint crc = 0;
            long total = 0;
            var buffer = ArrayPool<byte>.Shared.Rent(DecompressionBufferBytes);
            try
            {
                int read;
                while ((read = gzip.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Check before writing so the output never grows past the limit.
                    if (total + read > _maxDecompressedBytes)
                    {
                        throw new CacheSizeLimitExceededException(
                            $"Decompressed cache payload exceeds the {_maxDecompressedBytes}-byte limit",
                            _maxDecompressedBytes);
                    }

                    crc = UpdateCrc32(crc, buffer.AsSpan(0, read));
                    output.Write(buffer, 0, read);
                    total += read;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            if (crc != expectedCrc || (uint)total != expectedSize)
            {
                throw new CacheDecompressionException("Compressed cache payload is truncated or corrupted (GZIP trailer mismatch)");
            }

            return output.ToArray();
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException)
        {
            throw new CacheDecompressionException("Compressed cache payload is malformed or truncated", ex);
        }
    }

    /// <summary>
    /// Updates a running CRC-32 (IEEE 802.3, the checksum used by GZIP) with the given bytes.
    /// </summary>
    private static uint UpdateCrc32(uint crc, ReadOnlySpan<byte> data)
    {
        crc = ~crc;
        foreach (var b in data)
        {
            crc = Crc32Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }
        return ~crc;
    }

    private static uint[] BuildCrc32Table()
    {
        var table = new uint[256];
        for (uint n = 0; n < table.Length; n++)
        {
            uint c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }
            table[n] = c;
        }
        return table;
    }
}
