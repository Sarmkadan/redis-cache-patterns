// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.RegularExpressions;
using RedisCachePatterns.Configuration;
using RedisCachePatterns.Exceptions;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Utility for validating business entities
/// </summary>
public static class ValidationHelper
{
    private static readonly Regex EmailRegex = new(
        @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public static void ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ValidationException("Username cannot be empty");

        if (username.Length < AppConstants.Validation.MIN_USERNAME_LENGTH)
            throw new ValidationException($"Username must be at least {AppConstants.Validation.MIN_USERNAME_LENGTH} characters");

        if (username.Length > AppConstants.Validation.MAX_USERNAME_LENGTH)
            throw new ValidationException($"Username cannot exceed {AppConstants.Validation.MAX_USERNAME_LENGTH} characters");
    }

    public static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("Email cannot be empty");

        if (!EmailRegex.IsMatch(email))
            throw new ValidationException("Invalid email format");
    }

    public static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ValidationException("Password cannot be empty");

        if (password.Length < AppConstants.Validation.MIN_PASSWORD_LENGTH)
            throw new ValidationException($"Password must be at least {AppConstants.Validation.MIN_PASSWORD_LENGTH} characters");
    }

    public static void ValidateProductName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Product name cannot be empty");

        if (name.Length < AppConstants.Validation.MIN_PRODUCT_NAME_LENGTH)
            throw new ValidationException($"Product name must be at least {AppConstants.Validation.MIN_PRODUCT_NAME_LENGTH} characters");

        if (name.Length > AppConstants.Validation.MAX_PRODUCT_NAME_LENGTH)
            throw new ValidationException($"Product name cannot exceed {AppConstants.Validation.MAX_PRODUCT_NAME_LENGTH} characters");
    }

    public static void ValidatePrice(decimal price)
    {
        if (price < AppConstants.Validation.MIN_PRICE)
            throw new ValidationException("Price cannot be negative");

        if (price > AppConstants.Validation.MAX_PRICE)
            throw new ValidationException($"Price cannot exceed {AppConstants.Validation.MAX_PRICE}");
    }

    public static void ValidateQuantity(int quantity, string fieldName = "Quantity")
    {
        if (quantity <= 0)
            throw new ValidationException($"{fieldName} must be greater than zero");
    }

    public static void ValidateNotNull<T>(T? value, string fieldName) where T : class
    {
        if (value == null)
            throw new ValidationException($"{fieldName} cannot be null");
    }

    public static void ValidateNotNullOrEmpty(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException($"{fieldName} cannot be empty");
    }

    public static Dictionary<string, List<string>> GetValidationErrors(Action validator)
    {
        var errors = new Dictionary<string, List<string>>();
        try
        {
            validator();
        }
        catch (ValidationException ex)
        {
            if (ex.Errors.Any())
            {
                errors = ex.Errors;
            }
            else
            {
                errors["general"] = new List<string> { ex.Message };
            }
        }
        catch (Exception ex)
        {
            errors["general"] = new List<string> { ex.Message };
        }

        return errors;
    }
}
