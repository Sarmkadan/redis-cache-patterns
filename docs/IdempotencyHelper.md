# IdempotencyHelper

`IdempotencyHelper` provides a mechanism for ensuring that an operation is executed exactly once, even if the same call is made multiple times with identical parameters. It uses Redis to track whether a given key has already been processed, stores the result of the operation, and allows subsequent callers to retrieve that cached result without re-executing the underlying logic. This is useful for protecting against duplicate requests in distributed systems, such as payment processing or order submission.

## API

### `public IdempotencyHelper`

The constructor. Initializes a new instance of the helper bound to a specific idempotency key. The key is typically derived from a correlation ID, request payload hash, or other unique identifier that defines the scope of the idempotent operation.

- **Parameters**: Accepts the Redis connection configuration and the idempotency key string.
- **Exceptions**: May throw if the underlying Redis connection cannot be established during initialization.

### `public bool IsProcessed`

Gets a value indicating whether the operation associated with this key has already been marked as processed. Returns `true` if the key exists in the idempotency store; otherwise `false`.

### `public void MarkAsProcessed<T>`

Marks the operation as processed and stores a result of type `T`. This should be called after the operation completes successfully. Once marked, subsequent calls with the same key will see `IsProcessed == true` and can retrieve the stored result.

- **Type Parameter `T`**: The type of the result being stored.
- **Parameters**: The result object to persist.
- **Exceptions**: May throw if the Redis write operation fails.

### `public T? GetResult<T>`

Retrieves the previously stored result for this idempotency key. Returns the deserialized result cast to type `T`, or `null` if no result has been stored or the key has not been processed.

- **Type Parameter `T`**: The expected type of the stored result.
- **Returns**: The cached result, or `null`.
- **Exceptions**: May throw if deserialization fails due to a type mismatch.

### `public async Task<T> ExecuteIdempotentlyAsync<T>`

The primary entry point. Checks whether the key has already been processed. If it has, returns the cached result immediately without invoking the provided operation. If it has not, executes the operation, marks the key as processed, stores the result, and returns it.

- **Type Parameter `T`**: The return type of the operation.
- **Parameters**: A `Func<Task<T>>` representing the operation to execute idempotently.
- **Returns**: The result of the operation, either freshly executed or retrieved from cache.
- **Exceptions**: Propagates exceptions thrown by the operation itself. May throw if Redis operations fail.

### `public int CleanupExpiredRecords`

A static or instance method that removes idempotency records from Redis that have exceeded their configured time-to-live. This prevents unbounded growth of the idempotency store.

- **Returns**: The number of records removed.
- **Exceptions**: May throw if the Redis cleanup command fails.

### `public string Key`

Gets the idempotency key associated with this helper instance. This is the unique identifier used to track whether the operation has been processed.

### `public object? Result`

Gets the raw stored result object, if any. This is the value that was passed to `MarkAsProcessed` or stored by `ExecuteIdempotentlyAsync`. Returns `null` if no result has been stored.

### `public DateTime ProcessedAt`

Gets the timestamp indicating when the operation was marked as processed. Returns `DateTime.MinValue` or equivalent default if the key has not yet been processed.

## Usage

### Example 1: Protecting a Payment Endpoint

```csharp
public async Task<PaymentResult> ProcessPaymentAsync(string requestId, PaymentDetails details)
{
    var helper = new IdempotencyHelper(redisConnection, $"payment:{requestId}");

    return await helper.ExecuteIdempotentlyAsync(async () =>
    {
        // This block runs only once per requestId
        var result = await _paymentGateway.ChargeAsync(details);
        await _auditLog.RecordAsync(requestId, result.TransactionId);
        return result;
    });
}
```

### Example 2: Checking Status Before Acting

```csharp
public async Task<IActionResult> HandleWebhookAsync(string eventId)
{
    var helper = new IdempotencyHelper(_redis, $"webhook:{eventId}");

    if (helper.IsProcessed)
    {
        var cached = helper.GetResult<WebhookResponse>();
        _logger.LogInformation("Webhook {EventId} already processed at {Time}", eventId, helper.ProcessedAt);
        return Ok(cached);
    }

    var response = await ProcessWebhookPayloadAsync();
    helper.MarkAsProcessed(response);
    return Ok(response);
}
```

## Notes

- **Thread Safety**: The helper itself does not guarantee distributed atomicity for the check-then-act pattern. `ExecuteIdempotentlyAsync` should be relied upon for safe execution, as it internally handles the race condition where two concurrent callers attempt to process the same key.
- **Result Type Mismatch**: Calling `GetResult<T>` with a type `T` that differs from the type originally stored will cause a deserialization error. Ensure consistency between the type used in `MarkAsProcessed` and subsequent `GetResult` calls.
- **Null Results**: Storing a `null` result via `MarkAsProcessed` is permitted. `GetResult<T>` will return `null` in that case, which is indistinguishable from the key not being processed. Use `IsProcessed` to disambiguate.
- **Expiration and Cleanup**: Idempotency records have a finite TTL configured at initialization. `CleanupExpiredRecords` is a maintenance operation that can be called periodically or on a schedule to remove stale entries that were not naturally evicted by Redis TTL.
- **Exception Handling**: If the operation passed to `ExecuteIdempotentlyAsync` throws, the key is not marked as processed. The next call with the same key will attempt the operation again, which is the intended behavior for transient failures.
