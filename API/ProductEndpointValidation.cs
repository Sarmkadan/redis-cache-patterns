#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.API;

/// <summary>
/// Validation helpers for ProductEndpoint operations
/// </summary>
public static class ProductEndpointValidation
{
    /// <summary>
    /// Validates a <see cref="ProductEndpoint"/> instance and returns a list of validation errors.
    /// </summary>
    /// <remarks>
    /// ProductEndpoint is a stateless service class that delegates all validation to its method parameters.
    /// This validation method always returns an empty list since there are no instance fields to validate.
    /// </remarks>
    /// <param name="value">The <see cref="ProductEndpoint"/> instance to validate.</param>
    /// <returns>List of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this ProductEndpoint value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return Array.Empty<string>();
    }

    /// <summary>
    /// Checks if a <see cref="ProductEndpoint"/> instance is valid.
    /// </summary>
    /// <param name="value">The <see cref="ProductEndpoint"/> instance to check.</param>
    /// <returns>True if valid; false otherwise.</returns>
    public static bool IsValid(this ProductEndpoint value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that a <see cref="ProductEndpoint"/> instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The <see cref="ProductEndpoint"/> instance to validate.</param>
    /// <exception cref="ArgumentException">Thrown if value is invalid with detailed error messages.</exception>
    public static void EnsureValid(this ProductEndpoint value)
    {
        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"ProductEndpoint validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }
}