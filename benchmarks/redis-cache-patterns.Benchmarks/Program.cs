#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Running;
using RedisCachePatterns.Benchmarks;

BenchmarkRunner.Run(
[
    typeof(CacheKeyBenchmarks),
    typeof(SerializationBenchmarks),
    typeof(CompressionBenchmarks),
]);
