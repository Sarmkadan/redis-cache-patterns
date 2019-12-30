# OrderService

A service class responsible for managing order lifecycle operations including creation, retrieval, status updates, and cancellation. It interacts with a repository layer and applies Redis-based caching strategies to optimize read performance for common queries.

## API

### `public OrderService(...)`

Initializes a new instance of the `OrderService` class with required dependencies for order persistence and caching.

### `public async Task<Order?> GetOrderByIdAsync(Guid orderId)`

Retrieves an order by its unique identifier. Returns `null` if the order does not exist. Throws `ArgumentException` if `orderId` is empty.

### `public async Task<Order?> GetOrderByNumberAsync(string orderNumber)`

Retrieves an order by its order number. Returns `null` if no order with the specified number exists. Throws `ArgumentException` if `orderNumber` is null or whitespace.

### `public async Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId)`

Retrieves all orders associated with a given user. Returns an empty enumerable if the user has no orders. Throws `ArgumentException` if `userId` is empty.

### `public async Task<Order> CreateOrderAsync(Order order)`

Creates a new order in the system. Returns the created order with updated identifiers. Throws `ArgumentNullException` if `order` is null or invalid.

### `public async Task<bool> ConfirmOrderAsync(Guid orderId)`

Confirms an order, transitioning it from "pending" to "confirmed" status. Returns `true` if the update succeeded; otherwise `false`. Throws `ArgumentException` if `orderId` is empty. Throws `InvalidOperationException` if the order is not in a confirm-able state.

### `public async Task<bool> ShipOrderAsync(Guid orderId)`

Marks an order as shipped. Returns `true` if the update succeeded; otherwise `false`. Throws `ArgumentException` if `orderId` is empty. Throws `InvalidOperationException` if the order is not in a shippable state.

### `public async Task<bool> CompleteOrderAsync(Guid orderId)`

Marks an order as completed. Returns `true` if the update succeeded; otherwise `false`. Throws `ArgumentException` if `orderId` is empty. Throws `InvalidOperationException` if the order is not in a completable state.

### `public async Task<bool> CancelOrderAsync(Guid orderId)`

Cancels an order. Returns `true` if the update succeeded; otherwise `false`. Throws `ArgumentException` if `orderId` is empty. Throws `InvalidOperationException` if the order is not in a cancellable state.

### `public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status)`

Retrieves all orders matching the specified status. Returns an empty enumerable if no orders match. Throws `ArgumentOutOfRangeException` if `status` is not a defined enum value.

### `public async Task<IEnumerable<Order>> GetPendingOrdersAsync()`

Retrieves all orders currently in "pending" status. Returns an empty enumerable if no pending orders exist.

### `public async Task<IEnumerable<Order>> GetOrdersInDateRangeAsync(DateTime start, DateTime end)`

Retrieves all orders whose creation date falls within the specified range (inclusive). Returns an empty enumerable if no orders fall within the range. Throws `ArgumentException` if `start` is after `end`.

## Usage
