// existing content ...

## RedisConnection

The `RedisConnection` class provides a managed Redis connection with retry logic and health checks. It allows you to establish a connection to Redis, check if the connection is active, and disconnect from Redis when needed.

### Usage Example
```csharp
var connectionString = "localhost:6379";
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<RedisConnection>();

var redisConnection = new RedisConnection(connectionString, logger);

// Check connection status
var isConnected = await redisConnection.IsConnectedAsync();
Console.WriteLine($"Is connected: {isConnected}");

// Get database
var database = redisConnection.GetDatabase();
Console.WriteLine($"Database: {database}");

// Disconnect from Redis
await redisConnection.DisconnectAsync();
