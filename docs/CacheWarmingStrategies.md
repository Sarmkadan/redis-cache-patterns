# Cache Warming Strategies

The types in `Services/CacheWarmingStrategies.cs` provide reusable ways to populate or refresh an `ICacheService`. They support ordered delegate execution, priority-based warming, bounded parallel warming, pattern-based refreshes, and recurring execution through a scheduler.

All types are in the `RedisCachePatterns.Services` namespace.

## Enum: `WarmingPriority`

Defines the order used by `PriorityWarmingStrategy`.

| Value | Numeric value | Description |
|-------|---------------|-------------|
| `Low` | `0` | Background or speculative entries, loaded last. |
| `Normal` | `1` | The default priority. |
| `High` | `2` | Important entries loaded before normal entries. |
| `Critical` | `3` | Essential entries loaded first. |

## Class: `WarmingEntry`

Describes one cache entry to warm.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Key` | `string` | `string.Empty` | Cache key under which the value is stored. |
| `ValueFactory` | `Func<Task<object?>>` | A factory returning `null` | Asynchronously loads the value. A `null` result skips the entry. |
| `Expiration` | `TimeSpan?` | `null` | Optional expiration passed to `ICacheService.SetAsync`; `null` uses the cache service's default behavior. |
| `Priority` | `WarmingPriority` | `WarmingPriority.Normal` | Priority used by `PriorityWarmingStrategy`. |

## Interface: `ICacheWarmingStrategy`

Defines a named cache-warming operation.

### Property: `Name`

```csharp
string Name { get; }
```

Returns the human-readable strategy name used in diagnostics.

### Method: `ExecuteAsync`

```csharp
Task<int> ExecuteAsync(ICacheService cacheService)
```

Executes the strategy against `cacheService` and returns the number of values successfully written. Concrete strategies handle their expected loading and write failures internally rather than failing the entire entry set.

## Class: `DelegateWarmingStrategy`

Warms a fixed list of entries sequentially in declaration order.

### Constructor

```csharp
public DelegateWarmingStrategy(
    string name,
    IEnumerable<WarmingEntry> entries,
    ILogger<DelegateWarmingStrategy> logger)
```

- `name`: Human-readable strategy name.
- `entries`: Entries to evaluate in enumeration order. The sequence is copied during construction.
- `logger`: Logger used for successful writes and per-entry failures.

### Method: `ExecuteAsync`

Calls each entry's `ValueFactory`, then writes each non-null value with its optional expiration. A failed factory or cache write is logged and does not prevent later entries from running. The returned count includes only successful writes.

## Class: `PriorityWarmingStrategy`

Groups entries by priority and processes the groups from `Critical` through `Low`. Entries within a priority bucket execute sequentially in insertion order.

### Constructor

```csharp
public PriorityWarmingStrategy(
    string name,
    ILogger<PriorityWarmingStrategy> logger)
```

### Method: `Add`

```csharp
public PriorityWarmingStrategy Add(WarmingEntry entry)
```

Adds an entry to the bucket identified by `entry.Priority` and returns the same strategy instance for fluent configuration.

### Method: `ExecuteAsync`

Processes every non-empty priority bucket in descending enum order. Null factory results are skipped, while factory and cache-write exceptions are logged per entry. The method returns the total number of successful writes across all priorities.

## Class: `ParallelWarmingStrategy`

Warms a fixed entry list concurrently while limiting active entry operations with a semaphore.

### Constructor

```csharp
public ParallelWarmingStrategy(
    string name,
    IEnumerable<WarmingEntry> entries,
    ILogger<ParallelWarmingStrategy> logger,
    int maxDegreeOfParallelism = 4)
```

- `name`: Human-readable strategy name.
- `entries`: Entries to warm. The sequence is copied during construction.
- `logger`: Logger used for diagnostics.
- `maxDegreeOfParallelism`: Maximum number of entry operations allowed at once; defaults to `4`.

### Method: `ExecuteAsync`

Creates one task per entry and bounds concurrent execution to `maxDegreeOfParallelism`. It awaits all entry tasks, counts successful writes with an atomic increment, skips null factory results, and logs failures without stopping other tasks.

## Class: `PatternRefreshWarmingStrategy`

Enumerates existing keys matching a glob-style pattern and reloads their values. This strategy refreshes matching keys; it does not discover entries that are absent from the cache.

### Constructor

```csharp
public PatternRefreshWarmingStrategy(
    string name,
    string keyPattern,
    Func<string, Task<object?>> reloadFn,
    TimeSpan? expiration,
    ILogger<PatternRefreshWarmingStrategy> logger)
