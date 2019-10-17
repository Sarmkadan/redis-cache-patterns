# InventoryRepository

The `InventoryRepository` class provides an asynchronous interface for interacting with inventory data persisted within a Redis cache. It facilitates common data retrieval operations, including querying specific stock items, aggregating product quantities, and identifying inventory falling below defined thresholds.

## API

### GetByProductAndWarehouseAsync
Retrieves the `InventoryItem` for a specified product within a given warehouse.

- **Parameters:**
  - `string productId`: The unique identifier of the product.
  - `string warehouseId`: The unique identifier of the warehouse.
- **Returns:** `Task<InventoryItem?>`: The `InventoryItem` if found; otherwise, `null`.
- **Throws:** Throws an exception if the underlying connection to Redis fails or if the query operation encounters an error.

### GetByProductAsync
Retrieves all `InventoryItem` records associated with a specific product across all warehouses.

- **Parameters:**
  - `string productId`: The unique identifier of the product.
- **Returns:** `Task<IEnumerable<InventoryItem>>`: A collection of `InventoryItem` instances for the product.
- **Throws:** Throws an exception if the underlying connection to Redis fails or if the query operation encounters an error.

### GetLowStockItemsAsync
Identifies all `InventoryItem` records where the quantity is strictly less than the specified threshold.

- **Parameters:**
  - `int threshold`: The quantity limit used to identify low-stock items.
- **Returns:** `Task<IEnumerable<InventoryItem>>`: A collection of `InventoryItem` instances meeting the criteria.
- **Throws:** Throws an exception if the underlying connection to Redis fails or if the query operation encounters an error.

### GetTotalQuantityAsync
Calculates the sum of available quantities for a specific product across all warehouses.

- **Parameters:**
  - `string productId`: The unique identifier of the product.
- **Returns:** `Task<int>`: The total quantity of the product.
- **Throws:** Throws an exception if the underlying connection to Redis fails or if the query operation encounters an error.

## Usage

### Example 1: Retrieving a Specific Item
```csharp
var repository = new InventoryRepository(redisConnection);
var item = await repository.GetByProductAndWarehouseAsync("prod-123", "wh-west");

if (item != null)
{
    Console.WriteLine($"Found {item.Quantity} units in {item.WarehouseId}.");
}
```

### Example 2: Monitoring Low Stock
```csharp
var repository = new InventoryRepository(redisConnection);
var lowStockItems = await repository.GetLowStockItemsAsync(threshold: 10);

foreach (var item in lowStockItems)
{
    Console.WriteLine($"Restock alert: Product {item.ProductId} has only {item.Quantity} remaining in {item.WarehouseId}.");
}
```

## Notes

- **Connectivity:** All methods rely on an active Redis connection. Implementations should handle potential connectivity exceptions appropriately.
- **Asynchrony:** The repository utilizes `Task`-based asynchronous patterns to prevent blocking the calling thread during I/O operations.
- **Thread Safety:** The `InventoryRepository` implementation should be thread-safe, allowing instances to be shared across requests. This is typically ensured by utilizing thread-safe underlying Redis client libraries.
- **Nullability:** `GetByProductAndWarehouseAsync` returns a nullable type (`InventoryItem?`). Callers must verify the result is not `null` before accessing properties of the returned item.
