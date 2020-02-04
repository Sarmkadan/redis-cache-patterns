#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Buffers;
using System.IO.Compression;
using System.Text;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Compression utilities using ArrayPool&lt;byte&gt;.Shared for temporary buffers.
/// Renting from the pool avoids large short-lived allocations on the LOH and
/// reduces GC pressure at high cache throughput.
/// </summary>
public static class CompressionUtil
{
    /// <summary>
    /// Compresses a string using GZIP, reusing a pooled input buffer.
    /// </summary>
    public static byte[] CompressString(string data)
    {
        int maxByteCount = Encoding.UTF8.GetMaxByteCount(data.Length);
        byte[] rentedInput = ArrayPool<byte>.Shared.Rent(maxByteCount);
        try
        {
            int written = Encoding.UTF8.GetBytes(data, 0, data.Length, rentedInput, 0);
            return CompressBytes(rentedInput.AsSpan(0, written));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedInput);
        }
    }

    /// <summary>
    /// Compresses a byte span using GZIP.
    /// </summary>
    public static byte[] CompressBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
        {
            // GZipStream never flushes a header/trailer when zero bytes are ever
            // written to it, so a zero-length input would otherwise round-trip to a
            // zero-length "compressed" output. Return the canonical bytes of an
            // empty gzip stream instead, so callers always get a valid archive.
            return EmptyGzipBytes;
        }

        using var output = new MemoryStream(data.Length / 2 + 128); // educated initial capacity
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(data);
        }
        return output.ToArray();
    }

    private static readonly byte[] EmptyGzipBytes =
        Convert.FromHexString("1f8b080000000000000303000000000000000000");

    /// <summary>
    /// Kept for backward compatibility; delegates to the span overload.
    /// </summary>
    public static byte[] CompressBytes(byte[] data) =>
        CompressBytes(data.AsSpan());

    /// <summary>
    /// Decompresses GZIP data to a string, reusing a pooled output buffer.
    /// </summary>
    public static string DecompressString(byte[] data)
    {
        var bytes = DecompressBytes(data);
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Decompresses GZIP data to a byte array.
    /// Uses ArrayPool for an intermediate read buffer to avoid per-call allocation.
    /// </summary>
    public static byte[] DecompressBytes(byte[] data)
    {
        using var input = new MemoryStream(data, writable: false);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);

        // Grow into the output stream in pooled chunks
        const int chunkSize = 4096;
        byte[] rentedChunk = ArrayPool<byte>.Shared.Rent(chunkSize);
        try
        {
            using var output = new MemoryStream(data.Length * 4); // heuristic pre-size
            int read;
            while ((read = gzip.Read(rentedChunk, 0, rentedChunk.Length)) > 0)
                output.Write(rentedChunk, 0, read);
            return output.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedChunk);
        }
    }

    /// <summary>
    /// Calculates compression ratio as a percentage of the compressed size relative to original.
    /// </summary>
    public static double GetCompressionRatio(int originalSize, int compressedSize) =>
        originalSize > 0 ? (double)compressedSize / originalSize * 100 : 0;

    /// <summary>
    /// Returns true when compression saves at least <paramref name="minSavingsPercent"/> percent.
    /// </summary>
    public static bool IsCompressionWorthwhile(
        int originalSize, int compressedSize, double minSavingsPercent = 10) =>
        (100 - GetCompressionRatio(originalSize, compressedSize)) >= minSavingsPercent;
}
