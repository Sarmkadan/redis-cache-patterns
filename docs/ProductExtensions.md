# ProductExtensions

The `ProductExtensions` class provides a set of static extension methods designed to enhance the `Product` domain type, facilitating common inventory management and reporting operations within the `redis-cache-patterns` system. These utilities centralize domain-specific calculations and formatting logic to ensure consistency and promote code reuse across service layers.

## API

### CalculateInventoryValue
Calculates the total monetary value of current stock for a given product.

*   **Parameters:** `this Product product`
*   **Returns:** A `decimal` representing the total inventory value (Quantity × UnitPrice).
*   **Throws:** `ArgumentNullException` if the `product` is null.

### FormatForDisplay
Generates a human-readable string representation of the product, suitable for UI or logging purposes.

*   **Parameters:** `this Product product`
*   **Returns:** A `string` containing a formatted summary of the product, typically including its name and SKU.
*   **Throws:** `ArgumentNullException` if the `product` is null.

### NeedsReorder
Determines whether a product's current stock levels fall below the defined reorder threshold.

*   **Parameters:** `this Product product`
*   **Returns:** A `bool` indicating `true` if current inventory is below the reorder point, otherwise `false`.
*   **Throws:** `ArgumentNullException` if the `product` is null.

### CalculatePotentialRevenue
Calculates the projected revenue should the entire current inventory be sold at the current unit price.

*   **Parameters:** `this Product product`
*   **Returns:** A `decimal` representing the total potential revenue.
*   **Throws:** `ArgumentNullException` if the `product` is null.

## Usage

### Inventory Reporting
```csharp
using RedisCachePatterns.Extensions;

public void GenerateReport(Product item)
{
    var display = item.FormatForDisplay();
    var value = item.CalculateInventoryValue();
    
    Console.WriteLine($"{display} | Current Value: {value:C}");
}
```

### Reorder Workflow Trigger
```csharp
using RedisCachePatterns.Extensions;

public void ProcessInventoryUpdate(Product item)
{
    if (item.NeedsReorder())
    {
        var projectedRevenue = item.CalculatePotentialRevenue();
        _reorderService.CreatePurchaseOrder(item, projectedRevenue);
    }
}
```

## Notes

*   **Edge Cases:** All methods perform a null check on the `product` instance and will throw an `ArgumentNullException` if invoked on a null reference. Ensure product data is validated before processing.
*   **Thread Safety:** As these methods are implemented as static stateless extension methods, they are inherently thread-safe. They do not modify the state of the `Product` object passed to them.
*   **Performance:** These methods are intended for lightweight calculations. Avoid performing heavy operations within these extensions to maintain system responsiveness during bulk processing tasks.
