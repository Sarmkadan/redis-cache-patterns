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
    /// Validates a ProductEndpoint instance and returns a list of validation errors
    /// </summary>
    /// <param name="value">The ProductEndpoint instance to validate</param>
    /// <returns>List of human-readable validation problems; empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public static IReadOnlyList<string> Validate(this ProductEndpoint value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // ProductEndpoint has no instance state to validate
        // All validation is performed on method parameters when methods are called

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Checks if a ProductEndpoint instance is valid
    /// </summary>
    /// <param name="value">The ProductEndpoint instance to check</param>
    /// <returns>True if valid; false otherwise</returns>
    public static bool IsValid(this ProductEndpoint value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that a ProductEndpoint instance is valid, throwing an exception if not
    /// </summary>
    /// <param name="value">The ProductEndpoint instance to validate</param>
    /// <exception cref="ArgumentException">Thrown if value is invalid with detailed error messages</exception>
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