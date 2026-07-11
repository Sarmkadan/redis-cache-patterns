#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace RedisCachePatterns.Events;

/// <summary>
/// Provides validation helpers for <see cref="OrderCreatedEvent"/> instances
/// </summary>
public static class OrderCreatedEventValidation
{
    /// <summary>
    /// Validates an <see cref="OrderCreatedEvent"/> instance and returns a list of validation errors.
    /// </summary>
    /// <param name="value">The event to validate</param>
    /// <returns>An empty list if valid; otherwise, a list of human-readable error messages</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this OrderCreatedEvent? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate OrderId
        if (value.OrderId <= 0)
        {
            errors.Add($"OrderId must be a positive integer, but was {value.OrderId}.");
        }

        // Validate UserId
        if (value.UserId <= 0)
        {
            errors.Add($"UserId must be a positive integer, but was {value.UserId}.");
        }

        // Validate TotalAmount
        if (value.TotalAmount <= 0m)
        {
            errors.Add(string.Create(CultureInfo.InvariantCulture, $"TotalAmount must be a positive decimal, but was {value.TotalAmount:F2}."));
        }
        else if (value.TotalAmount > 1_000_000m)
        {
            errors.Add(string.Create(CultureInfo.InvariantCulture, $"TotalAmount exceeds maximum allowed value of 1,000,000.00, but was {value.TotalAmount:F2}."));
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="OrderCreatedEvent"/> is valid.
    /// </summary>
    /// <param name="value">The event to check</param>
    /// <returns>True if the event is valid; otherwise, false</returns>
    public static bool IsValid(this OrderCreatedEvent? value)
    {
        return value is not null && Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="OrderCreatedEvent"/> is valid, throwing an <see cref="ArgumentException"/> if it is not.
    /// </summary>
    /// <param name="value">The event to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when the event contains validation errors</exception>
    public static void EnsureValid(this OrderCreatedEvent? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            var errorMessage = string.Join("\n- ", errors);
            throw new ArgumentException($"OrderCreatedEvent is invalid. Validation errors:\n- {errorMessage}");
        }
    }
}