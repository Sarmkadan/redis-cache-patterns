# CacheValidation

The `CacheValidation` class provides a set of static guard methods for the arguments that cache operations receive within the `redis-cache-patterns` library. Each method checks one kind of input (a key, a pattern, a value, a delegate, an expiration) and throws a standard exception if the input is unusable. The class also builds `CacheException` instances with a consistent message format.

The goal is that every cache service fails the same way for the same bad input, so callers see one set of messages regardless of which implementation they use.

`CacheValidation` is declared in `Exceptions/CacheValidation.cs`. It is a `static` class with no state and no constructor, and it does not depend on Redis or any other service.

## API

### `ValidateKey`

Validates a cache key.

- **Parameters**:
  - `key` (string?): The cache key to validate.
  - `paramName` (string): The parameter name used in the exception. Defaults to `"key"`.
- **Return value**: `void`.
- **Exceptions**:
  - Throws `ArgumentNullException` if `key` is `null`. Message: `Cache key cannot be null.`
  - Throws `ArgumentException` if `key` is empty or whitespace. Message: `Cache key cannot be empty or whitespace.`

The method is annotated with `[NotNull]`, so after a successful call the compiler treats `key` as non-null. The check does not look at the key's format or length. Key format rules are handled by `CacheKeyValidation`.

### `ValidatePattern`

Validates a cache pattern, such as a wildcard used to select keys for invalidation.

- **Parameters**:
  - `pattern` (string?): The cache pattern to validate.
  - `paramName` (string): The parameter name used in the exception. Defaults to `"pattern"`.
- **Return value**: `void`.
- **Exceptions**:
  - Throws `ArgumentNullException` if `pattern` is `null`. Message: `Cache pattern cannot be null.`
  - Throws `ArgumentException` if `pattern` is empty or whitespace. Message: `Cache pattern cannot be empty or whitespace.`

The method checks only for emptiness. It does not parse the pattern or check which wildcard characters it uses.

### `ValidateValue<T>`

Validates a cache value for `null`.

- **Type parameter**:
  - `T`: The type of the value. No constraint is applied.
- **Parameters**:
  - `value` (T?): The cache value to validate.
  - `paramName` (string): The parameter name used in the exception. Defaults to `"value"`.
- **Return value**: `void`.
- **Exceptions**: Throws `ArgumentNullException` if `value` is `null`. Message: `Cache value cannot be null.`

The null check applies only where `T` can be `null`, which means reference types and `Nullable<T>`. For a non-nullable value type such as `int`, the argument can never be `null`, so the method never throws. The check does not look at the value's contents or size.

### `ValidateLoadFunction`

Validates the load function passed to a cache-aside call such as `GetOrLoadAsync`.

- **Parameters**:
  - `loadFn` (Delegate?): The load function to validate.
  - `paramName` (string): The parameter name used in the exception. Defaults to `"loadFn"`.
- **Return value**: `void`.
- **Exceptions**: Throws `ArgumentNullException` if `loadFn` is `null`. Message: `Load function cannot be null.`

The parameter type is the non-generic `Delegate`, so the method accepts any delegate type. It does not check the delegate's signature or return type.

### `ValidatePersistFunction`

Validates the persist function passed to a write-through call.

- **Parameters**:
  - `persistFn` (Delegate?): The persist function to validate.
  - `paramName` (string): The parameter name used in the exception. Defaults to `"persistFn"`.
- **Return value**: `void`.
- **Exceptions**: Throws `ArgumentNullException` if `persistFn` is `null`. Message: `Persist function cannot be null.`

As with `ValidateLoadFunction`, the signature of the delegate is not checked.

### `ValidateExpiration`

Validates an optional expiration time.

- **Parameters**:
  - `expiration` (TimeSpan?): The expiration time to validate. `null` means no expiration and is accepted.
  - `paramName` (string): The parameter name used in the exception. Defaults to `"expiration"`.
- **Return value**: `void`.
- **Exceptions**: Throws `ArgumentOutOfRangeException` if `expiration` has a value and that value is zero or negative. Message: `Expiration must be a positive duration.`

The method does not check for an upper bound on the duration.

