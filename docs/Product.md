# Product

The `Product` class is a domain entity representing a product in an inventory or e-commerce system. It is designed for use with Redis caching patterns to reduce database load and improve read performance. The class encapsulates core product attributes, pricing, stock management, and a collection of associated order items. It also exposes computed properties (`IsLowStock`, `IsAvailable`) and methods to safely update key fields.

## API

### Properties

- **`public int Id`**  
  Gets or sets the unique identifier of the product. This is typically the primary key in the data store.

- **`public string Name`**  
  Gets or sets the product name. Should not be null or empty in a valid state.

- **`public string Description`**  
  Gets or sets a textual description of the product.

- **`public string Sku`**  
  Gets or sets the stock‑keeping unit (SKU) – a unique alphanumeric code used for inventory tracking.

- **`public decimal Price`**  
  Gets or sets the unit price of the product. A negative value is invalid and may cause unexpected behavior in downstream calculations.

- **`public int StockQuantity`**  
  Gets or sets the current number of units available in inventory. A negative value indicates a data inconsistency.

- **`public int ReorderLevel`**  
  Gets or sets the stock level at which a reorder should be triggered. Used by the `IsLowStock` property.

- **`public string Category`**  
  Gets or sets the product category (e.g., "Electronics", "Clothing").

- **`public bool IsActive`**  
  Gets or sets whether the product is currently active and available for sale. Inactive products are typically hidden from the storefront.

- **`public DateTime CreatedAt`**  
  Gets or sets the timestamp when the product was first created.

- **`public DateTime? UpdatedAt`**  
  Gets or sets the timestamp of the last update to the product. `null` if never updated.

- **`public string? ImageUrl`**  
  Gets or sets the URL of the product image. May be `null` if no image is associated.

- **`public double Rating`**  
  Gets or sets the average customer rating (e.g., from 1.0 to 5.0). Values outside this range are not enforced by the class.

- **`public int ReviewCount`**  
  Gets or sets the total number of customer reviews submitted for the product.

- **`public List<OrderItem> OrderItems`**  
  Gets or sets the collection of `OrderItem` records that reference this product. This is a navigation property used for entity relationships.

- **`public bool IsLowStock`**  
  Gets a value indicating whether the current `StockQuantity` is less than or equal to the `ReorderLevel`. This is a computed, read‑only property.

- **`public bool IsAvailable`**  
  Gets a value indicating whether the product can be purchased. Typically returns `true` when `IsActive` is `true` and `StockQuantity` is greater than zero. The exact logic may depend on business rules.

### Methods

- **`public void UpdateStock(int newQuantity)`**  
  Updates the `StockQuantity` to the specified value. After the update, the `UpdatedAt` timestamp is set to the current UTC time.  
  **Parameters:**  
  - `newQuantity` – The new stock quantity. Negative values are accepted but may lead to inconsistent state.  
  **Returns:** `void`  
  **Throws:** None (no validation is performed by this method).

- **`public void UpdatePrice(decimal newPrice)`**  
  Updates the `Price` to the specified value. The `UpdatedAt` timestamp is refreshed.  
  **Parameters:**  
  - `newPrice` – The new price. Negative values are not validated.  
  **Returns:** `void`  
  **Throws:** None.

- **`public void UpdateDetails(string name, string description, string category, string? imageUrl)`**  
  Updates the product’s `Name`, `Description`, `Category`, and `ImageUrl` in a single call. The `UpdatedAt` timestamp is set to the current UTC time.  
  **Parameters:**  
  - `name` – New product name.  
  - `description` – New description.  
  - `category` – New category.  
  - `imageUrl` – New image URL (can be `null`).  
  **Returns:** `void`  
  **Throws:** None.

## Usage

### Example 1: Creating a product and updating stock

```csharp
var product = new Product
{
    Id = 101,
    Name = "Wireless Mouse",
    Description = "Ergonomic wireless mouse with USB receiver",
    Sku = "WM-2024",
    Price = 29.99m,
    StockQuantity = 50,
    ReorderLevel = 10,
    Category = "Electronics",
    IsActive = true,
    CreatedAt = DateTime.UtcNow,
    Rating = 4.5,
    ReviewCount = 120,
    ImageUrl = "https://example.com/images/mouse.png"
};

// Simulate a sale of 5 units
product.UpdateStock(product.StockQuantity - 5);

if (product.IsLowStock)
{
    Console.WriteLine($"Product {product.Name} is low on stock (current: {product.StockQuantity}).");
}
```

### Example 2: Caching product data with Redis

```csharp
// Assume a Redis cache service is available
var cacheKey = $"product:{product.Id}";
var cachedProduct = await cacheService.GetAsync<Product>(cacheKey);

if (cachedProduct != null)
{
    // Use cached product
    if (cachedProduct.IsAvailable)
    {
        Console.WriteLine($"Product '{cachedProduct.Name}' is available. Price: {cachedProduct.Price:C}");
    }
}
else
{
    // Load from database and cache
    var dbProduct = await dbContext.Products.FindAsync(productId);
    if (dbProduct != null)
    {
        await cacheService.SetAsync(cacheKey, dbProduct, TimeSpan.FromMinutes(10));
        // Use dbProduct
    }
}
```

## Notes

- **Edge cases:**  
  - `Price`, `StockQuantity`, and `Rating` are not validated by the class. Negative prices or stock quantities can be assigned and may cause issues in business logic.  
  - `ImageUrl` can be `null`; consumers should handle missing images gracefully.  
  - `IsLowStock` and `IsAvailable` are computed based on the current property values. They are not cached and will reflect changes immediately after property assignments or method calls.  
  - The `OrderItems` list is a mutable reference; modifications to the list (add/remove) are not automatically persisted.

- **Thread safety:**  
  This class is **not thread‑safe**. Concurrent reads and writes to its properties or methods from multiple threads can lead to inconsistent state. If the `Product` instance is shared across threads (e.g., in a cached object accessed by multiple requests), external synchronization (e.g., locks, immutable snapshots, or concurrent collections) must be used. The `UpdateStock`, `UpdatePrice`, and `UpdateDetails` methods do not perform any atomic operations or locking.
