# ProductService

`ProductService` provides a high-level abstraction over product data operations in the `redis-cache-patterns` project, combining database persistence with Redis caching strategies. It exposes asynchronous methods for retrieving products by identifier, SKU, or category, performing full-text searches, managing product lifecycle (create, update, delete), and executing targeted stock and price updates. The service encapsulates cache-aside, cache-through, and cache-invalidation patterns to maintain consistency between the primary data store and the cache layer.

## API

### ProductService

Constructor. Initializes a new instance of the service with the required database context and Redis cache dependencies. The specific injected dependencies are internal to the implementation.

### GetProductByIdAsync

```csharp
public async Task<Product?> GetProductByIdAsync(int productId)
```

Retrieves a product by its unique integer identifier. Follows a cache-aside pattern: checks the cache first and, on a miss, fetches from the database and populates the cache before returning.

**Parameters:**
- `productId` (`int`): The primary key of the product.

**Returns:**
- `Task<Product?>`: The matching product instance, or `null` if no product with the given identifier exists.

**Throws:**
- `ArgumentException`: When `productId` is less than or equal to zero.
- `RedisConnectionException`: When the cache layer is unreachable and the operation cannot gracefully degrade (behavior depends on internal resilience configuration).

---

### GetProductBySkuAsync

```csharp
public async Task<Product?> GetProductBySkuAsync(string sku)
```

Retrieves a product by its stock-keeping unit string. The SKU is treated as an alternate business key and is indexed in both the database and the cache for direct lookup.

**Parameters:**
- `sku` (`string`): The case-sensitive SKU of the product.

**Returns:**
- `Task<Product?>`: The matching product, or `null` if the SKU is not found.

**Throws:**
- `ArgumentNullException`: When `sku` is `null`.
- `ArgumentException`: When `sku` is empty or consists only of whitespace.

---

### GetProductsByCategoryAsync

```csharp
public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category)
```

Returns all products belonging to a given category. Results may be cached as a collection keyed by category name. The returned sequence is a materialized snapshot; subsequent modifications to products are not reflected in the enumeration.

**Parameters:**
- `category` (`string`): The category name to filter by.

**Returns:**
- `Task<IEnumerable<Product>>`: A collection of products in the specified category. Returns an empty enumeration if the category exists but contains no products, or if the category itself does not exist.

**Throws:**
- `ArgumentNullException`: When `category` is `null`.
- `ArgumentException`: When `category` is empty or whitespace.

---

### CreateProductAsync

```csharp
public async Task<Product> CreateProductAsync(Product product)
```

Persists a new product to the database and seeds the cache with the created entity. The `Product` argument should not have its identity fields pre-assigned; the database generates the primary key and the returned instance reflects the persisted state.

**Parameters:**
- `product` (`Product`): A populated product object without a pre-assigned identifier.

**Returns:**
- `Task<Product>`: The newly created product with its database-generated identifier and any default values applied.

**Throws:**
- `ArgumentNullException`: When `product` is `null`.
- `ValidationException`: When required fields (e.g., SKU, name) are missing or violate uniqueness constraints.

---

### UpdateProductAsync

```csharp
public async Task<Product> UpdateProductAsync(Product product)
```

Updates an existing product in the database and refreshes or invalidates the corresponding cache entry. The product must carry a valid identifier that matches an existing record; partial updates are not supported—the entire entity is overwritten.

**Parameters:**
- `product` (`Product`): The product instance with updated fields and a valid identifier.

**Returns:**
- `Task<Product>`: The updated product as persisted.

**Throws:**
- `ArgumentNullException`: When `product` is `null`.
- `KeyNotFoundException`: When no product with the given identifier exists.
- `ConcurrencyException`: When an optimistic concurrency conflict is detected (if versioning is enabled).

---

### DeleteProductAsync

```csharp
public async Task<bool> DeleteProductAsync(int productId)
```

Removes a product from the database and evicts its cache entry. Returns a boolean indicating whether a record was actually deleted.

**Parameters:**
- `productId` (`int`): The identifier of the product to delete.

**Returns:**
- `Task<bool>`: `true` if a product was found and deleted; `false` if no product with the given identifier existed.

**Throws:**
- `ArgumentException`: When `productId` is less than or equal to zero.

---

### GetLowStockProductsAsync

```csharp
public async Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold)
```

Queries products whose current stock level is at or below the specified threshold. This method is typically used for inventory alerts and may bypass the cache to ensure real-time accuracy, or use a dedicated low-stock cached set that is refreshed on stock changes.

**Parameters:**
- `threshold` (`int`): The inclusive upper bound for stock quantity to be considered low.

**Returns:**
- `Task<IEnumerable<Product>>`: Products with stock quantity ≤ `threshold`. Returns an empty enumeration if no products meet the criterion.

**Throws:**
- `ArgumentOutOfRangeException`: When `threshold` is negative.

---

### SearchProductsAsync

```csharp
public async Task<IEnumerable<Product>> SearchProductsAsync(string query)
```

