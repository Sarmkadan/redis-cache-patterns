# DistributedLockHelper

Helper that simplifies acquiring, releasing, and executing code under a Redis‑based distributed lock. It encapsulates the typical SET‑NX/PX lock pattern and provides both manual lock control and convenient execute‑while‑locked overloads.

## API

### DistributedLockHelper
**Purpose**  
Creates a new lock helper bound to a specific Redis resource.

**Parameters**  
- `redis` (`IConnectionMultiplexer`): Multiplexer used to communicate with the Redis server.  
- `resourceId` (`string`): Unique identifier of the lock (often a key name).  
- `lockTimeout` (`TimeSpan?`, optional): Maximum time to wait when attempting to acquire the lock; if null, the helper uses a default timeout.  
- `expiry` (`TimeSpan?`, optional): Duration after which the lock automatically expires; if null, a sensible default is applied.

**Return value**  
An instance ready for lock operations.

**Exceptions**  
- `ArgumentNullException` if `redis` or `resourceId` is null.  
- `ObjectDisposedException` if the helper has already been disposed.

### AcquireAsync
**Purpose**  
Attempts to acquire the distributed lock.

**Parameters**  
- `cancellationToken` (`CancellationToken`, optional): Token to observe for cancellation requests.

**Return value**  
`Task<bool>`: `true` if the lock was successfully acquired; `false` if the acquisition timed out or was cancelled.

**Exceptions**  
- `ObjectDisposedException` if the helper is disposed.  
- `OperationCanceledException` if the supplied token is cancelled before completion.  
- `TimeoutException` if the underlying Redis operation exceeds the configured lock timeout.

### ReleaseAsync
**Purpose**  
Releases the lock if it is currently held by this helper.

**Parameters**  
- `cancellationToken` (`CancellationToken`, optional): Token to observe for cancellation requests.

**Return value**  
`Task<bool>`: `true` if the lock was released; `false` if the lock was not held or could not be released.

**Exceptions**  
- `ObjectDisposedException` if the helper is disposed.  
- `OperationCanceledException` if the supplied token is cancelled before completion.

### ExecuteAsync
**Purpose**  
Executes an asynchronous action while holding the lock, acquiring and releasing it automatically.

**Parameters**  
- `action` (`Func<Task>`): Asynchronous delegate to run under lock protection.  
- `cancellationToken` (`CancellationToken`, optional): Token to observe for cancellation requests.

**Return value**  
`Task<bool>`: `true` if the lock was acquired and the action completed; `false` if the lock could not be acquired.

**Exceptions**  
- `ObjectDisposedException` if the helper is disposed.  
- `OperationCanceledException` if the supplied token is cancelled before completion.  
- Any exception thrown by `action` is propagated upward; the lock is still released before propagation.

### ExecuteAsync<TResult>
**Purpose**  
Executes an asynchronous function while holding the lock, acquiring and releasing it automatically, and returns the function’s result.

**Parameters**  
- `func` (`Func<Task<TResult>>`): Asynchronous delegate that returns a result.  
- `cancellationToken` (`CancellationToken`, optional): Token to observe for cancellation requests.

**Return value**  
`Task<TResult>`: The result of `func` if the lock was acquired; otherwise the task completes with a `false`‑like sentinel (the method returns `default(TResult)` and the caller should check the returned `Task<bool>`‑style outcome via an overload or wrapper—see usage). In this design the method returns the result directly and throws if the lock cannot be acquired.

**Exceptions**  
- `ObjectDisposedException` if the helper is disposed.  
- `OperationCanceledException` if the supplied token is cancelled before completion.  
- `TimeoutException` if lock acquisition exceeds the lock timeout.  
- Any exception thrown by `func` is propagated upward; the lock is still released before propagation.

### DisposeAsync
**Purpose**  
Asynchronously releases any held resources (e.g., cancels pending lock attempts) and disposes the underlying Redis connection if owned.

**Parameters**  
None.

**Return value**  
`ValueTask` that completes when disposal is finished.

**Exceptions**  
- None under normal operation; disposing multiple times is safe.

### Dispose
**Purpose**  
Synchronously releases any held resources and disposes the underlying Redis connection if owned.

**Parameters**  
None.

**Return value**  
None.

**Exceptions**  
- None under normal operation; disposing multiple times is safe.

## Usage

### Manual lock acquisition and release
```csharp
using var redis = ConnectionMultiplexer.Connect("localhost");
using var lockHelper = new DistributedLockHelper(redis, "my-resource");

// Try to acquire the lock with a 5‑second wait
if (await lockHelper.AcquireAsync(CancellationToken.None))
{
    try
    {
        // Critical section – only one instance can run this at a time
        await DoWorkAsync();
    }
    finally
    {
        // Always release the lock when done
        await lockHelper.ReleaseAsync(CancellationToken.None);
    }
}
else
{
    // Could not obtain lock; handle contention (e.g., retry, fallback, logging)
    Logger.Warning("Lock not acquired for my-resource");
}
```

### Using the execute‑while‑locked helper
```csharp
using var redis = ConnectionMultiplexer.Connect("localhost");
using var lockHelper = new DistributedLockHelper(redis, "cache-update");

// Execute a function that updates a cached value; the lock is managed automatically
bool success = await lockHelper.ExecuteAsync(async () =>
{
    var freshData = await FetchData = await GetFreshDataFromSourceAsync();
    await redis.GetDatabase().StringSetAsync("my-cache-key", fresh);
    return true; // indicate success
}, CancellationToken.None);

if (!success)
{
    // Lock could not be acquired; optionally retry or alert
    Logger.Error("Failed to acquire lock for cache update");
}
```

## Notes
- The helper is **not reentrant**: attempting to acquire the lock again from the same thread while it is already held will fail unless the lock is first released.  
- Lock acquisition respects the supplied `cancellationToken`; if cancellation is requested before the lock is obtained, `AcquireAsync` returns `false` and `ExecuteAsync` overloads return `false`/`default` accordingly.  
- If the helper is disposed while a lock is held, the lock is **not** automatically released; callers should ensure they release any held lock before disposal to avoid leaving stale locks in Redis.  
- All asynchronous methods are safe to call concurrently from multiple threads; internal Redis operations are serialized per‑instance, but concurrent calls will contend for the lock as expected.  
- The default lock expiry (if none is provided) is chosen to be longer than the expected maximum execution time of the protected code to reduce the risk of automatic expiration while work is in progress. Adjust the `expiry` parameter if your workloads exceed this default.  
- Errors from the underlying Redis connection (e.g., timeouts, server unavailability) are bubbled up as exceptions; callers should handle them according to their resilience strategy.
