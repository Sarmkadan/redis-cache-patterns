# CacheConfiguration

`CacheConfiguration` is a configuration class for Redis cache clients, encapsulating connection details, timeouts, and cache behavior settings. It provides a structured way to define and parse Redis connection parameters, enabling consistent cache initialization across applications.

## API

### `string ConnectionString`
Gets or sets the Redis connection string. This string includes the server address, port, and optional authentication details required to establish a connection to the Redis instance. Must be a valid Redis connection string or an exception will be thrown during connection attempts.

### `int DatabaseId`
Gets or sets the Redis database identifier to use for cache operations. Valid values are non-negative integers within the Redis server's supported range. Defaults to `0` if not specified.

### `int ConnectTimeoutMs`
Gets or sets the connection timeout in milliseconds. Determines how long the client will wait to establish a connection to the Redis server before failing. Must be a non-negative integer; values less than `1` are treated as `1`.

### `int SyncTimeoutMs`
Gets or sets the synchronous operation timeout in milliseconds. Defines the maximum duration for synchronous Redis commands to complete before timing out. Must be a non-negative integer; values less than `1` are treated as `1`.

### `bool EnableCompression`
Gets or sets a value indicating whether to enable network-level compression for cache data. When `true`, data transmitted between the client and server is compressed to reduce bandwidth usage. Enabling compression may increase CPU usage on both client and server.

### `int MaxCacheSizeBytes`
Gets or sets the maximum size in bytes for the local cache. When the total size of cached items exceeds this value, older or less frequently used items are evicted according to the `EvictionPolicy`. A value of `0` indicates no size limit.

### `string EvictionPolicy`
Gets or sets the eviction policy for cache entries when `MaxCacheSizeBytes` is exceeded. Supported values are `"LRU"` (Least Recently Used), `"LFU"` (Least Frequently Used), and `"FIFO"` (First-In-First-Out). Defaults to `"LRU"` if not specified or invalid.

### `static CacheConfiguration FromEnvironment()`
Creates a `CacheConfiguration` instance by parsing environment variables. Reads configuration from standard environment variables such as `REDIS_CONNECTION_STRING`, `REDIS_DATABASE_ID`, etc. Returns a new instance with values populated from the environment. Throws `InvalidOperationException` if required environment variables are missing or invalid.

### `override string ToString()`
Returns a string representation of the configuration. Includes a summary of key settings such as `ConnectionString`, `DatabaseId`, and `MaxCacheSizeBytes`, with sensitive values masked for security. Useful for logging and debugging without exposing credentials.

## Usage
