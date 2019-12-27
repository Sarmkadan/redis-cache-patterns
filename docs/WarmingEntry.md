# WarmingEntry

A lightweight descriptor used by the cache‑warming infrastructure to define what should be warmed into the Redis cache, how the value is produced, and which warming strategy should apply.

## API

### `public string Key`
Gets or sets the cache key associated with this warming entry.  
- **Return value:** the key string.  
- **Exceptions:** none.

### `public Func<Task<object?>> ValueFactory`
Gets or sets the asynchronous factory that produces the value to be cached.  
- **Return value:** a delegate that, when invoked, returns a `Task<object?>` representing the cached value.  
- **Exceptions:** none (the delegate itself may throw when invoked).

### `public TimeSpan? Expiration`
Gets or sets the optional expiration time for the cached value. If `null`, the item uses the cache’s default expiration policy.  
- **Return value:** a nullable `TimeSpan` representing the lifetime, or `null`.  
- **Exceptions:** none.

### `public WarmingPriority Priority`
Gets or sets the priority level that determines the order in which this entry is processed by priority‑aware strategies.  
- **Return value:** a member of the `WarmingPriority` enumeration.  
- **Exceptions:** none.

### `public DelegateWarmingStrategy DelegateWarmingStrategy`
Gets or sets the delegate‑based warming strategy associated with this entry.  
- **Return value:** an instance of `DelegateWarmingStrategy` that defines how the `ValueFactory` is executed.  
- **Exceptions:** none.

### `public override async Task<int> ExecuteAsync`
Executes the warming operation for this entry using its configured strategy.  
- **Parameters:** none.  
- **Return value:** a `Task<int>` yielding the number of items successfully warmed (typically `0` or `1`).  
- **Exceptions:** propagates any exception thrown by `ValueFactory`; may also throw `ObjectDisposedException` if the entry has been disposed.

### `public PriorityWarmingStrategy PriorityWarmingStrategy`
Gets or sets the priority‑based warming strategy associated with this entry.  
- **Return value:** an instance of `PriorityWarmingStrategy` that groups entries by priority.  
- **Exceptions:** none.

### `public PriorityWarmingStrategy Add`
Adds this warming entry to the priority‑based strategy and returns the strategy for fluent chaining.  
- **Parameters:** none.  
- **Return value:** the same `PriorityWarmingStrategy` instance to allow further configuration.  
- **Exceptions:** throws `InvalidOperationException` if the entry is already part of another strategy.

### `public ParallelWarmingStrategy ParallelWarmingStrategy`
Gets or sets the parallel warming strategy associated with this entry.  
- **Return value:** an instance of `ParallelWarmingStrategy` that executes multiple entries concurrently.  
- **Exceptions:** none.

### `public override async Task<int> ExecuteAsync`
Executes the warming operation using the parallel strategy.  
- **Parameters:** none.  
- **Return value:** a `Task<int>` yielding the count of entries warmed in parallel.  
- **Exceptions:** propagates exceptions from any underlying `ValueFactory`; may throw `ObjectDisposedException`.

### `public PatternRefreshWarmingStrategy PatternRefreshWarmingStrategy`
Gets or sets the pattern‑refresh warming strategy associated with this entry.  
- **Return value:** an instance of `PatternRefreshWarmingStrategy` that warms keys matching a specific pattern.  
- **Exceptions:** none.

### `public override async Task<int> ExecuteAsync`
Executes the warming operation using the pattern‑refresh strategy.  
- **Parameters:** none.  
- **Return value:** a `Task<int>` yielding the number of keys warmed that matched the pattern.  
- **Exceptions:** propagates exceptions from `ValueFactory`; may throw `ObjectDisposedException`.

### `public CacheWarmingScheduler CacheWarmingScheduler`
Gets or sets the scheduler responsible for triggering warming cycles for this entry.  
- **Return value:** an instance of `CacheWarmingScheduler`.  
- **Exceptions:** none.

### `public void Start`
Begins the warming process according to the attached scheduler and strategy.  
- **Parameters:** none.  
- **Return value:** none.  
- **Exceptions:** throws `ObjectDisposedException` if the entry has been disposed; throws `InvalidOperationException` if no scheduler is assigned.

### `public void Stop`
Halts any ongoing warming operations and prevents further scheduled executions.  
- **Parameters:** none.  
- **Return value:** none.  
- **Exceptions:** throws `ObjectDisposedException` if the entry has been disposed.

### `public void Dispose`
Releases all resources held by the warming entry, including any internal timers or cancellations.  
- **Parameters:** none.  
- **Return value:** none.  
- **Exceptions:** none.

## Usage

### Basic warming entry with a delegate factory
```csharp
var entry = new WarmingEntry
{
    Key = "user:123:profile",
    ValueFactory = async () =>
    {
        // Simulate fetching from a data store
        return await GetUserProfileFromDbAsync(123);
    },
    Expiration = TimeSpan.FromMinutes(30),
    Priority = WarmingPriority.High
};

// Assign a simple delegate strategy
entry.DelegateWarmingStrategy = new DelegateWarmingStrategy();

// Attach to a scheduler and start warming
var scheduler = new CacheWarmingScheduler();
entry.CacheWarmingScheduler = scheduler;
scheduler.Add(entry);
scheduler.Start();

// Later, when shutting down
scheduler.Stop();
entry.Dispose();
```

### Using the priority‑based warming strategy
```csharp
var highPri = new WarmingEntry
{
    Key = "config:latest",
    ValueFactory = async () => await LoadLatestConfigAsync(),
    Expiration = TimeSpan.FromHours(1),
    Priority = WarmingPriority.High
};

var lowPri = new WarmingEntry
{
    Key = "metrics:aggregated",
    ValueFactory = async () => await ComputeAggregatedMetricsAsync(),
    Expiration = TimeSpan.FromMinutes(5),
    Priority = WarmingPriority.Low
};

var priorityStrategy = new PriorityWarmingStrategy();
// Fluent Add calls
priorityStrategy.Add(highPri).Add(lowPri);

var scheduler = new CacheWarmingScheduler();
priorityStrategy.Scheduler = scheduler;
scheduler.Start();

// The scheduler will process high‑priority entries before low‑priority ones.
```

## Notes

- Setting `ValueFactory` to `null` will cause `ExecuteAsync` to throw a `NullReferenceException` when the factory is invoked.  
- `Expiration` must be `null` or a non‑negative `TimeSpan`; negative values are not validated by the type itself but will be rejected by the underlying cache provider.  
- The `Priority` value influences ordering only when the entry is used with a `PriorityWarmingStrategy`; other strategies ignore it.  
- Properties are not thread‑safe; concurrent modification of `Key`, `ValueFactory`, `Expiration`, `Priority`, or strategy references should be synchronized externally.  
- `Start` and `Stop` may be called multiple times; subsequent calls after the first have no effect unless the scheduler has been reset.  
- `Dispose` should be called exactly once; after disposal, any attempt to use `Start`, `Stop`, or `ExecuteAsync` will result in an `ObjectDisposedException`.  
- The `Add` method on `PriorityWarmingStrategy` is intended for fluent configuration; calling it on an entry already assigned to another strategy will invalidate the previous assignment and may lead to undefined behavior if the old strategy is still active.  
- Implementations of the various strategy classes (`DelegateWarmingStrategy`, `ParallelWarmingStrategy`, `PatternRefreshWarmingStrategy`) are expected to handle their own internal concurrency; the `WarmingEntry` type itself does not enforce locking beyond what is described.
