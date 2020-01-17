# WriteThroughExampleExtensions

The `WriteThroughExampleExtensions` class provides a set of extension methods designed to implement the write-through caching pattern for product entities. By ensuring that database updates and cache invalidation or updates occur atomically or synchronously, this class facilitates maintaining data consistency between primary storage and Redis cache.

## API

### Methods

- `public static async Task<OperationResult<Product>> GetOrCreateProductWriteThroughAsync(int productId)`: Retrieves a product from the cache. If the product is not found, it is fetched from the database, written to the cache, and returned.
- `public static async Task<OperationResult> UpdateValidProductsWriteThroughAsync(IEnumerable<Product> products)`: Updates a collection of products in the database and synchronizes the cache state accordingly.
- `public static async Task<OperationResult<ProductPriceUpdateResult>> UpdateProductPriceWithTrackingAsync(int productId, decimal newPrice)`: Updates a product's price in the database and the cache, returning a `ProductPriceUpdateResult` containing tracking information.
- `public static async Task<OperationResult<Product>> UpsertProductWriteThroughAsync(Product product)`: Performs an upsert (update or insert) operation on a product in the database and updates the corresponding cache entry.

### Properties

- `public int ProductId`: The unique identifier of the product.
- `public decimal OldPrice`: The price of the product prior to an update operation.
- `public decimal NewPrice`: The new price of the product after an update operation.
- `public bool PriceChanged`: A flag indicating whether the price was modified during the operation.
- `public string? Message`: An optional message providing context or details regarding the operation.

## Usage

### Example 1: Retrieving or Creating a Product
```csharp
public async Task<Product> GetProduct(int id)
{
    var result = await _db.GetOrCreateProductWriteThroughAsync(id);
    if (!result.Success)
    {
        throw new Exception($"Failed to retrieve product: {result.Error}");
    }
    return result.Value;
}
```

### Example 2: Updating a Product Price
```csharp
public async Task UpdatePrice(int productId, decimal newPrice)
{
    var result = await _db.UpdateProductPriceWithTrackingAsync(productId, newPrice);
    if (result.Success && result.Value.PriceChanged)
    {
        Console.WriteLine($"Price updated for {result.Value.ProductId} from {result.Value.OldPrice} to {result.Value.NewPrice}.");
    }
}
```

## Notes

- **Thread Safety**: As these methods are asynchronous and operate on external dependencies (database and Redis), thread safety depends on the implementation of the underlying database context and the Redis client used.
- **Consistency**: The write-through pattern implemented here assumes that the cache update is intended to be synchronous with the database update. If the database update succeeds but the cache update fails, the system may enter an inconsistent state. Consider implementing retry policies or compensation logic for such failures.
- **Performance**: While write-through ensures consistency, it can increase latency on write operations, as the application must wait for both the database and the cache to acknowledge the update.
