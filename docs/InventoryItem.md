# InventoryItem

Represents a single inventory item stored in a warehouse, tracking quantities, locations, and stock control thresholds. Used by inventory management systems to maintain accurate stock levels, handle reservations, and trigger reordering logic.

## API

### `public int Id`
Unique identifier for the inventory item within the system. Immutable after creation.

### `public int ProductId`
Identifier of the product this inventory item represents. Used to link to product catalog data.

### `public string Warehouse`
Name or code of the warehouse where the item is stored. Used to scope inventory operations.

### `public int QuantityOnHand`
Current physical count of items in stock. Updated during stock counts and movements.

### `public int QuantityReserved`
Number of items currently reserved for pending orders or allocations. Must be subtracted from `QuantityAvailable` when fulfilling orders.

### `public int QuantityAvailable`
Calculated as `QuantityOnHand - QuantityReserved`. Read-only derived value indicating items available for new reservations.

### `public string Location`
Storage location within the warehouse (e.g., "A12-03-04"). Used for picking and stock organization.

### `public DateTime LastCountedAt`
Timestamp of the most recent physical inventory count for this item.

### `public DateTime? LastMovedAt`
Timestamp when the item was last transferred between locations or warehouses, if applicable.

### `public DateTime? LastUpdated`
Timestamp of the last modification to any field except stock counts or movements. Used for change tracking.

### `public int MinimumLevel`
Minimum stock level threshold. When `QuantityAvailable` falls below this value, the item is considered low stock.

### `public int ReorderPoint`
Stock level at which a reorder should be triggered to avoid stockouts.

### `public int MaxStock`
Maximum stock level allowed. Used to prevent overstocking during receiving operations.

### `public bool IsLowStock`
Flag indicating whether the item is below the `MinimumLevel` threshold. Updated automatically when `QuantityAvailable` changes.

### `public bool CanReserve`
Flag indicating whether the item can be reserved for orders. When `false`, reservation operations throw `InvalidOperationException`.

### `public void Reserve(int quantity)`
Reserves a specified quantity of the item for an order or allocation.

- **Parameters**: `quantity` – Number of items to reserve. Must be positive and not exceed `QuantityAvailable`.
- **Throws**:
  - `ArgumentOutOfRangeException` if `quantity <= 0` or `quantity > QuantityAvailable`.
  - `InvalidOperationException` if `CanReserve` is `false`.

### `public void ReleaseReservation(int quantity)`
Releases a previously reserved quantity back to available stock.

- **Parameters**: `quantity` – Number of items to release. Must be positive and not exceed `QuantityReserved`.
- **Throws**:
  - `ArgumentOutOfRangeException` if `quantity <= 0` or `quantity > QuantityReserved`.

### `public void ReceiveStock(int quantity, string? location = null)`
Adds stock to the item’s inventory.

- **Parameters**:
  - `quantity` – Positive number of items received.
  - `location` – Optional new storage location. If provided, updates the `Location` field.
- **Throws**:
  - `ArgumentOutOfRangeException` if `quantity <= 0`.
  - `InvalidOperationException` if `quantity > MaxStock - QuantityOnHand`.

### `public void DispatchStock(int quantity)`
Removes stock from the item’s inventory due to order fulfillment or loss.

- **Parameters**: `quantity` – Positive number of items dispatched.
- **Throws**:
  - `ArgumentOutOfRangeException` if `quantity <= 0` or `quantity > QuantityOnHand`.

### `public void AdjustCount(int newCount)`
Updates the physical stock count and recalculates derived values.

- **Parameters**: `newCount` – New `QuantityOnHand` value. Must be non-negative.
- **Throws**: `ArgumentOutOfRangeException` if `newCount < 0`.

## Usage

### Example 1: Receiving and reserving stock
