# OrderRepositoryExtensions

The `OrderRepositoryExtensions` class is a static collection of extension methods for `OrderRepository`. It adds three query helpers: listing a user's orders by status, checking whether an order number exists, and counting orders in a date range. The class is in the `RedisCachePatterns.Infrastructure.Repositories` namespace, the same as `OrderRepository`, so no extra `using` is needed for it. The methods only read data and do not change the repository.

The methods are declared on the concrete `OrderRepository` type, not on `IOrderRepository`. A variable typed as `IOrderRepository` must be cast to `OrderRepository` before these methods can be called.

## API

### GetOrdersByUserAndStatusAsync

```csharp
public static async Task<IReadOnlyList<Order>> GetOrdersByUserAndStatusAsync(this OrderRepository repository, int userId, OrderStatus status)
```

Returns the orders that belong to one user and have the given status. The method calls `GetByUserIdAsync` and filters the result in memory.

- **repository**: The `OrderRepository` instance.
- **userId**: The ID of the user. It is not validated. An unknown ID returns an empty list.
- **status**: The `OrderStatus` to keep (`Pending`, `Confirmed`, `Shipped`, `Completed`, or `Cancelled`).
- **Returns**: A read-only list of matching orders. The list is empty when nothing matches. The order is the one returned by `GetByUserIdAsync`, which is newest first by `CreatedAt`.
- **Throws**: `ArgumentNullException` when `repository` is null.

### OrderExistsAsync

```csharp
public static async Task<bool> OrderExistsAsync(this OrderRepository repository, string orderNumber)
```

Checks whether an order with the given order number exists. The method calls `GetByOrderNumberAsync`, so the match ignores case.

- **repository**: The `OrderRepository` instance.
- **orderNumber**: The order number to look for.
- **Returns**: `true` if an order with this number exists; otherwise, `false`.
- **Throws**: `ArgumentNullException` when `repository` is null. `ArgumentNullException` when `orderNumber` is null. `ArgumentException` when `orderNumber` is empty. See the notes about the XML documentation for the null case.

### CountOrdersInDateRangeAsync

```csharp
public static async Task<int> CountOrdersInDateRangeAsync(this OrderRepository repository, DateTime startDate, DateTime endDate)
```

Counts the orders whose `CreatedAt` falls within the range. The method calls `GetOrdersInDateRangeAsync` and counts the orders it returns.

- **repository**: The `OrderRepository` instance.
- **startDate**: The start of the range (inclusive).
- **endDate**: The end of the range (inclusive).
- **Returns**: The number of matching orders. `0` when none match.
- **Throws**: `ArgumentNullException` when `repository` is null. `ArgumentOutOfRangeException` when `startDate` is later than `endDate`.

## Usage

### Listing a user's pending orders

```csharp
using RedisCachePatterns.Domain;
using RedisCachePatterns.Infrastructure.Repositories;

public class PendingOrderReport(OrderRepository repository)
{
    public async Task<int> CountPendingAsync(int userId)
    {
        IReadOnlyList<Order> pending = await repository.GetOrdersByUserAndStatusAsync(userId, OrderStatus.Pending);
        return pending.Count;
    }
}
```

### Checking for an order number

```csharp
if (await repository.OrderExistsAsync(orderNumber))
{
    // The order number is already in use
}
```

### Counting orders for a day

```csharp
var start = new DateTime(2026, 10, 1);
var end = new DateTime(2026, 10, 1, 23, 59, 59, 999);

int count = await repository.CountOrdersInDateRangeAsync(start, end);
```

Both ends of the range are inclusive. A plain `new DateTime(2026, 10, 1)` as `endDate` would leave out every order created later on that day, so set `endDate` to the end of the day.

## Notes

*   **Exceptions are deferred:** The methods are `async`, so argument checks do not throw when the method is called. The exception is stored in the returned `Task` and thrown when the task is awaited.
*   **Null `orderNumber` throws `ArgumentNullException`:** The XML documentation of `OrderExistsAsync` says that a null or empty `orderNumber` throws `ArgumentException`. The code calls `ArgumentException.ThrowIfNullOrEmpty`, which throws `ArgumentNullException` for null and `ArgumentException` for empty. Callers that catch only `ArgumentException` will miss the null case.
*   **Order numbers are matched without case:** `GetByOrderNumberAsync` compares with `StringComparison.OrdinalIgnoreCase`, so `ord-100` and `ORD-100` refer to the same order.
*   **Counting loads the matching orders:** `CountOrdersInDateRangeAsync` does not run a count query. The repository builds the full list of matching orders, and the extension counts that list. The cost grows with the number of matches.
*   **Filtering happens in memory:** `GetOrdersByUserAndStatusAsync` loads all orders for the user and filters them with LINQ. The repository keeps its data in memory (`_data`, guarded by `_lock`), so the extension adds no I/O of its own.
*   **No caching:** The extensions do not read from or write to Redis. Nothing in the current tree calls them, so they have no caller yet.
