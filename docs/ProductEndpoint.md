# ProductEndpoint

The `ProductEndpoint` class provides the API surface for managing `Product` entities, handling retrieval, creation, updating, and deletion operations. It is designed to work within a system utilizing Redis-based caching patterns to optimize data access and performance.

## API

### `ProductEndpoint`
Initializes a new instance of the `ProductEndpoint` class.

### `GetProductByIdAsync(string id)`
Retrieves a product by its unique identifier.
*   **Parameters:** `string id` (The unique identifier of the product).
*   **Returns:** `Task<ApiResponse<Product?>>` containing the product if found, otherwise `null`.
*   **Throws:** `ArgumentNullException` if `id` is null or empty.

### `GetLowStockProductsAsync()`
Retrieves a collection of products currently categorized as having low stock levels.
*   **Returns:** `Task<ApiResponse<IEnumerable<Product>>>` containing the list of products with low stock.

### `CreateProductAsync(Product product)`
Registers a new product in the system.
*   **Parameters:** `Product product` (The product object to create).
*   **Returns:** `Task<ApiResponse<Product>>` containing the created product.
*   **Throws:** `ArgumentException` if the provided product fails validation.

### `UpdateProductAsync(string id, Product product)`
Updates the details of an existing product.
*   **Parameters:**
    *   `string id` (The unique identifier of the product to update).
    *   `Product product` (The updated product data).
*   **Returns:** `Task<ApiResponse<Product?>>` containing the updated product if successful, otherwise `null`.
*   **Throws:** `KeyNotFoundException` if the product with the specified `id` does not exist.

### `DeleteProductAsync(string id)`
Removes a product from the system.
*   **Parameters:** `string id` (The unique identifier of the product to delete).
*   **Returns:** `Task<ApiResponse<bool>>` containing `true` if the deletion was successful, otherwise `false`.

## Usage

### Example 1: Retrieving a Product
```csharp
var endpoint = new ProductEndpoint(logger, cacheService, repository);
var response = await endpoint.GetProductByIdAsync("prod-001");

if (response.Success && response.Data != null)
{
    Console.WriteLine($"Product Name: {response.Data.Name}");
}
else
{
    Console.WriteLine("Product not found or error occurred.");
}
```

### Example 2: Creating a New Product
```csharp
var newProduct = new Product { Name = "Example Widget", StockQuantity = 50 };
var endpoint = new ProductEndpoint(logger, cacheService, repository);
var response = await endpoint.CreateProductAsync(newProduct);

if (response.Success)
{
    Console.WriteLine($"Created product with ID: {response.Data.Id}");
}
```

## Notes

*   **Thread Safety:** The `ProductEndpoint` is designed to be used in high-concurrency environments. It assumes that the injected dependencies (such as the database repository and the Redis cache client) are themselves thread-safe.
*   **Caching Strategy:** Methods returning `ApiResponse<T>` assume that data is checked against the cache before hitting the primary data store. Updates and deletions should invalidate or refresh the corresponding cache keys to maintain data consistency.
*   **Handling Nulls:** `GetProductByIdAsync` and `UpdateProductAsync` return `Product?`. Consumers must check the `Success` property of the `ApiResponse<T>` and the `Data` property for `null` to avoid `NullReferenceException`.
*   **Error Handling:** Exceptions should be caught by the caller to handle scenarios where the underlying data store or cache service is unavailable.
