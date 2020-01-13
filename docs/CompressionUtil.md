# CompressionUtil

The `CompressionUtil` class provides a set of static utility methods for compressing and decompressing data using the GZip algorithm, specifically optimized for caching scenarios within the `redis-cache-patterns` project. It supports operations on both string and byte array inputs, includes functionality to calculate compression efficiency, and offers a heuristic method to determine if the overhead of compression is justified for a given payload size.

## API

### `CompressString`
Compresses a standard .NET string into a GZip-compressed byte array.
*   **Parameters:** `string input` – The uncompressed string to compress.
*   **Returns:** `byte[]` – The compressed binary data.
*   **Throws:** `ArgumentNullException` if `input` is null; `ArgumentException` if the string encoding fails.

### `CompressBytes` (Overload 1)
Compresses a raw byte array into a GZip-compressed byte array.
*   **Parameters:** `byte[] input` – The uncompressed binary data.
*   **Returns:** `byte[]` – The compressed binary data.
*   **Throws:** `ArgumentNullException` if `input` is null.

### `CompressBytes` (Overload 2)
Compresses a segment of a byte array into a GZip-compressed byte array.
*   **Parameters:** 
    *   `byte[] input` – The source binary data.
    *   `int offset` – The zero-based byte offset in `input` at which to begin compressing.
    *   `int count` – The number of bytes to compress.
*   **Returns:** `byte[]` – The compressed binary data.
*   **Throws:** `ArgumentNullException` if `input` is null; `ArgumentOutOfRangeException` if `offset` or `count` are invalid for the provided array.

### `DecompressString`
Decompresses a GZip-compressed byte array back into a UTF-8 string.
*   **Parameters:** `byte[] compressedData` – The compressed binary data.
*   **Returns:** `string` – The decompressed string.
*   **Throws:** `ArgumentNullException` if `compressedData` is null; `InvalidDataException` if the input is not a valid GZip stream.

### `DecompressBytes`
Decompresses a GZip-compressed byte array back into a raw byte array.
*   **Parameters:** `byte[] compressedData` – The compressed binary data.
*   **Returns:** `byte[]` – The decompressed binary data.
*   **Throws:** `ArgumentNullException` if `compressedData` is null; `InvalidDataException` if the input is not a valid GZip stream.

### `GetCompressionRatio`
Calculates the compression ratio achieved between original and compressed data sizes.
*   **Parameters:** 
    *   `int originalSize` – The size of the data before compression.
    *   `int compressedSize` – The size of the data after compression.
*   **Returns:** `double` – The ratio represented as `compressedSize / originalSize`. A value less than 1.0 indicates space savings.
*   **Throws:** `DivideByZeroException` if `originalSize` is 0.

### `IsCompressionWorthwhile`
Determines whether compressing a payload of a specific size is beneficial based on the overhead of the GZip header versus the potential size reduction.
*   **Parameters:** `int dataSize` – The size of the uncompressed data in bytes.
*   **Returns:** `bool` – `true` if compression is likely to reduce the total storage footprint; otherwise, `false`.
*   **Throws:** None.

## Usage

### Example 1: Caching a Large JSON Payload
This example demonstrates compressing a string before storing it in a cache and decompressing it upon retrieval.

```csharp
using System;
using System.Text;

public class CacheService
{
    public void StoreLargeObject(string key, string jsonPayload)
    {
        // Only compress if the utility deems it worthwhile
        if (CompressionUtil.IsCompressionWorthwhile(Encoding.UTF8.GetByteCount(jsonPayload)))
        {
            byte[] compressed = CompressionUtil.CompressString(jsonPayload);
            // Simulate cache set: cache.Set(key, compressed);
            Console.WriteLine($"Stored compressed payload of {compressed.Length} bytes.");
        }
        else
        {
            // Simulate cache set: cache.Set(key, Encoding.UTF8.GetBytes(jsonPayload));
            Console.WriteLine("Payload too small; storing uncompressed.");
        }
    }

    public string RetrieveObject(byte[] cachedData)
    {
        // Attempt to decompress; logic may vary based on metadata flags
        return CompressionUtil.DecompressString(cachedData);
    }
}
```

### Example 2: Analyzing Compression Efficiency
This example calculates the specific compression ratio for a binary blob to monitor cache efficiency metrics.

```csharp
using System;
using System.IO;

public class CompressionAnalyzer
{
    public void AnalyzeFile(Stream inputStream)
    {
        using (var ms = new MemoryStream())
        {
            inputStream.CopyTo(ms);
            byte[] originalBytes = ms.ToArray();
            
            byte[] compressedBytes = CompressionUtil.CompressBytes(originalBytes);
            
            double ratio = CompressionUtil.GetCompressionRatio(originalBytes.Length, compressedBytes.Length);
            
            Console.WriteLine($"Original: {originalBytes.Length} bytes");
            Console.WriteLine($"Compressed: {compressedBytes.Length} bytes");
            Console.WriteLine($"Ratio: {ratio:F2} ({(1 - ratio) * 100:F1}% reduction)");
        }
    }
}
```

## Notes

*   **Thread Safety:** All methods in `CompressionUtil` are static and stateless. They rely solely on local variables and standard library streams created within the method scope, making the class fully thread-safe for concurrent use without external locking.
*   **Small Payload Overhead:** GZip compression introduces a fixed header overhead (typically 18–20 bytes). For very small inputs, the compressed result may be larger than the original. Always utilize `IsCompressionWorthwhile` before compressing data in high-throughput caching layers to avoid negative performance impacts.
*   **Exception Handling:** Decompression methods assume the input is a valid GZip stream. If the data source is untrusted or potentially corrupted, wrap calls to `DecompressString` and `DecompressBytes` in a `try-catch` block handling `InvalidDataException`.
*   **Encoding:** `CompressString` and `DecompressString` strictly use UTF-8 encoding. Ensure that any string data passed to these methods is compatible with UTF-8 to prevent data loss during the round-trip.
