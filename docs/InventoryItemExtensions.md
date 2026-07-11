# InventoryItemExtensions

Provides a set of extension methods to facilitate common inventory management operations on <c>InventoryItem</c> entities. These methods simplify calculations for stock levels, reservation logic, and status reporting, ensuring consistent behavior across the application when handling inventory data.

## API

<dl>
  <dt><code>public static bool IsOverstocked(this InventoryItem item)</code></dt>
  <dd>Determines if the item is overstocked. Returns <c>true</c> if current stock levels exceed established maximum thresholds; otherwise, returns <c>false</c>.</dd>

  <dt><code>public static double GetReservedPercentage(this InventoryItem item)</code></dt>
  <dd>Calculates the ratio of reserved inventory to the total quantity on hand. Returns a value between 0.0 and 1.0. If the total quantity on hand is zero, returns 0.0.</dd>

  <dt><code>public static double GetStockPercentage(this InventoryItem item)</code></dt>
  <dd>Calculates the ratio of available inventory to the total quantity on hand. Returns a value between 0.0 and 1.0. If the total quantity on hand is zero, returns 0.0.</dd>

  <dt><code>public static bool TryReserve(this InventoryItem item, int quantity)</code></dt>
  <dd>Attempts to reserve a specified <c>quantity</c> of items. If the available quantity is sufficient, it updates the reservation state and returns <c>true</c>; otherwise, it returns <c>false</c>.</dd>

  <dt><code>public static string GetStockStatus(this InventoryItem item)</code></dt>
  <dd>Returns a string representing the current stock status based on availability, such as "In Stock", "Low Stock", or "Out of Stock".</dd>

  <dt><code>public static int? GetDaysSinceLastCount(this InventoryItem item)</code></dt>
  <dd>Calculates the number of days elapsed since the <c>LastCountedAt</c> timestamp. Returns the integer count of days.</dd>

  <dt><code>public static int? GetDaysSinceLastMovement(this InventoryItem item)</code></dt>
  <dd>Calculates the number of days elapsed since the <c>LastMovedAt</c> timestamp. Returns <c>null</c> if <c>LastMovedAt</c> is not set; otherwise, returns the integer count of days.</dd>
</dl>

## Usage

### Checking Stock Status and Overstock

```csharp
var item = repository.GetById(itemId);

if (item.IsOverstocked())
{
    logger.LogWarning("Item {Id} is overstocked in warehouse {Warehouse}", item.Id, item.Warehouse);
}

string status = item.GetStockStatus();
Console.WriteLine($"Current status: {status}");
```

### Safely Reserving Stock

```csharp
var item = repository.GetById(itemId);
int quantityToReserve = 5;

if (item.TryReserve(quantityToReserve))
{
    repository.Update(item);
    Console.WriteLine("Reservation successful.");
}
else
{
    Console.WriteLine("Insufficient stock available for reservation.");
}
```

## Notes

*   **Thread Safety**: These extension methods are not inherently thread-safe. They operate directly on the provided `InventoryItem` instance. If the `InventoryItem` instance is shared across multiple threads, appropriate external synchronization mechanisms must be employed to prevent race conditions during updates (e.g., in `TryReserve`).
*   **Division by Zero**: `GetReservedPercentage` and `GetStockPercentage` safely handle cases where the total quantity on hand is zero by returning 0.0, avoiding division-by-zero exceptions.
*   **Null Handling**: These methods expect a non-null `InventoryItem` instance. Passing a null reference will result in a `NullReferenceException`.
*   **Time Sensitivity**: `GetDaysSinceLastCount` and `GetDaysSinceLastMovement` rely on `DateTime.UtcNow`. Results may vary slightly depending on the system clock synchronization.