```

- `name`: Human-readable strategy name.
- `keyPattern`: Pattern passed to `ICacheService.GetKeysByPatternAsync`, such as `product:*`.
- `reloadFn`: Loads a fresh value for each matching key. Returning `null` skips that key.
- `expiration`: Expiration applied to every refreshed value.
- `logger`: Logger used for scan, refresh, and completion diagnostics.

### Method: `ExecuteAsync`

First obtains matching keys from the cache service. If enumeration fails, the error is logged and the method returns `0`. Each key is then reloaded and written sequentially; individual reload or write failures are logged and processing continues. The return value is the number of successful refreshes.

## Class: `CacheWarmingScheduler`

Runs a `CacheWarmingService` immediately and then at a fixed interval by using a `Timer`. The scheduler implements `IDisposable`.

### Constructor

```csharp
public CacheWarmingScheduler(
    CacheWarmingService warmingService,
    ILogger<CacheWarmingScheduler> logger,
    TimeSpan? interval = null)
```

- `warmingService`: Service whose registered strategies are executed. Cannot be `null`.
- `logger`: Logger for scheduler lifecycle and execution diagnostics. Cannot be `null`.
- `interval`: Delay between timer callbacks; defaults to six hours.

**Exceptions:** Throws `ArgumentNullException` when `warmingService` or `logger` is `null`.

### Method: `Start`

```csharp
public void Start()
```

Starts the timer with a zero due time, so the first warming cycle is triggered immediately. Later callbacks occur at the configured interval.

**Exceptions:** Throws `InvalidOperationException` if the scheduler is already running.

### Method: `Stop`

```csharp
public void Stop()
```

Stops and disposes the timer. Calling `Stop` before `Start`, or after the scheduler has already stopped, is a no-op.

### Method: `Dispose`

```csharp
public void Dispose()
```

Stops the scheduler and releases its timer. It is safe to call when the scheduler is not running.

## Usage

### Ordered and parallel warming

```csharp
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Services;

var ordered = new DelegateWarmingStrategy(
    "reference-data",
    new[]
    {
        new WarmingEntry
        {
            Key = "countries:all",
            ValueFactory = async () => await repository.GetCountriesAsync(),
            Expiration = TimeSpan.FromHours(12)
        }
    },
    loggerFactory.CreateLogger<DelegateWarmingStrategy>());

var parallel = new ParallelWarmingStrategy(
    "popular-products",
    productIds.Select(id => new WarmingEntry
    {
        Key = $"product:{id}",
        ValueFactory = async () => await repository.GetProductAsync(id),
        Expiration = TimeSpan.FromMinutes(30)
    }),
    loggerFactory.CreateLogger<ParallelWarmingStrategy>(),
    maxDegreeOfParallelism: 8);

int orderedCount = await ordered.ExecuteAsync(cacheService);
int parallelCount = await parallel.ExecuteAsync(cacheService);
```

### Priority warming

```csharp
var priorityStrategy = new PriorityWarmingStrategy(
        "startup-data",
        loggerFactory.CreateLogger<PriorityWarmingStrategy>())
    .Add(new WarmingEntry
    {
        Key = "configuration:current",
        Priority = WarmingPriority.Critical,
        ValueFactory = async () => await repository.GetConfigurationAsync()
    })
    .Add(new WarmingEntry
    {
        Key = "recommendations:default",
        Priority = WarmingPriority.Low,
        ValueFactory = async () => await repository.GetDefaultRecommendationsAsync()
    });

int warmed = await priorityStrategy.ExecuteAsync(cacheService);
```

### Pattern refresh and scheduling

```csharp
var refresh = new PatternRefreshWarmingStrategy(
    "product-refresh",
    "product:*",
    async key => await repository.ReloadProductByCacheKeyAsync(key),
    TimeSpan.FromMinutes(30),
    loggerFactory.CreateLogger<PatternRefreshWarmingStrategy>());

warmingService.AddStrategy(refresh);

using var scheduler = new CacheWarmingScheduler(
    warmingService,
    loggerFactory.CreateLogger<CacheWarmingScheduler>(),
    TimeSpan.FromHours(1));

scheduler.Start();
// Call scheduler.Stop() during graceful application shutdown.
```

## Notes

- A returned count represents successful cache writes, not attempted entries. Null values and failed operations are not counted.
- `DelegateWarmingStrategy`, `PriorityWarmingStrategy`, and `PatternRefreshWarmingStrategy` process entries sequentially. `ParallelWarmingStrategy` is the bounded-concurrency option.
- Configure a `PriorityWarmingStrategy` before executing it. Although its dictionary coordinates bucket lookup, each bucket is a mutable `List<WarmingEntry>` and should not be modified concurrently with other additions or execution.
- Choose `maxDegreeOfParallelism` according to the capacity of both Redis and the upstream data source. Values less than one cause semaphore construction to fail when `ExecuteAsync` begins.
- Timer callbacks are not serialized. If a warming cycle takes longer than the configured interval, scheduler executions can overlap; select an interval that exceeds the expected run time or coordinate execution in the warming service.
- `Stop` prevents future timer callbacks but does not cancel a warming cycle already in progress.
