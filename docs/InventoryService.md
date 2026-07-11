# InventoryService

The `InventoryService` class provides an asynchronous interface for managing inventory state within the `redis-cache-patterns` project, leveraging Redis for high-performance caching and data consistency. It handles core inventory operations including retrieval by various keys, stock reservation and release mechanisms, and stock movement (receiving and dispatching). The service is designed to minimize database load by utilizing cached data patterns while ensuring atomic updates for critical stock modifications.

## API

### `public InventoryService`
Initializes a new instance of the `InventoryService` class. This constructor typically injects necessary dependencies such as Redis connection multiplexers, database contexts, or logging facilities required to execute cached inventory operations.

### `public async Task<InventoryItem?> GetInventoryByIdAsync`
Retrieves a specific inventory item using its unique identifier.
*   **Parameters**: Accepts a unique identifier (typically `string` or `Guid`, depending on project configuration) for the inventory record.
*   **Return Value**: Returns an `InventoryItem` object if found; otherwise, returns `null`.
*   **Exceptions**: May throw exceptions related to Redis connectivity or serialization errors if the cache is unreachable or corrupted.

### `public async Task<InventoryItem?> GetByProductAndWarehouseAsync`
Fetches an inventory record based on the composite key of a specific product and warehouse location.
*   **Parameters**: Requires a product identifier and a warehouse identifier.
*   **Return Value**: Returns the matching `InventoryItem` or `null` if no stock exists for that combination.
*   **Exceptions**: Throws on cache access failures or invalid argument formats.

### `public async Task<IEnumerable<InventoryItem>> GetInventoryByProductAsync`
Retrieves all inventory records associated with a specific product across all warehouses.
*   **Parameters**: Accepts a product identifier.
*   **Return Value**: Returns an enumerable collection of `InventoryItem` objects. Returns an empty collection if no records are found.
*   **Exceptions**: Propagates errors occurring during cache scanning or key pattern matching.

### `public async Task<bool> ReserveInventoryAsync`
Attempts to atomically reserve a specified quantity of stock for a given item, preventing other processes from allocating the same units.
*   **Parameters**: Requires an item identifier and the quantity to reserve.
*   **Return Value**: Returns `true` if the reservation was successful; `false` if insufficient stock is available or the item is locked.
*   **Exceptions**: May throw during race condition handling if the underlying Redis script fails unexpectedly.

### `public async Task<bool> ReleaseReservationAsync`
Releases a previously held reservation on inventory stock, making the units available for other transactions.
*   **Parameters**: Requires the reservation identifier or the original item context used to create the reservation.
*   **Return Value**: Returns `true` if the release was successful; `false` if the reservation did not exist or had already expired.
*   **Exceptions**: Throws if the consistency check between the cache and the release token fails.

### `public async Task<bool> ReceiveStockAsync`
Increments the stock count for a specific inventory item, representing new goods arriving at a warehouse.
*   **Parameters**: Accepts the item identifier and the quantity to add.
*   **Return Value**: Returns `true` upon successful update of the cache and persistent store; `false` if the operation was rejected due to validation rules.
*   **Exceptions**: Throws on concurrency conflicts if the record was modified during the update window.

### `public async Task<bool> DispatchStockAsync`
Decrements the stock count for a specific inventory item, representing goods leaving the warehouse.
*   **Parameters**: Accepts the item identifier and the quantity to remove.
*   **Return Value**: Returns `true` if stock was successfully deducted; `false` if insufficient stock exists to fulfill the dispatch.
*   **Exceptions**: Propagates errors if the resulting stock level would violate negative inventory constraints.

### `public async Task<IEnumerable<InventoryItem>> GetLowStockItemsAsync`
Retrieves a list of inventory items where the current quantity falls below a defined threshold.
*   **Parameters**: Optionally accepts a threshold value; if omitted, uses a system default.
*   **Return Value**: Returns a collection of `InventoryItem` objects requiring restocking.
*   **Exceptions**: Throws if the index or key pattern used to query low stock items is unavailable.

### `public async Task<int> GetTotalProductQuantityAsync`
Calculates the aggregate quantity of a specific product available across all warehouse locations.
*   **Parameters**: Accepts a product identifier.
*   **Return Value**: Returns the total sum of quantities as an integer.
*   **Exceptions**: Throws if aggregation logic fails due to missing shard data or cache timeouts.

## Usage

### Example 1: Reserving Stock for an Order
This example demonstrates retrieving inventory for a specific product and warehouse, then attempting to reserve units for an incoming order.

```csharp
public async Task ProcessOrderAsync(string productId, string warehouseId, int quantity)
{
    var inventory = await _inventoryService.GetByProductAndWarehouseAsync(productId, warehouseId);
    
    if (inventory == null)
    {
        throw new InvalidOperationException("Inventory record not found.");
    }

    var isReserved = await _inventoryService.ReserveInventoryAsync(inventory.Id, quantity);
    
    if (!isReserved)
    {
        // Handle insufficient stock or concurrency conflict
        Console.WriteLine($"Failed to reserve {quantity} units for product {productId}.");
        return;
    }

    // Proceed with order creation logic
    await CreateOrderRecordAsync(productId, quantity);
}
```

### Example 2: Restocking and Verifying Levels
This example illustrates receiving new stock and subsequently checking if the product still appears in the low-stock report.

```csharp
public async Task RestockProductAsync(string productId, int incomingQuantity)
{
    // Assume we know the inventory ID or retrieve it first
    var items = await _inventoryService.GetInventoryByProductAsync(productId);
    
    foreach (var item in items)
    {
        await _inventoryService.ReceiveStockAsync(item.Id, incomingQuantity / items.Count());
    }

    // Verify if the product still requires attention
    var lowStockItems = await _inventoryService.GetLowStockItemsAsync();
    var stillLow = lowStockItems.Any(x => x.ProductId == productId);

    if (!stillLow)
    {
        Console.WriteLine($"Product {productId} is now sufficiently stocked.");
    }
}
```

## Notes

*   **Concurrency and Atomicity**: Methods modifying state (`ReserveInventoryAsync`, `ReleaseReservationAsync`, `ReceiveStockAsync`, `DispatchStockAsync`) utilize atomic Redis operations (such as Lua scripts) to prevent race conditions. However, callers should always check the boolean return value to confirm success, as a `false` return indicates a logical failure (e.g., insufficient stock) rather than a system exception.
*   **Cache Consistency**: Read operations (`Get...Async`) prioritize the cache. In high-write scenarios, there may be a brief window of inconsistency between the cache and the primary data store depending on the specific cache-aside or write-through strategy implemented in the project.
*   **Null Handling**: Single-item retrieval methods return `null` rather than throwing exceptions when an entity is not found. Callers must implement null checks before accessing properties of the returned `InventoryItem`.
*   **Thread Safety**: The service instance itself is thread-safe for concurrent read operations. Write operations are serialized at the key level within Redis, ensuring data integrity even when multiple threads attempt to modify the same inventory item simultaneously.
*   **Duplicate Signature**: The API definition includes two declarations of `ReleaseReservationAsync`. In implementation, these likely represent overloads with different parameter sets (e.g., one accepting a reservation ID and another accepting an item ID), though specific parameter details are abstracted in the signature list. Ensure the correct overload is selected based on the available context.
