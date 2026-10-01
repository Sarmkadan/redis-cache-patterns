#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Services;

/// <summary>
/// Handles serialization and deserialization of cache values with optional compression
/// </summary>
public class CacheSerializer
{
    private readonly ILogger<CacheSerializer> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly int _compressionThresholdBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheSerializer"/> class
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="compressionThresholdBytes">Minimum size in bytes before compression is applied (default 1024)</param>
    public CacheSerializer(ILogger<CacheSerializer> logger, int compressionThresholdBytes = 1024)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _compressionThresholdBytes = compressionThresholdBytes;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Serializes an object to a Redis-storable byte array, applying compression for large payloads
    /// </summary>
    /// <typeparam name="T">Type of the object to serialize</typeparam>
    /// <param name="value">Object to serialize</param>
    /// <returns>Serialized byte array</returns>
    public byte[] Serialize<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var json = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);

        if (json.Length >= _compressionThresholdBytes)
        {
            var compressed = Compress(json);
            _logger.LogDebug(
                "Serialized {Type}: {Original}B -> {Compressed}B (compressed)",
                typeof(T).Name, json.Length, compressed.Length);
            return AddCompressionHeader(compressed);
        }

        _logger.LogDebug("Serialized {Type}: {Size}B", typeof(T).Name, json.Length);
        return AddCompressionHeader(json, compressed: false);
    }

    /// <summary>
    /// Deserializes a byte array back to the target type, handling decompression if needed
    /// </summary>
    /// <typeparam name="T">Target type</typeparam>
    /// <param name="data">Raw byte array from Redis</param>
    /// <returns>Deserialized object</returns>
    public T? Deserialize<T>(byte[] data)
    {
        if (data == null || data.Length == 0)
            return default;

        var (isCompressed, payload) = ReadCompressionHeader(data);
        var json = isCompressed ? Decompress(payload) : payload;

        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    /// <summary>
    /// Computes the serialized size of a value without storing it
    /// </summary>
    /// <typeparam name="T">Type of the object</typeparam>
    /// <param name="value">Object to measure</param>
    /// <returns>Size in bytes after serialization</returns>
    public int ComputeSize<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Serialize(value).Length;
    }

    /// <summary>
    /// Attempts to deserialize data, returning false on failure instead of throwing
    /// </summary>
    /// <typeparam name="T">Target type</typeparam>
    /// <param name="data">Raw byte array</param>
    /// <param name="result">Deserialized result if successful</param>
    /// <returns>True if deserialization succeeded</returns>
    public bool TryDeserialize<T>(byte[] data, out T? result)
    {
        try
        {
            result = Deserialize<T>(data);
            return result is not null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize {Type}", typeof(T).Name);
            result = default;
            return false;
        }
    }

    private static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] AddCompressionHeader(byte[] data, bool compressed = true)
    {
        var result = new byte[data.Length + 1];
        result[0] = compressed ? (byte)1 : (byte)0;
        Buffer.BlockCopy(data, 0, result, 1, data.Length);
        return result;
    }

    private static (bool IsCompressed, byte[] Payload) ReadCompressionHeader(byte[] data)
    {
        if (data.Length < 2)
            return (false, data);

        var isCompressed = data[0] == 1;
        var payload = new byte[data.Length - 1];
        Buffer.BlockCopy(data, 1, payload, 0, payload.Length);
        return (isCompressed, payload);
    }
}
