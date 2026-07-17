# DistributedLockHelperExtensions

Provides extension methods for acquiring, releasing, and executing work under distributed locks backed by Redis. These helpers wrap the underlying `IDistributedLockManager` to offer retry logic, batch lock acquisition, and lock state inspection, simplifying common distributed-locking patterns in cache-heavy applications.

## API

### AcquireAsync

```csharp
public static async Task<bool> AcquireAsync(
    this IDistributedLockManager manager,
    string resourceKey,
    TimeSpan expiry,
    CancellationToken cancellationToken = default)
```

Attempts to acquire a distributed lock for the specified resource.

**Parameters:**
- `manager` — the lock manager instance.
- `resourceKey` — the unique key identifying the resource to lock.
- `expiry` — the duration after which the lock automatically expires.
- `cancellationToken` — optional cancellation token.

**Returns:** `true` if the lock was acquired; otherwise `false`.

**Throws:** `ArgumentNullException` if `manager` or `resourceKey` is `null`. `ArgumentException` if `resourceKey` is empty or whitespace.

---

### ExecuteWithRetryAsync

```csharp
public static async Task<bool> ExecuteWithRetryAsync(
    this IDistributedLockManager manager,
    string resourceKey,
    TimeSpan expiry,
    Func<Task> action,
    int maxRetries = 3,
    TimeSpan? retryDelay = null,
    CancellationToken cancellationToken = default)
```

Acquires a lock and executes the specified action, retrying on failure to acquire.

**Parameters:**
- `manager` : the lock manager instance.
- `resourceKey` : the key identifying the resource to lock.
- `expiry` : lock expiry duration.
- `action` : the delegate to execute while the lock is held.
- `maxRetries` : maximum number of acquisition attempts (default 3).
- `retryDelay` : delay between retries; defaults to a short internal value if `null`.
- `cancellationToken` : optional cancellation token.

**Returns:** `true` if the action executed successfully under the lock; `false` if the lock could not be acquired after all retries.

**Throws:** `ArgumentNullException` if `manager`, `resourceKey`, or `action` is `null`. `ArgumentException` if `resourceKey` is empty. Exceptions thrown by `action` propagate to the caller.

---

### ExecuteWithRetryAsync\<TResult\>

```csharp
public static async Task<TResult?> ExecuteWithRetryAsync<TResult>(
    this IDistributedLockManager manager,
    string resourceKey,
    TimeSpan expiry,
    Func<Task<TResult>> action,
    int maxRetries = 3,
    TimeSpan? retryDelay = null,
    CancellationToken cancellationToken = default)
```

Acquires a lock and executes an asynchronous function that returns a result, retrying on lock-acquisition failure.

**Parameters:**
- `manager` : the lock manager instance.
- `resourceKey` : the key identifying the resource to lock.
- `expiry` : lock expiry duration.
- `action` : the asynchronous function to execute while the lock is held.
- `maxRetries` : maximum number of acquisition attempts (default 3).
- `retryDelay` : delay between retries; a default is used if `null`.
- `cancellationToken` : optional cancellation token.

**Returns:** The result of `action` if the lock was acquired and the action completed; `default(TResult?)` if the lock could not be acquired after all retries.

**Throws:** `ArgumentNullException` if `manager`, `resourceKey`, or `action` is `null`. `ArgumentException` if `resourceKey` is empty. Exceptions from `action` propagate.

---

### ExecuteBatchAsync

```csharp
public static async Task<bool> ExecuteBatchAsync(
    this IDistributedLockManager manager,
    IEnumerable<string> resourceKeys,
    TimeSpan expiry,
    Func<Task> action,
    CancellationToken cancellationToken = default)
```

Acquires locks for multiple resources as a batch and executes an action only if all locks are acquired.

**Parameters:**
- `manager` : the lock manager instance.
- `resourceKeys` : the collection of resource keys to lock.
- `expiry` : lock expiry duration applied to each lock.
- `action` : the asynchronous action to execute while all locks are held.
- `cancellationToken` : optional cancellation token.

**Returns:** `true` if all locks were acquired and the action executed; `false` if any lock could not be acquired. Locks that were acquired are released before returning `false`.

