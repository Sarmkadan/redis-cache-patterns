# CompressionBenchmarks

A benchmarking harness that measures the performance of compression and decompression operations on small and large string payloads. It provides dedicated benchmark methods for compressing, decompressing, and performing full round-trip transformations, enabling consistent profiling of the compression pipeline under varying data sizes.

## API

### `public void Setup()`

Prepares the benchmark environment before each iteration. Initializes the input data for both small and large payload scenarios, ensuring each benchmark method operates on consistent, pre-built content. This method is invoked automatically by the benchmarking infrastructure and is not called directly by user code.

- **Parameters:** None
- **Return value:** `void`
- **Exceptions:** No exceptions are documented for this method.

### `public byte[] CompressSmall()`

Compresses a small string payload and returns the resulting compressed byte array. Designed to measure compression throughput and allocation cost for compact data.

- **Parameters:** None
- **Return value:** `byte[]` containing the compressed representation of the small payload.
- **Exceptions:** May throw if the underlying compression library encounters invalid state or unsupported data, though the input is controlled by `Setup`.

### `public byte[] CompressLarge()`

Compresses a large string payload and returns the resulting compressed byte array. Designed to measure compression throughput and allocation cost for substantial data volumes.

- **Parameters:** None
- **Return value:** `byte[]` containing the compressed representation of the large payload.
- **Exceptions:** May throw if the underlying compression library encounters invalid state or unsupported data, though the input is controlled by `Setup`.

### `public string DecompressSmall()`

Decompresses the previously compressed small payload back to its original string form. Measures decompression speed and memory usage for small compressed data.

- **Parameters:** None
- **Return value:** `string` representing the decompressed original small payload.
- **Exceptions:** May throw if the compressed data is corrupted, truncated, or incompatible with the decompression algorithm.

### `public string DecompressLarge()`

Decompresses the previously compressed large payload back to its original string form. Measures decompression speed and memory usage for large compressed data.

- **Parameters:** None
- **Return value:** `string` representing the decompressed original large payload.
- **Exceptions:** May throw if the compressed data is corrupted, truncated, or incompatible with the decompression algorithm.

### `public string RoundTripLarge()`

Performs a full compress-then-decompress cycle on the large payload in a single measured operation. Captures the end-to-end cost of compression followed immediately by decompression, reflecting realistic caching workflows where data is stored compressed and retrieved for decompression.

- **Parameters:** None
- **Return value:** `string` representing the decompressed original large payload after the round-trip.
- **Exceptions:** May throw at either the compression or decompression stage under the same conditions as `CompressLarge` and `DecompressLarge`.

## Usage

```csharp
// Running the benchmarks via BenchmarkDotNet
var summary = BenchmarkRunner.Run<CompressionBenchmarks>();

// Accessing results programmatically
foreach (var report in summary.Reports)
{
    Console.WriteLine($"{report.BenchmarkCase.Descriptor.WorkloadMethod.Name}: "
                      + $"{report.ResultStatistics.Mean} ns");
}
```

```csharp
// Manual invocation for ad-hoc profiling (bypasses benchmark infrastructure)
var benchmarks = new CompressionBenchmarks();
benchmarks.Setup();

byte[] compressedSmall = benchmarks.CompressSmall();
string decompressedSmall = benchmarks.DecompressSmall();

byte[] compressedLarge = benchmarks.CompressLarge();
string roundTripped = benchmarks.RoundTripLarge();

Console.WriteLine($"Small payload compressed to {compressedSmall.Length} bytes");
Console.WriteLine($"Round-trip successful: {roundTripped.Length} chars");
```

## Notes

- **State dependency:** All benchmark methods depend on state initialized by `Setup`. Calling any benchmark method without prior invocation of `Setup` will result in null-reference exceptions or undefined behavior, as the internal payload fields will not be populated.
- **Thread safety:** This class is not designed for concurrent use. Benchmark methods mutate or read internal state without synchronization. Concurrent invocation from multiple threads will cause data races and unpredictable results.
- **Input consistency:** The small and large payloads are fixed strings generated during `Setup`. Changing the compression algorithm or payload content requires modifying the `Setup` implementation; the public API surface does not accept custom input.
- **Exception surface:** Exceptions are propagated directly from the underlying compression library. No custom exception wrapping or recovery logic is present in the benchmark methods.
- **Return value validity:** `DecompressSmall`, `DecompressLarge`, and `RoundTripLarge` return strings that should match the original payloads exactly. Any mismatch indicates a defect in the compression round-trip logic or data corruption.
