# CacheKeyBenchmarksExtensions

CacheKeyBenchmarksExtensions provides a set of static extension methods designed for manipulating cache key strings within benchmarking scenarios, enabling developers to consistently apply metadata like Time-to-Live (TTL), versions, hashes, or key combinations to base cache keys for performance testing and analysis in Redis-based caching architectures.

## API

### WithTtl
Appends a Time-to-Live value to the base cache key. This is commonly used in benchmarking to distinguish keys based on their intended expiration duration.

### CombineKeys
Concatenates a base cache key with one or more sub-keys to construct a unique, hierarchical, or composite identifier for caching scenarios.

### WithVersion
Appends a version string to the base cache key. This facilitates cache key versioning, ensuring that retrieved entries are compatible with specific application logic or data models.

### WithHash
Appends a hash string to the base cache key. This is typically employed to ensure cache key uniqueness based on complex input parameters or to support integrity verification.

## Usage

### Example 1: Creating a composite key with versioning
```csharp
string baseKey = "user_profile";
string versionedKey = baseKey.WithVersion("v1");
string compositeKey = versionedKey.CombineKeys("12345", "settings");

// Results in a formatted string suitable for cache operations.
```

### Example 2: Applying TTL and Hash for benchmark tests
```csharp
string baseKey = "data_packet";
string benchmarkKey = baseKey
    .WithTtl(TimeSpan.FromMinutes(10))
    .WithHash("a1b2c3d4");

// Generates a specialized key string used for isolating cache performance metrics.
```

## Notes

- **Thread Safety:** These extension methods operate on immutable `string` instances and do not maintain internal state. Therefore, they are thread-safe and can be used concurrently in multi-threaded benchmarking applications.
- **Null Handling:** As these are extension methods for `string`, passing a `null` reference as the base key will result in a `NullReferenceException` at runtime. Ensure all base keys are properly initialized before invoking these methods.
- **Performance:** These methods involve string concatenation and formatting. While acceptable for the majority of benchmarking scenarios, they should be used judiciously if keys are generated inside tight, high-frequency loops where string allocation overhead is a concern.
