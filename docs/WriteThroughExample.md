# WriteThroughExample

`WriteThroughExample` demonstrates the write-through caching pattern for product data using Redis. It provides asynchronous methods to create, update, and delete products while ensuring cache consistency by writing changes to both the primary data store and the cache in a single atomic operation.

## API

### `WriteThroughExample`

The class encapsulates dependencies required for write-through operations, including a product repository and a Redis cache client.

### `public async Task<OperationResult<Product>> UpdateProductWriteThroughAsync(Product product)`

Updates an existing product in both the primary data store and Redis cache.

- **Parameters**:
  - `product`: The product with updated values to persist.
- **Return value**: An `OperationResult<Product>` containing the updated product if successful, or error details if the operation fails.
- **Exceptions**:
  - Throws `ArgumentNullException` if `product` is `null`.
  - Throws `InvalidOperationException` if the product identifier is invalid or missing.

### `public async Task<OperationResult<Product>> CreateProductWriteThroughAsync(Product product)`

Creates a new product in both the primary data store and Redis cache.

- **Parameters**:
  - `product`: The product to create.
- **Return value**: An `OperationResult<Product>` containing the created product if successful, or error details if the operation fails.
- **Exceptions**:
  - Throws `ArgumentNullException` if `product` is `null`.
  - Throws `InvalidOperationException` if the product identifier is already in use.

### `public async Task<OperationResult> DeleteProductWriteThroughAsync(int productId)`

Deletes a product from both the primary data store and Redis cache.

- **Parameters**:
  - `productId`: The identifier of the product to delete.
- **Return value**: An `OperationResult` indicating success or failure.
- **Exceptions**:
  - Throws `ArgumentException` if `productId` is less than or equal to zero.

### `public async Task<OperationResult> UpdateProductPriceAsync(int productId, decimal newPrice)`

Updates the price of a product in both the primary data store and Redis cache.

- **Parameters**:
  - `productId`: The identifier of the product to update.
  - `newPrice`: The new price value.
- **Return value**: An `OperationResult` indicating success or failure.
- **Exceptions**:
  - Throws `ArgumentException` if `productId` is less than or equal to zero.
  - Throws `ArgumentOutOfRangeException` if `newPrice` is negative.

### `public async Task<OperationResult> BulkUpdateProductsWriteThroughAsync(IEnumerable<Product> products)`

Updates multiple products in both the primary data store and Redis cache in a single batch.

- **Parameters**:
  - `products`: An enumerable of products to update.
- **Return value**: An `OperationResult` indicating success or failure.
- **Exceptions**:
  - Throws `ArgumentNullException` if `products` is `null`.
  - Throws `InvalidOperationException` if any product in the collection is invalid or missing an identifier.

## Usage
