# RedisCachePatternsOptions

Represents the configuration options for a Redis-based caching layer that implements common caching patterns such as cache-aside, distributed invalidation, and eviction control. This class is typically used to bind application settings (e.g., from `appsettings.json`) and is passed to the cache service constructor. All properties have sensible defaults where applicable, but the connection string must be provided.

## API

### `public string ConnectionString`

The Redis server connection string (e.g., `localhost:6379,password=secret`).  
**Purpose:** Specifies the endpoint and authentication details for the Redis instance.  
**Parameters:** None (property).  
**Return value:** The current connection string value.  
**Throws:** No direct throw, but a `null` or empty value will cause an `ArgumentException` when the cache service attempts to connect.

### `public int DatabaseId`

The logical Redis database index to use (default is typically 0).  
**Purpose:** Selects which database within the Redis instance to operate on.  
**Parameters:** None (property).  
**Return value:** The database index (0–15 for most Redis configurations).  
**Throws:** No direct throw, but values outside the supported range may cause runtime errors when executing commands.

### `public int ConnectTimeoutMs`

The timeout in milliseconds for establishing a connection to Redis.  
**Purpose:** Controls how long the client waits for the initial TCP connection to succeed.  
**Parameters:** None (property).  
**Return value:** The timeout value in milliseconds.  
**Throws:** No direct throw, but a value of zero or negative may result in immediate timeout or undefined behavior.

### `public int SyncTimeoutMs`

The timeout in milliseconds for synchronous operations (e.g., `GET`, `SET`).  
**Purpose:** Limits the wait time for a single Redis command to complete when called synchronously.  
**Parameters:** None (property).  
**Return value:** The timeout value in milliseconds.  
**Throws:** No direct throw, but a value of zero or negative may cause synchronous calls to block indefinitely or throw a `TimeoutException`.

### `public bool EnableCompression`

Whether to compress cache values (e.g., using GZip or Brotli) before storing them in Redis.  
**Purpose:** Reduces memory usage for large or repetitive data at the cost of CPU overhead.  
**Parameters:** None (property).  
**Return value:** `true` if compression is enabled; otherwise `false`.  
**Throws:** None.

### `public int MaxCacheSizeBytes`

The maximum total size (in bytes) of cached data allowed before eviction is triggered.  
**Purpose:** Enforces a memory budget for the cache; used by the eviction policy to decide which entries to remove.  
**Parameters:** None (property).  
**Return value:** The size limit in bytes. A value of `0` typically means unlimited.  
**Throws:** No direct throw, but negative values are invalid and may cause unexpected behavior.

### `public string EvictionPolicy`

The eviction algorithm to use when the cache exceeds `MaxCacheSizeBytes`.  
**Purpose:** Determines which cached entries are removed first (e.g., LRU, LFU, TTL-based).  
**Parameters:** None (property).  
**Return value:** A string such as `"LRU"`, `"LFU"`, `"TTL"`, or `"None"`.  
**Throws:** No direct throw, but an unrecognized policy string may be ignored or cause a runtime exception when the cache attempts to evict.

### `public DistributedInvalidationOptions DistributedInvalidation`

Configuration for cross-instance cache invalidation using Redis pub/sub or keyspace notifications.  
**Purpose:** Allows multiple application instances to notify each other when a cache entry is updated or removed, keeping all instances consistent.  
**Parameters:** None (property).  
**Return value:** An instance of `DistributedInvalidationOptions` (may be `null` if not configured).  
**Throws:** None, but a `null` value disables distributed invalidation.

## Usage

### Example 1: Binding from configuration

```csharp
// appsettings.json
{
  "RedisCache": {
    "ConnectionString": "myredis.contoso.com:6380,password=abc123",
    "DatabaseId": 1,
    "ConnectTimeoutMs": 5000,
    "SyncTimeoutMs": 3000,
    "EnableCompression": true,
    "MaxCacheSizeBytes": 104857600,
    "EvictionPolicy": "LRU",
    "DistributedInvalidation": {
      "ChannelName": "cache:invalidations",
      "Enabled": true
    }
  }
}

// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.Configure<RedisCachePatternsOptions>(
        Configuration.GetSection("RedisCache"));
    services.AddSingleton<ICacheService, RedisCacheService>();
}
```

### Example 2: Programmatic instantiation

```csharp
var options = new RedisCachePatternsOptions
{
    ConnectionString = "localhost:6379",
    DatabaseId = 0,
    ConnectTimeoutMs = 2000,
    SyncTimeoutMs = 1000,
    EnableCompression = false,
    MaxCacheSizeBytes = 0, // unlimited
    EvictionPolicy = "None",
    DistributedInvalidation = new DistributedInvalidationOptions
    {
        ChannelName = "myapp:cache",
        Enabled = true
    }
};

var cache = new RedisCacheService(options);
await cache.SetAsync("key", "value");
```

## Notes

- **Thread safety:** Instances of `RedisCachePatternsOptions` are not thread-safe for modification. After the options are passed to a cache service, they should be treated as immutable. Concurrent reads are safe.
- **ConnectionString:** Must not be `null` or empty. If left unset, the cache service will throw an `ArgumentException` at construction time.
- **DatabaseId:** Redis supports databases 0–15 by default. Using an index outside this range will cause a `RedisServerException` when the first command is executed.
- **Timeouts:** Both `ConnectTimeoutMs` and `SyncTimeoutMs` should be positive integers. A value of `0` or less may cause the underlying `StackExchange.Redis` client to behave unpredictably (e.g., infinite wait or immediate failure).
- **MaxCacheSizeBytes:** A value of `0` disables the size-based eviction limit. Negative values are not supported and may lead to overflow or incorrect comparisons.
- **EvictionPolicy:** The supported values depend on the cache implementation. Common values are `"LRU"`, `"LFU"`, `"TTL"`, and `"None"`. An unrecognized string may be silently ignored or cause a runtime exception.
- **DistributedInvalidation:** If this property is `null`, distributed invalidation is disabled. The nested `DistributedInvalidationOptions` class should be configured with a valid channel name and `Enabled = true` for the feature to work.
- **Compression:** When `EnableCompression` is `true`, the cache service will attempt to compress values before storing them. Very small values may actually increase in size due to compression overhead. The compression algorithm is implementation-defined (typically GZip).
