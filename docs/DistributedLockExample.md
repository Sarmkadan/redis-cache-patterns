# DistributedLockExample

Demonstrates patterns for coordinating concurrent access to shared resources in a distributed Redis-backed environment. This type provides ready-to-use implementations of locking, stampede protection, and multi-resource lock acquisition, each tailored to a specific business operation such as order processing, retrieval, refund, and shipment confirmation.

## API

### `DistributedLockExample`

Instantiates the example class. The constructor expects the dependencies required to connect to Redis and manage distributed locks internally; these are injected by the host and are not part of the public surface documented here.

### `async Task<OperationResult> ProcessOrderWithLockAsync`

Acquires a single exclusive distributed lock on the order resource and executes the processing logic under that lock.

- **Parameters:** Order identifier and processing details (exact parameter names are internal to the implementation; the public signature accepts the order ID and a payload object).
- **Returns:** `OperationResult` indicating success or a specific failure reason (e.g., lock acquisition failure, processing error).
- **Throws:** `ArgumentNullException` when the order ID is null or empty. `RedisConnectionException` when the underlying Redis connection cannot be established during the lock attempt.

### `async Task<Order?> GetOrderWithStampedeProtectionAsync`

Retrieves an order while preventing cache-stampede scenarios. Instead of a traditional lock, it uses a protective pattern that allows only one caller to regenerate the cached value when it is missing or expired, while other concurrent callers wait for that result or fall back to a stale value.

- **Parameters:** The order identifier.
- **Returns:** The `Order` instance if found; `null` when the order does not exist in the primary data store.
- **Throws:** `ArgumentNullException` when the order ID is null or empty. `RedisConnectionException` when the Redis coordination channel is unreachable.

### `async Task<OperationResult> RefundOrderWithTimeoutLockAsync`

Attempts to acquire a distributed lock with a finite timeout before performing a refund operation. If the lock cannot be obtained within the configured time window, the operation is abandoned to avoid indefinite blocking.

- **Parameters:** Order identifier and refund details (amount, reason). The timeout is governed by internal configuration.
- **Returns:** `OperationResult` with a status of `Success` when the refund is processed, or `LockTimeout` when the lock was not acquired in time.
- **Throws:** `ArgumentNullException` when the order ID is null or empty. `InvalidOperationException` when the order is not in a refundable state. `RedisConnectionException` when Redis is unavailable.

### `async Task<OperationResult> ConfirmAndShipOrderWithLocksAsync`

Coordinates two related resources—order confirmation and inventory shipment—by acquiring locks on both the order and the inventory item before proceeding. The locks are obtained in a consistent order to prevent deadlocks, and both must be held simultaneously for the operation to succeed.

- **Parameters:** Order identifier and shipment details (carrier, tracking number).
- **Returns:** `OperationResult` indicating success, lock-acquisition failure on either resource, or a business-rule violation (e.g., order already shipped).
- **Throws:** `ArgumentNullException` when the order ID or shipment details are null. `RedisConnectionException` when Redis is unavailable. `DeadlockPreventionException` (internal, surfaced as a faulted task) when the lock-ordering invariant is violated due to misconfiguration.

## Usage

### Example 1: Processing an order with exclusive lock and stampede-protected read

```csharp
var example = new DistributedLockExample(/* injected dependencies */);

// Safely retrieve the order without causing a cache stampede.
Order? order = await example.GetOrderWithStampedeProtectionAsync("order-12345");
if (order is null)
{
    Console.WriteLine("Order not found.");
    return;
}

// Process the order under an exclusive distributed lock.
OperationResult result = await example.ProcessOrderWithLockAsync("order-12345", processingDetails);
if (result.IsSuccess)
{
    Console.WriteLine($"Order {order.Id} processed successfully.");
}
else
{
    Console.WriteLine($"Processing failed: {result.Error}");
}
```

### Example 2: Refund with timeout and coordinated shipment confirmation

```csharp
var example = new DistributedLockExample(/* injected dependencies */);

// Attempt a refund, giving up if the lock cannot be acquired quickly.
OperationResult refundResult = await example.RefundOrderWithTimeoutLockAsync("order-67890", refundDetails);
if (refundResult.Status == OperationStatus.LockTimeout)
{
    Console.WriteLine("Could not acquire lock in time; refund aborted.");
    return;
}

if (!refundResult.IsSuccess)
{
    Console.WriteLine($"Refund failed: {refundResult.Error}");
    return;
}

// Confirm and ship, requiring locks on both order and inventory.
OperationResult shipResult = await example.ConfirmAndShipOrderWithLocksAsync("order-67890", shipmentDetails);
Console.WriteLine(shipResult.IsSuccess
    ? "Order confirmed and shipped."
    : $"Shipment failed: {shipResult.Error}");
```

## Notes

- **Lock ordering in `ConfirmAndShipOrderWithLocksAsync`:** The implementation acquires locks in a deterministic global order (e.g., lexicographic by resource key) to prevent deadlocks across concurrent callers. Changing the resource key format without updating the ordering logic can reintroduce deadlock risk.
- **Stampede protection vs. locking:** `GetOrderWithStampedeProtectionAsync` does not hold an exclusive lock for the duration of the data-store read. It uses a short-lived coordination primitive to elect a single regenerator. Callers that arrive while regeneration is in flight may receive a stale-but-valid cached value if one exists, rather than blocking.
- **Timeout behavior:** `RefundOrderWithTimeoutLockAsync` uses a configurable timeout. If the lock is held by another process for longer than the timeout, the method returns a non-success result immediately; it does not queue or retry. Callers must implement their own retry/compensation logic if desired.
- **Thread safety:** All public methods are safe to invoke concurrently from multiple threads or tasks. The underlying Redis primitives are used in a non-blocking async fashion, and the class holds no mutable shared state beyond the injected Redis multiplexer, which is itself thread-safe.
- **Exception propagation:** Redis connectivity exceptions are surfaced to callers rather than swallowed, allowing the application to decide on fallback behavior (e.g., circuit breaking). Business-logic violations such as refunding an already-shipped order throw `InvalidOperationException` synchronously before any lock is attempted.
