# DistributedInvalidationOptions

`DistributedInvalidationOptions` configures the limits and delivery behavior used by `DistributedInvalidationBroadcaster`. It controls the Redis Pub/Sub channel, the in-memory history limit, cache-key and pattern limits, the pattern-invalidation batch limit, and optional Redis Stream publishing. All properties have defaults, so the class can be used without an object initializer.

## API

### `PubSubChannel`
`public string PubSubChannel`

The Redis Pub/Sub channel used to publish and receive immediate invalidation notifications. Every application instance that participates in the same invalidation group must use the same channel. Defaults to `"cache:invalidation:broadcast"`.

### `MaxHistorySize`
`public int MaxHistorySize`

The maximum number of invalidation history entries retained in memory by the broadcaster. When the limit is reached, entries are removed from the queue before new entries are recorded. Defaults to `500`.

### `MaxKeyLength`
`public int MaxKeyLength`

The maximum permitted cache-key length. The broadcaster rejects a longer key with `InvalidOperationException` before publishing it. Defaults to `1024`.

Although the option is described as a byte limit, the current broadcaster compares it with `string.Length`; the effective limit is therefore measured in UTF-16 code units rather than encoded UTF-8 bytes.

### `MaxKeyPatternLength`
`public int MaxKeyPatternLength`

The maximum permitted cache-key pattern length. The broadcaster rejects a longer pattern with `InvalidOperationException` before publishing it. Defaults to `1024`.

As with `MaxKeyLength`, the current implementation compares this value with `string.Length`, so the effective limit is measured in UTF-16 code units.

### `MaxPatternInvalidationBatchSize`
`public int MaxPatternInvalidationBatchSize`

The configured maximum number of keys that may be invalidated by one pattern operation. Defaults to `1000`. This property expresses the intended bulk-invalidation safety limit, but the current `DistributedInvalidationBroadcaster` implementation does not read or enforce it.

### `UseStreamFallback`
`public bool UseStreamFallback`

Controls whether the broadcaster also publishes invalidation events through `IRedisStreamInvalidationService` after publishing them with Redis Pub/Sub. Defaults to `true`. Stream publishing occurs only when this option is enabled and an `IRedisStreamInvalidationService` has been supplied; otherwise Pub/Sub remains the only delivery mechanism.

### `ToString`
`public override string ToString()`

Returns a diagnostic summary containing `PubSubChannel`, `MaxHistorySize`, and `UseStreamFallback`. The key-length and pattern batch settings are not included.

## Usage

### Registering customized options

```csharp
var invalidationOptions = new DistributedInvalidationOptions
{
    PubSubChannel = "orders:cache:invalidation",
    MaxHistorySize = 1_000,
    MaxKeyLength = 512,
    MaxKeyPatternLength = 256,
    MaxPatternInvalidationBatchSize = 500,
    UseStreamFallback = true
};

services.AddDistributedInvalidation(invalidationOptions);
```

Passing `null` or omitting the argument to `AddDistributedInvalidation` registers a new instance containing the defaults.

### Using Pub/Sub without stream fallback

```csharp
services.AddDistributedInvalidation(new DistributedInvalidationOptions
{
    PubSubChannel = "catalog:cache:invalidation",
    UseStreamFallback = false
});
```

## Notes

- Use a dedicated `PubSubChannel` when separate applications or environments share a Redis deployment but must not invalidate one another's caches.
- `UseStreamFallback` does not register the stream service. The service must be registered separately and supplied to the broadcaster for stream publishing to occur.
- The class does not validate values when properties are assigned. Choose positive limits; zero or negative length and history limits can cause operations to fail or produce unintuitive retention behavior.
- Cache keys and patterns containing null, carriage-return, or newline characters are rejected independently of the configured length limits.
- Configure an instance during application startup and treat it as immutable after registration. The properties are mutable and provide no synchronization for concurrent changes.