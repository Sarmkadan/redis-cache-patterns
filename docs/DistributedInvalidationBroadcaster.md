# DistributedInvalidationBroadcaster

`DistributedInvalidationBroadcaster` coordinates cache invalidation across application nodes. It publishes exact-key or glob-pattern invalidation events through Redis Pub/Sub for immediate delivery, can additionally publish them to a Redis Stream, applies received events to the local cache, and keeps a bounded in-memory history of outbound broadcasts.

## Types

### `InvalidationHistoryEntry`

Represents one outbound invalidation attempt recorded by the local broadcaster.

* **`EventId`** (`string`): Identifier copied from the invalidation event. Defaults to a new GUID string.
* **`CacheKey`** (`string?`): Exact key for a key-based invalidation; otherwise `null`.
* **`KeyPattern`** (`string?`): Glob pattern for a pattern-based invalidation; otherwise `null`.
* **`Reason`** (`InvalidationReason`): Reason for the invalidation. Defaults to `DataUpdate`.
* **`Source`** (`string`): Service or component that requested the invalidation.
* **`OccurredAt`** (`DateTime`): UTC time at which the history entry was created.
* **`NodesNotified`** (`long`): Subscriber count returned by Redis Pub/Sub, or `-1` when broadcasting fails.

### `IDistributedInvalidationBroadcaster`

Defines the exact-key broadcast, pattern broadcast, subscription, and history APIs. Use this interface when consuming the broadcaster through dependency injection.

### `DistributedInvalidationBroadcaster`

The default implementation of `IDistributedInvalidationBroadcaster`. Each instance has a unique node identifier so it can ignore its own Pub/Sub messages.

## API

### Constructor

```csharp
public DistributedInvalidationBroadcaster(
    IRedisConnection redisConnection,
    ICacheService cacheService,
    ILogger<DistributedInvalidationBroadcaster> logger,
    DistributedInvalidationOptions? options = null,
    IRedisStreamInvalidationService? streamService = null)
```

Creates a broadcaster and attaches a handler to the Redis connection's `ConnectionRestored` event.

* **`redisConnection`:** Supplies the Redis connection used for Pub/Sub. Cannot be `null`.
* **`cacheService`:** Removes local cache entries when remote events arrive and flushes the cache after a reconnect. Cannot be `null`.
* **`logger`:** Records subscription, broadcast, validation, and cache-removal activity. Cannot be `null`.
* **`options`:** Configures the Pub/Sub channel, validation limits, history capacity, and stream fallback. When omitted, default options are created.
* **`streamService`:** Optional reliable-delivery publisher. It is used only when supplied and `UseStreamFallback` is enabled.

Passing `null` for a required dependency throws `ArgumentNullException`.

### `BroadcastAsync`

```csharp
public Task BroadcastAsync(
    string cacheKey,
    InvalidationReason reason = InvalidationReason.DataUpdate,
    string source = "",
    CancellationToken cancellationToken = default)
```

Broadcasts an invalidation for one exact cache key. An empty or `null` key throws `ArgumentException`. Before publishing, the method also rejects keys longer than `MaxKeyLength` or containing null, carriage-return, or newline characters with `InvalidOperationException`.

The event is published to the configured Redis Pub/Sub channel. If stream fallback is enabled and a stream service is available, it is then published to the stream. The cancellation token is passed to stream publishing; Redis Pub/Sub publishing itself does not receive the token.

### `BroadcastPatternAsync`

```csharp
public Task BroadcastPatternAsync(
    string keyPattern,
    InvalidationReason reason = InvalidationReason.DataUpdate,
    string source = "",
    CancellationToken cancellationToken = default)
```

Broadcasts an invalidation for keys matching a glob-style pattern such as `product:*`. An empty or `null` pattern throws `ArgumentException`. Patterns longer than `MaxKeyPatternLength` or containing null, carriage-return, or newline characters are rejected with `InvalidOperationException`.

Delivery and cancellation behavior are the same as for `BroadcastAsync`.

