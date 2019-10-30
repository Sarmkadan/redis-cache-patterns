# InventoryServiceTests
The `InventoryServiceTests` class is designed to test the functionality of the `InventoryService` class, which is responsible for managing inventory data using a Redis cache. This test class covers various scenarios, including cache hits, repository calls, and reservation operations, to ensure the `InventoryService` behaves as expected.

## API
* `public InventoryServiceTests`: The constructor for the `InventoryServiceTests` class.
* `public async Task GetInventoryByIdAsync_WhenCacheHit_ReturnsWithoutRepositoryCall`: Tests that the `GetInventoryByIdAsync` method returns the inventory data from the cache without calling the repository when a cache hit occurs.
* `public async Task GetInventoryByIdAsync_UsesCorrectCacheKey`: Verifies that the `GetInventoryByIdAsync` method uses the correct cache key to store and retrieve inventory data.
* `public async Task GetByProductAndWarehouseAsync_RetrievesInventoryByProductAndWarehouse`: Tests the `GetByProductAndWarehouseAsync` method to ensure it retrieves the correct inventory data based on the product and warehouse.
* `public async Task GetInventoryByProductAsync_ReturnsMultipleWarehouses`: Tests the `GetInventoryByProductAsync` method to ensure it returns inventory data for multiple warehouses.
* `public async Task ReserveInventoryAsync_WhenLockAcquiredAndStockAvailable_ReservesAndReturnsTrue`: Tests the `ReserveInventoryAsync` method to ensure it reserves the inventory and returns `true` when the lock is acquired and stock is available.
* `public async Task ReserveInventoryAsync_WhenLockNotAcquired_ReturnsFalseWithoutModifyingInventory`: Verifies that the `ReserveInventoryAsync` method returns `false` without modifying the inventory when the lock cannot be acquired.
* `public async Task ReserveInventoryAsync_WhenStockInsufficient_ThrowsException`: Tests the `ReserveInventoryAsync` method to ensure it throws an exception when the stock is insufficient.
* `public async Task ReserveInventoryAsync_ReleasesLockEvenOnException`: Verifies that the `ReserveInventoryAsync` method releases the lock even when an exception occurs.
* `public async Task ReserveInventoryAsync_InvalidatesInventoryCaches`: Tests the `ReserveInventoryAsync` method to ensure it invalidates the inventory caches after a successful reservation.
* `public async Task ReleaseReservationAsync_WhenInventoryExists_ReleasesReservation`: Tests the `ReleaseReservationAsync` method to ensure it releases the reservation when the inventory exists.
* `public async Task GetLowStockItemsAsync_ReturnsItemsBelowReorderPoint`: Tests the `GetLowStockItemsAsync` method to ensure it returns items with stock levels below the reorder point.

## Usage
The following examples demonstrate how to use the `InventoryServiceTests` class:
```csharp
// Example 1: Testing cache hit scenario
var inventoryServiceTests = new InventoryServiceTests();
await inventoryServiceTests.GetInventoryByIdAsync_WhenCacheHit_ReturnsWithoutRepositoryCall();

// Example 2: Testing reservation scenario
var inventoryServiceTests = new InventoryServiceTests();
await inventoryServiceTests.ReserveInventoryAsync_WhenLockAcquiredAndStockAvailable_ReservesAndReturnsTrue();
```

## Notes
When using the `InventoryServiceTests` class, consider the following edge cases and thread-safety remarks:
* The `ReserveInventoryAsync` method uses a lock to ensure thread safety, but it may still throw exceptions if the lock cannot be acquired or if the stock is insufficient.
* The `GetLowStockItemsAsync` method returns items with stock levels below the reorder point, but it does not account for items with zero or negative stock levels.
* The `ReleaseReservationAsync` method releases the reservation only when the inventory exists, so it may not work as expected if the inventory is deleted or modified concurrently.
* The `InventoryServiceTests` class uses a Redis cache, which may have its own set of limitations and edge cases, such as cache expiration, cache size limits, and network connectivity issues.
