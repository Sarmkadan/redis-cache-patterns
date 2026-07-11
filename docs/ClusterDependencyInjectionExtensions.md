# ClusterDependencyInjectionExtensions

Provides extension methods for `IServiceCollection` to register Redis cluster connectivity services and a validation helper that probes cluster topology. The type is part of the `redis-cache-patterns` library and simplifies wiring up `IConnectionMultiplexer`-backed cluster clients with sensible defaults and optional full-stack instrumentation.

## API

### AddRedisCluster (first overload)

```csharp
public static IServiceCollection AddRedisCluster(
    this IServiceCollection services,
    string configuration,
    Action<ClusterOptions>? configureOptions = null)
```

Registers a Redis cluster connection and related cache services using only the mandatory configuration string. An optional `ClusterOptions` callback allows tuning timeouts, retry policies, and endpoint resolvers.

- **services**: The dependency injection container to modify.
- **configuration**: A Redis connection string containing multiple seed endpoints (comma- or space-delimited).
- **configureOptions**: Optional delegate that receives a `ClusterOptions` instance for fine-grained control.
- **Returns**: The same `IServiceCollection` for chaining.
- **Throws**: `ArgumentException` when `configuration` is null or whitespace. `RedisConnectionException` may be thrown later at first resolution if the configuration is syntactically valid but unreachable (not thrown during registration).

### AddRedisCluster (second overload)

```csharp
public static IServiceCollection AddRedisCluster(
    this IServiceCollection services,
    Action<ClusterOptions> configureOptions)
```

Registers a Redis cluster connection where the configuration string is supplied inside the `ClusterOptions` callback rather than as a separate parameter. Useful when options are built from an external settings object.

- **services**: The dependency injection container to modify.
- **configureOptions**: Required delegate that must set `ClusterOptions.Configuration` (or equivalent seed endpoint property) before returning.
- **Returns**: The same `IServiceCollection` for chaining.
- **Throws**: `ArgumentNullException` when `configureOptions` is null. `InvalidOperationException` during resolution if the configuration string was never assigned inside the callback.

### AddRedisClusterWithFullStack

```csharp
public static IServiceCollection AddRedisClusterWithFullStack(
    this IServiceCollection services,
    string configuration,
    Action<ClusterOptions>? configureOptions = null)
```

Same registration surface as the first `AddRedisCluster` overload, but additionally registers telemetry, logging, and health-check services that form a full-stack observability layer around the cluster connection. Intended for production environments where diagnostics are required.

- **services**: The dependency injection container to modify.
- **configuration**: A Redis connection string containing multiple seed endpoints.
- **configureOptions**: Optional delegate that receives a `ClusterOptions` instance.
- **Returns**: The same `IServiceCollection` for chaining.
- **Throws**: Same as the first `AddRedisCluster` overload. Additional internal registrations may throw if required telemetry sinks are unavailable at first resolution.

### ValidateClusterConnectionAsync

```csharp
public static async Task<ClusterInfo?> ValidateClusterConnectionAsync(
    IConnectionMultiplexer multiplexer,
    CancellationToken cancellationToken = default)
```

Probes the connected cluster and returns topology metadata, or `null` if the multiplexer is not connected to a cluster. This is a diagnostic helper, not a registration method.

- **multiplexer**: An already-established `IConnectionMultiplexer` instance.
- **cancellationToken**: Allows cancelling the probe.
- **Returns**: A `ClusterInfo` object containing slot ranges, node endpoints, and replica assignments when the multiplexer is connected to a cluster; `null` when connected to a standalone instance or when the connection is down.
- **Throws**: `ArgumentNullException` when `multiplexer` is null. `OperationCanceledException` when the token is cancelled. `RedisException` when the probe command fails due to network errors.

## Usage

### Minimal cluster registration

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRedisCluster(
    "redis-cluster-0:6379,redis-cluster-1:6379,redis-cluster-2:6379",
    options =>
    {
        options.ConnectTimeout = TimeSpan.FromSeconds(10);
        options.KeepAlive = TimeSpan.FromSeconds(30);
    });

builder.Services.AddSingleton<ICacheService, RedisCacheService>();

var app = builder.Build();
```

### Full-stack registration with connection validation

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRedisClusterWithFullStack(
    "redis-cluster-0:6379,redis-cluster-1:6379,redis-cluster-2:6379");

var app = builder.Build();

var multiplexer = app.Services.GetRequiredService<IConnectionMultiplexer>();
var clusterInfo = await ClusterDependencyInjectionExtensions.ValidateClusterConnectionAsync(
    multiplexer,
    app.Lifetime.ApplicationStopping);

if (clusterInfo is not null)
{
    logger.LogInformation("Cluster topology: {Endpoints}", clusterInfo.EndPoints);
}
```

## Notes

- All `AddRedisCluster*` methods register `IConnectionMultiplexer` as a singleton. The underlying connection is established lazily on first use unless `ClusterOptions` explicitly requests eager connection.
- `ValidateClusterConnectionAsync` is a static utility that does not participate in dependency injection. It must be called with an already-resolved multiplexer instance.
- The second `AddRedisCluster` overload requires the caller to set the configuration string inside the options callback; failing to do so results in an error only when the multiplexer is first resolved, not at registration time.
- Thread safety: Registration methods are safe to call concurrently during service collection setup (they only modify the collection). `ValidateClusterConnectionAsync` is safe to call from any thread, but concurrent calls against the same multiplexer may increase server load; the method itself does not lock.
- Cancellation of `ValidateClusterConnectionAsync` may leave the multiplexer in a healthy state; the cancellation only abandons the probe, it does not close the connection.
- When using `AddRedisClusterWithFullStack`, ensure that the host builder has configured logging and health-check infrastructure beforehand; otherwise the additional registrations may resolve to no-op implementations.
