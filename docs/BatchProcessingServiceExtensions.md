# BatchProcessingServiceExtensions

The `BatchProcessingServiceExtensions` class provides a suite of static extension methods designed to simplify interactions with batch processing systems implemented within the `redis-cache-patterns` library. These methods provide high-level abstractions for managing queued data, enabling developers to efficiently enqueue multiple items, monitor queue size, manage state, and implement threshold-based batching logic without directly interacting with underlying Redis primitives.

## API

### EnqueueRange&lt;T&gt;
Enqueues a collection of items into the associated processing queue.

*   **Parameters:** `IEnumerable&lt;T&gt; items` - The collection of objects to be added to the queue.
*   **Return Value:** `void`
*   **Throws:** `ArgumentNullException` if `items` is null.

### GetQueueSize&lt;T&gt;
Retrieves the current number of pending items in the queue for the specified type.

*   **Parameters:** None.
*   **Return Value:** `int` representing the total number of items currently in the queue.

### ClearQueue&lt;T&gt;
Removes all pending items from the queue associated with the specified type.

*   **Parameters:** None.
*   **Return Value:** `int` representing the number of items that were removed.

### EnqueueIfBelowThreshold&lt;T&gt;
Attempts to enqueue a single item only if the current queue size is strictly less than the specified limit.

*   **Parameters:**
    *   `T item` - The object to enqueue.
    *   `int threshold` - The maximum allowable size of the queue for the operation to proceed.
*   **Return Value:** `bool` - `true` if the item was enqueued; `false` if the queue size reached or exceeded the threshold.

## Usage

### Bulk Enqueuing
```csharp
var items = new List<Product> 
{ 
    new Product { Id = 1, Name = "Widget" }, 
    new Product { Id = 2, Name = "Gadget" } 
};

// Assuming _batchService implements IBatchProcessingService
_batchService.EnqueueRange(items);
```

### Threshold-Controlled Batching
```csharp
var newInventoryItem = new InventoryItem { Sku = "A123", Count = 10 };
const int MaxBatchSize = 50;

if (_batchService.EnqueueIfBelowThreshold(newInventoryItem, MaxBatchSize))
{
    Console.WriteLine("Item added to processing queue.");
}
else
{
    Console.WriteLine("Queue is full, skipping enqueue.");
}
```

## Notes

*   **Thread Safety:** While these extension methods are static, they rely on the underlying implementation of `IBatchProcessingService` and the Redis client driver. Users should assume that operations on the Redis queue are atomic at the command level, but individual application-level transactions may require external distributed locking if strict consistency across multiple calls is required.
*   **Generic Constraints:** These methods operate on generic types. The internal implementation expects type `T` to be serializable, typically via JSON, to be stored within Redis.
*   **Edge Cases:** In scenarios involving high-concurrency, `GetQueueSize` should be treated as a point-in-time estimate. The actual size may change immediately after the method returns due to concurrent processing by background workers.
