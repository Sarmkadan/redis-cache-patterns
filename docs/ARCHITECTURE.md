# Architecture

This document describes the actual structure of the codebase as it is today. If something
here disagrees with the code, the code wins - fix the doc.

## What this project is

`RedisCachePatterns` is a **class library** (see `RedisCachePatterns.csproj`,
`OutputType=Library`, net10.0) that packages production-grade Redis caching patterns on top
of StackExchange.Redis: cache-aside, write-through, distributed locks, tag/pattern
invalidation, cross-instance invalidation (pub/sub + streams), compression, negative
caching, circuit breaking, warming, and metrics.

The repo also carries demo entry points (`Program.cs`, `Program.Web.cs`), example programs
(`examples/`), tests (`tests/`) and benchmarks (`benchmarks/`), but all of those are
excluded from library compilation via `Compile Remove` in the csproj. Only the library
itself is what `dotnet build` produces and what gets packed as `sarmkadan.redis-cache-patterns`.

## High-level layout

```
Services/           Cache pattern implementations + demo business services
Infrastructure/
  Cache/            IRedisConnection / RedisConnection (+ cluster variants)
  Repositories/     IRepository<T> + in-memory demo repositories
Domain/             Entities (Product, Order, User, ...) and cache value objects
                    (CachePolicy, CacheEntry, DistributedLock, CacheKeyMetadata)
Configuration/      DI extensions, options types, CacheConfiguration
Utilities/          CacheKeyBuilder, RetryHelper, CompressionUtil, IdempotencyHelper, ...
Monitoring/         CacheMetricsCollector, HealthCheckService, DiagnosticsProvider,
                    CacheAnalyticsDashboard
Events/             EventPublisher, CacheEventListener, OrderEventHandler
Middleware/         ASP.NET-style middleware (compiled but only usable from a host app)
API/                Minimal-API endpoint classes (CacheEndpoint, ProductEndpoint, ...)
BackgroundWorkers/  Timer-based workers (cleanup, warming, inventory rebalance)
Extensions/         ServiceCollection + Redis Streams extensions
Exceptions/         CacheException, BusinessException
Results/            OperationResult
CLI/                Simple command parser + cache/product commands (demo)
Integration/        ExternalApiClient, WebhookHandler (demo integrations)
```

## The core abstraction: ICacheService

`Services/ICacheService.cs` is the single contract every consumer codes against. The
actual surface (as of now):

- **Cache-aside**: `GetOrLoadAsync<T>(key, loadFn, ttl?)`, `GetAsync<T>`, `SetAsync<T>`,
  `GetOrLoadWithSlidingExpirationAsync<T>`, `GetKeyMetadataAsync`
- **Write-through**: `WriteAsync<T>(key, value, persistFn, ttl?)` - persists via
  `persistFn` first, caches only on success
- **Invalidation**: `RemoveAsync`, `RemoveByPatternAsync`, `ExistsAsync`,
  `GetExpirationAsync`, `GetKeysByPatternAsync`, `FlushAsync`
- **Distributed locks**: `AcquireLockAsync(lockKey, lockValue, duration)`,
  `ReleaseLockAsync`, `RenewLockAsync` - value-checked so only the owner can release
- **Policies & stats**: `SetPolicyAsync` / `GetPolicyAsync` (per-key `CachePolicy` used
  when a call passes no TTL), `GetStatisticsAsync` returning `CacheStatistics`
  (keys, memory, hits/misses, computed hit rate)

### Implementations

| Class | Notes |
|---|---|
| `RedisCacheService` | The primary single-node implementation. Policy lookups go through a `FrozenDictionary` snapshot swapped under a `Lock` (lock-free hot-path reads). Keeps per-key recompute-time estimates for XFetch-style early expiration. Deserialization failures are treated as misses: the corrupt entry is evicted and reloaded. |
| `RedisClusterCacheService` | Cluster-aware variant. Normal ops rely on StackExchange.Redis MOVED/ASK routing; the added value is fanning out SCAN, FLUSHDB and statistics aggregation across all master nodes in parallel. |
| `CompressedCacheService` | Decorator over another `ICacheService` that gzips values above a threshold. Registered manually when wanted - it is not wired by default DI. |

Supporting services that are *not* `ICacheService` implementations but compose with it:
`NegativeCacheService` (caches "not found" results with a short TTL so repeated misses do
not hammer the source), `CacheCircuitBreakerService` (open/half-open/closed state around
cache calls so a dead Redis degrades to source-of-truth reads instead of timing out every
request), `CacheTagService` (tag -> key sets for group invalidation),
`CacheInvalidationService`, `CacheWarmingService` + `CacheWarmingStrategies`
(delegate/priority/parallel/pattern-refresh strategies plus a scheduler), and
`BatchProcessingService<T>`.

## Cross-instance invalidation

Two mechanisms exist, deliberately layered:

1. **`DistributedInvalidationBroadcaster`** (`IDistributedInvalidationBroadcaster`) -
   pub/sub based. Publishes `CacheInvalidationEvent`s on a Redis channel; subscribers
   remove the key/pattern from their local view. Keeps a bounded in-memory history
   (`ConcurrentQueue<InvalidationHistoryEntry>`) for diagnostics.
2. **`RedisStreamCacheInvalidationService`** - a `BackgroundService` consuming a Redis
   Stream with a consumer group. Pub/sub is fire-and-forget: an instance that is down
   misses the message. The stream is the reliable fallback - the broadcaster optionally
   dual-publishes to it (`DistributedInvalidationOptions.UseStreamFallback`), and
   restarted instances catch up from their group offset.

Trade-off recorded on purpose: pub/sub for latency, streams for delivery guarantees.
Neither replaces TTLs - policies still cap staleness if both channels fail.

