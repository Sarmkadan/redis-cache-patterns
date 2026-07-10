# RedisConnection

The `RedisConnection` class serves as a high-level wrapper around the `StackExchange.Redis` `IConnectionMultiplexer`, providing a centralized mechanism for managing Redis connection lifecycles within the `redis-cache-patterns` project. It encapsulates the retrieval of the underlying connection and database instances, simplifies health monitoring, and ensures orderly termination of the connection, abstracting the complexities of low-level `StackExchange.Redis` configuration and maintenance.

## API

### RedisConnection()
Initializes a new instance of the `RedisConnection` class.

### IConnectionMultiplexer GetConnection()
Retrieves the underlying `IConnectionMultiplexer` instance managed by this connection wrapper.
*   **Returns:** `IConnectionMultiplexer` - The active multiplexer instance.

### IDatabase GetDatabase()
Retrieves the default `IDatabase` instance from the underlying multiplexer.
*   **Returns:** `IDatabase` - The database interface for performing Redis operations.

### async Task<bool> IsConnectedAsync()
Determines the current connectivity status of the Redis instance.
*   **Returns:** `Task<bool>` - Returns `true` if the connection is active and responsive; otherwise, `false`.

### async Task DisconnectAsync()
Gracefully closes the connection to the Redis server and disposes of the underlying resources.
*   **Returns:** `Task` - A task that completes when the disconnection process has finished.

### string GetConnectionString()
Retrieves the connection string currently used by the `RedisConnection` instance.
*   **Returns:** `string` - The Redis connection string.

## Usage

```csharp
// Example 1: Basic usage for setting and getting values
var redisConnection = new RedisConnection();
var db = redisConnection.GetDatabase();

// Setting a value
await db.StringSetAsync("mykey", "myvalue");

// Getting a value
string value = await db.StringGetAsync("mykey");
Console.WriteLine(value);
```

```csharp
// Example 2: Checking connection health and disconnecting
var redisConnection = new RedisConnection();

if (await redisConnection.IsConnectedAsync())
{
    Console.WriteLine("Redis is connected.");
    
    // Perform operations...
    
    await redisConnection.DisconnectAsync();
    Console.WriteLine("Redis disconnected successfully.");
}
else
{
    Console.WriteLine("Redis is not connected.");
}
```

## Notes

*   **Thread Safety:** The underlying `IConnectionMultiplexer` is designed to be thread-safe and is intended to be shared and reused across an application. Consequently, instances of `RedisConnection` should be treated as singleton or long-lived objects.
*   **Async Operations:** All methods involving network I/O (`IsConnectedAsync`, `DisconnectAsync`) are asynchronous and should be awaited to avoid blocking the calling thread.
*   **Disposal:** While `DisconnectAsync` handles the graceful shutdown of the connection, users should ensure it is called during application shutdown to prevent resource leaks.
*   **Exception Handling:** `StackExchange.Redis` may throw exceptions if the connection fails during operations; callers should implement appropriate error handling strategies (e.g., retries) when invoking methods on the `IDatabase` instance returned by `GetDatabase()`.
