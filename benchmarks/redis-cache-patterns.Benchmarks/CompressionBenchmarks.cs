#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Utilities;

namespace RedisCachePatterns.Benchmarks;

/// <summary>
/// Benchmarks for GZIP compression applied to large cache entries.
/// Validates that ArrayPool-based implementation reduces allocations
/// compared to direct byte array allocation.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class CompressionBenchmarks
{
    private string _smallPayload = null!;
    private string _largePayload = null!;
    private byte[] _compressedSmall = null!;
    private byte[] _compressedLarge = null!;

    [GlobalSetup]
    public void Setup()
    {
        // ~300 B — below typical compression threshold
        _smallPayload = SerializationHelper.Serialize(new Product
        {
            Id = 1,
            Name = "Wireless Headphones",
            Sku = "WH-001",
            Price = 149.99m,
            StockQuantity = 50,
            Category = "Electronics",
        });

        // ~4 KB — a list of products, above compression threshold
        var products = Enumerable.Range(1, 20).Select(i => new Product
        {
            Id = i,
            Name = $"Product {i} — Premium Edition with Extended Warranty",
            Description = $"High-quality item #{i} with full manufacturer support and 2-year warranty",
            Sku = $"PROD-{i:D4}-PREM",
            Price = 49.99m + i,
            StockQuantity = 100 + i * 3,
            ReorderLevel = 20,
            Category = "General Merchandise",
            IsActive = true,
            Rating = 3.5 + (i % 3) * 0.5,
            ReviewCount = i * 47,
        }).ToList();

        _largePayload = SerializationHelper.Serialize(products);

        _compressedSmall = CompressionUtil.CompressString(_smallPayload);
        _compressedLarge = CompressionUtil.CompressString(_largePayload);
    }

    [Benchmark(Baseline = true, Description = "Compress small payload (~300 B)")]
    public byte[] CompressSmall() => CompressionUtil.CompressString(_smallPayload);

    [Benchmark(Description = "Compress large payload (~4 KB)")]
    public byte[] CompressLarge() => CompressionUtil.CompressString(_largePayload);

    [Benchmark(Description = "Decompress small payload")]
    public string DecompressSmall() => CompressionUtil.DecompressString(_compressedSmall);

    [Benchmark(Description = "Decompress large payload")]
    public string DecompressLarge() => CompressionUtil.DecompressString(_compressedLarge);

    [Benchmark(Description = "Round-trip compress + decompress (large)")]
    public string RoundTripLarge()
    {
        var compressed = CompressionUtil.CompressString(_largePayload);
        return CompressionUtil.DecompressString(compressed);
    }
}
