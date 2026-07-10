# DistributedLock

Represents a lease‑based lock acquired from a Redis instance. The type holds the lock’s metadata (key, value, timestamps, and retry policy) and provides methods to acquire, release, and renew the lock while exposing its current state.

## API

### Key
- **Purpose:** The Redis key used to store the lock.
- **Type:** `string` (read‑only after construction).
- **Exceptions:** None.

### LockValue
- **Purpose:** The unique value set in Redis for this lock instance (typically a combination of holder identifier and random suffix).
- **Type:** `string` (read‑only after construction).
- **Exceptions:** None.

### AcquiredAt
- **Purpose:** UTC timestamp indicating when the lock was successfully acquired.
- **Type:** `DateTime` (read‑only; `DateTime.MinValue` if the lock has not been acquired).
- **Exceptions:** None.

### Duration
- **Purpose:** The lease time that is requested from Redis for each lock acquisition.
- **Type:** `TimeSpan` (read‑only after construction).
- **Exceptions:** None.

### HolderIdentifier
- **Purpose:** Identifier of the client or service that attempts to hold the lock.
- **Type:** `string` (read‑only after construction).
- **Exceptions:** None.

### RetryCount
- **Purpose:** Number of acquisition attempts that will be made before giving up.
- **Type:** `int` (read‑only after construction; zero or positive).
- **Exceptions:** None.

### RetryDelay
- **Purpose:** Delay between consecutive acquisition attempts.
- **Type:** `TimeSpan` (read‑only after construction; zero or positive).
- **Exceptions:** None.

### IsAcquired
- **Purpose:** Indicates whether the lock is currently held by this instance.
- **Type:** `bool` (read‑only; updates after `Acquire` or `Release`).
- **Exceptions:** None.

### Constructors
- **DistributedLock(string key, string holderIdentifier, TimeSpan duration)**
  - **Purpose:** Creates a lock specification with default retry policy (3 attempts, 200 ms delay).
  - **Parameters:**
    - `key`: Redis key for the lock; must not be `null` or whitespace.
    - `holderIdentifier`: Identifier of the lock holder; must not be `null` or whitespace.
    - `duration`: Desired lease time; must be greater than `TimeSpan.Zero`.
  - **Exceptions:**
    - `ArgumentNullException` if `key` or `holderIdentifier` is `null`.
    - `ArgumentException` if `key` or `holderIdentifier` is empty/whitespace.
    - `ArgumentOutOfRangeException` if `duration` ≤ `TimeSpan.Zero`.

- **DistributedLock(string key, string holderIdentifier, TimeSpan duration, int retryCount, TimeSpan retryDelay)**
  - **Purpose:** Creates a lock specification with explicit retry policy.
  - **Parameters:** Same as above plus:
    - `retryCount`: Number of acquisition attempts; must be ≥ 0.
    - `retryDelay`: Delay between attempts; must be ≥ `TimeSpan.Zero`.
  - **Exceptions:** Same as the first constructor, plus:
    - `ArgumentOutOfRangeException` if `retryCount` < 0 or `retryDelay` < `TimeSpan.Zero`.

### Acquire
- **Purpose:** Attempts to obtain the lock from Redis, retrying according to the configured policy.
- **Signature:** `public void Acquire()`
- **Return:** None.
- **Exceptions:**
  - `TimeoutException` if the lock cannot be acquired after all retry attempts.
  - `IOException` (or derived) if communication with Redis fails.
  - `InvalidOperationException` if `IsAcquired` is already `true` (re‑entrant acquisition not allowed).

### Release
- **Purpose:** Releases the lock if it is currently held by this instance.
- **Signature:** `public void Release()`
- **Return:** None.
- **Exceptions:**
  - `InvalidOperationException` if `IsAcquired` is `false` or the stored lock value does not match `LockValue`.
  - `IOException` if the release command fails to reach Redis.

### CanRenew
- **Purpose:** Determines whether the lock can be renewed based on remaining lease time and holder identity.
- **Signature:** `public bool CanRenew { get; }`
- **Return:** `true` if the lock is held and sufficient time remains to extend the lease; otherwise `false`.
- **Exceptions:** None.

### RenewLock
- **Purpose:** Extends the lock’s lease in Redis for another `Duration` period.
- **Signature:** `public void RenewLock()`
- **Return:** None.
- **Exceptions:**
  - `InvalidOperationException` if the lock is not held (`IsAcquired` is `false`) or `CanRenew` is `false`.
  - `IOException` if the renewal command fails.

### ToString
- **Purpose:** Returns a string representation useful for debugging, showing key, holder, acquisition state, and timestamps.
- **Signature:** `public override string ToString()`
- **Return:** `string` containing the lock’s diagnostic information.
- **Exceptions:** None.

## Usage

### Basic acquisition and release
```csharp
var locker = new DistributedLock(
    key: "resource:widget:123",
    holderIdentifier: Environment.MachineName,
    duration: TimeSpan.FromSeconds(30));

locker.Acquire();
try
{
    // Critical section – access shared resource
    ProcessWidget(123);
}
finally
{
    locker.Release();   // Ensure lock is freed even if an exception occurs
}
```

### Lock with automatic renewal
```csharp
var locker = new DistributedLock(
    key: "cache:lock:session-999",
    holderIdentifier: Guid.NewGuid().ToString(),
    duration: TimeSpan.FromSeconds(15),
    retryCount: 5,
    retryDelay: TimeSpan.FromMilliseconds(100));

locker.Acquire();
try
{
    var renewalTimer = new PeriodicTimer(TimeSpan.FromSeconds(5));
    await foreach (var _ in renewalTimer.WaitForNextTickAsync())
    {
        if (!locker.CanRenew)
            break;   // lock is about to expire or already lost
        locker.RenewLock();
    }

    // Perform long‑running operation while lock is held
    await DoLongRunningWorkAsync();
}
finally
{
    locker.Release();
}
```

## Notes
- The members `Key`, `LockValue`, `AcquiredAt`, `Duration`, `HolderIdentifier`, `RetryCount`, `RetryDelay`, and `IsAcquired` are read‑only after construction; only `IsAcquired` may change during the object's lifetime.
- `Acquire` is **not** thread‑safe. Calling it concurrently from multiple threads on the same `DistributedLock` instance can lead to undefined behavior; external synchronization is required if shared access is needed.
- The lock does **not** automatically renew itself; callers must invoke `RenewLock` (or implement a renewal loop) to prevent expiration during long operations.
- Clock skew between the client and Redis server can affect the perceived remaining lease; `CanRenew` uses the client’s clock only, so consider a safety margin when scheduling renewals.
- If the Redis instance experiences a network partition, `Release` may fail to delete the key, leaving the lock orphaned until its natural expiration; applications should be tolerant of such scenarios.
- The type does not implement `IDisposable`; explicit calls to `Release` (or use of a `try/finally` block) are required to free resources.
