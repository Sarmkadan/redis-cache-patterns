# OrderServiceExtensions

Extension methods for `OrderService` that provide cached and asynchronous access to order data using Redis. These methods wrap underlying service calls with caching patterns, retry logic, and optional data transformation.

## API

### `TryGetOrderByIdAsync`

Attempts to retrieve an `Order` by its unique identifier. First checks the cache; if not found, fetches from the underlying service and caches the result.

- **Parameters**
  - `orderService` – The `OrderService` instance used to fetch the order if not in cache.
  - `orderId` – The unique identifier of the order to retrieve.
  - `cacheKey` – Optional cache key override; if `null`, a default key is generated using the order ID.
  - `cancellationToken` – Optional token to monitor for cancellation requests.

- **Returns**
  - `Task<Order?>` – The order if found; otherwise `null`.

- **Exceptions**
  - Throws `ArgumentException` if `orderId` is invalid (e.g., empty or whitespace).
  - Propagates exceptions from `orderService.GetOrderByIdAsync`.

---

### `GetOrdersByStatusPagedAsync`

Retrieves a paged subset of orders filtered by status, with optional cache support.

- **Parameters**
  - `orderService` – The `OrderService` instance used to fetch orders if not in cache.
  - `status` – The order status to filter by.
  - `pageIndex` – Zero-based page index.
  - `pageSize` – Number of orders per page.
  - `cacheKey` – Optional cache key override.
  - `cancellationToken` – Optional token to monitor for cancellation requests.

- **Returns**
  - `Task<IReadOnlyList<Order>>` – A read-only list of orders matching the status and page.

- **Exceptions**
  - Throws `ArgumentException` if `status` is `null` or `pageIndex`/`pageSize` are invalid.
  - Propagates exceptions from `orderService.GetOrdersByStatusAsync`.

---

### `CountOrdersByStatusAsync`

Returns the total count of orders matching a given status, optionally served from cache.

- **Parameters**
  - `orderService` – The `OrderService` instance used to fetch the count if not in cache.
  - `status` – The order status to count.
  - `cacheKey` – Optional cache key override.
  - `cancellationToken` – Optional token to monitor for cancellation requests.

- **Returns**
  - `Task<int>` – The number of orders with the specified status.

- **Exceptions**
  - Throws `ArgumentException` if `status` is `null`.
  - Propagates exceptions from `orderService.CountOrdersByStatusAsync`.

---
### `GetOrdersFromLastDaysAsync`

Fetches all orders created within the last specified number of days, with optional caching.

- **Parameters**
  - `orderService` – The `OrderService` instance used to fetch orders if not in cache.
  - `days` – Number of days to look back (must be positive).
  - `cacheKey` – Optional cache key override.
  - `cancellationToken` – Optional token to monitor for cancellation requests.

- **Returns**
  - `Task<IEnumerable<Order>>` – An enumerable of orders created within the time window.

- **Exceptions**
  - Throws `ArgumentException` if `days` is not positive.
  - Propagates exceptions from `orderService.GetOrdersFromLastDaysAsync`.

---
### `CountUserOrdersAsync`

Returns the total number of orders placed by a specific user, optionally cached.

- **Parameters**
  - `orderService` – The `OrderService` instance used to fetch the count if not in cache.
  - `userId` – The unique identifier of the user.
  - `cacheKey` – Optional cache key override.
  - `cancellationToken` – Optional token to monitor for cancellation requests.

- **Returns**
  - `Task<int>` – The number of orders for the user.

- **Exceptions**
  - Throws `ArgumentException` if `userId` is invalid.
  - Propagates exceptions from `orderService.CountUserOrdersAsync`.

---
### `TryGetOrderByNumberAsync`

Attempts to retrieve an `Order` by its order number (e.g., invoice or reference number). Checks cache first.

- **Parameters**
  - `orderService` – The `OrderService` instance used to fetch the order if not in cache.
  - `orderNumber` – The order number to search for.
  - `cacheKey` – Optional cache key override.
  - `cancellationToken` – Optional token to monitor for cancellation requests.

- **Returns**
  - `Task<Order?>` – The order if found; otherwise `null`.

- **Exceptions**
  - Throws `ArgumentException` if `orderNumber` is `null` or whitespace.
  - Propagates exceptions from `orderService.TryGetOrderByNumberAsync`.

---
### `GetOrdersByStatusWithFormattedStatusAsync`

Retrieves all orders matching a status, with the status field transformed to a human-readable string. Results are cached.

- **Parameters**
  - `orderService` – The `OrderService` instance used to fetch orders if not in cache.
  - `status` – The order status to filter by.
  - `cacheKey` – Optional cache key override.
  - `cancellationToken` – Optional token to monitor for cancellation requests.

- **Returns**
  - `Task<IEnumerable<Order>>` – An enumerable of orders with formatted status strings.

- **Exceptions**
  - Throws `ArgumentException` if `status` is `null`.
  - Propagates exceptions from `orderService.GetOrdersByStatusAsync`.

---
### `GetOrdersInDateRangeFormattedAsync`

Fetches orders within a specified date range, with status fields formatted for display. Uses caching.

- **Parameters**
  - `orderService` – The `OrderService` instance used to fetch orders if not in cache.
  - `startDate` – Inclusive start of the date range.
  - `endDate` – Inclusive end of the date range.
  - `cacheKey` – Optional cache key override.
  - `cancellationToken` – Optional token to monitor for cancellation requests.

- **Returns**
  - `Task<IEnumerable<Order>>` – An enumerable of orders within the date range with formatted status.

- **Exceptions**
  - Throws `ArgumentException` if `startDate` is after `endDate`.
  - Propagates exceptions from `orderService.GetOrdersInDateRangeAsync`.

---

## Usage
