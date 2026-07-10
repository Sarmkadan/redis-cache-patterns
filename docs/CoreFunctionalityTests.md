# CoreFunctionalityTests
The `CoreFunctionalityTests` class is designed to validate the core functionality of key management in the redis-cache-patterns project. It provides a set of test methods to ensure that key building, validation, normalization, and parsing are performed correctly. These tests are crucial for guaranteeing the reliability and consistency of the caching mechanism, which relies heavily on properly formatted and managed keys.

## API
The `CoreFunctionalityTests` class includes the following public test methods:
- `BuildKey_ShouldReturnCorrectFormattedKey`: Tests if the key building process returns a correctly formatted key.
- `BuildKey_ShouldIgnoreNullParameters`: Verifies that the key building process ignores null parameters.
- `BuildPattern_ShouldReturnWildcardPattern`: Checks if the pattern building process returns a wildcard pattern as expected.
- `IsValidKey_ShouldReturnCorrectValidationResult`: Validates that the key validation process returns the correct result based on the input key.
- `NormalizeKey_ShouldReturnLowercaseAndTrimmedKey`: Tests if the key normalization process correctly converts keys to lowercase and trims them.
- `ParseKey_ShouldReturnCorrectParts`: Ensures that the key parsing process correctly identifies and returns the parts of a given key.

## Usage
Here are two examples demonstrating how to use the `CoreFunctionalityTests` class in a C# environment:
```csharp
// Example 1: Using BuildKey tests
[TestMethod]
public void TestBuildKey()
{
    var coreFunctionalityTests = new CoreFunctionalityTests();
    coreFunctionalityTests.BuildKey_ShouldReturnCorrectFormattedKey();
    coreFunctionalityTests.BuildKey_ShouldIgnoreNullParameters();
}

// Example 2: Using key validation and normalization tests
[TestMethod]
public void TestKeyValidationAndNormalization()
{
    var coreFunctionalityTests = new CoreFunctionalityTests();
    coreFunctionalityTests.IsValidKey_ShouldReturnCorrectValidationResult();
    coreFunctionalityTests.NormalizeKey_ShouldReturnLowercaseAndTrimmedKey();
}
```

## Notes
- **Edge Cases**: The tests cover various edge cases, including null parameters and keys with different casing. However, it's essential to consider additional edge cases based on specific requirements, such as extremely long keys or keys containing special characters.
- **Thread Safety**: Since these tests do not modify external state and primarily focus on validating the behavior of key management functions, they are considered thread-safe. Nevertheless, when integrating these tests into a larger test suite or application, ensure that the overall test environment remains thread-safe to avoid unexpected behavior.
