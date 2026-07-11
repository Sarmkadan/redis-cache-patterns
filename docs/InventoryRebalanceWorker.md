# InventoryRebalanceWorker

A lightweight worker component that monitors and rebalances inventory levels for a specific product by comparing current stock against a predefined reorder threshold. It provides start/stop lifecycle control and exposes key inventory metrics for external monitoring.

## API

### `public InventoryRebalanceWorker(int productId, int currentStock, int reorderLevel)`

Constructs a new inventory rebalancer for the specified product.

- **productId**: Unique identifier of the product to monitor.
- **currentStock**: Initial stock quantity for the product.
- **reorderLevel**: Threshold below which rebalancing should be triggered.

### `public void Start()`

Begins monitoring the inventory level. Rebalancing actions are executed asynchronously when the current stock falls below the configured reorder level.

- **Throws**: `InvalidOperationException` if the worker is already running or has been disposed.

### `public void Stop()`

Halts active monitoring and rebalancing operations. Pending rebalancing actions may complete before the worker fully stops.

### `public void Dispose()`

Releases all resources used by the worker. After disposal, the worker cannot be restarted.

### `public int ProductId`

Gets the unique identifier of the product being monitored.

- **Return value**: The product identifier.

### `public int CurrentStock`

Gets the current inventory level for the product.

- **Return value**: The current stock quantity.

### `public int ReorderLevel`

Gets the configured reorder threshold for the product.

- **Return value**: The reorder level.

## Usage

```csharp
// Example 1: Basic usage with manual stock updates
var worker = new InventoryRebalanceWorker(productId: 42, currentStock: 150, reorderLevel: 50);
worker.Start();

// Simulate stock depletion
worker.CurrentStock = 40; // Triggers rebalancing logic

worker.Stop();
worker.Dispose();
```

```csharp
// Example 2: Integration with inventory service
var inventoryService = new InventoryService();
var worker = new InventoryRebalanceWorker(
    productId: 101,
    currentStock: inventoryService.GetStock(101),
    reorderLevel: 30
);

worker.Start();

try
{
    // Wait for rebalancing to occur
    await Task.Delay(TimeSpan.FromMinutes(5));
}
finally
{
    worker.Stop();
    worker.Dispose();
}
```

## Notes

- **Thread safety**: The worker is not thread-safe for concurrent access to `CurrentStock`. External synchronization is required when modifying stock levels from multiple threads.
- **Disposal**: Ensure `Dispose()` is called to release any background resources. The worker becomes unusable after disposal.
- **Edge cases**: If `CurrentStock` is set below zero, rebalancing logic may trigger unexpectedly. Validate stock levels before assignment.
- **Lifecycle**: `Start()` may throw if called after `Stop()` or `Dispose()`. Check worker state before invoking.
