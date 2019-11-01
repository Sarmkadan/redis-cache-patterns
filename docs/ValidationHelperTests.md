# ValidationHelperTests

The `ValidationHelperTests` class serves as the comprehensive unit test suite for the `ValidationHelper` utility within the `redis-cache-patterns` project. It rigorously verifies the correctness of input validation logic for critical data entities such as usernames, email addresses, passwords, and product names. By covering both valid inputs and a wide spectrum of invalid scenarios (including nulls, empty strings, whitespace, and length constraints), this class ensures that the underlying validation helpers consistently enforce data integrity rules and throw appropriate `ValidationException` instances when constraints are violated.

## API

The following public members define the test cases implemented in this class. These methods do not accept parameters or return values in the traditional sense; instead, they utilize a testing framework (such as xUnit or NUnit) to assert behavior.

### Username Validation Tests

*   **`ValidateUsername_WithValidUsername_DoesNotThrow`**
    Verifies that a username meeting all length and character requirements passes validation without raising an exception.

*   **`ValidateUsername_WithNull_ThrowsValidationException`**
    Confirms that passing a `null` username triggers a `ValidationException`.

*   **`ValidateUsername_WithEmpty_ThrowsValidationException`**
    Confirms that passing an empty string (`""`) triggers a `ValidationException`.

*   **`ValidateUsername_WithWhitespace_ThrowsValidationException`**
    Confirms that passing a string containing only whitespace characters triggers a `ValidationException`.

*   **`ValidateUsername_WithTooShort_ThrowsValidationException`**
    Confirms that a username below the minimum character threshold triggers a `ValidationException`.

*   **`ValidateUsername_WithTooLong_ThrowsValidationException`**
    Confirms that a username exceeding the maximum character threshold triggers a `ValidationException`.

### Email Validation Tests

*   **`ValidateEmail_WithValidEmail_DoesNotThrow`**
    Verifies that a syntactically correct email address passes validation without raising an exception.

*   **`ValidateEmail_WithInvalidFormat_ThrowsValidationException`**
    Confirms that an email string failing standard format checks (e.g., missing top-level domain structure) triggers a `ValidationException`.

*   **`ValidateEmail_WithMissingAt_ThrowsValidationException`**
    Confirms that an email string lacking the `@` symbol triggers a `ValidationException`.

*   **`ValidateEmail_WithMissingDomain_ThrowsValidationException`**
    Confirms that an email string lacking a domain component after the `@` symbol triggers a `ValidationException`.

*   **`ValidateEmail_WithNull_ThrowsValidationException`**
    Confirms that passing a `null` email address triggers a `ValidationException`.

*   **`ValidateEmail_WithEmpty_ThrowsValidationException`**
    Confirms that passing an empty string triggers a `ValidationException`.

### Password Validation Tests

*   **`ValidatePassword_WithValidPassword_DoesNotThrow`**
    Verifies that a password meeting complexity and length requirements passes validation without raising an exception.

*   **`ValidatePassword_WithNull_ThrowsValidationException`**
    Confirms that passing a `null` password triggers a `ValidationException`.

*   **`ValidatePassword_WithEmpty_ThrowsValidationException`**
    Confirms that passing an empty string triggers a `ValidationException`.

*   **`ValidatePassword_WithTooShort_ThrowsValidationException`**
    Confirms that a password below the minimum length threshold triggers a `ValidationException`.

### Product Name Validation Tests

*   **`ValidateProductName_WithValidName_DoesNotThrow`**
    Verifies that a product name meeting length requirements passes validation without raising an exception.

*   **`ValidateProductName_WithNull_ThrowsValidationException`**
    Confirms that passing a `null` product name triggers a `ValidationException`.

*   **`ValidateProductName_WithEmpty_ThrowsValidationException`**
    Confirms that passing an empty string triggers a `ValidationException`.

*   **`ValidateProductName_WithTooShort_ThrowsValidationException`**
    Confirms that a product name below the minimum length threshold triggers a `ValidationException`.

## Usage

These tests are executed automatically by the test runner during the build pipeline. Below are examples of how the logic being tested is typically consumed in application code, reflecting the scenarios covered by the test suite.

### Example 1: Validating User Registration Input
This example demonstrates handling the validation flow for a new user, ensuring that both username and email constraints are met before proceeding.

```csharp
try 
{
    // Simulates the logic verified by ValidateUsername_WithValidUsername_DoesNotThrow
    // and ValidateEmail_WithValidEmail_DoesNotThrow
    ValidationHelper.ValidateUsername("jdoe_2024");
    ValidationHelper.ValidateEmail("jdoe@example.com");
    
    // Proceed with registration logic
    Console.WriteLine("User input is valid.");
}
catch (ValidationException ex)
{
    // Handles scenarios covered by *ThrowsValidationException tests
    Console.WriteLine($"Registration failed: {ex.Message}");
}
```

### Example 2: Validating Product Catalog Updates
This example illustrates validation for product data, specifically checking for nulls and length constraints as verified by the product name test methods.

```csharp
public void UpdateProduct(string productName)
{
    // Ensures the product name is not null, empty, or too short
    // Covers: ValidateProductName_WithNull_ThrowsValidationException, etc.
    ValidationHelper.ValidateProductName(productName);

    // Database update logic follows only if no exception is thrown
    Repository.SaveProduct(productName);
}
```

## Notes

*   **Exception Consistency**: All failure scenarios across username, email, password, and product name validations consistently throw `ValidationException`. Callers should rely on this specific exception type for error handling rather than generic `Exception` types.
*   **Whitespace Handling**: The username validation explicitly treats strings containing only whitespace as invalid, distinct from empty strings. This prevents users from bypassing presence checks using space characters.
*   **Thread Safety**: As this class consists entirely of stateless test methods verifying static or instance helper logic, the tests themselves are thread-safe and can be executed in parallel by the test runner. The underlying `ValidationHelper` methods being tested should also be assumed stateless and thread-safe given their functional nature.
*   **Boundary Conditions**: The test suite specifically targets boundary conditions (too short, too long). Implementations relying on these helpers should ensure that the defined min/max lengths in the helper match the database schema constraints to avoid runtime database errors after successful validation.
