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
/// Benchmarks for cache key construction — called on every cache operation,
/// so allocation efficiency matters at high throughput.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class CacheKeyBenchmarks
{
    [Benchmark(Baseline = true, Description = "User key (2 segments)")]
    public string UserKey() => CacheKeyBuilder.User(12345);

    [Benchmark(Description = "Product key (2 segments)")]
    public string ProductKey() => CacheKeyBuilder.Product(99);

    [Benchmark(Description = "Inventory key (5 segments)")]
    public string InventoryKey() =>
        CacheKeyBuilder.InventoryByProductAndWarehouse(42, "warehouse-east");

    [Benchmark(Description = "Generic BuildKey — 4 mixed parts")]
    public string GenericBuildKey() =>
        CacheKeyBuilder.BuildKey("orders", "status", "shipped", 7);

    [Benchmark(Description = "Entity key via CacheKeyHelper")]
    public string EntityKey() => CacheKeyHelper.BuildEntityKey<Product>(99);

    [Benchmark(Description = "Wildcard pattern via CacheKeyHelper")]
    public string PatternKey() =>
        CacheKeyHelper.BuildPattern("product", "category", "electronics");

    [Benchmark(Description = "Collection key via CacheKeyHelper")]
    public string CollectionKey() => CacheKeyHelper.BuildCollectionKey<Product>("active");
}
