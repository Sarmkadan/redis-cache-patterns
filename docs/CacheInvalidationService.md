# CacheInvalidationService

Provides a centralized mechanism for invalidating cached entries by tag, pattern, or explicit dependency chains. It maintains internal mappings between cache keys and their associated tags, enabling bulk invalidation without requiring knowledge of every individual key.

## API

### CacheInvalidationService

```csharp
public CacheInvalidationService(/* dependencies injected by container */)
```

Constructs a new instance of the service. The constructor accepts dependencies required for cache access and tag-key mapping storage; these are typically provided through dependency injection.

### RegisterKeyWithTags

```csharp
public void RegisterKeyWithTags(string key, params string[] tags)
```

Associates a cache key with one or more tags. Subsequent calls to `InvalidateByTagAsync` for any of the registered tags will remove this key from the cache.

**Parameters:**
- `key` — The cache key to register.
- `tags` — One or more tag strings to associate with the key.

**Throws:**
- `ArgumentNullException` if `key` is null.
- `ArgumentException` if `key` is empty or consists only of whitespace.

### InvalidateByTagAsync

```csharp
public async Task InvalidateByTagAsync(string tag)
```

Removes all cache entries associated with the specified tag. The tag-to-key mapping is removed after invalidation completes.

**Parameters:**
- `tag` — The tag whose associated keys should be invalidated.

**Returns:** A `Task` representing the asynchronous invalidation operation.

**Throws:**
- `ArgumentNullException` if `tag` is null.
- `ArgumentException` if `tag` is empty or consists only of whitespace.

### InvalidateByPatternAsync

```csharp
public async Task InvalidateByPatternAsync(string pattern)
```

Removes all cache entries whose keys match the given glob-style pattern. Pattern matching is performed against the underlying cache store, not against the tag mappings.

**Parameters:**
- `pattern` — A glob pattern (e.g., `"user:*"`, `"product:???"`) to match against cache keys.

**Returns:** A `Task` representing the asynchronous invalidation operation.

**Throws:**
- `ArgumentNullException` if `pattern` is null.
- `ArgumentException` if `pattern` is empty or consists only of whitespace.

### InvalidateWithDependenciesAsync

```csharp
public async Task InvalidateWithDependenciesAsync(string key)
```

Removes the specified cache key and recursively invalidates any keys that declare it as a dependency. Dependency relationships must be established separately through the cache abstraction used by this service.

**Parameters:**
- `key` — The primary cache key to invalidate, along with its dependents.

**Returns:** A `Task` representing the asynchronous invalidation operation.

**Throws:**
- `ArgumentNullException` if `key` is null.
- `ArgumentException` if `key` is empty or consists only of whitespace.

### GetKeysByTag

```csharp
public IEnumerable<string> GetKeysByTag(string tag)
```

Retrieves all cache keys currently associated with the specified tag. Returns a snapshot of the mapping at the time of the call; subsequent registrations or invalidations are not reflected in the returned enumerable.

**Parameters:**
- `tag` — The tag whose associated keys should be retrieved.

**Returns:** An `IEnumerable<string>` containing the cache keys associated with the tag. Returns an empty sequence if the tag has no registered keys.

**Throws:**
- `ArgumentNullException` if `tag` is null.
- `ArgumentException` if `tag` is empty or consists only of whitespace.

## Usage

### Example 1: Tag-Based Invalidation for a Multi-Entity Update

```csharp
// Register related keys under a common tag when populating the cache
cacheInvalidationService.RegisterKeyWithTags("user:42", "entity:user:42");
cacheInvalidationService.RegisterKeyWithTags("user:42:orders", "entity:user:42");
cacheInvalidationService.RegisterKeyWithTags("user:42:preferences", "entity:user:42");

// Later, when user 42 is updated, invalidate all related entries at once
await cacheInvalidationService.InvalidateByTagAsync("entity:user:42");
```

### Example 2: Pattern-Based Cleanup During Maintenance

```csharp
// Invalidate all cached search results across all locales
await cacheInvalidationService.InvalidateByPatternAsync("search:*:results");

// Invalidate all session data for a specific region
await cacheInvalidationService.InvalidateByPatternAsync("session:eu-*");
```

## Notes

- **Tag mapping storage is in-memory.** The associations between keys and tags exist only for the lifetime of the service instance. If the process restarts, previously registered tag mappings are lost, though the underlying cache entries may persist.
- **`GetKeysByTag` returns a snapshot.** The returned enumerable is evaluated immediately and does not reflect changes made after the call. Iterating over it multiple times yields the same set of keys.
- **`InvalidateByPatternAsync` operates directly on the cache store.** It does not consult or update the tag-key mappings. Keys removed by pattern may leave orphaned entries in the tag mapping, which will remain until explicitly invalidated by tag or until the service instance is disposed.
- **Thread safety.** Registration and tag-based invalidation methods are safe to call concurrently from multiple threads. The internal mapping data structure uses synchronization to prevent corruption during concurrent reads and writes.
- **Empty tags and keys.** All methods reject null, empty, or whitespace-only string arguments for key and tag parameters. Passing a valid but previously unseen tag to `InvalidateByTagAsync` or `GetKeysByTag` is not an error — invalidation becomes a no-op, and key retrieval returns an empty sequence.
- **Dependency invalidation depth.** `InvalidateWithDependenciesAsync` follows the dependency chain transitively. Circular dependencies are not explicitly guarded against; the implementation relies on the cache abstraction to detect and handle cycles.