## Connectivity

`Infrastructure/Cache/` holds `IRedisConnection` / `RedisConnection` (lazy
`ConnectionMultiplexer`, `IsConnectedAsync`, ping) and `IRedisClusterConnection` /
`RedisClusterConnection` for cluster topologies. Everything above this layer talks
`IRedisConnection`, never the multiplexer directly - that is the seam used by tests to
substitute fakes.

## Demo domain (repositories + business services)

`Domain/`, `Infrastructure/Repositories/` and the `UserService` / `ProductService` /
`OrderService` / `InventoryService` classes exist to *demonstrate* the patterns against a
realistic shape (products, orders, inventory), not to be a real store: the repositories
are in-memory. They show cache-aside on reads, write-through on mutations, and
distributed-lock-guarded order confirmation. Treat them as reference wiring, replaceable
by your own repositories.

## Dependency injection

`Configuration/DependencyInjectionExtensions.cs` exposes:

- `AddRedisCachePatterns(connectionString = "localhost:6379")` - full demo stack:
  connection, `RedisCacheService`, all four repositories and business services
  (singletons).
- `AddRedisCache(CacheConfiguration?)` - just connection + cache service, for consumers
  who only want the caching layer. Config falls back to
  `CacheConfiguration.FromEnvironment()`.
- `ValidateRedisConnectionAsync(IServiceProvider)` - startup ping helper.

There is also an options-based path (`RedisCachePatternsOptions`, configured in
`Program.cs` from `REDIS_*` environment variables) and cluster registration in
`ClusterDependencyInjectionExtensions`.

## Key design decisions and their trade-offs

- **One wide `ICacheService` instead of per-pattern interfaces.** Consumers get a single
  injection point and patterns compose (write-through calls into the same policy/TTL
  machinery as cache-aside). Cost: the interface is large, and decorators like
  `CompressedCacheService` must implement all of it even when only get/set matter.
- **Single atomic GET, never EXISTS-then-GET.** Documented directly on the interface:
  a key can expire between the two calls (TOCTOU), so implementations must check
  `HasValue` on one GET and treat null as a miss. This is why there is no
  `TryGet`-style two-step API.
- **Lock ownership by value.** `AcquireLockAsync` takes a caller-supplied `lockValue`
  (instance id) and release/renew verify it, so a slow instance cannot release a lock
  that has since been re-acquired by someone else. Comparison uses `RedisValue ==` to
  avoid string allocation on the hot path. This is the single-key lock, not Redlock -
  good enough with a single master + failover, consciously not multi-master safe.
- **JSON (System.Text.Json) as the only wire format.** Debuggable in redis-cli and
  schema-tolerant; slower and fatter than MessagePack/protobuf. The compression decorator
  exists precisely to claw back the size cost for large values.
- **Metadata as sibling hash keys** (`{key}:meta` with createdAt/lastAccessed/hitCount/
  size fields) rather than wrapping values in envelopes. Values stay clean JSON readable
  by other clients; cost is a second key per entry and no atomicity between value and
  metadata updates.
- **Per-key `CachePolicy` registry inside the service** (frozen-snapshot dictionary)
  instead of forcing every call site to pass TTLs. Central place to tune expiration;
  cost is process-local state - policies are not shared across instances unless set on
  each.

## Data flow: cache-aside read

```
caller -> ICacheService.GetOrLoadAsync(key, loadFn, ttl?)
            |- GET key (single atomic read)
            |    hit  -> deserialize -> update :meta -> return
            |    corrupt -> evict, fall through as miss
            |- miss -> loadFn() (timed; feeds XFetch recompute estimate)
            |- SET key ttl (ttl ?? policy ?? no expiry)
            '- return value
```

## Data flow: distributed invalidation

```
instance A: broadcaster.BroadcastInvalidationAsync(key)
   |- PUBLISH invalidation channel  (fast path)
   '- XADD invalidation stream      (if UseStreamFallback)

instance B (alive):    pub/sub handler -> cacheService.RemoveAsync(key)
instance C (was down): RedisStreamCacheInvalidationService catches up
                       from consumer-group offset on restart
```

## Extension points

- Implement `ICacheService` for another backend, or decorate an existing one
  (`CompressedCacheService` is the template for decorators).
- Add a `CacheWarmingStrategy` subclass and register it with the scheduler.
- `IRepository<T>` for real data stores in place of the in-memory demos.
- `OutputFormatter` (`Formatters/`) for new CLI output formats (json/csv/xml exist).
- `RedisStreamOptions` / `DistributedInvalidationOptions` to retune the invalidation
  pipeline without code changes.

## Known limitations

- The Dockerfile publishes the project and sets `ENTRYPOINT dotnet RedisCachePatterns.dll`,
  but the csproj is a **library** with `Program.cs`/`Program.Web.cs` excluded from
  compilation, so the container as defined has no entry point to run. The demo entry
  points are reference code; hosting them requires a small host project (or re-including
  them with `OutputType=Exe`).
- Repositories are in-memory; there is no real database integration.
- `RemoveByPatternAsync`/`GetKeysByPatternAsync` use SCAN - O(keyspace) operations meant
  for admin/maintenance, not hot request paths.
- Cache policies and invalidation history are per-process, not replicated.
- Locks are single-master; no Redlock quorum.

## Tests and benchmarks

`tests/redis-cache-patterns.Tests` (xUnit) covers utilities, the warming strategies,
compression, batch processing, the broadcaster and the demo services, plus mock-based
integration tests for cache-aside and write-through - no live Redis required.
`benchmarks/redis-cache-patterns.Benchmarks` (BenchmarkDotNet) measures key building,
serialization, compression and lock acquisition.
