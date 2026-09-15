# Redis Streams Extensions

Documentation for the `RedisStreamsExtensions` and related types in the `RedisCachePatterns.Extensions` namespace, providing utility methods for working with Redis Streams for cache invalidation.

## Class: `RedisStreamOptions`

Configuration options for `RedisStreamCacheInvalidationService`.

| Property | Type | Default Value | Description |
|----------|------|---------------|-------------|
| `StreamKey` | `string` | `"cache:invalidation:stream"` | Gets or sets the Redis stream key that holds invalidation events. |
| `ConsumerGroup` | `string` | `"cache-invalidation-group"` | Gets or sets the consumer group name shared by all instances of this service. Each instance competes for messages, so every invalidation event is processed exactly once. |
| `ConsumerName` | `string` | `${Environment.MachineName}-${Guid.NewGuid():N}` | Gets or sets the unique consumer name for this service instance within the group. Defaults to a combination of the machine name and a random suffix to avoid collisions in horizontally-scaled deployments. |
| `BatchSize` | `int` | `50` | Gets or sets the maximum number of stream messages to read in a single `XREADGROUP` call. |
| `MaxStreamLength` | `int` | `10_000` | Gets or sets the approximate maximum number of entries the stream will retain (MAXLEN). Older entries are trimmed automatically by Redis. |
| `PollingInterval` | `TimeSpan` | `250 ms` | Gets or sets how long the consumer waits before polling again when no messages are available. |
| `ErrorRetryDelay` | `TimeSpan` | `5 s` | Gets or sets how long the consumer waits before retrying after an unhandled error. |

## Static Class: `RedisStreamsExtensions`

Extension methods for working with Redis Streams.

### Method: `CreateStreamMessage`

```csharp
public static NameValueEntry[] CreateStreamMessage(
    this string eventId,
    string? cacheKey = null,
    string? keyPattern = null,
    InvalidationReason reason = InvalidationReason.DataUpdate,
    string source = "")
```

Creates a new `NameValueEntry` array for a Redis Stream message.

**Parameters:**
- `eventId`: The unique identifier for the event.
- `cacheKey`: The cache key to invalidate, or `null`.
- `keyPattern`: The key pattern to invalidate, or `null`.
- `reason`: The reason for invalidation. Defaults to `InvalidationReason.DataUpdate`.
- `source`: The source service name for tracing. Defaults to empty string.

**Returns:** An array of `NameValueEntry` ready for `IDatabase.StreamAddAsync`.

**Exceptions:**
- `ArgumentException`: Thrown when both `cacheKey` and `keyPattern` are `null` or whitespace.
- `ArgumentNullException`: Thrown when `eventId` is `null` or whitespace.

**Remarks:** The method creates a message with the following fields:
- `eventId`: The unique identifier for the event
- `cacheKey`: The cache key to invalidate (empty string if null)
- `keyPattern`: The key pattern to invalidate (empty string if null)
- `reason`: The invalidation reason as string
- `source`: The source service name
- `occurredAt`: Timestamp in ISO 8601 format (UtcNow.ToString("O"))

### Method: `ParseStreamMessage`

```csharp
public static Dictionary<string, string> ParseStreamMessage(this StreamEntry message)
```

Parses a Redis Stream message into a dictionary of field names and values.

**Parameters:**
- `message`: The Redis Stream message to parse.

**Returns:** A dictionary containing the message fields and their values.

**Exceptions:**
- `ArgumentNullException`: Thrown when `message` is `null`.

### Method: `TryGetCacheKey`

```csharp
public static bool TryGetCacheKey(this StreamEntry message, out string? cacheKey)
```

Attempts to extract the cache key from a Redis Stream message.

**Parameters:**
- `message`: The Redis Stream message to parse.
- `cacheKey`: Receives the cache key if present.

**Returns:** `true` if a cache key was found; otherwise, `false`.

**Exceptions:**
- `ArgumentNullException`: Thrown when `message` is `null`.

### Method: `TryGetKeyPattern`

```csharp
public static bool TryGetKeyPattern(this StreamEntry message, out string? keyPattern)
```

Attempts to extract the key pattern from a Redis Stream message.

**Parameters:**
- `message`: The Redis Stream message to parse.
- `keyPattern`: Receives the key pattern if present.

**Returns:** `true` if a key pattern was found; otherwise, `false`.

**Exceptions:**
- `ArgumentNullException`: Thrown when `message` is `null`.

### Method: `TryGetInvalidationReason`

```csharp
public static bool TryGetInvalidationReason(this StreamEntry message, out InvalidationReason reason)
```

Attempts to extract the invalidation reason from a Redis Stream message.

**Parameters:**
- `message`: The Redis Stream message to parse.
- `reason`: Receives the invalidation reason.

**Returns:** `true` if a valid reason was found; otherwise, `false`.

**Exceptions:**
- `ArgumentNullException`: Thrown when `message` is `null`.

### Method: `AddRedisStreamInvalidation`

```csharp
public static IServiceCollection AddRedisStreamInvalidation(
    this IServiceCollection services,
    Action<RedisStreamOptions>? configure = null)
```

Registers `RedisStreamCacheInvalidationService` as both a long-running `Microsoft.Extensions.Hosting.IHostedService` (consumer) and an `IRedisStreamInvalidationService` (producer), so any service in the application can publish cross-instance invalidation events with a single injection.

**Parameters:**
- `services`: The `IServiceCollection` to configure.
- `configure`: Optional delegate to customise `RedisStreamOptions` (stream key, consumer group name, batch size, etc.).

**Returns:** The same `IServiceCollection` instance for fluent chaining.

**Exceptions:**
- `ArgumentNullException`: Thrown when `services` is `null`.

**Usage Example:**
```csharp
builder.Services.AddRedisCachePatterns();
builder.Services.AddRedisStreamInvalidation(opts =>
{
    opts.StreamKey = "myapp:cache:events";
    opts.ConsumerGroup = "myapp-cache-group";
    opts.BatchSize = 100;
});
```