### `SubscribeAsync`

```csharp
public Task SubscribeAsync(CancellationToken cancellationToken = default)
```

Subscribes the instance to `DistributedInvalidationOptions.PubSubChannel`. Subscription failures are logged and rethrown. The current implementation does not pass the cancellation token to the Redis subscription operation.

For each valid message from another broadcaster instance, the receiver calls `RemoveAsync` for an exact key or `RemoveByPatternAsync` for a pattern. Malformed, oversized, or otherwise invalid messages are logged and rejected without escaping the callback.

### `GetHistory`

```csharp
public IReadOnlyList<InvalidationHistoryEntry> GetHistory()
```

Returns a snapshot of outbound broadcast attempts stored by this instance. Entries are enqueued in chronological order, so the current implementation returns the oldest retained entry first. The queue is trimmed to `MaxHistorySize` after each append.

Both successful and failed attempts are recorded. A failure is rethrown after its history entry is marked with `NodesNotified = -1`.

## Usage

```csharp
using Microsoft.Extensions.DependencyInjection;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Services;

var broadcaster = serviceProvider
    .GetRequiredService<IDistributedInvalidationBroadcaster>();

// Establish the local node's Pub/Sub subscription during startup.
await broadcaster.SubscribeAsync(stoppingToken);

// Invalidate one key on the other subscribed nodes.
await broadcaster.BroadcastAsync(
    "product:42",
    InvalidationReason.DataUpdate,
    "CatalogService",
    stoppingToken);

// Invalidate a group of keys.
await broadcaster.BroadcastPatternAsync(
    "product:list:*",
    InvalidationReason.ManualPurge,
    "AdminPortal",
    stoppingToken);
```

```csharp
foreach (var entry in broadcaster.GetHistory())
{
    var target = entry.CacheKey ?? entry.KeyPattern;
    Console.WriteLine($"{entry.OccurredAt:u} {target} ({entry.NodesNotified} subscribers)");
}
```

## Delivery behavior

1. The sender creates a `CacheInvalidationEvent` and wraps it with its instance identifier and current connection generation.
2. Redis Pub/Sub delivers the serialized message to currently subscribed nodes.
3. When configured, the sender also publishes the event through `IRedisStreamInvalidationService` for reliable fallback delivery.
4. A receiving instance ignores messages carrying its own instance identifier. Other current-generation messages remove the exact key or matching local keys.
5. When the Redis connection is restored, the broadcaster increments its generation and synchronously flushes the local cache to avoid serving entries that may have become stale while disconnected. A subsequently received older-generation message also causes a local flush.

## Validation and limits

* Incoming messages are limited to 64 KiB when measured as UTF-8 and are validated before JSON deserialization.
* Keys and patterns use the configured maximum lengths. The implementation compares `string.Length`, despite error messages describing these values as byte lengths.
* Event metadata allows at most 100 entries. Metadata keys are limited to 256 characters, values to 1,024 characters, and neither may contain a null character.
* The public broadcast methods do not expose metadata, but these checks protect received events and any internally constructed event passed to the core broadcaster.

## Notes

* **Pub/Sub subscriber count:** `NodesNotified` is the count returned by Redis for the publish operation. It does not prove that each subscriber successfully removed its local cache entry.
* **Local invalidation:** The originating instance ignores its own Pub/Sub message and does not directly remove its own cache entry. Callers must update or invalidate the originating node's cache separately when required.
* **History scope:** History is local, in memory, and records outbound attempts only. Received invalidations are not appended.
* **Stream fallback:** Supplying a stream service alone is not enough; `UseStreamFallback` must also be enabled. Stream consumption is handled outside this class.
* **Reconnect handling:** The constructor subscribes to `ConnectionRestored`. The class does not implement `IDisposable` and does not detach that event handler.
* **Subscription lifecycle:** `SubscribeAsync` does not track subscription state or explicitly unsubscribe. Redis client subscription behavior determines what happens if it is called more than once.
