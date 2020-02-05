// ... existing content ...

## InventoryItemExtensions

The `InventoryItemExtensions` class provides a set of extension methods for evaluating and manipulating inventory items. These extensions simplify the process of assessing stock levels and reserving items.

### Usage Examples

```csharp
var inventoryItem = new InventoryItem { StockLevel = 100, ReservedQuantity = 20, LastCountDate = DateTime.Now.AddDays(-10), LastMovementDate = DateTime.Now.AddDays(-5) };

if (inventoryItem.IsOverstocked)
{
    Console.WriteLine("The item is overstocked.");
}

var reservedPercentage = inventoryItem.GetReservedPercentage();
Console.WriteLine($"Reserved percentage: {reservedPercentage}%");

var stockPercentage = inventoryItem.GetStockPercentage();
Console.WriteLine($"Stock percentage: {stockPercentage}%");

if (inventoryItem.TryReserve(10))
{
    Console.WriteLine("Reservation successful.");
}

var stockStatus = inventoryItem.GetStockStatus();
Console.WriteLine($"Stock status: {stockStatus}");

var daysSinceLastCount = inventoryItem.GetDaysSinceLastCount();
Console.WriteLine($"Days since last count: {daysSinceLastCount}");

var daysSinceLastMovement = inventoryItem.GetDaysSinceLastMovement();
Console.WriteLine($"Days since last movement: {daysSinceLastMovement}");
}
```

## CacheEntryExtensions

The `CacheEntryExtensions` class provides extension methods for evaluating cache entry states, tags, and expiry information. These methods simplify checking if an entry is active, stale, or matches specific tag criteria.

### Usage Examples

```csharp
var entry = new CacheEntry
{
    ActiveUntil = DateTime.Now.AddHours(1),
    Tags = new[] { "product", "inventory" }
};

if (entry.IsActive)
{
    Console.WriteLine("Entry is active.");
}

if (entry.IsStale)
{
    Console.WriteLine("Entry is stale.");
}

Console.WriteLine($"Time to expiry: {entry.GetTimeToExpiryFormatted}");

Console.WriteLine($"Entry status: {entry.GetDetailedStatus}");

if (entry.HasAllTags("product", "inventory"))
{
    Console.WriteLine("Entry has all required tags.");
}

if (entry.HasAnyTag("inventory"))
{
    Console.WriteLine("Entry has at least one matching tag.");
}
```

// ... existing content ...
