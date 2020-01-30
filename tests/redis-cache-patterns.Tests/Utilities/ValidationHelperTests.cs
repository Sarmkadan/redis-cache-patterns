#nullable enable
using FluentAssertions;
using RedisCachePatterns.Exceptions;
using RedisCachePatterns.Utilities;
using Xunit;

namespace RedisCachePatterns.Tests.Utilities;

/// <summary>
/// Provides unit tests for the <see cref="ValidationHelper"/> class.
/// Tests various validation methods including username, email, password, product name, price, quantity,
/// and general validation helper methods to ensure proper validation behavior and exception throwing.
/// </summary>
public class ValidationHelperTests
{
    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateUsername"/> does not throw an exception
    /// when provided with a valid username.
    /// </summary>
    public void ValidateUsername_WithValidUsername_DoesNotThrow()
    {
        Action act = () => ValidationHelper.ValidateUsername("validuser");
        act.Should().NotThrow();
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateUsername"/> throws a <see cref="ValidationException"/>
    /// when provided with a null username.
    /// </summary>
    public void ValidateUsername_WithNull_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateUsername(null!);
        act.Should().Throw<ValidationException>()
        .WithMessage("*empty*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateUsername"/> throws a <see cref="ValidationException"/>
    /// when provided with an empty username.
    /// </summary>
    public void ValidateUsername_WithEmpty_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateUsername("");
        act.Should().Throw<ValidationException>()
        .WithMessage("*empty*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateUsername"/> throws a <see cref="ValidationException"/>
    /// when provided with a whitespace-only username.
    /// </summary>
    public void ValidateUsername_WithWhitespace_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateUsername(" ");
        act.Should().Throw<ValidationException>()
        .WithMessage("*empty*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateUsername"/> throws a <see cref="ValidationException"/>
    /// when provided with a username that is too short (less than 3 characters).
    /// </summary>
    public void ValidateUsername_WithTooShort_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateUsername("ab");
        act.Should().Throw<ValidationException>()
        .WithMessage("*at least*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateUsername"/> throws a <see cref="ValidationException"/>
    /// when provided with a username that exceeds the maximum length (255 characters).
    /// </summary>
    public void ValidateUsername_WithTooLong_ThrowsValidationException()
    {
        var longName = new string('a', 256);
        Action act = () => ValidationHelper.ValidateUsername(longName);
        act.Should().Throw<ValidationException>()
        .WithMessage("*exceed*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateEmail"/> does not throw an exception
    /// when provided with a valid email address.
    /// </summary>
    public void ValidateEmail_WithValidEmail_DoesNotThrow()
    {
        Action act = () => ValidationHelper.ValidateEmail("user@example.com");
        act.Should().NotThrow();
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateEmail"/> throws a <see cref="ValidationException"/>
    /// when provided with an invalid email format.
    /// </summary>
    public void ValidateEmail_WithInvalidFormat_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateEmail("notanemail");
        act.Should().Throw<ValidationException>()
        .WithMessage("*Invalid email*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateEmail"/> throws a <see cref="ValidationException"/>
    /// when provided with an email missing the @ symbol.
    /// </summary>
    public void ValidateEmail_WithMissingAt_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateEmail("userexample.com");
        act.Should().Throw<ValidationException>()
        .WithMessage("*Invalid email*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateEmail"/> throws a <see cref="ValidationException"/>
    /// when provided with an email missing the domain part after @.
    /// </summary>
    public void ValidateEmail_WithMissingDomain_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateEmail("user@");
        act.Should().Throw<ValidationException>()
        .WithMessage("*Invalid email*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateEmail"/> throws a <see cref="ValidationException"/>
    /// when provided with a null email.
    /// </summary>
    public void ValidateEmail_WithNull_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateEmail(null!);
        act.Should().Throw<ValidationException>()
        .WithMessage("*empty*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateEmail"/> throws a <see cref="ValidationException"/>
    /// when provided with an empty email.
    /// </summary>
    public void ValidateEmail_WithEmpty_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateEmail("");
        act.Should().Throw<ValidationException>()
        .WithMessage("*empty*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidatePassword"/> does not throw an exception
    /// when provided with a valid password.
    /// </summary>
    public void ValidatePassword_WithValidPassword_DoesNotThrow()
    {
        Action act = () => ValidationHelper.ValidatePassword("SecurePassword123");
        act.Should().NotThrow();
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidatePassword"/> throws a <see cref="ValidationException"/>
    /// when provided with a null password.
    /// </summary>
    public void ValidatePassword_WithNull_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidatePassword(null!);
        act.Should().Throw<ValidationException>()
        .WithMessage("*empty*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidatePassword"/> throws a <see cref="ValidationException"/>
    /// when provided with an empty password.
    /// </summary>
    public void ValidatePassword_WithEmpty_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidatePassword("");
        act.Should().Throw<ValidationException>()
        .WithMessage("*empty*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidatePassword"/> throws a <see cref="ValidationException"/>
    /// when provided with a password that is too short (less than 8 characters).
    /// </summary>
    public void ValidatePassword_WithTooShort_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidatePassword("short");
        act.Should().Throw<ValidationException>()
        .WithMessage("*at least*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateProductName"/> does not throw an exception
    /// when provided with a valid product name.
    /// </summary>
    public void ValidateProductName_WithValidName_DoesNotThrow()
    {
        Action act = () => ValidationHelper.ValidateProductName("Premium Widget");
        act.Should().NotThrow();
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateProductName"/> throws a <see cref="ValidationException"/>
    /// when provided with a null product name.
    /// </summary>
    public void ValidateProductName_WithNull_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateProductName(null!);
        act.Should().Throw<ValidationException>()
        .WithMessage("*empty*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateProductName"/> throws a <see cref="ValidationException"/>
    /// when provided with an empty product name.
    /// </summary>
    public void ValidateProductName_WithEmpty_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateProductName("");
        act.Should().Throw<ValidationException>()
        .WithMessage("*empty*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateProductName"/> throws a <see cref="ValidationException"/>
    /// when provided with a product name that is too short (less than 2 characters).
    /// </summary>
    public void ValidateProductName_WithTooShort_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateProductName("A");
        act.Should().Throw<ValidationException>()
        .WithMessage("*at least*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateProductName"/> throws a <see cref="ValidationException"/>
    /// when provided with a product name that exceeds the maximum length (255 characters).
    /// </summary>
    public void ValidateProductName_WithTooLong_ThrowsValidationException()
    {
        var longName = new string('a', 256);
        Action act = () => ValidationHelper.ValidateProductName(longName);
        act.Should().Throw<ValidationException>()
        .WithMessage("*exceed*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidatePrice"/> does not throw an exception
    /// when provided with a valid positive price.
    /// </summary>
    public void ValidatePrice_WithValidPrice_DoesNotThrow()
    {
        Action act = () => ValidationHelper.ValidatePrice(99.99m);
        act.Should().NotThrow();
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidatePrice"/> does not throw an exception
    /// when provided with a zero price (edge case for free items).
    /// </summary>
    public void ValidatePrice_WithZeroPrice_DoesNotThrow()
    {
        Action act = () => ValidationHelper.ValidatePrice(0m);
        act.Should().NotThrow();
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidatePrice"/> throws a <see cref="ValidationException"/>
    /// when provided with a negative price.
    /// </summary>
    public void ValidatePrice_WithNegativePrice_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidatePrice(-10m);
        act.Should().Throw<ValidationException>()
        .WithMessage("*negative*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidatePrice"/> throws a <see cref="ValidationException"/>
    /// when provided with a price that exceeds the maximum allowed value.
    /// </summary>
    public void ValidatePrice_WithTooHighPrice_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidatePrice(decimal.MaxValue);
        act.Should().Throw<ValidationException>()
        .WithMessage("*exceed*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateQuantity"/> does not throw an exception
    /// when provided with a positive quantity.
    /// </summary>
    public void ValidateQuantity_WithPositiveQuantity_DoesNotThrow()
    {
        Action act = () => ValidationHelper.ValidateQuantity(10);
        act.Should().NotThrow();
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateQuantity"/> throws a <see cref="ValidationException"/>
    /// when provided with a zero quantity.
    /// </summary>
    public void ValidateQuantity_WithZeroQuantity_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateQuantity(0);
        act.Should().Throw<ValidationException>()
        .WithMessage("*greater than zero*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateQuantity"/> throws a <see cref="ValidationException"/>
    /// when provided with a negative quantity.
    /// </summary>
    public void ValidateQuantity_WithNegativeQuantity_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateQuantity(-5);
        act.Should().Throw<ValidationException>()
        .WithMessage("*greater than zero*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateQuantity"/> throws a <see cref="ValidationException"/>
    /// and includes the custom field name in the error message when validation fails.
    /// </summary>
    /// <param name="value">The quantity value to validate.</param>
    /// <param name="fieldName">The name of the field being validated, used in error messages.</param>
    public void ValidateQuantity_WithCustomFieldName_IncludesFieldNameInMessage()
    {
        Action act = () => ValidationHelper.ValidateQuantity(-1, "Stock");
        act.Should().Throw<ValidationException>()
        .WithMessage("*Stock*greater than zero*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateNotNull"/> throws a <see cref="ValidationException"/>
    /// when provided with a null value.
    /// </summary>
    /// <typeparam name="T">The type of object being validated.</typeparam>
    /// <param name="value">The value to validate, which is null.</param>
    /// <param name="fieldName">The name of the field being validated.</param>
    public void ValidateNotNull_WithNullValue_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateNotNull<object>(null, "TestField");
        act.Should().Throw<ValidationException>()
        .WithMessage("*TestField*cannot be null*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateNotNull"/> does not throw an exception
    /// when provided with a non-null value.
    /// </summary>
    public void ValidateNotNull_WithNonNullValue_DoesNotThrow()
    {
        var obj = new object();
        Action act = () => ValidationHelper.ValidateNotNull(obj, "TestField");
        act.Should().NotThrow();
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateNotNullOrEmpty"/> does not throw an exception
    /// when provided with a valid non-empty string.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="fieldName">The name of the field being validated.</param>
    public void ValidateNotNullOrEmpty_WithValidString_DoesNotThrow()
    {
        Action act = () => ValidationHelper.ValidateNotNullOrEmpty("valid", "Field");
        act.Should().NotThrow();
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateNotNullOrEmpty"/> throws a <see cref="ValidationException"/>
    /// when provided with a null string.
    /// </summary>
    /// <param name="value">The string value to validate, which is null.</param>
    /// <param name="fieldName">The name of the field being validated.</param>
    public void ValidateNotNullOrEmpty_WithNull_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateNotNullOrEmpty(null, "Field");
        act.Should().Throw<ValidationException>()
        .WithMessage("*Field*empty*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateNotNullOrEmpty"/> throws a <see cref="ValidationException"/>
    /// when provided with an empty string.
    /// </summary>
    /// <param name="value">The string value to validate, which is empty.</param>
    /// <param name="fieldName">The name of the field being validated.</param>
    public void ValidateNotNullOrEmpty_WithEmpty_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateNotNullOrEmpty("", "Field");
        act.Should().Throw<ValidationException>()
        .WithMessage("*Field*empty*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateNotNullOrEmpty"/> throws a <see cref="ValidationException"/>
    /// when provided with a whitespace-only string.
    /// </summary>
    /// <param name="value">The string value to validate, which contains only whitespace.</param>
    /// <param name="fieldName">The name of the field being validated.</param>
    public void ValidateNotNullOrEmpty_WithWhitespace_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateNotNullOrEmpty(" ", "Field");
        act.Should().Throw<ValidationException>()
        .WithMessage("*Field*empty*");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.GetValidationErrors"/> returns an empty dictionary
    /// when validation succeeds without throwing exceptions.
    /// </summary>
    public void GetValidationErrors_WithSuccessfulValidation_ReturnsEmptyDictionary()
    {
        var errors = ValidationHelper.GetValidationErrors(() =>
        {
            ValidationHelper.ValidateUsername("validuser");
        });

        errors.Should().BeEmpty();
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.GetValidationErrors"/> returns a dictionary with errors
    /// when a <see cref="ValidationException"/> is thrown during validation.
    /// </summary>
    public void GetValidationErrors_WithValidationException_ReturnsErrors()
    {
        var errors = ValidationHelper.GetValidationErrors(() =>
        {
            ValidationHelper.ValidateUsername("");
        });

        errors.Should().NotBeEmpty();
        errors.Should().ContainKey("general");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.GetValidationErrors"/> captures and returns general exceptions
    /// that occur during validation.
    /// </summary>
    public void GetValidationErrors_WithGeneralException_CapturesError()
    {
        var errors = ValidationHelper.GetValidationErrors(() =>
        {
            throw new InvalidOperationException("Test error");
        });

        errors.Should().NotBeEmpty();
        errors["general"].Should().Contain("Test error");
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="ValidationHelper.ValidateEmail"/> accepts various valid email formats
    /// including simple addresses, addresses with dots, plus tags, and different domains.
    /// </summary>
    public void ValidateEmail_WithVariousValidFormats_AllPass()
    {
        var validEmails = new[]
        {
            "simple@example.com",
            "user.name@example.com",
            "user+tag@example.co.uk",
            "test123@domain.org"
        };

        foreach (var email in validEmails)
        {
            Action act = () => ValidationHelper.ValidateEmail(email);
            act.Should().NotThrow($"Email {email} should be valid");
        }
    }
}