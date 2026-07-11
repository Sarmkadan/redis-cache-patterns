# CacheConfigurationBuilder
The `CacheConfigurationBuilder` class is a utility for constructing and configuring cache settings in a flexible and fluent manner. It allows developers to define default expiration times, add custom cache policies, and enable features such as compression, warming, and monitoring. This builder pattern enables the creation of `CacheServiceOptions` instances with ease, making it simpler to manage cache configurations throughout an application.

## API
### Methods
* `WithDefaultExpiration`: Sets the default expiration time for cache entries. Returns the current `CacheConfigurationBuilder` instance for method chaining. Throws no exceptions.
* `AddPolicy`: Adds a custom cache policy to the configuration. Returns the current `CacheConfigurationBuilder` instance for method chaining. Throws no exceptions.
* `EnableCompression`: Enables compression for cache entries. Returns the current `CacheConfigurationBuilder` instance for method chaining. Throws no exceptions.
* `EnableWarming`: Enables warming for cache entries. Returns the current `CacheConfigurationBuilder` instance for method chaining. Throws no exceptions.
* `EnableMonitoring`: Enables monitoring for cache entries. Returns the current `CacheConfigurationBuilder` instance for method chaining. Throws no exceptions.
* `Build`: Constructs a `CacheServiceOptions` instance based on the current configuration. Returns a `CacheServiceOptions` instance. Throws no exceptions.
### Properties
* `DefaultExpiration`: Gets the default expiration time for cache entries. Returns a `TimeSpan` value.
* `Policies`: Gets the list of custom cache policies. Returns a `List<Domain.CachePolicy>` instance.
* `CompressionEnabled`: Gets a value indicating whether compression is enabled. Returns a `bool` value.
* `CompressionThresholdBytes`: Gets the threshold in bytes for compression. Returns an `int` value.
* `WarmingEnabled`: Gets a value indicating whether warming is enabled. Returns a `bool` value.
* `MonitoringEnabled`: Gets a value indicating whether monitoring is enabled. Returns a `bool` value.
### Overrides
* `ToString`: Returns a string representation of the current `CacheConfigurationBuilder` instance. Returns a `string` value.

## Usage
The following examples demonstrate how to use the `CacheConfigurationBuilder` class to create `CacheServiceOptions` instances:
```csharp
// Example 1: Basic configuration
var cacheOptions = new CacheConfigurationBuilder()
    .WithDefaultExpiration(TimeSpan.FromHours(1))
    .AddPolicy(new Domain.CachePolicy { Name = "Policy1" })
    .EnableCompression()
    .Build();

// Example 2: Advanced configuration
var advancedCacheOptions = new CacheConfigurationBuilder()
    .WithDefaultExpiration(TimeSpan.FromDays(7))
    .AddPolicy(new Domain.CachePolicy { Name = "Policy2" })
    .AddPolicy(new Domain.CachePolicy { Name = "Policy3" })
    .EnableWarming()
    .EnableMonitoring()
    .Build();
```

## Notes
When using the `CacheConfigurationBuilder` class, consider the following:
* The `DefaultExpiration` property sets the default expiration time for all cache entries. If a custom policy is added with a specific expiration time, it will override the default expiration time for that policy.
* The `CompressionThresholdBytes` property determines the minimum size of cache entries that will be compressed. Entries smaller than this threshold will not be compressed.
* The `CacheConfigurationBuilder` class is not thread-safe. If multiple threads need to access and modify the same `CacheConfigurationBuilder` instance, proper synchronization mechanisms should be implemented to avoid concurrency issues.
* The `Build` method constructs a new `CacheServiceOptions` instance based on the current configuration. It does not modify the existing `CacheConfigurationBuilder` instance.
