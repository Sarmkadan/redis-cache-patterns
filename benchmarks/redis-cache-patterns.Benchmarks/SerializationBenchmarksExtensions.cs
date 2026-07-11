#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Domain;

namespace RedisCachePatterns.Benchmarks;

/// <summary>
/// Extension methods for <see cref="SerializationBenchmarks"/> providing additional serialization scenarios
/// and helper methods for benchmarking different serialization approaches.
/// </summary>
public static class SerializationBenchmarksExtensions
{
    /// <summary>
    /// Serializes the product multiple times and returns the concatenated result.
    /// Useful for testing serialization throughput with multiple operations.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="count">Number of times to serialize the product. Must be positive.</param>
    /// <returns>Concatenated JSON strings.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is not positive.</exception>
    public static string SerializeProductMultiple(this SerializationBenchmarks benchmarks, int count)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        return string.Concat(Enumerable.Repeat(benchmarks.SerializeProduct(), count));
    }

    /// <summary>
    /// Deserializes the product multiple times and returns the last deserialized instance.
    /// Useful for testing deserialization throughput with multiple operations.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="count">Number of times to deserialize the product. Must be positive.</param>
    /// <returns>The last deserialized product.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is not positive.</exception>
    public static Product? DeserializeProductMultiple(this SerializationBenchmarks benchmarks, int count)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        Product? lastProduct = null;
        for (int i = 0; i < count; i++)
        {
            lastProduct = benchmarks.DeserializeProduct();
        }

        return lastProduct;
    }

    /// <summary>
    /// Serializes and immediately deserializes the product, returning the round-trip result.
    /// Useful for testing serialization/deserialization correctness.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>The deserialized product.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/>.</exception>
    public static Product? RoundTripProduct(this SerializationBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        var serialized = benchmarks.SerializeProduct();
        return benchmarks.DeserializeProduct();
    }

    /// <summary>
    /// Serializes and immediately deserializes the order, returning the round-trip result.
    /// Useful for testing serialization/deserialization correctness with complex objects.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>The deserialized order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/>.</exception>
    public static Order? RoundTripOrder(this SerializationBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        var serialized = benchmarks.SerializeOrder();
        return benchmarks.DeserializeOrder();
    }
}