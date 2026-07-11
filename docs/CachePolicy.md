# CachePolicy
The `CachePolicy` type is designed to manage caching behavior in applications, providing a flexible way to control cache expiration, compression, and size limits. It allows developers to define a caching strategy that suits their specific needs, enabling efficient data storage and retrieval.

## API
### Properties
* `Key`: A string identifier for the cache policy.
* `DefaultExpiration`: A `TimeSpan` representing the default expiration time for cached items.
* `Pattern`: A `CachePattern` enum value indicating the caching pattern to use.
* `UseCompression`: A boolean indicating whether compression is enabled for cached items.
* `MaxSize`: An integer representing the maximum size of the cache.
* `IsActive`: A boolean indicating whether the cache policy is currently active.
* `CreatedAt`: A `DateTime` representing the time when the cache policy was created.
* `UpdatedAt`: A nullable `DateTime` representing the time when the cache policy was last updated.
* `Description`: A nullable string providing a description of the cache policy.

### Constructors
* `CachePolicy`: Initializes a new instance of the `CachePolicy` class.
* `CachePolicy`: Initializes a new instance of the `CachePolicy` class (overload).

### Methods
* `UpdateExpiration`: Updates the expiration time for the cache policy.
* `SetPattern`: Sets the caching pattern for the cache policy.
* `EnableCompression`: Enables compression for the cache policy.
* `DisableCompression`: Disables compression for the cache policy.
* `Disable`: Disables the cache policy.
* `Enable`: Enables the cache policy.
* `ToString`: Returns a string representation of the cache policy.

## Usage
The following examples demonstrate how to use the `CachePolicy` type:
```csharp
// Create a new cache policy with default expiration and compression enabled
var policy = new CachePolicy();
policy.DefaultExpiration = TimeSpan.FromHours(1);
policy.EnableCompression();
```

```csharp
// Update the caching pattern and expiration time for an existing cache policy
var existingPolicy = new CachePolicy();
existingPolicy.SetPattern(CachePattern.LeastRecentlyUsed);
existingPolicy.UpdateExpiration(TimeSpan.FromMinutes(30));
```

## Notes
When using the `CachePolicy` type, consider the following edge cases and thread-safety remarks:
* The `MaxSize` property should be set carefully to avoid cache overflow.
* The `UseCompression` property may impact performance, as compression and decompression operations can be resource-intensive.
* The `IsActive` property should be checked before attempting to update or disable the cache policy.
* The `CachePolicy` class is not thread-safe by default; developers should implement synchronization mechanisms when accessing or modifying cache policies from multiple threads.
* The `UpdateExpiration` and `SetPattern` methods may throw exceptions if the provided values are invalid or if the cache policy is not active.
