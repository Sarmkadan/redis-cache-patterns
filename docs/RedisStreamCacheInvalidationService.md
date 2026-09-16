# RedisStreamCacheInvalidationService

`RedisStreamCacheInvalidationService` publishes cache invalidation events to a Redis Stream and runs as a background consumer that removes matching Redis keys. It supports exact-key and glob-pattern invalidation through `IRedisStreamInvalidationService` and uses a Redis consumer group to distribute stream entries among registered consumers.

## Types

### `IRedisStreamInvalidationService`

Defines the producer API for publishing a pre-built invalidation event, an exact cache key, or a key pattern. Consumers should depend on this interface when they only need to publish invalidations.

### `RedisStreamCacheInvalidationService`

The producer and consumer implementation. It derives from `BackgroundService`, publishes entries through `IDatabase.StreamAddAsync`, and continuously reads new entries through a Redis Stream consumer group.

## API

### Constructor

```csharp
public RedisStreamCacheInvalidationService(
    IRedisConnection redisConnection,
    ILogger<RedisStreamCacheInvalidationService> logger,
    RedisStreamOptions options)
```

Creates the service with its Redis connection, logger, and stream configuration.

* **`redisConnection`:** Provides the Redis database, connection, endpoints, and servers. Passing `null` throws `ArgumentNullException`.
* **`logger`:** Records publishing, consumer lifecycle, invalidation, and failure details. Passing `null` throws `ArgumentNullException`.
* **`options`:** Controls the stream key, consumer group and name, batch size, stream length, and retry intervals. If `null` is supplied directly, the constructor uses a new `RedisStreamOptions` instance.

### `PublishAsync`

```csharp
public Task PublishAsync(
    CacheInvalidationEvent invalidationEvent,
    CancellationToken cancellationToken = default)
```

Publishes an existing event to the configured stream. The stream entry contains `eventId`, `cacheKey`, `keyPattern`, `reason`, `source`, and `occurredAt`. Null key or pattern values are written as empty strings, and `occurredAt` is generated from `DateTime.UtcNow` when the entry is built.

Passing a `null` event throws `ArgumentNullException`. Redis publishing errors are logged and rethrown. The current implementation accepts the cancellation token but does not pass it to the Redis operation.

### `PublishAsync` for an exact key

```csharp
public Task PublishAsync(
    string cacheKey,
    InvalidationReason reason = InvalidationReason.DataUpdate,
    string source = "",
    CancellationToken cancellationToken = default)
```

Creates and publishes an event targeting one Redis key. A `null`, empty, or whitespace-only key throws `ArgumentException`. The cancellation token is forwarded to the event overload, subject to that overload's cancellation behavior.

### `PublishPatternAsync`

```csharp
public Task PublishPatternAsync(
    string keyPattern,
    InvalidationReason reason = InvalidationReason.DataUpdate,
    string source = "",
    CancellationToken cancellationToken = default)
```

Creates and publishes an event targeting every Redis key matching a glob-style pattern such as `product:*`. A `null`, empty, or whitespace-only pattern throws `ArgumentException`.

### Background execution

```csharp
protected override Task ExecuteAsync(CancellationToken stoppingToken)
```

Creates the configured consumer group at `StreamPosition.NewMessages` if necessary, then polls for new messages in batches. An existing group produces Redis's `BUSYGROUP` response, which is treated as a normal startup condition.

Each successfully handled message is acknowledged after its invalidation completes. A message that throws during processing is logged and left unacknowledged. When a poll returns no entries, the service waits for `PollingInterval`; after an unhandled loop error, it waits for `ErrorRetryDelay` before retrying.

## Usage

Register the same service instance as both the producer and hosted consumer with `AddRedisStreamInvalidation`:

```csharp
using RedisCachePatterns.Extensions;

builder.Services.AddRedisStreamInvalidation(options =>
{
    options.StreamKey = "catalog:cache:invalidations";
    options.ConsumerGroup = "catalog-cache-workers";
    options.BatchSize = 100;
    options.MaxStreamLength = 20_000;
});
```

Inject `IRedisStreamInvalidationService` wherever invalidations originate:

```csharp
using RedisCachePatterns.Domain;
using RedisCachePatterns.Services;

public sealed class ProductUpdater
{
    private readonly IRedisStreamInvalidationService _invalidations;

    public ProductUpdater(IRedisStreamInvalidationService invalidations)
    {
        _invalidations = invalidations;
    }

    public async Task ProductChangedAsync(int productId, CancellationToken cancellationToken)
    {
        await _invalidations.PublishAsync(
            $"product:{productId}",
            InvalidationReason.DataUpdate,
            nameof(ProductUpdater),
            cancellationToken);

        await _invalidations.PublishPatternAsync(
            "product:list:*",
            InvalidationReason.DependencyChange,
            nameof(ProductUpdater),
            cancellationToken);
    }
}
```

A caller can also supply an event explicitly:

```csharp
await invalidations.PublishAsync(new CacheInvalidationEvent
{
    CacheKey = "tenant:42:settings",
    Reason = InvalidationReason.ConfigurationChange,
    Source = "SettingsService"
}, cancellationToken);
```

## Processing behavior

1. The producer appends an entry to `RedisStreamOptions.StreamKey`, applying `MaxStreamLength` during the append.
2. The hosted service reads new entries for its configured consumer group and consumer name, up to `BatchSize` entries at a time.
3. If `cacheKey` is non-empty, the consumer deletes that exact Redis key.
4. Otherwise, if `keyPattern` is non-empty, the consumer enumerates matching keys on every non-replica endpoint and deletes them through the configured database.
5. Successfully dispatched entries are acknowledged as one batch.

## Notes

* **Consumer-group semantics:** Consumers sharing a group compete for messages; each stream entry is delivered to one consumer in that group, not broadcast to every instance. Use distinct group names when every deployment or logical consumer must process every event.
* **Exact keys take precedence:** If an entry contains both a non-empty `cacheKey` and `keyPattern`, only the exact key is deleted because dispatch returns after exact-key handling.
* **Empty targets are acknowledged:** An entry with neither a usable key nor pattern performs no deletion but completes successfully and is acknowledged.
* **Pending entries are not reclaimed:** Reads request only new messages. The service does not inspect, claim, or retry entries left in the consumer group's pending entries list after a process failure.
* **Pattern invalidation cost:** Pattern handling materializes all matching keys from each primary server before deleting them. Broad patterns can consume substantial time and memory on large Redis deployments.
* **Event fields:** The publisher does not serialize `CacheInvalidationEvent.Metadata`, and it writes a fresh publication timestamp instead of the event's `OccurredAt` value. The consumer reads `cacheKey`, `keyPattern`, `reason`, and `source`; an absent or unrecognized reason defaults to `DataUpdate`.
* **Cancellation:** The stopping token is checked between messages and controls polling and retry delays. Redis stream reads, acknowledgements, exact-key deletes, and server key enumeration do not receive that token. Pattern invalidation checks it only before dispatch begins and between batch messages, not while enumerating or deleting matched keys.
