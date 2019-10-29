# ProductServiceTests

Integration and unit tests for the `ProductService` class, verifying behavior around caching patterns, repository interactions, and validation rules using Redis as a distributed cache. Tests cover scenarios for product retrieval, creation, updates, and deletion, including cache key scoping and invalidation strategies.

## API

### `ProductServiceTests`

Initializes a new instance of the test class with required dependencies, including a mocked repository, cache invalidation service, and Redis cache client.

### `GetProductByIdAsync_WhenCacheReturnsProduct_DoesNotCallRepository`

Ensures that when a product is found in the cache, the underlying repository is not queried. The test simulates a cached product and asserts that the repository's `GetByIdAsync` method is never invoked.

### `GetProductByIdAsync_UsesCorrectlyScopedCacheKey`

Validates that the cache key used for product retrieval includes a scoped prefix derived from the product type or tenant context. The test checks that the key follows the expected format and is correctly constructed before cache lookup.

### `CreateProductAsync_WhenSkuAlreadyExists_ThrowsValidationException`

Confirms that attempting to create a product with an existing SKU results in a `ValidationException`. The test sets up a repository that returns an existing product for the given SKU and asserts that the service throws the expected exception.

### `CreateProductAsync_WhenSkuIsNew_PersistsProductAndCachesIt`

Verifies that a new product is both persisted to the repository and cached in Redis when its SKU is unique. The test asserts that the repository's `AddAsync` method is called and that the resulting product is stored in the cache with the correct key.

### `DeleteProductAsync_WhenProductDoesNotExist_ReturnsFalseWithoutDeletion`

Ensures that attempting to delete a non-existent product returns `false` and does not trigger any repository or cache operations. The test simulates a missing product and asserts that the service returns the expected result without side effects.

### `UpdateProductPriceAsync_WhenProductDoesNotExist_ThrowsNotFoundException`

Validates that updating the price of a non-existent product throws a `NotFoundException`. The test configures the repository to return `null` for the given product ID and asserts that the service throws the expected exception.

### `UpdateProductStockAsync_WhenResultingStockIsLow_InvalidatesLowStockCacheEntry`

Checks that updating a product's stock to a low level triggers invalidation of a cache entry tagged as `low-stock`. The test simulates a stock update that crosses the low-stock threshold and asserts that the cache invalidation service is invoked with the correct tag.

## Usage