Performs a full-text or prefix-based search across product names, descriptions, and possibly SKUs. The implementation may delegate to database full-text indexes or a Redis search module. Results are not cached by default due to the variability of search terms.

**Parameters:**
- `query` (`string`): The search term.

**Returns:**
- `Task<IEnumerable<Product>>`: Products matching the search query, ordered by relevance or a default sort. Returns an empty enumeration when no matches are found.

**Throws:**
- `ArgumentNullException`: When `query` is `null`.
- `ArgumentException`: When `query` is empty or whitespace.

---

### UpdateProductPriceAsync

```csharp
public async Task UpdateProductPriceAsync(int productId, decimal newPrice)
```

Updates only the price field of a product. This is a targeted partial update that modifies the database row and then refreshes or invalidates the cache entry for the affected product. It avoids the overhead of a full entity replacement.

**Parameters:**
- `productId` (`int`): The identifier of the product.
- `newPrice` (`decimal`): The new price value.

**Returns:**
- `Task`: A task representing the asynchronous operation.

**Throws:**
- `ArgumentException`: When `productId` is less than or equal to zero.
- `ArgumentOutOfRangeException`: When `newPrice` is negative.
- `KeyNotFoundException`: When no product with the given identifier exists.

---

### UpdateProductStockAsync

```csharp
public async Task UpdateProductStockAsync(int productId, int newStock)
```

Updates only the stock quantity field of a product. Like `UpdateProductPriceAsync`, this performs a targeted update and refreshes or invalidates the relevant cache entry. It may also trigger a refresh of the low-stock cached set if the new stock level crosses the configured threshold.

**Parameters:**
- `productId` (`int`): The identifier of the product.
- `newStock` (`int`): The new stock quantity.

**Returns:**
- `Task`: A task representing the asynchronous operation.

**Throws:**
- `ArgumentException`: When `productId` is less than or equal to zero.
- `ArgumentOutOfRangeException`: When `newStock` is negative.
- `KeyNotFoundException`: When no product with the given identifier exists.

## Usage

### Example 1: Creating a product and retrieving it by SKU

```csharp
var service = new ProductService(dbContext, cache);

var newProduct = new Product
{
    Sku = "LAP-9940",
    Name = "Ultrabook Pro 15",
    Category = "Laptops",
    Price = 1299.99m,
    Stock = 45
};

Product created = await service.CreateProductAsync(newProduct);

// Later, in a separate request context:
Product? fetched = await service.GetProductBySkuAsync("LAP-9940");
if (fetched is not null)
{
    Console.WriteLine($"Price: {fetched.Price:C}");
}
```

### Example 2: Monitoring low stock and applying a targeted update

```csharp
var service = new ProductService(dbContext, cache);

IEnumerable<Product> lowStock = await service.GetLowStockProductsAsync(threshold: 10);

foreach (var product in lowStock)
{
    Console.WriteLine($"Restocking {product.Sku} (current: {product.Stock})");
    int additionalUnits = product.Stock < 5 ? 20 : 10;
    await service.UpdateProductStockAsync(product.Id, product.Stock + additionalUnits);
}

// Verify the threshold is no longer breached for those products.
IEnumerable<Product> stillLow = await service.GetLowStockProductsAsync(threshold: 10);
Console.WriteLine($"Products still low: {stillLow.Count()}");
```

## Notes

- **Cache staleness**: Methods that return `Product` or `IEnumerable<Product>` may serve slightly stale data if a cache entry has not yet been invalidated by a concurrent update. The targeted `UpdateProductPriceAsync` and `UpdateProductStockAsync` methods minimize the window by performing immediate cache refresh or eviction, but race conditions between a read and a concurrent write are possible under high contention.
- **Null returns vs. empty collections**: `GetProductByIdAsync` and `GetProductBySkuAsync` return `null` for missing entities. Collection-returning methods (`GetProductsByCategoryAsync`, `GetLowStockProductsAsync`, `SearchProductsAsync`) never return `null`; they return an empty enumerable when no results exist.
- **Partial updates**: `UpdateProductPriceAsync` and `UpdateProductStockAsync` are the only members that perform field-level updates. `UpdateProductAsync` replaces the entire entity and should be used when multiple fields change simultaneously.
- **Thread safety**: The service itself is stateless aside from its injected dependencies. It is safe to call from multiple threads concurrently, provided the underlying database context and cache client are themselves thread-safe or scoped appropriately (e.g., via dependency injection with transient or scoped lifetimes). No internal locking is performed; consistency guarantees rely on the database’s concurrency control and the cache’s atomic operations.
- **Validation**: Mutating methods (`CreateProductAsync`, `UpdateProductAsync`, `UpdateProductPriceAsync`, `UpdateProductStockAsync`) validate arguments eagerly and will throw before touching the database or cache when preconditions are violated.
- **Idempotency of `DeleteProductAsync`**: Calling `DeleteProductAsync` with a non-existent identifier returns `false` and does not throw (aside from invalid argument guards). Repeated calls with the same identifier after the first successful deletion will continue to return `false`.