### `CreateOperationException` (inner exception overload)

Creates a `CacheException` for a failed cache operation, using the inner exception's message.

- **Parameters**:
  - `operation` (string): The name of the cache operation that failed, such as `"get"` or `"set"`.
  - `innerException` (Exception): The exception that caused the failure. It is attached as the inner exception of the result.
- **Return value**: `CacheException` with the message `Cache {operation} failed: {innerException.Message}`.
- **Exceptions**: None thrown by this method for valid input. It does not check its arguments, so passing `null` for `innerException` throws a `NullReferenceException` on the call, when the message is built.

### `CreateOperationException` (custom message overload)

Creates a `CacheException` for a failed cache operation with a caller-supplied message.

- **Parameters**:
  - `operation` (string): The name of the cache operation that failed.
  - `message` (string): A custom description of the failure.
  - `innerException` (Exception?): The exception that caused the failure. Optional. If `null`, the result has no inner exception.
- **Return value**: `CacheException` with the message `Cache {operation} failed: {message}`.
- **Exceptions**: None thrown by this method.

## Message Format

Both `CreateOperationException` overloads use the same prefix, `Cache {operation} failed: `. The `ValidateKey`, `ValidatePattern`, and other guard methods use fixed messages, so the text is the same for every caller.

Guard exceptions are created with the `paramName` argument, so the exception's `ParamName` property holds the name of the offending parameter. The `Message` property also includes it, in the form `(Parameter 'key')`.

## Usage

### Example 1: Guarding a Key Argument

```csharp
public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
{
    CacheValidation.ValidateKey(key);

    // Safe to use key here; the compiler treats it as non-null after the call.
    return await _cache.GetStringAsync(key, cancellationToken);
}
```

### Example 2: Checking Parameter Names

```csharp
try
{
    CacheValidation.ValidateKey(string.Empty, "cacheKey");
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.ParamName);
    // cacheKey
    Console.WriteLine(ex.Message);
    // Cache key cannot be empty or whitespace. (Parameter 'cacheKey')
}
```

### Example 3: Validating an Expiration

```csharp
// Accepted: no expiration.
CacheValidation.ValidateExpiration(null);

// Accepted: positive duration.
CacheValidation.ValidateExpiration(TimeSpan.FromMinutes(5));

// Throws ArgumentOutOfRangeException.
CacheValidation.ValidateExpiration(TimeSpan.Zero);
```

### Example 4: Wrapping a Failure

```csharp
try
{
    await _redis.StringGetAsync(key);
}
catch (TimeoutException ex)
{
    throw CacheValidation.CreateOperationException("get", ex);
    // Message: "Cache get failed: <ex.Message>", with ex as InnerException.
}

// Custom message, no inner exception.
var error = CacheValidation.CreateOperationException("set", "Value exceeds the size limit");
// Message: "Cache set failed: Value exceeds the size limit"
```

## Notes

*   **Static Methods:** All members are static methods on `CacheValidation`. They are called on the class (for example, `CacheValidation.ValidateKey(key)`), not on an instance.
*   **Guard Semantics:** The `Validate*` methods return `void` and throw on failure. They do not return a result, so a caller that needs a `bool` must catch the exception or write its own check.
*   **Shallow Checks:** The methods check for `null`, emptiness, and sign only. They do not check key format, pattern syntax, value size, delegate signatures, or expiration limits.
*   **Exception Types:** `ValidateKey` and `ValidatePattern` throw `ArgumentNullException` for `null` and `ArgumentException` for blank strings. `ValidateExpiration` throws `ArgumentOutOfRangeException`. Callers that catch `ArgumentException` will also catch `ArgumentNullException`, because it derives from it.
*   **Cache Failures vs. Argument Errors:** The guard methods throw argument exceptions, not `CacheException`. Only the `CreateOperationException` methods produce `CacheException`. This follows the convention that invalid arguments are programming errors, while cache failures are runtime errors. See `CacheException` for the failure hierarchy.
*   **No Current Callers:** As of this writing, nothing else in the repository calls `CacheValidation`. The class is a shared helper for the cache services to use.
