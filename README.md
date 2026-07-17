// ... (rest of the file remains the same)

## OrderServiceExtensions

The `OrderServiceExtensions` class provides a set of extension methods for the `OrderService` class, offering additional convenience and batch operations for managing orders. These methods enable you to retrieve orders by ID, status, or date range, as well as count orders by status or user. 

Here is an example of how to use the `OrderServiceExtensions` methods:

```csharp
using RedisCachePatterns.Services;

// Assume an existing OrderService instance (implementation details omitted)
OrderService orderService = /* obtain order service instance */;

// Try to get an order by its ID
var order = await orderService.TryGetOrderByIdAsync(123);

// Get orders by status with pagination
var orders = await orderService.GetOrdersByStatusPagedAsync(OrderStatus.Pending, 1, 10);

// Count orders by status
var count = await orderService.CountOrdersByStatusAsync(OrderStatus.Pending);

// Get orders from the last 7 days
var recentOrders = await orderService.GetOrdersFromLastDaysAsync(7);

// Count orders for a specific user
var userOrderCount = await orderService.CountUserOrdersAsync(123);

// Try to get an order by its order number
var orderNumber = await orderService.TryGetOrderByNumberAsync("ORD-12345");

// Get orders by status with formatted status
var formattedOrders = await orderService.GetOrdersByStatusWithFormattedStatusAsync(OrderStatus.Pending);

// Get orders in a specific date range
var dateRangeOrders = await orderService.GetOrdersInDateRangeFormattedAsync(DateTime.Now.AddDays(-7), DateTime.Now);
```

## CacheMonitor

The `CacheMonitor` class is responsible for tracking cache performance and health. It provides methods to retrieve cache statistics, print statistics, track cache entries, and calculate average hit rates. 

// ... (rest of the file remains the same)
```