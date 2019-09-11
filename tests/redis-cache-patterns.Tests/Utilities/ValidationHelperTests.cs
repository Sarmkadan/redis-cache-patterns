#nullable enable
using FluentAssertions;
using RedisCachePatterns.Exceptions;
using RedisCachePatterns.Utilities;
using Xunit;

namespace RedisCachePatterns.Tests.Utilities;

public class ValidationHelperTests
{
    [Fact]
    public void ValidateUsername_WithValidUsername_DoesNotThrow()
    {
        Action act = () => ValidationHelper.ValidateUsername("validuser");
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateUsername_WithNull_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateUsername(null!);
        act.Should().Throw<ValidationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void ValidateUsername_WithEmpty_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateUsername("");
        act.Should().Throw<ValidationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void ValidateUsername_WithWhitespace_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateUsername("   ");
        act.Should().Throw<ValidationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void ValidateUsername_WithTooShort_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateUsername("ab");
        act.Should().Throw<ValidationException>()
            .WithMessage("*at least*");
    }

    [Fact]
    public void ValidateUsername_WithTooLong_ThrowsValidationException()
    {
        var longName = new string('a', 256);
        Action act = () => ValidationHelper.ValidateUsername(longName);
        act.Should().Throw<ValidationException>()
            .WithMessage("*exceed*");
    }

    [Fact]
    public void ValidateEmail_WithValidEmail_DoesNotThrow()
    {
        Action act = () => ValidationHelper.ValidateEmail("user@example.com");
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateEmail_WithInvalidFormat_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateEmail("notanemail");
        act.Should().Throw<ValidationException>()
            .WithMessage("*Invalid email*");
    }

    [Fact]
    public void ValidateEmail_WithMissingAt_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateEmail("userexample.com");
        act.Should().Throw<ValidationException>()
            .WithMessage("*Invalid email*");
    }

    [Fact]
    public void ValidateEmail_WithMissingDomain_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateEmail("user@");
        act.Should().Throw<ValidationException>()
            .WithMessage("*Invalid email*");
    }

    [Fact]
    public void ValidateEmail_WithNull_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateEmail(null!);
        act.Should().Throw<ValidationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void ValidateEmail_WithEmpty_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateEmail("");
        act.Should().Throw<ValidationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void ValidatePassword_WithValidPassword_DoesNotThrow()
    {
        Action act = () => ValidationHelper.ValidatePassword("SecurePassword123");
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidatePassword_WithNull_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidatePassword(null!);
        act.Should().Throw<ValidationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void ValidatePassword_WithEmpty_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidatePassword("");
        act.Should().Throw<ValidationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void ValidatePassword_WithTooShort_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidatePassword("short");
        act.Should().Throw<ValidationException>()
            .WithMessage("*at least*");
    }

    [Fact]
    public void ValidateProductName_WithValidName_DoesNotThrow()
    {
        Action act = () => ValidationHelper.ValidateProductName("Premium Widget");
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateProductName_WithNull_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateProductName(null!);
        act.Should().Throw<ValidationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void ValidateProductName_WithEmpty_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateProductName("");
        act.Should().Throw<ValidationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void ValidateProductName_WithTooShort_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateProductName("A");
        act.Should().Throw<ValidationException>()
            .WithMessage("*at least*");
    }

    [Fact]
    public void ValidateProductName_WithTooLong_ThrowsValidationException()
    {
        var longName = new string('a', 256);
        Action act = () => ValidationHelper.ValidateProductName(longName);
        act.Should().Throw<ValidationException>()
            .WithMessage("*exceed*");
    }

    [Fact]
    public void ValidatePrice_WithValidPrice_DoesNotThrow()
    {
        Action act = () => ValidationHelper.ValidatePrice(99.99m);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidatePrice_WithZeroPrice_DoesNotThrow()
    {
        Action act = () => ValidationHelper.ValidatePrice(0m);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidatePrice_WithNegativePrice_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidatePrice(-10m);
        act.Should().Throw<ValidationException>()
            .WithMessage("*negative*");
    }

    [Fact]
    public void ValidatePrice_WithTooHighPrice_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidatePrice(decimal.MaxValue);
        act.Should().Throw<ValidationException>()
            .WithMessage("*exceed*");
    }

    [Fact]
    public void ValidateQuantity_WithPositiveQuantity_DoesNotThrow()
    {
        Action act = () => ValidationHelper.ValidateQuantity(10);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateQuantity_WithZeroQuantity_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateQuantity(0);
        act.Should().Throw<ValidationException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public void ValidateQuantity_WithNegativeQuantity_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateQuantity(-5);
        act.Should().Throw<ValidationException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public void ValidateQuantity_WithCustomFieldName_IncludesFieldNameInMessage()
    {
        Action act = () => ValidationHelper.ValidateQuantity(-1, "Stock");
        act.Should().Throw<ValidationException>()
            .WithMessage("*Stock*greater than zero*");
    }

    [Fact]
    public void ValidateNotNull_WithNullValue_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateNotNull<object>(null, "TestField");
        act.Should().Throw<ValidationException>()
            .WithMessage("*TestField*cannot be null*");
    }

    [Fact]
    public void ValidateNotNull_WithNonNullValue_DoesNotThrow()
    {
        var obj = new object();
        Action act = () => ValidationHelper.ValidateNotNull(obj, "TestField");
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateNotNullOrEmpty_WithValidString_DoesNotThrow()
    {
        Action act = () => ValidationHelper.ValidateNotNullOrEmpty("valid", "Field");
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateNotNullOrEmpty_WithNull_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateNotNullOrEmpty(null, "Field");
        act.Should().Throw<ValidationException>()
            .WithMessage("*Field*empty*");
    }

    [Fact]
    public void ValidateNotNullOrEmpty_WithEmpty_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateNotNullOrEmpty("", "Field");
        act.Should().Throw<ValidationException>()
            .WithMessage("*Field*empty*");
    }

    [Fact]
    public void ValidateNotNullOrEmpty_WithWhitespace_ThrowsValidationException()
    {
        Action act = () => ValidationHelper.ValidateNotNullOrEmpty("   ", "Field");
        act.Should().Throw<ValidationException>()
            .WithMessage("*Field*empty*");
    }

    [Fact]
    public void GetValidationErrors_WithSuccessfulValidation_ReturnsEmptyDictionary()
    {
        var errors = ValidationHelper.GetValidationErrors(() =>
        {
            ValidationHelper.ValidateUsername("validuser");
        });

        errors.Should().BeEmpty();
    }

    [Fact]
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
