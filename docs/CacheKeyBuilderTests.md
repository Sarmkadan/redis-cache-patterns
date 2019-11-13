# CacheKeyBuilderTests
The `CacheKeyBuilderTests` class is a test suite designed to verify the correctness of the `CacheKeyBuilder` class, which is responsible for constructing and manipulating cache keys in a Redis-based caching system. This test class ensures that the `CacheKeyBuilder` behaves as expected, producing well-formed cache keys and handling edge cases correctly.

## API
The `CacheKeyBuilderTests` class contains a set of test methods that cover various aspects of the `CacheKeyBuilder` class. These methods include:
* `BuildKey_WithMultipleParts_ReturnsColonSeparatedString`: Verifies that the `BuildKey` method returns a colon-separated string when given multiple parts.
* `BuildKey_WithNullPart_SubstitutesNullLiteral`: Tests that the `BuildKey` method substitutes a null literal when a null part is provided.
* `User_ReturnsUserPrefixedKey`: Checks that the `User` method returns a user-prefixed key.
* `Product_ReturnsProductPrefixedKey`: Verifies that the `Product` method returns a product-prefixed key.
* `ProductBySku_ReturnsSkuScopedKey`: Tests that the `ProductBySku` method returns a SKU-scoped key.
* `OrdersByUser_ReturnsUserScopedOrderKey`: Checks that the `OrdersByUser` method returns a user-scoped order key.
* `InventoryByProductAndWarehouse_ReturnsFullyQualifiedKey`: Verifies that the `InventoryByProductAndWarehouse` method returns a fully qualified key.
* `DistributedLock_ReturnsLockPrefixedKey`: Tests that the `DistributedLock` method returns a lock-prefixed key.
* `GeneratePattern_AppendsSeparatorAndWildcard`: Checks that the `GeneratePattern` method appends a separator and wildcard.
* `IsValidKey_WithWellFormedKey_ReturnsTrue`: Verifies that the `IsValidKey` method returns true for a well-formed key.
* `IsValidKey_WithEmptyString_ReturnsFalse`: Tests that the `IsValidKey` method returns false for an empty string.
* `IsValidKey_WithWhitespaceOnly_ReturnsFalse`: Checks that the `IsValidKey` method returns false for a string containing only whitespace.
* `IsValidKey_WithNewlineCharacter_ReturnsFalse`: Verifies that the `IsValidKey` method returns false for a string containing a newline character.
* `IsValidKey_WithCarriageReturn_ReturnsFalse`: Tests that the `IsValidKey` method returns false for a string containing a carriage return.
* `NormalizeKey_ConvertsToLowercaseAndTrims`: Checks that the `NormalizeKey` method converts the key to lowercase and trims it.
* `ParseKey_SplitsSegmentsOnColon`: Verifies that the `ParseKey` method splits segments on colons.
* `GetPrefix_ReturnsFirstSegmentOnly`: Tests that the `GetPrefix` method returns the first segment only.
* `BuildEntityKey_UsesLowercaseTypeNameWithEntitySegment`: Checks that the `BuildEntityKey` method uses the lowercase type name with the entity segment.
* `BuildLockKey_PrependsLockPrefix`: Verifies that the `BuildLockKey` method prepends the lock prefix.
* `BuildPattern_WithNoParameters_AppendsWildcard`: Tests that the `BuildPattern` method appends a wildcard when no parameters are provided.

## Usage
Here are two examples of using the `CacheKeyBuilder` class:
```csharp
// Example 1: Building a cache key for a user
var cacheKeyBuilder = new CacheKeyBuilder();
var userKey = cacheKeyBuilder.User("username");
Console.WriteLine(userKey); // Output: "user:username"

// Example 2: Building a cache key for a product
var cacheKeyBuilder = new CacheKeyBuilder();
var productKey = cacheKeyBuilder.Product("product-id");
Console.WriteLine(productKey); // Output: "product:product-id"
```

## Notes
The `CacheKeyBuilder` class is designed to handle various edge cases, such as null or empty input, and to produce well-formed cache keys. The class is also thread-safe, as it does not maintain any internal state. However, users should be aware that the `BuildKey` method will substitute a null literal for any null parts, and that the `IsValidKey` method will return false for keys containing only whitespace or newline characters. Additionally, the `NormalizeKey` method will convert keys to lowercase and trim them, which may affect the behavior of the `ParseKey` method.
