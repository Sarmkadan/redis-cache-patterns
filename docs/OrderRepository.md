# OrderRepository

The `OrderRepository` class provides an asynchronous interface for retrieving `Order` entities from the data store within the `redis-cache-patterns` solution. It centralizes data access logic to facilitate efficient querying by user, order identifier, current status, and temporal ranges, supporting the application's caching strategy.

## API

### GetByUserIdAsync
Retrieves a collection of all orders associated with the specified user.
- **Signature:** `public async Task<IEnumerable<Order>> GetByUserIdAsync(string userId)`
- **Parameters:** `userId` (string) - The unique identifier of the user.
- **Returns:** An `IEnumerable<Order>` containing the orders for the user, or an empty collection if none are found.
- **Throws:** `ArgumentNullException` if `userId` is null or whitespace.

### GetByOrderNumberAsync
Retrieves a single order by its unique order number.
- **Signature:** `public async Task<Order?> GetByOrderNumberAsync(string orderNumber)`
- **Parameters:** `orderNumber` (string) - The unique identifier of the order.
- **Returns:** The `Order` instance if found; otherwise, `null`.
- **Throws:** `ArgumentNullException` if `orderNumber` is null or whitespace.

### GetByStatusAsync
Retrieves a collection of orders that match the provided status.
- **Signature:** `public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status)`
- **Parameters:** `status` (OrderStatus) - The status to filter orders by.
- **Returns:** An `IEnumerable<Order>` containing the matching orders.

### GetOrdersInDateRangeAsync
Retrieves a collection of orders created within the specified temporal range, inclusive of boundaries.
- **Signature:** `public async Task<IEnumerable<Order>> GetOrdersInDateRangeAsync(DateTime start, DateTime end)`
- **Parameters:**
    - `start` (DateTime) - The start of the date range.
    - `end` (DateTime) - The end of the date range.
- **Returns:** An `IEnumerable<Order>` containing orders created within the range.
- **Throws:** `ArgumentException` if `start` is greater than `end`.

## Usage

### Example 1: Fetching orders for a user
```csharp
var repository = new OrderRepository(redisConnection);
var userOrders = await repository.GetByUserIdAsync("user-123");

foreach (var order in userOrders)
{
    Console.WriteLine($"Order {order.OrderNumber} status: {order.Status}");
}
```

### Example 2: Retrieving a specific order
```csharp
var repository = new OrderRepository(redisConnection);
var order = await repository.GetByOrderNumberAsync("ORD-98765");

if (order != null)
{
    Console.WriteLine($"Found order: {order.Id}");
}
else
{
    Console.WriteLine("Order not found.");
}
```

## Notes

- **Thread Safety:** This class is designed to be thread-safe, assuming the underlying Redis client implementation adheres to standard thread-safe patterns. It is intended to be registered as a singleton or scoped service within the dependency injection container.
- **Data Consistency:** As this repository interacts with a cached data store, results may reflect the consistency level configured for the underlying cache.
- **Input Validation:** All methods validate input parameters and will throw exceptions if invalid arguments are provided. Null or whitespace strings are rejected for user and order number lookups.
- **Nullability:** `GetByOrderNumberAsync` is the only member returning a nullable type, explicitly indicating that the requested order may not exist. Other methods return empty collections when no matching data is found to avoid null reference exceptions in consuming code.
