# CacheKeyMetadata
The `CacheKeyMetadata` type provides information about a cache key, including its identifier, usage statistics, and timestamps. This metadata can be used to monitor cache performance, identify frequently accessed keys, and manage cache storage.

## API
* `public string Key`: The unique identifier of the cache key. This property does not take any parameters and does not throw any exceptions.
* `public long HitCount`: The number of times the cache key has been accessed. This property does not take any parameters and does not throw any exceptions.
* `public DateTime? LastAccessed`: The timestamp of the last time the cache key was accessed, or `null` if the key has not been accessed. This property does not take any parameters and does not throw any exceptions.
* `public DateTime? CreatedAt`: The timestamp when the cache key was created, or `null` if the creation time is not available. This property does not take any parameters and does not throw any exceptions.
* `public long SizeBytes`: The size of the cache key in bytes. This property does not take any parameters and does not throw any exceptions.

## Usage
The following examples demonstrate how to use the `CacheKeyMetadata` type:
```csharp
// Example 1: Retrieving cache key metadata
CacheKeyMetadata metadata = cache.GetMetadata("myKey");
Console.WriteLine($"Key: {metadata.Key}, Hit Count: {metadata.HitCount}, Last Accessed: {metadata.LastAccessed}");
```

```csharp
// Example 2: Updating cache key metadata
CacheKeyMetadata metadata = new CacheKeyMetadata
{
    Key = "myKey",
    HitCount = 10,
    LastAccessed = DateTime.Now,
    CreatedAt = DateTime.Now.AddDays(-1),
    SizeBytes = 1024
};
cache.UpdateMetadata(metadata);
```

## Notes
When working with `CacheKeyMetadata`, consider the following edge cases:
* If a cache key is not found, its metadata may not be available, and properties like `LastAccessed` and `CreatedAt` may be `null`.
* The `HitCount` property may not be up-to-date in real-time, as it may be updated asynchronously.
* The `SizeBytes` property may not reflect the actual size of the cache key if it has been compressed or encoded.
Regarding thread-safety, the `CacheKeyMetadata` type is designed to be immutable, making it safe to access and use from multiple threads concurrently. However, when updating cache key metadata, ensure that the update operation is thread-safe to avoid data inconsistencies.
