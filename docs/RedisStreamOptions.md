# RedisStreamOptions

Configuration class for Redis Stream-based cache invalidation. It defines the stream key, consumer group identity, polling behaviour, and operational limits used by the background invalidation service. An instance of this class is typically registered via the `AddRedisStreamInvalidation` extension method and consumed by the stream listener to coordinate message processing across multiple application instances.

## API

### StreamKey
`public string StreamKey`

The Redis key identifying the stream used for invalidation messages. All cooperating application instances must use the same stream key to receive consistent invalidation events.

### ConsumerGroup
`public string ConsumerGroup`

The name of the Redis consumer group that this instance joins. Consumer groups allow multiple instances to read from the same stream without duplicate processing; each message is delivered to exactly one consumer in the group.

### ConsumerName
`public string ConsumerName`

A unique identifier for this consumer within the consumer group. Redis uses this name to track which messages have been delivered to and acknowledged by this specific instance. Must be unique across all consumers in the same group.

### BatchSize
`public int BatchSize`

The maximum number of stream entries to read in a single polling operation. Larger values reduce round trips but increase memory pressure per batch. Must be greater than zero.

### MaxStreamLength
`public int MaxStreamLength`

The approximate maximum number of entries the stream retains. When set, the stream is trimmed after adding messages, keeping only the most recent entries up to this limit. A value of zero or less disables trimming.

### PollingInterval
`public TimeSpan PollingInterval`

The delay between consecutive polls of the stream when no messages are pending. Shorter intervals reduce latency but increase CPU usage and network calls. Must be a positive duration.

### ErrorRetryDelay
`public TimeSpan ErrorRetryDelay`

The delay before retrying after a transient failure (e.g. connection loss, timeout). This prevents tight retry loops during outages. Must be a positive duration.

### AddRedisStreamInvalidation
`public static IServiceCollection AddRedisStreamInvalidation(this IServiceCollection services, Action<RedisStreamOptions> configure)`

Registers the Redis Stream invalidation infrastructure into the dependency injection container.

**Parameters:**
- `services` — The `IServiceCollection` to add services to.
- `configure` — A delegate that receives a `RedisStreamOptions` instance for configuration.

**Returns:** The same `IServiceCollection` for chaining.

**Throws:** `ArgumentNullException` if `services` or `configure` is `null`.

## Usage

### Example 1: Basic registration with explicit options

```csharp
services.AddRedisStreamInvalidation(options =>
{
    options.StreamKey = "cache-invalidation-stream";
    options.ConsumerGroup = "web-servers";
    options.ConsumerName = $"instance-{Environment.MachineName}";
    options.BatchSize = 50;
    options.MaxStreamLength = 10_000;
    options.PollingInterval = TimeSpan.FromSeconds(2);
    options.ErrorRetryDelay = TimeSpan.FromSeconds(5);
});
```

### Example 2: Registration using configuration binding

```csharp
var redisSection = configuration.GetSection("Redis:StreamInvalidation");
services.AddRedisStreamInvalidation(options =>
{
    options.StreamKey = redisSection["StreamKey"];
    options.ConsumerGroup = redisSection["ConsumerGroup"];
    options.ConsumerName = $"{redisSection["ConsumerPrefix"]}-{Guid.NewGuid():N}";
    options.BatchSize = int.Parse(redisSection["BatchSize"] ?? "100");
    options.MaxStreamLength = int.Parse(redisSection["MaxLength"] ?? "5000");
    options.PollingInterval = TimeSpan.FromMilliseconds(
        double.Parse(redisSection["PollingMs"] ?? "1000"));
    options.ErrorRetryDelay = TimeSpan.FromSeconds(
        double.Parse(redisSection["RetryDelaySeconds"] ?? "10"));
});
```

## Notes

- **ConsumerName uniqueness:** Redis tracks pending messages per consumer name. If two instances share the same consumer name, one will claim messages the other cannot acknowledge, causing message loss or indefinite pending entries. Always assign a unique name per instance (e.g. using machine name, process ID, or a GUID).
- **BatchSize and latency:** A `BatchSize` of 1 minimises per-batch memory but increases the number of Redis round trips. Values above 100 are rarely beneficial unless message rates are extremely high.
- **MaxStreamLength trimming:** Trimming is approximate; Redis may retain slightly more entries than specified. Setting this to a low value on a high-throughput stream risks evicting unprocessed messages if consumers fall behind.
- **PollingInterval zero:** Setting `PollingInterval` to `TimeSpan.Zero` causes continuous polling without delay, which can saturate the CPU and Redis connection. Prefer at least 100 ms in production.
- **ErrorRetryDelay and backpressure:** During extended outages, the fixed delay applies between every retry. There is no built-in exponential backoff; combine with external circuit-breaker patterns if needed.
- **Thread safety:** `RedisStreamOptions` is a plain configuration object. Its properties are not synchronised. Configure it once at startup via `AddRedisStreamInvalidation` and treat it as immutable thereafter. The background listener reads values without locking, assuming they do not change at runtime.
