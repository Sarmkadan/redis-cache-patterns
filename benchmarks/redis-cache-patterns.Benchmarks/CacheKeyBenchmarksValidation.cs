#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;

namespace RedisCachePatterns.Benchmarks;

/// <summary>
/// Validation helpers for <see cref="CacheKeyBenchmarks"/> cache key benchmarks.
/// Validates that generated keys follow expected patterns and constraints.
/// </summary>
public static class CacheKeyBenchmarksValidation
{
    /// <summary>
    /// Validates a <see cref="CacheKeyBenchmarks"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The benchmarks instance to validate.</param>
    /// <returns>An empty list if valid; otherwise, a list of validation error messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this CacheKeyBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        ValidateKey(value.UserKey(), nameof(value.UserKey), errors,
            expectedPrefix: "user",
            minLength: 5, // "user:12345"
            maxLength: 15);

        ValidateKey(value.ProductKey(), nameof(value.ProductKey), errors,
            expectedPrefix: "product",
            minLength: 9, // "product:99"
            maxLength: 15);

        ValidateKey(value.InventoryKey(), nameof(value.InventoryKey), errors,
            expectedPrefix: "inventory",
            minSegments: 5, // "inventory:product:42:warehouse:warehouse-east"
            minLength: 30);

        ValidateKey(value.GenericBuildKey(), nameof(value.GenericBuildKey), errors,
            expectedPrefix: "orders",
            minSegments: 4, // "orders:status:shipped:7"
            minLength: 20);

        ValidateKey(value.EntityKey(), nameof(value.EntityKey), errors,
            expectedPrefix: "product",
            minSegments: 3, // "product:entity:99"
            minLength: 15);

        ValidateKey(value.PatternKey(), nameof(value.PatternKey), errors,
            expectedSuffix: ":*",
            minLength: 15); // "product:category:electronics:*"

        ValidateKey(value.CollectionKey(), nameof(value.CollectionKey), errors,
            expectedPrefix: "product",
            minSegments: 2, // "product:collection:active"
            minLength: 20);

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="CacheKeyBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmarks instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this CacheKeyBenchmarks value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="CacheKeyBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmarks instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is invalid, containing a list of validation errors.</exception>
    public static void EnsureValid(this CacheKeyBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"CacheKeyBenchmarks validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    private static void ValidateKey(
        string? key,
        string memberName,
        List<string> errors,
        string? expectedPrefix = null,
        string? expectedSuffix = null,
        int? minSegments = null,
        int? minLength = null,
        int? maxLength = null)
    {
        if (string.IsNullOrEmpty(key))
        {
            errors.Add($"{memberName}: Key is null or empty");
            return;
        }

        if (key.Contains('\n') || key.Contains('\r'))
        {
            errors.Add($"{memberName}: Key contains invalid newline characters");
        }

        if (key.Length > 512)
        {
            errors.Add($"{memberName}: Key exceeds maximum length of 512 characters (actual: {key.Length})");
        }

        if (expectedPrefix != null && !key.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            errors.Add($"{memberName}: Key does not start with expected prefix '{expectedPrefix}' (actual: '{key.Split(':')[0]}')");
        }

        if (expectedSuffix != null && !key.EndsWith(expectedSuffix, StringComparison.Ordinal))
        {
            errors.Add($"{memberName}: Key does not end with expected suffix '{expectedSuffix}' (actual: '{key[^expectedSuffix.Length..]}')");
        }

        if (minLength.HasValue && key.Length < minLength.Value)
        {
            errors.Add($"{memberName}: Key is too short (minimum: {minLength.Value} characters, actual: {key.Length})");
        }

        if (maxLength.HasValue && key.Length > maxLength.Value)
        {
            errors.Add($"{memberName}: Key exceeds maximum length (maximum: {maxLength.Value} characters, actual: {key.Length})");
        }

        var segments = key.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (minSegments.HasValue && segments.Length < minSegments.Value)
        {
            errors.Add($"{memberName}: Key has insufficient segments (minimum: {minSegments.Value} segments, actual: {segments.Length})");
        }

        if (segments.Length > 0 && string.IsNullOrWhiteSpace(segments[0]))
        {
            errors.Add($"{memberName}: Key has empty prefix segment");
        }
    }
}
