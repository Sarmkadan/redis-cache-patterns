// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.IO.Compression;
using System.Text;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Compression utilities for reducing data size in cache and transit
/// Provides gzip compression and decompression operations
/// </summary>
public static class CompressionUtil
{
    /// <summary>
    /// Compresses string data using gzip
    /// </summary>
    public static byte[] CompressString(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        return CompressBytes(bytes);
    }

    /// <summary>
    /// Compresses byte array using gzip
    /// </summary>
    public static byte[] CompressBytes(byte[] data)
    {
        using (var output = new MemoryStream())
        {
            using (var gzip = new GZipStream(output, CompressionMode.Compress))
            {
                gzip.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }
    }

    /// <summary>
    /// Decompresses gzip data to string
    /// </summary>
    public static string DecompressString(byte[] data)
    {
        var bytes = DecompressBytes(data);
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Decompresses gzip data to byte array
    /// </summary>
    public static byte[] DecompressBytes(byte[] data)
    {
        using (var input = new MemoryStream(data))
        {
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            {
                using (var output = new MemoryStream())
                {
                    gzip.CopyTo(output);
                    return output.ToArray();
                }
            }
        }
    }

    /// <summary>
    /// Calculates compression ratio
    /// </summary>
    public static double GetCompressionRatio(int originalSize, int compressedSize)
    {
        return originalSize > 0 ? (double)compressedSize / originalSize * 100 : 0;
    }

    /// <summary>
    /// Determines if compression is worthwhile
    /// </summary>
    public static bool IsCompressionWorthwhile(int originalSize, int compressedSize, double minSavingsPercent = 10)
    {
        var ratio = GetCompressionRatio(originalSize, compressedSize);
        return (100 - ratio) >= minSavingsPercent;
    }
}
