# DependencyInjectionExtensions

The `DependencyInjectionExtensions` class is a static collection of extension methods for `IServiceCollection` and `IServiceProvider`. It wires up the Redis connection, the cache service, the repositories and business services of the `redis-cache-patterns` library, and offers a circuit breaker decorator and a startup connection check. The type lives in the `RedisCachePatterns.Configuration` namespace. Registration methods return the same `IServiceCollection`, so calls can be chained.

## API

### AddRedisCachePatterns

```csharp
public static IServiceCollection AddRedisCachePatterns(
    this IServiceCollection services,
    string redisConnectionString = "localhost:6379")
```

Registers the full demo stack: the Redis connection, the cache service, the repositories, the cache tag service, and the business services. Every registration is a singleton.

- **services**: The `IServiceCollection` to add services to.
- **redisConnectionString**: Redis connection string. Defaults to `"localhost:6379"`.
- **Returns**: The same `IServiceCollection`.
- **Throws**: `ArgumentNullException` when `services` is null. `ArgumentException` when `redisConnectionString` is null or empty. A whitespace-only string is not rejected.

Registered services:

| Service type | Implementation |
|---|---|
| `IRedisConnection` | `RedisConnection`, created from `redisConnectionString` |
| `ICacheService` | `RedisCacheService` |
| `IUserRepository` | `UserRepository` |
| `IProductRepository` | `ProductRepository` |
| `IOrderRepository` | `OrderRepository` |
| `IInventoryRepository` | `InventoryRepository` |
| `ICacheTagService` | `CacheTagService` |
| `UserService`, `ProductService`, `OrderService`, `InventoryService` | Registered as themselves |

### AddRedisCache

```csharp
public static IServiceCollection AddRedisCache(
    this IServiceCollection services,
    CacheConfiguration? configuration = null)
```

Registers only the Redis connection and the cache service, without repositories or business services. Use it when an application consumes the caching patterns but brings its own data layer.

- **services**: The `IServiceCollection` to add services to.
- **configuration**: Optional cache configuration. When `null`, the configuration is built from environment variables by `CacheConfiguration.FromEnvironment()`.
- **Returns**: The same `IServiceCollection`.
- **Throws**: `ArgumentNullException` when `services` is null.

Only `CacheConfiguration.ConnectionString` is passed to the connection. The other properties (`DatabaseId`, timeouts, compression, and so on) are read but not applied by this method.

### AddCircuitBreakerCache

```csharp
public static IServiceCollection AddCircuitBreakerCache(
    this IServiceCollection services,
    int failureThreshold = 5,
    TimeSpan? breakDuration = null)
```

Wraps the registered `ICacheService` in a `CircuitBreakerCacheService` decorator. After `failureThreshold` consecutive failures the circuit opens, and calls are short-circuited until `breakDuration` has passed.

- **services**: The `IServiceCollection` to add services to.
- **failureThreshold**: Number of consecutive failures before the circuit opens. Defaults to `5`.
- **breakDuration**: How long the circuit stays open before recovery is attempted. Defaults to 30 seconds when `null`.
- **Returns**: The same `IServiceCollection`.
- **Throws**: `ArgumentNullException` when `services` is null. The decorator constructor throws `ArgumentOutOfRangeException` at resolution time when `failureThreshold` is not positive.

### ValidateRedisConnectionAsync

```csharp
public static async Task ValidateRedisConnectionAsync(this IServiceProvider serviceProvider)
```

Resolves `IRedisConnection` and checks whether it is connected. It is meant to be called once at startup. A failed check is logged as a warning and does not throw. An exception from the connection check is logged as an error and then rethrown.

- **serviceProvider**: The `IServiceProvider` to resolve services from, usually `app.Services`.
- **Returns**: A `Task` that completes when the check finishes.
- **Throws**:
    - `ArgumentNullException` when `serviceProvider` is null.
    - `InvalidOperationException` when `IRedisConnection` or a non-generic `ILogger` is not registered. This happens before the check runs.
    - The original exception when `IsConnectedAsync()` throws.

## Usage

### Full stack registration

```csharp
using RedisCachePatterns.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRedisCachePatterns("localhost:6379");

var app = builder.Build();
```

### Consumer registration from environment variables

```csharp
using RedisCachePatterns.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Reads REDIS_CONNECTION, falling back to "localhost:6379"
builder.Services.AddRedisCache();

var app = builder.Build();
```

### Explicit configuration

```csharp
using RedisCachePatterns.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRedisCache(new CacheConfiguration
{
    ConnectionString = "redis-primary:6379",
    DatabaseId = 2
});

var app = builder.Build();
```

### Startup connection check

```csharp
using RedisCachePatterns.Configuration;

var app = builder.Build();

await app.Services.ValidateRedisConnectionAsync();

app.Run();
```

## Notes

*   **Registration lifetime:** All services registered here are singletons. `IRedisConnection` is created by a factory on first resolution, so a bad connection string surfaces at that point, not when the method is called.
*   **Environment variables:** `AddRedisCache()` without an argument reads these variables through `CacheConfiguration.FromEnvironment()`: `REDIS_CONNECTION` (default `localhost:6379`), `REDIS_DATABASE`, `REDIS_CONNECT_TIMEOUT`, `REDIS_SYNC_TIMEOUT`, `REDIS_COMPRESSION`, `REDIS_MAX_SIZE`, and `REDIS_EVICTION_POLICY`. Only `REDIS_CONNECTION` is used by the connection, as described above. The environment is read when the method is called, not when the service is resolved.
*   **Circuit breaker resolution risk:** `AddCircuitBreakerCache` resolves `ICacheService` from inside its own factory. The factory is appended last, so single-service resolution returns the same factory, which can recurse without end. This was determined by reading the code, not by running it. The decorator chain in `ServiceRegistration` uses a private `Decorate<TInterface>` helper that replaces the existing descriptor instead, so that path avoids the problem. Test this method before relying on it.
*   **Duplicate registrations:** Calling `AddRedisCachePatterns` and `AddRedisCache` on the same collection adds two `IRedisConnection` and two `ICacheService` descriptors. For single-service resolution the last registration wins, and an earlier one may still be created if it is resolved as an `IEnumerable<T>`.
*   **Logger requirement for validation:** `ValidateRedisConnectionAsync` resolves the non-generic `ILogger`, which `AddLogging()` does not register by default. Without a registration for it, the call throws `InvalidOperationException` before the connection is checked. Register `ILogger` yourself, or use a host that provides it.
*   **Thread safety:** Registration methods only modify the `IServiceCollection` and are not thread-safe. Call them during application setup on one thread. `ValidateRedisConnectionAsync` is safe to call from any thread.
*   **Not registered here:** None of these methods registers `IRepository<T>`, `IRedisClusterConnection`, or the cluster variants. Use `ClusterDependencyInjectionExtensions` for cluster topologies.
