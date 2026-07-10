# ProductRepository

The `ProductRepository` class serves as a data access layer abstraction for interacting with product information, specifically designed to support caching strategies, such as those implemented with Redis, to enhance performance and reduce database load. It encapsulates the necessary logic to query, filter, and retrieve `Product` entities, ensuring that consumption of product data remains consistent across the application while abstracting the underlying data persistence and caching mechanics.

## API

### GetByCategoryAsync
Retrieves a collection of products belonging to the specified category.
*   **Parameters:** `string category` (The identifier for the product category).
*   **Returns:** `Task<IEnumerable<Product>>` (An enumerable collection of products; returns an empty collection if no products match the category).
*   **Exceptions:** Throws `ArgumentNullException` if the category is null or empty.

### GetBySkuAsync
Retrieves a single product identified by its unique Stock Keeping Unit (SKU).
*   **Parameters:** `string sku` (The unique SKU identifier).
*   **Returns:** `Task<Product?>` (The requested product, or `null` if no product with the specified SKU is found).
*   **Exceptions:** Throws `ArgumentNullException` if the SKU is null or empty.

### GetLowStockProductsAsync
Retrieves a collection of products that are currently below their minimum required stock levels.
*   **Returns:** `Task<IEnumerable<Product>>` (An enumerable collection of products with low stock, or an empty collection if all stock levels are sufficient).

### SearchByNameAsync
Performs a search for products whose names match or contain the provided search term.
*   **Parameters:** `string nameQuery` (The string used to filter products by name).
*   **Returns:** `Task<IEnumerable<Product>>` (An enumerable collection of matching products, or an empty collection if no matches are found).
*   **Exceptions:** Throws `ArgumentNullException` if the name query is null or empty.

## Usage

### Retrieving a Product by SKU
```csharp
var repository = new ProductRepository(cache, dbContext);
Product? product = await repository.GetBySkuAsync("LAPTOP-001");

if (product != null)
{
    Console.WriteLine($"Found: {product.Name}, Price: {product.Price}");
}
```

### Filtering Products by Category
```csharp
var repository = new ProductRepository(cache, dbContext);
var electronics = await repository.GetByCategoryAsync("Electronics");

foreach (var product in electronics)
{
    Console.WriteLine($"Available: {product.Name}");
}
```

## Notes

*   **Thread Safety:** This class is designed to be thread-safe, assuming the underlying cache and database clients utilized in the implementation are also thread-safe.
*   **Caching Consistency:** Because this repository integrates with caching mechanisms, there may be brief periods of inconsistency between the cache and the primary data store depending on the configured cache invalidation strategy and expiration policies.
*   **Null Handling:** Methods returning `Task<IEnumerable<Product>>` will return an empty collection rather than `null` to simplify consumer logic. Methods returning `Task<Product?>` use nullable reference types to explicitly indicate the potential for a missing result.
