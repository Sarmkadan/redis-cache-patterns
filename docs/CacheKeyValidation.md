# CacheKeyValidation

The `CacheKeyValidation` class provides a set of static guard methods for the cache keys and key patterns that cache operations receive within the `redis-cache-patterns` library. Cache service implementations call these methods at the start of each operation so that every implementation rejects the same bad input with the same exception type.

`CacheKeyValidation` is declared in `Services/CacheKeyValidation.cs`. It is a `static` class with no state and no constructor, and it does not depend on Redis or any other service. The methods only inspect their arguments; they never modify them.

## API

### `ValidateKey`

Validates a single cache key.

- **Parameters**:
  - `key` (string): The cache key to validate.
  - `paramName` (string): The parameter name used in the exception. Defaults to `"key"`.
- **Return value**: `void`.
- **Exceptions**:
  - Throws `ArgumentNullException` if `key` is `null`. The exception carries `paramName` and the default framework message.
  - Throws `ArgumentNullException` if `key` is empty or whitespace. Message: `Cache key cannot be null or whitespace.`

The check does not trim the key, so a key with leading or trailing spaces (for example `" product:1 "`) passes. The method does not check the key's format or length. The `entity:id` convention is not enforced here.

### `ValidateKeyWithCacheException`

Validates a single cache key and reports a failure as a `CacheKeyNotFoundException`.

- **Parameters**:
  - `key` (string): The cache key to validate.
  - `paramName` (string): The parameter name. Defaults to `"key"`. It is passed to the exception as the key value (see Notes).
- **Return value**: `void`.
- **Exceptions**: Throws `CacheKeyNotFoundException` if `key` is `null`, empty, or whitespace. The `CacheKey` property is set to `paramName`, and the message is `Cache key '<paramName>' not found`.

Use this method when a missing key should look like a cache miss to the caller. Use `ValidateKey` when the input is simply wrong.

### `ValidateKeyCollection`

Validates a collection of cache keys, typically the input to a bulk operation.

- **Parameters**:
  - `keys` (`IEnumerable<string>`): The collection of keys to validate.
  - `paramName` (string): The parameter name used in the exception. Defaults to `"keys"`.
- **Return value**: `void`.
- **Exceptions**:
  - Throws `ArgumentNullException` if `keys` is `null`. The parameter name comes from the caller's expression, not from `paramName`.
  - Throws `ArgumentException` if any element is `null`, empty, or whitespace. Message: `Cache key collection cannot contain null or whitespace keys.`

The method enumerates the whole collection once and stops at the first invalid element. An empty collection passes. For a lazily evaluated sequence, the source is enumerated again by the operation that follows, so pass a materialized collection if the source is expensive to enumerate.

### `ValidatePattern`

Validates a cache key pattern, such as a wildcard used to select keys for invalidation.

- **Parameters**:
  - `pattern` (string): The cache pattern to validate.
  - `paramName` (string): The parameter name used in the exception. Defaults to `"pattern"`.
- **Return value**: `void`.
- **Exceptions**: Throws `ArgumentNullException` if `pattern` is `null`, empty, or whitespace. Message: `Cache pattern cannot be null or whitespace.`

The method checks only for emptiness. It does not parse the pattern or check which wildcard characters it uses.

## Defaults and Common Failures

- `ValidateKey(null)` and `ValidateKey("")` both throw `ArgumentNullException`, not `ArgumentException`. Callers that catch the two cases separately must handle this.
- `ValidateKey("   ")` also throws `ArgumentNullException`, because the whitespace check uses `string.IsNullOrWhiteSpace`.
- `ValidateKeyCollection` with a `null` element throws `ArgumentException`, while `ValidateKeyCollection(null)` throws `ArgumentNullException`. A caller that handles only one of these will miss the other.
- `ValidateKeyWithCacheException(string.Empty)` throws `CacheKeyNotFoundException` with the message `Cache key 'key' not found`. The text names the parameter, not the bad value, because the value is empty.

## Usage

### Example 1: Guarding a Single-Key Operation

```csharp
public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
{
    CacheKeyValidation.ValidateKey(key);

    return _cache.StringGetAsync(key, cancellationToken);
}
```

### Example 2: Guarding a Bulk Operation

```csharp
public async Task<IReadOnlyDictionary<string, string?>> GetManyAsync(
    IEnumerable<string> keys,
    CancellationToken cancellationToken = default)
{
    var materialized = keys.ToList();

    CacheKeyValidation.ValidateKeyCollection(materialized);

    return await FetchAsync(materialized, cancellationToken);
}
```

### Example 3: Catching the Failure

```csharp
try
{
    CacheKeyValidation.ValidateKey(string.Empty);
}
catch (ArgumentNullException ex)
{
    // ex.ParamName == "key"
    Console.WriteLine(ex.Message);
}
```

## Notes

*   **Doc Comments:** The XML comment on `ValidateKey` says that an empty key throws `CacheKeyNotFoundException`, and that it also throws `ArgumentException` for a key that is empty after trimming. Neither is what the code does. `ValidateKey` throws `ArgumentNullException` for `null`, empty, and whitespace keys. The behavior documented here is the code's behavior. Use `ValidateKeyWithCacheException` if `CacheKeyNotFoundException` is needed.
*   **Unreachable Check:** The trimmed-length check inside `ValidateKey` can never fire. `string.IsNullOrWhiteSpace` has already rejected any key that is empty after trimming, so that branch is dead code.
*   **Parameter Name Quirk:** `ValidateKeyWithCacheException` passes `paramName` into the `CacheKeyNotFoundException` constructor as the key value. A caller that sets a custom `paramName` will see that name in the `CacheKey` property and in the message, not the actual key.
*   **Null Collection:** `ValidateKeyCollection` uses `ArgumentNullException.ThrowIfNull`, which takes the parameter name from the caller's expression. The `paramName` argument is used only for the element check.
*   **Scope:** The class checks for emptiness and nulls, not for key format. Key format rules are handled by `CacheKeyBuilder` and `CacheKeyExtensions`. Pattern syntax is not validated.
*   **Consistency:** `CacheValidation` has a `ValidateKey` method with its own messages (`Cache key cannot be null.`, `Cache key cannot be empty or whitespace.`). That method and this one are separate. Services that need the `CacheKeyValidation` behavior should call this class directly.
