# CacheConfigurationBuilder
The `CacheConfigurationBuilder` class is a utility for constructing and configuring cache settings in a flexible and fluent manner. It allows developers to define default expiration times, add custom cache policies, and enable features such as compression, warming, and monitoring. This builder pattern enables the creation of `CacheServiceOptions` instances with ease, making it simpler to manage cache configurations throughout an application.

## API
### Methods
* `WithDefaultExpiration(TimeSpan expiration)`: Sets the default expiration time for cache entries. Returns the current `CacheConfigurationBuilder` instance for method chaining. Throws `ArgumentOutOfRangeException` if `expiration` is zero or negative.
* `WithDefaultExpiration(int seconds)`: Sets the default expiration time for cache entries in seconds. Returns the current `CacheConfigurationBuilder` instance for method chaining. Throws no exceptions.
* `AddPolicy(Domain.CachePolicy policy)`: Adds a custom cache policy to the configuration. Returns the current `CacheConfigurationBuilder` instance for method chaining. Throws no exceptions.
* `AddPolicy(string keyPattern, TimeSpan expiration)`: Adds a cache policy for the given key pattern with the given expiration, creating the `Domain.CachePolicy` internally. Returns the current `CacheConfigurationBuilder` instance for method chaining. Throws no exceptions.
* `EnableCompression(int thresholdBytes = 1024)`: Enables compression for cache entries and sets the size threshold in bytes. Returns the current `CacheConfigurationBuilder` instance for method chaining. Throws no exceptions.
* `EnableWarming`: Enables warming for cache entries. Returns the current `CacheConfigurationBuilder` instance for method chaining. Throws no exceptions.
* `EnableMonitoring`: Enables monitoring for cache entries. Returns the current `CacheConfigurationBuilder` instance for method chaining. Throws no exceptions.
* `Build`: Constructs a `CacheServiceOptions` instance based on the current configuration. Returns a `CacheServiceOptions` instance. Throws no exceptions.
### Properties
The builder itself exposes no public properties. The following properties belong to the `CacheServiceOptions` instance returned by `Build`:
* `DefaultExpiration`: Gets or sets the default expiration time for cache entries. Returns a `TimeSpan` value.
* `Policies`: Gets or sets the list of custom cache policies. Returns a `List<Domain.CachePolicy>` instance.
* `CompressionEnabled`: Gets or sets a value indicating whether compression is enabled. Returns a `bool` value.
* `CompressionThresholdBytes`: Gets or sets the threshold in bytes for compression. Returns an `int` value.
* `WarmingEnabled`: Gets or sets a value indicating whether warming is enabled. Returns a `bool` value.
* `MonitoringEnabled`: Gets or sets a value indicating whether monitoring is enabled. Returns a `bool` value.
### Overrides
* `ToString`: Defined on the `CacheServiceOptions` instance returned by `Build`, not on the builder. Returns a summary string with `DefaultExpiration`, `Compression`, `Warming`, and `Monitoring`. Returns a `string` value.

## Usage
The following examples demonstrate how to use the `CacheConfigurationBuilder` class to create `CacheServiceOptions` instances:
```csharp
// Example 1: Basic configuration
var cacheOptions = new CacheConfigurationBuilder()
    .WithDefaultExpiration(TimeSpan.FromHours(1))
    .AddPolicy(new Domain.CachePolicy { Key = "product:*", DefaultExpiration = TimeSpan.FromMinutes(30) })
    .EnableCompression()
    .Build();

// Example 2: Advanced configuration
var advancedCacheOptions = new CacheConfigurationBuilder()
    .WithDefaultExpiration(7 * 24 * 60 * 60)
    .AddPolicy("order:*", TimeSpan.FromMinutes(5))
    .AddPolicy(new Domain.CachePolicy { Key = "user:*", DefaultExpiration = TimeSpan.FromMinutes(15) })
    .EnableWarming()
    .EnableMonitoring()
    .Build();
```

## Notes
When using the `CacheConfigurationBuilder` class, consider the following:
* The `DefaultExpiration` property sets the default expiration time for all cache entries. Policies added with `AddPolicy` are stored in `Policies` with their own `DefaultExpiration`; `Build` does not merge them into the default expiration, so consumers of `Policies` decide which expiration applies to a given key.
* The `CompressionThresholdBytes` property determines the minimum size of cache entries that will be compressed. Entries smaller than this threshold will not be compressed.
* The `CacheConfigurationBuilder` class is not thread-safe. If multiple threads need to access and modify the same `CacheConfigurationBuilder` instance, proper synchronization mechanisms should be implemented to avoid concurrency issues.
* The `Build` method constructs a new `CacheServiceOptions` instance based on the current configuration. It does not modify the existing `CacheConfigurationBuilder` instance.
