# IRedisConnection

The `IRedisConnection` interface defines the shared Redis connection contract in the `RedisCachePatterns.Infrastructure.Cache` namespace. It gives application services access to the underlying StackExchange.Redis connection and database APIs while centralizing connectivity checks, shutdown, and connection configuration reporting.

Implementations are intended to own and reuse an `IConnectionMultiplexer`. The project provides `RedisConnection` for standalone Redis deployments, while `IRedisClusterConnection` extends this contract with cluster-specific operations.

## API

### `IConnectionMultiplexer GetConnection()`

Returns the active StackExchange.Redis connection multiplexer. An implementation may establish or restore the connection when this method is called, so callers should be prepared for connection-related exceptions.

The returned multiplexer is designed to be shared across concurrent operations. Callers should not create a new connection for each cache request or dispose a multiplexer owned by the `IRedisConnection` implementation.

### `IDatabase GetDatabase(int databaseId = 0)`

Returns an `IDatabase` instance for the requested logical Redis database. Omitting `databaseId` selects database `0`.

Support for nonzero database indexes depends on the implementation and Redis deployment. The standalone `RedisConnection` accepts indexes from `0` through `15`; Redis Cluster uses database `0` only.

### `Task<bool> IsConnectedAsync()`

Checks whether Redis is reachable and returns `true` when the implementation's connectivity check succeeds. It returns `false` when Redis cannot be reached or the check otherwise fails.

Because this is a point-in-time health check, a successful result does not guarantee that a later Redis operation will succeed.

### `Task DisconnectAsync()`

Closes the managed Redis connection and releases its resources. The returned task completes after the asynchronous shutdown work finishes.

Call this method during an orderly application shutdown when the owning composition root does not dispose the implementation automatically. After disconnection, an implementation may allow a later call to `GetConnection()` to create a new connection.

### `string GetConnectionString()`

Returns the Redis connection configuration represented as a string. The exact format is implementation-specific and may contain endpoints, credentials, or other sensitive configuration, so it should not be written to logs or exposed in diagnostics without redaction.

## Usage

### Read and Write Through a Database

```csharp
public async Task<string?> GetDisplayNameAsync(
    IRedisConnection connection,
    string userId)
{
    var database = connection.GetDatabase();
    RedisValue value = await database.StringGetAsync($"user:{userId}:display-name");

    return value.IsNull ? null : value.ToString();
}
```

### Check Connectivity

```csharp
public async Task<bool> IsRedisAvailableAsync(IRedisConnection connection)
{
    return await connection.IsConnectedAsync();
}
```

### Select a Logical Database

```csharp
var database = redisConnection.GetDatabase(databaseId: 2);
await database.StringSetAsync("settings:theme", "dark");
```

Use an explicit database index only when the selected implementation and deployment support it. Code that must work with Redis Cluster should use the default database.

## Notes

- **Connection lifetime:** Register implementations as singleton or otherwise long-lived services. `IConnectionMultiplexer` is thread-safe and intended to be reused.
- **Ownership:** Treat the returned `IConnectionMultiplexer` and `IDatabase` objects as borrowed dependencies. Connection shutdown belongs to the `IRedisConnection` implementation.
- **Health checks:** `IsConnectedAsync()` describes connectivity only at the time of the check. Redis commands still require normal timeout, retry, and exception handling.
- **Database selection:** Logical databases are a standalone Redis feature. Cluster implementations restrict access to database `0`.
- **Shutdown:** Await `DisconnectAsync()` during graceful shutdown so pending close operations can complete.
- **Sensitive data:** Connection strings can include secrets. Avoid logging the result of `GetConnectionString()` unless sensitive values have been removed.
