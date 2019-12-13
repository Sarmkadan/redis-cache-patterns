#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace RedisCachePatterns.Events;

/// <summary>
/// Provides validation helpers for <see cref="CacheHitEvent"/> instances
/// </summary>
public static class CacheHitEventValidation
{
    /// <summary>
    /// Validates a <see cref="CacheHitEvent"/> instance and returns a list of validation problems.
    /// </summary>
    /// <param name="value">The cache hit event to validate</param>
    /// <returns>A read-only list of human-readable validation problems; empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this CacheHitEvent? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(value.CacheKey))
        {
            problems.Add("CacheKey cannot be null or whitespace.");
        }

        if (value.DataSize < 0)
        {
            problems.Add("DataSize cannot be negative.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="CacheHitEvent"/> instance is valid.
    /// </summary>
    /// <param name="value">The cache hit event to check</param>
    /// <returns>True if the event is valid; otherwise, false</returns>
    public static bool IsValid(this CacheHitEvent? value)
    {
        return value is not null && Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that a <see cref="CacheHitEvent"/> instance is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The cache hit event to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if the event is invalid, containing a list of problems</exception>
    public static void EnsureValid(this CacheHitEvent? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"CacheHitEvent is invalid. Problems: {string.Join(" ", problems)}");
        }
    }
}