**Throws:** `ArgumentNullException` if `manager`, `resourceKeys`, or `action` is `null`. `ArgumentException` if `resourceKeys` is empty or contains a null/empty key. Exceptions from `action` propagate; acquired locks are released before the exception bubbles up.

---

### IsHeld

```csharp
public static bool IsHeld(
    this IDistributedLockManager manager,
    [NotNullWhen(true)] string? lockValueHex)
```

Determines whether a lock is currently held based on its stored value.

**Parameters:**
- `manager` : the lock manager instance.
- `lockValueHex` : the hex-encoded lock value to inspect. The `[NotNullWhen(true)]` attribute indicates that when this method returns `true`, the argument was not `null`.

**Returns:** `true` if the lock is held (and `lockValueHex` is not null); otherwise `false`.

**Throws:** `ArgumentNullException` if `manager` is `null`.

---

### GetLockValueHex

```csharp
public static string? GetLockValueHex(
    this IDistributedLockManager manager,
    string resourceKey)
```

Retrieves the current lock value for a resource, encoded as a hexadecimal string.

**Parameters:**
- `manager` : the lock manager instance.
- `resourceKey` : the key identifying the resource.

**Returns:** The hex-encoded lock value if the lock exists; `null` if the resource is not locked.

**Throws:** `ArgumentNullException` if `manager` or `resourceKey` is `null`. `ArgumentException` if `resourceKey` is empty.

---

## Usage

### Example 1: Simple lock with retry

```csharp
var manager = serviceProvider.GetRequiredService<IDistributedLockManager>();
string cacheKey = "inventory:product-42";

bool executed = await manager.ExecuteWithRetryAsync(
    resourceKey: cacheKey,
    expiry: TimeSpan.FromSeconds(30),
    action: async () =>
    {
        // Rebuild cached inventory data while holding the lock
        await RebuildInventoryCacheAsync(productId: 42);
    },
    maxRetries: 5,
    retryDelay: TimeSpan.FromMilliseconds(200));

if (!executed)
{
    // Fallback: another process is already rebuilding
    await ServeStaleInventoryAsync(productId: 42);
}
```

### Example 2: Batch lock for multi-resource consistency

```csharp
var manager = new IDistributedLockManager();
var keys = new[] { "cache:users", "cache:orders", "cache:products" };

bool allLocked = await manager.ExecuteBatchAsync(
    resourceKeys: keys,
    expiry: TimeSpan.FromSeconds(15),
    action: async () =>
    {
        // Invalidate all three caches atomically
        await InvalidateUserCacheAsync();
        await InvalidateOrderCacheAsync();
        await InvalidateProductCacheAsync();
    });

if (!allLocked)
{
    _logger.LogWarning("Could not acquire all locks for batch invalidation; will retry next cycle");
}
```

## Notes

- **Lock release on failure:** In `ExecuteBatchAsync`, if any lock in the batch cannot be acquired, all previously acquired locks are released before returning `false`. This prevents partial lock holding.
- **Retry behavior:** `ExecuteWithRetryAsync` overloads only retry on lock-acquisition failure, not on exceptions thrown by the action. If the action throws, the exception propagates immediately and no further retries are attempted.
- **Null result vs. failure:** The generic `ExecuteWithRetryAsync<TResult>` returns `default(TResult?)` (typically `null` for reference types) when the lock cannot be acquired. Callers must distinguish between a genuinely `null` result from a successful action and a failure to acquire the lock. Consider using a wrapper type or nullable context checks when `null` is a valid action result.
- **`IsHeld` and stale data:** `IsHeld` inspects the lock value at the moment of the call. The lock may expire or be released by another process immediately after the check, so the result should be treated as advisory.
- **`GetLockValueHex`:** Returns `null` when no lock exists. The hex format is an implementation detail of the underlying lock manager and should not be parsed or interpreted by callers.
- **Thread safety:** These extension methods are designed for use in asynchronous, concurrent environments. They delegate to the underlying `IDistributedLockManager`, which must itself be thread-safe. The extension methods do not introduce additional shared state.
- **Cancellation:** When a `CancellationToken` is signaled, lock acquisition attempts are abandoned and the method may return `false` or throw `OperationCanceledException` depending on the underlying manager's implementation.
