# CacheEntryExtensions

The `CacheEntryExtensions` class provides a set of static utility methods designed to evaluate, inspect, and filter `ICacheEntry` objects within the `redis-cache-patterns` library, facilitating streamlined cache state management and diagnostic operations.

## API

### IsActive
Determines whether a cache entry is currently valid and within its designated expiration window.
- **Parameters**: `ICacheEntry entry` - The cache entry to evaluate.
- **Returns**: `bool` - `true` if the entry is active and valid; otherwise, `false`.

### IsStale
Evaluates whether the cache entry has exceeded its soft expiration or is otherwise categorized as stale, indicating that it should potentially be refreshed.
- **Parameters**: `ICacheEntry entry` - The cache entry to evaluate.
- **Returns**: `bool` - `true` if the entry is stale; otherwise, `false`.

### GetTimeToExpiryFormatted
Calculates the remaining time until the cache entry expires and returns it as a human-readable string.
- **Parameters**: `ICacheEntry entry` - The cache entry to evaluate.
- **Returns**: `string` - A formatted string representing the time remaining (e.g., "00:05:30").

### GetDetailedStatus
Retrieves a comprehensive string representation of the cache entry's current state, including information about its activity and potential staleness.
- **Parameters**: `ICacheEntry entry` - The cache entry to evaluate.
- **Returns**: `string` - A descriptive string detailing the cache entry status.

### HasAllTags
Verifies if the cache entry possesses all of the specified tags.
- **Parameters**: 
  - `ICacheEntry entry` - The cache entry to evaluate.
  - `params string[] tags` - The tags to verify.
- **Returns**: `bool` - `true` if the entry contains every specified tag; otherwise, `false`.

### HasAnyTag
Verifies if the cache entry possesses at least one of the specified tags.
- **Parameters**: 
  - `ICacheEntry entry` - The cache entry to evaluate.
  - `params string[] tags` - The tags to verify.
- **Returns**: `bool` - `true` if the entry contains at least one of the specified tags; otherwise, `false`.

## Usage

### Evaluating Cache Entry State
```csharp
ICacheEntry cacheEntry = cache.GetEntry("user:123");

if (CacheEntryExtensions.IsActive(cacheEntry))
{
    Console.WriteLine($"Entry status: {CacheEntryExtensions.GetDetailedStatus(cacheEntry)}");
    Console.WriteLine($"Time remaining: {CacheEntryExtensions.GetTimeToExpiryFormatted(cacheEntry)}");
}
else if (CacheEntryExtensions.IsStale(cacheEntry))
{
    // Logic to trigger refresh for stale entry
    RefreshCacheEntry(cacheEntry);
}
```

### Tag-Based Filtering
```csharp
ICacheEntry cacheEntry = cache.GetEntry("product:456");

// Check if the entry belongs to both categories
if (CacheEntryExtensions.HasAllTags(cacheEntry, "electronics", "inventory"))
{
    // Perform bulk operation for electronics inventory
}

// Check if the entry belongs to any of these categories
if (CacheEntryExtensions.HasAnyTag(cacheEntry, "promo", "clearance"))
{
    // Apply special discount
}
```

## Notes

- **Null Handling**: These methods expect a non-null `ICacheEntry` reference. Passing a null reference will result in a `NullReferenceException`.
- **Thread Safety**: The thread safety of these operations is dependent on the implementation of the `ICacheEntry` being accessed. While these methods are read-only and do not mutate the entry state, they rely on the underlying object's thread-safety guarantees.
- **Tag Comparison**: The tag verification methods (`HasAllTags`, `HasAnyTag`) rely on the equality implementation of the tags provided by the `ICacheEntry`. Ensure consistent tag naming conventions (e.g., case sensitivity) across the application.
