# Redis Cache Patterns

... [existing content] ...

## WriteThroughExampleExtensions

The `WriteThroughExampleExtensions` class provides extension methods for the `WriteThroughExample` class, enabling advanced cache-aside patterns with write-through semantics. It includes methods for creating/fetching products, bulk updating validated products, tracking price changes, and atomic upserts with cache synchronization.

### Usage Example
```csharp
var example = new WriteThroughExample(cacheService, productRepository);

// Get or create product with write-through
var getResult = await example.GetOrCreateProductWriteThroughAsync(123, id => 
    Task.FromResult(new Product { Id = id, Name = "Default", Price = 9.99m }));

if (getResult.Success)
{
    var product = getResult.Value;
    
    // Update product price with tracking
    var priceResult = await example.UpdateProductPriceWithTrackingAsync(product.Id, 14.99m);
    if (priceResult.Success)
    {
        var tracking = priceResult.Value;
        Console.WriteLine($"Price changed: {tracking.OldPrice} → {tracking.NewPrice}");
    }
    
    // Upsert product (create or update)
    var upsertResult = await example.UpsertProductWriteThroughAsync(new Product 
    { 
        Id = product.Id, 
        Name = "Updated Name", 
        Price = 19.99m 
    });
    
    // Bulk update valid products
    var products = new List<Product> { /* ... */ };
    var bulkResult = await example.UpdateValidProductsWriteThroughAsync(products, 
        p => p.Price > 0 && p.Name.Length > 3, 
        p => p.MarkAsUpdated());
}
```

### ProductPriceUpdateResult Properties
- `ProductId`: The identifier of the product
- `OldPrice`: The price before update
- `NewPrice`: The price after update
- `PriceChanged`: Indicates if the price actually changed
- `Message`: Additional status information

... [existing content] ...
