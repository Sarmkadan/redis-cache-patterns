# CacheTagService

A utility service that enables tag-based cache invalidation patterns on top of Redis. It allows associating cache keys with one or more logical tags and provides methods to invalidate all keys tagged with a specific tag or set of tags. This is useful for scenarios where related cached data must be purged together (e.g., invalidating all product-related caches when a product is updated).

## API

### `CacheTagService`

The primary service class for managing tagged cache keys in Redis.

### `public async Task SetWithTagsAsync<T>(string key, T value, IEnumerable<string> tags)`

Associates a cache key with one or more tags and stores the value in Redis.

- **Parameters**:
  - `key`: The cache key to associate with tags.
  - `value`: The value to store in the cache.
  - `tags`: An enumerable of tag names to associate with the key.
- **Return value**: A `Task` representing the asynchronous operation.
- **Exceptions**:
  - Throws `ArgumentNullException` if `key` or `tags` is `null`.
  - Throws `ArgumentException` if `tags` contains any `null` or whitespace-only strings.

### `public async Task TagKeyAsync(string key, IEnumerable<string> tags)`

Adds additional tags to an existing cache key without modifying the cached value.

- **Parameters**:
  - `key`: The cache key to tag.
  - `tags`: An enumerable of tag names to associate with the key.
- **Return value**: A `Task` representing the asynchronous operation.
- **Exceptions**:
  - Throws `ArgumentNullException` if `key` or `tags` is `null`.
  - Throws `ArgumentException` if `tags` contains any `null` or whitespace-only strings.

### `public async Task<bool> UntagKeyAsync(string key, IEnumerable<string> tags)`

Removes specific tags from a cache key. The key remains in the cache unless explicitly removed.

- **Parameters**:
  - `key`: The cache key to untag.
  - `tags`: An enumerable of tag names to remove from the key.
- **Return value**: A `Task<bool>` where `true` indicates at least one tag was removed, `false` indicates no tags were removed.
- **Exceptions**:
  - Throws `ArgumentNullException` if `key` or `tags` is `null`.
  - Throws `ArgumentException` if `tags` contains any `null` or whitespace-only strings.

### `public async Task<IReadOnlyList<string>> GetKeysByTagAsync(string tag)`

Retrieves all cache keys associated with a specific tag.

- **Parameters**:
  - `tag`: The tag name to query.
- **Return value**: A `Task<IReadOnlyList<string>>` containing all keys tagged with the specified tag.
- **Exceptions**:
  - Throws `ArgumentNullException` if `tag` is `null`.
  - Throws `ArgumentException` if `tag` is whitespace-only.

### `public async Task<int> InvalidateTagAsync(string tag)`

Invalidates all cache keys associated with a specific tag by removing them from Redis.

- **Parameters**:
  - `tag`: The tag name whose associated keys should be invalidated.
- **Return value**: A `Task<int>` indicating the number of keys removed.
- **Exceptions**:
  - Throws `ArgumentNullException` if `tag` is `null`.
  - Throws `ArgumentException` if `tag` is whitespace-only.

### `public async Task<int> InvalidateTagsAsync(IEnumerable<string> tags)`

Invalidates all cache keys associated with any of the specified tags by removing them from Redis.

- **Parameters**:
  - `tags`: An enumerable of tag names whose associated keys should be invalidated.
- **Return value**: A `Task<int>` indicating the total number of keys removed across all tags.
- **Exceptions**:
  - Throws `ArgumentNullException` if `tags` is `null`.
  - Throws `ArgumentException` if `tags` contains any `null` or whitespace-only strings.

### `public static string BuildTagKey(string tag)`

Constructs a standardized Redis key for storing tag-to-keys mappings.

- **Parameters**:
  - `tag`: The tag name to convert into a Redis key.
- **Return value**: A `string` representing the Redis key used to store the set of keys associated with the tag.
- **Exceptions**:
  - Throws `ArgumentNullException` if `tag` is `null`.
  - Throws `ArgumentException` if `tag` is whitespace-only.

## Usage

### Example 1: Storing and invalidating by tag
