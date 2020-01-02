# CacheKeyHelper

The `CacheKeyHelper` class provides a set of static utility methods for constructing, validating, normalizing, and parsing cache keys used in the `redis-cache-patterns` library. It enforces a consistent key naming convention across entity keys, collection keys, lock keys, and temporary keys, and supports pattern‑based key matching for batch operations. All methods are stateless and designed for use in multi‑threaded environments.

## API

### `BuildKey`

Constructs a cache key from one or more segments.

- **Parameters**  
  `segments` – `params string[]` – The segments to concatenate into a key. At least one segment must be provided.
- **Returns**  
  `string` – The formatted cache key.
- **Throws**  
  `ArgumentNullException` if any segment is `null`.  
  `ArgumentException` if no segments are provided or if a segment is empty after trimming.

### `BuildEntityKey<T>`

Builds a cache key for an entity of type `T`, typically using the entity’s identifier.

- **Type Parameters**  
  `T` – The entity type, used to derive a prefix.
- **Parameters**  
  `id` – `string` – The unique identifier of the entity.
- **Returns**  
  `string` – The entity cache key.
- **Throws**  
  `ArgumentNullException` if `id` is `null`.  
  `ArgumentException` if `id` is empty or whitespace.

### `BuildCollectionKey<T>`

Builds a cache key for a collection of entities of type `T`, optionally scoped by a qualifier.

- **Type Parameters**  
  `T` – The entity type.
- **Parameters**  
  `qualifier` – `string` – An optional qualifier (e.g., a filter or category). May be `null` or empty.
- **Returns**  
  `string` – The collection cache key.
- **Throws**  
  (No exceptions beyond standard argument validation.)

### `BuildPattern`

Creates a glob‑style pattern string that can be used with `KEYS` or `SCAN` to match keys sharing a given prefix.

- **Parameters**  
  `prefix` – `string` – The key prefix to match.
- **Returns**  
  `string` – A pattern string (e.g., `prefix:*`).
- **Throws**  
  `ArgumentNullException` if `prefix` is `null`.  
  `ArgumentException` if `prefix` is empty.

### `BuildEntityPattern<T>`

Creates a pattern that matches all entity keys of type `T`.

- **Type Parameters**  
  `T` – The entity type.
- **Returns**  
  `string` – The entity key pattern.
- **Throws**  
  (No exceptions.)

### `IsValidKey`

Validates whether a string is a well‑formed cache key according to the library’s conventions.

- **Parameters**  
  `key` – `string` – The key to validate.
- **Returns**  
  `bool` – `true` if the key is valid; otherwise `false`.
- **Throws**  
  (No exceptions; returns `false` for `null` or empty input.)

### `NormalizeKey`

Normalizes a cache key by trimming whitespace, converting to a consistent case, and removing invalid characters.

- **Parameters**  
  `key` – `string` – The key to normalize.
- **Returns**  
  `string` – The normalized key.
- **Throws**  
  `ArgumentNullException` if `key` is `null`.  
  `ArgumentException` if the key is empty after normalization.

### `ParseKey`

Splits a cache key into its constituent segments.

- **Parameters**  
  `key` – `string` – The key to parse.
- **Returns**  
  `string[]` – An array of segments.
- **Throws**  
  `ArgumentNullException` if `key` is `null`.  
  `ArgumentException` if the key does not follow the expected format.

### `GetPrefix`

Extracts the prefix (the first segment) from a cache key.

- **Parameters**  
  `key` – `string` – The key.
- **Returns**  
  `string` – The prefix segment.
- **Throws**  
  `ArgumentNullException` if `key` is `null`.  
  `ArgumentException` if the key is empty or malformed.

### `BuildLockKey`

Constructs a distributed lock key for a given resource.

- **Parameters**  
  `resource` – `string` – The name of the resource to lock.
- **Returns**  
  `string` – The lock key.
- **Throws**  
  `ArgumentNullException` if `resource` is `null`.  
  `ArgumentException` if `resource` is empty.

### `BuildLockPattern`

Creates a pattern that matches all lock keys.

- **Parameters**  
  (None)
- **Returns**  
  `string` – The lock key pattern.
- **Throws**  
  (No exceptions.)

### `BuildTemporaryKey`

Constructs a key intended for short‑lived cached data, often with an embedded expiration hint.

- **Parameters**  
  `baseKey` – `string` – The base key to make temporary.
- **Returns**  
  `string` – The temporary key.
- **Throws**  
  `ArgumentNullException` if `baseKey` is `null`.  
  `ArgumentException` if `baseKey` is empty.

## Usage

### Example 1: Building and validating entity keys

```csharp
using RedisCachePatterns;

// Build a key for a customer entity with ID "12345"
string customerKey = CacheKeyHelper.BuildEntityKey<Customer>("12345");
// Result: "Customer:12345" (assuming default prefix)

// Validate the key before using it
if (CacheKeyHelper.IsValidKey(customerKey))
{
    // Use the key in a Redis operation
    var cachedCustomer = await cache.GetAsync<Customer>(customerKey);
}
```

### Example 2: Creating lock keys and patterns

```csharp
using RedisCachePatterns;

// Build a lock key for a specific order
string orderLockKey = CacheKeyHelper.BuildLockKey("order:9876");
// Result: "Lock:order:9876"

// Build a pattern to find all active locks
string allLocksPattern = CacheKeyHelper.BuildLockPattern();
// Result: "Lock:*"

// Use the pattern with Redis SCAN
var lockKeys = await cache.ScanAsync(allLocksPattern);
```

## Notes

- **Thread safety** – All methods are static and operate only on their input parameters; no shared mutable state is used. The class is inherently thread‑safe.
- **Edge cases**  
  - `null` or empty inputs cause `ArgumentNullException` or `ArgumentException` (except `IsValidKey`, which returns `false`).  
  - `BuildCollectionKey<T>` accepts a `null` qualifier, producing a key without a qualifier segment.  
  - `NormalizeKey` removes characters that are not allowed in the key format; keys that become empty after normalization throw an exception.  
  - `ParseKey` expects keys built by the helper methods; arbitrary strings may fail validation.  
- **Key format** – The exact key structure (separator, prefix conventions) is determined by the library’s configuration. The helper methods abstract this format so that callers do not need to know the details.
