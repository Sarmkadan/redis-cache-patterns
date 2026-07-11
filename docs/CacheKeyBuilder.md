# CacheKeyBuilder

Utility class for constructing consistent and predictable Redis cache key strings across the application. It centralizes key naming conventions to avoid duplication, typos, and inconsistencies when interacting with Redis, ensuring that all components use the same key structure for the same logical entities.

## API

### `public static string BuildKey(params string[] parts)`

Combines multiple key segments into a single Redis key using the configured separator (`:`). The segments are joined in the order provided, with no leading or trailing separators.

- **Parameters**
  - `parts`: Zero or more non-null strings representing key segments.
- **Return value**
  - A single string representing the combined key.
- **Exceptions**
  - Throws `ArgumentNullException` if any element in `parts` is `null`.

---

### `public static string User(Guid userId)`

Constructs a cache key for a user by their unique identifier.

- **Parameters**
  - `userId`: The unique identifier of the user.
- **Return value**
  - A string in the format `user:{userId}`.
- **Exceptions**
  - Throws `ArgumentException` if `userId` is `Guid.Empty`.

---

### `public static string UserByUsername(string username)`

Constructs a cache key for a user lookup by username.

- **Parameters**
  - `username`: The username to look up.
- **Return value**
  - A string in the format `user:by-username:{username}`.
- **Exceptions**
  - Throws `ArgumentNullException` if `username` is `null`.
  - Throws `ArgumentException` if `username` is empty or whitespace.

---

### `public static string UserByEmail(string email)`

Constructs a cache key for a user lookup by email address.

- **Parameters**
  - `email`: The email address to look up.
- **Return value**
  - A string in the format `user:by-email:{email}`.
- **Exceptions**
  - Throws `ArgumentNullException` if `email` is `null`.
  - Throws `ArgumentException` if `email` is empty or whitespace.

---
### `public static string UsersByRole(string role)`

Constructs a cache key for a collection of users belonging to a specific role.

- **Parameters**
  - `role`: The role name.
- **Return value**
  - A string in the format `user:by-role:{role}`.
- **Exceptions**
  - Throws `ArgumentNullException` if `role` is `null`.
  - Throws `ArgumentException` if `role` is empty or whitespace.

---
### `public static string Product(Guid productId)`

Constructs a cache key for a product by its unique identifier.

- **Parameters**
  - `productId`: The unique identifier of the product.
- **Return value**
  - A string in the format `product:{productId}`.
- **Exceptions**
  - Throws `ArgumentException` if `productId` is `Guid.Empty`.

---
### `public static string ProductBySku(string sku)`

Constructs a cache key for a product lookup by SKU.

- **Parameters**
  - `sku`: The stock keeping unit identifier.
- **Return value**
  - A string in the format `product:by-sku:{sku}`.
- **Exceptions**
  - Throws `ArgumentNullException` if `sku` is `null`.
  - Throws `ArgumentException` if `sku` is empty or whitespace.

---
### `public static string ProductsByCategory(string category)`

Constructs a cache key for a collection of products belonging to a specific category.

- **Parameters**
  - `category`: The category name.
- **Return value**
  - A string in the format `product:by-category:{category}`.
- **Exceptions**
  - Throws `ArgumentNullException` if `category` is `null`.
  - Throws `ArgumentException` if `category` is empty or whitespace.

---
### `public static string ProductSearch(string query, string? category = null)`

Constructs a cache key for a product search result set.

- **Parameters**
  - `query`: The search query string.
  - `category`: Optional category filter.
- **Return value**
  - A string in the format `product:search:{query}` or `product:search:{query}:{category}` if `category` is provided.
- **Exceptions**
  - Throws `ArgumentNullException` if `query` is `null`.
  - Throws `ArgumentException` if `query` is empty or whitespace.

---
### `public static string Order(Guid orderId)`

Constructs a cache key for an order by its unique identifier.

- **Parameters**
  - `orderId`: The unique identifier of the order.
- **Return value**
  - A string in the format `order:{orderId}`.
- **Exceptions**
  - Throws `ArgumentException` if `orderId` is `Guid.Empty`.

---
### `public static string OrderByNumber(string orderNumber)`

Constructs a cache key for an order lookup by order number.

- **Parameters**
  - `orderNumber`: The order number.
- **Return value**
  - A string in the format `order:by-number:{orderNumber}`.
- **Exceptions**
  - Throws `ArgumentNullException` if `orderNumber` is `null`.
  - Throws `ArgumentException` if `orderNumber` is empty or whitespace.

---
### `public static string OrdersByUser(Guid userId)`

Constructs a cache key for a collection of orders belonging to a specific user.

- **Parameters**
  - `userId`: The unique identifier of the user.
- **Return value**
  - A string in the format `order:by-user:{userId}`.
- **Exceptions**
  - Throws `ArgumentException` if `userId` is `Guid.Empty`.

---
### `public static string OrdersByStatus(string status)`

Constructs a cache key for a collection of orders with a specific status.

- **Parameters**
  - `status`: The order status.
- **Return value**
  - A string in the format `order:by-status:{status}`.
- **Exceptions**
  - Throws `ArgumentNullException` if `status` is `null`.
  - Throws `ArgumentException` if `status` is empty or whitespace.

---
### `public static string Inventory(Guid productId, Guid warehouseId)`

Constructs a cache key for inventory of a product at a specific warehouse.

- **Parameters**
  - `productId`: The unique identifier of the product.
  - `warehouseId`: The unique identifier of the warehouse.
- **Return value**
  - A string in the format `inventory:{productId}:{warehouseId}`.
- **Exceptions**
  - Throws `ArgumentException` if either `productId` or `warehouseId` is `Guid.Empty`.

---
### `public static string InventoryByProduct(Guid productId)`

Constructs a cache key for inventory entries for a specific product across all warehouses.

- **Parameters**
  - `productId`: The unique identifier of the product.
- **Return value**
  - A string in the format `inventory:by-product:{productId}`.
- **Exceptions**
  - Throws `ArgumentException` if `productId` is `Guid.Empty`.

---
### `public static string DistributedLock(string resource)`

Constructs a cache key suitable for use as a distributed lock identifier.

- **Parameters**
  - `resource`: The name of the resource to lock.
- **Return value**
  - A string in the format `lock:{resource}`.
- **Exceptions**
  - Throws `ArgumentNullException` if `resource` is `null`.
  - Throws `ArgumentException` if `resource` is empty or whitespace.

---
### `public static string GeneratePattern(string prefix)`

Generates a Redis pattern string for use with `KEYS` or `SCAN` commands, matching all keys that start with the given prefix.

- **Parameters**
  - `prefix`: The key prefix to match.
- **Return value**
  - A string in the format `{prefix}*`.
- **Exceptions**
  - Throws `ArgumentNullException` if `prefix` is `null`.
  - Throws `ArgumentException` if `prefix` is empty or whitespace.

## Usage
