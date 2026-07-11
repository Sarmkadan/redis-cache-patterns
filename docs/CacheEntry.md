# CacheEntry

`CacheEntry` represents a single entry in a cache system, encapsulating metadata and operational state for cached data. It is used to track and manage cache entries with detailed information about their lifecycle, access patterns, and categorization through tags. This class is typically employed in scenarios requiring fine-grained control over cache behavior, such as monitoring cache efficiency, implementing eviction policies, or managing tagged cache invalidation.

## API

### Properties

- **`public string Key`**  
  Gets the unique identifier for the cache entry. This value is immutable after initialization.

- **`public string DataType`**  
  Gets the type of data stored in the cache entry (e.g., "string", "json"). Used to distinguish between different serialization formats or data structures.

- **`public long SizeInBytes`**  
  Gets the size of the cached data in bytes. This value is set during entry creation and does not update automatically if the underlying data changes.

- **`public DateTime CreatedAt`**  
  Gets the UTC timestamp when the cache entry was created. This value is immutable.

- **`public DateTime? ExpiresAt`**  
  Gets the UTC timestamp when the cache entry will expire, or `null` if no expiration is set. Can be modified via `SetExpiration`.

- **`public DateTime LastAccessedAt`**  
  Gets the UTC timestamp of the last access (hit or miss) to the cache entry. Updated automatically by `RecordHit`, `RecordMiss`, and `UpdateLastAccess`.

- **`public int AccessCount`**  
  Gets the total number of accesses (hits + misses) to the cache entry. Incremented by `RecordHit` and `RecordMiss`.

- **`public int HitCount`**  
  Gets the number of successful cache hits. Incremented by `RecordHit`.

- **`public int MissCount`**  
  Gets the number of cache misses. Incremented by `RecordMiss`.

- **`public string Status`**  
  Gets the current status of the cache entry (e.g., "active", "expired", "invalid"). Modified by `Invalidate` and `SetExpiration`.

- **`public string? Tags`**  
  Gets the comma-separated list of tags associated with the cache entry, or `null` if no tags are assigned. Modified by `AddTag`.

### Methods

- **`public void RecordHit()`**  
  Increments `HitCount` and `AccessCount`, and updates `LastAccessedAt` to the current UTC time.  
  Does not throw exceptions.

- **`public void RecordMiss()`**  
  Increments `MissCount` and `AccessCount`, and updates `LastAccessedAt` to the current UTC time.  
  Does not throw exceptions.

- **`public void UpdateLastAccess()`**  
  Updates `LastAccessedAt` to the current UTC time without modifying hit/miss counters.  
  Does not throw exceptions.

- **`public void SetExpiration(DateTime? expiresAt)`**  
  Sets the expiration time for the cache entry. If `expiresAt` is `null`, the entry will not expire.  
  Does not throw exceptions.

- **`public void Invalidate()`**  
  Marks the cache entry as invalid by setting `Status` to "invalid".  
  Does not throw exceptions.

- **`public void AddTag(string tag)`**  
  Adds a tag to the entry. If `Tags` is `null`, it initializes it with the provided tag.  
  Does not throw exceptions.

- **`public bool HasTag(string tag)`**  
  Returns `true` if the specified tag exists in the `Tags` list, otherwise `false`.  
  Does not throw exceptions.

- **`public override string ToString()`**  
  Returns a string representation of the cache entry, including key, status, and access statistics.  
  Does not throw exceptions.

## Usage

```csharp
// Example 1: Creating and managing a cache entry
var entry = new CacheEntry("user:123", "json", 1024);
entry.SetExpiration(DateTime.UtcNow.AddMinutes(30));
entry.RecordHit();
entry.AddTag("user-profile");
Console.WriteLine(entry.ToString());
// Output: Key=user:123, Status=active, Hits=1, Misses=0, Size=1024B
```

```csharp
// Example 2: Checking tags and invalidating entries
var entry = new CacheEntry("session:abc", "string", 256);
entry.AddTag("session");
entry.AddTag("temporary");

if (entry.HasTag("session"))
{
    Console.WriteLine("Session entry found. Invalidating...");
    entry.Invalidate();
}
// Status is now "invalid"
```

## Notes

- Thread Safety: This class is not thread-safe. Concurrent access to properties like `HitCount`, `AccessCount`, or `LastAccessedAt` may result in race conditions. External synchronization is required in multi-threaded environments.

- Expiration Handling: The `ExpiresAt` property does not automatically update the `Status` field. Consumers must explicitly check expiration logic and update `Status` accordingly.

- Tag Management: Tags are stored as a comma-separated string. Duplicate tags added via `AddTag` will not be deduplicated. Parsing tags for complex operations should account for potential formatting issues.

- Null Handling: `ExpiresAt` and `Tags` are nullable. Methods like `HasTag` return `false` if `Tags` is `null`, and `SetExpiration` accepts `null` to remove expiration.
