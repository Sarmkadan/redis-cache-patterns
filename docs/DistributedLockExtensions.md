# DistributedLockExtensions

DistributedLockExtensions provides a set of extension methods for managing distributed locks in a Redis-based environment. These methods simplify common concurrency control tasks, including acquiring locks with built-in retry logic, performing safe releases, monitoring lock expiration, and dynamically extending lock durations, ensuring robust synchronization across distributed service instances.

## API

### TryAcquireWithRetry
Attempts to acquire a distributed lock, utilizing an internal retry mechanism to handle transient failures or contention.
*   **Parameters**: `DistributedLock lock`, `TimeSpan timeout`, `int retryCount`
*   **Return Value**: `bool` indicating `true` if the lock was successfully acquired, `false` otherwise.
*   **Exceptions**: Throws `ArgumentNullException` if the `lock` instance is null.

### SafeRelease
Releases a distributed lock safely, suppressing exceptions that may occur if the lock is already released, invalid, or unreachable.
*   **Parameters**: `DistributedLock lock`
*   **Return Value**: `bool` indicating `true` if the lock was successfully released, `false` if the lock was not active or release failed.
*   **Exceptions**: Throws `ArgumentNullException` if the `lock` instance is null.

### IsAboutToExpire
Evaluates whether the remaining time-to-live (TTL) of the distributed lock is less than the specified threshold, indicating that the lock lease is nearing expiration.
*   **Parameters**: `DistributedLock lock`, `TimeSpan threshold`
*   **Return Value**: `bool` indicating `true` if the lock is nearing expiration, `false` otherwise.
*   **Exceptions**: Throws `ArgumentNullException` if the `lock` instance is null.

### WithExtendedDuration
Updates the lease duration of an existing distributed lock by adding the specified time increment.
*   **Parameters**: `DistributedLock lock`, `TimeSpan additionalDuration`
*   **Return Value**: A `DistributedLock` instance reflecting the extended duration.
*   **Exceptions**: Throws `ArgumentNullException` if the `lock` instance is null.

## Usage

```csharp
// Example 1: Acquiring and releasing a lock
DistributedLock myLock = GetLock("resource_key");
if (myLock.TryAcquireWithRetry(TimeSpan.FromSeconds(5), 3))
{
    try
    {
        // Perform critical section work
    }
    finally
    {
        myLock.SafeRelease();
    }
}
```

```csharp
// Example 2: Monitoring and extending a long-running operation
DistributedLock myLock = GetLock("long_task_key");
// ... operation in progress ...

if (myLock.IsAboutToExpire(TimeSpan.FromSeconds(10)))
{
    myLock = myLock.WithExtendedDuration(TimeSpan.FromMinutes(1));
}
```

## Notes

*   **Thread Safety**: While `DistributedLockExtensions` provides thread-safe access to the extension methods themselves, the underlying `DistributedLock` instances are managed in Redis. Ensure that your application logic correctly handles the distributed nature of these locks to prevent race conditions across service instances.
*   **Redis Connectivity**: These methods rely on an active Redis connection. Network interruptions or Redis node failures during acquisition or release may lead to `TryAcquireWithRetry` returning `false` or `SafeRelease` failing, even if the lock state appears valid locally.
*   **Clock Skew**: `IsAboutToExpire` calculations rely on the synchronization between the application server clock and the Redis server clock. Significant clock drift can affect the accuracy of the expiration threshold.
