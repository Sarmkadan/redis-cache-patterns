# Order

The `Order` class represents a customer order in an e-commerce system. It encapsulates order details such as items, status, financial amounts, shipping and billing addresses, and provides methods to manage items and transition the order through its lifecycle.

## API

- **`public int Id`**  
  Unique identifier for the order.

- **`public int UserId`**  
  Identifier of the user who placed the order.

- **`public User? User`**  
  Navigation property to the associated `User` object. May be `null` if the user is not loaded.

- **`public string OrderNumber`**  
  Human-readable order number (e.g., "ORD-12345").

- **`public OrderStatus Status`**  
  Current status of the order (e.g., `Pending`, `Confirmed`, `Shipped`, `Completed`).

- **`public DateTime CreatedAt`**  
  Timestamp when the order was created.

- **`public DateTime? CompletedAt`**  
  Timestamp when the order was completed. Set when the status transitions to `Completed`; `null` otherwise.

- **`public string ShippingAddress`**  
  Shipping address for the order.

- **`public string BillingAddress`**  
  Billing address for the order.

- **`public decimal TotalAmount`**  
  Total amount of the order, typically the sum of item prices, tax, and shipping cost.

- **`public decimal TaxAmount`**  
  Tax amount applied to the order.

- **`public decimal ShippingCost`**  
  Shipping cost for the order.

- **`public string? Notes`**  
  Optional notes or comments on the order. May be `null`.

- **`public string? TrackingNumber`**  
  Optional tracking number for shipped orders. May be `null`.

- **`public List<OrderItem> Items`**  
  Collection of order items. Direct modification of this list is allowed, but callers should use `AddItem` and `RemoveItem` for consistency.

- **`public void AddItem(OrderItem item)`**  
  Adds the specified `item` to the `Items` collection.  
  **Parameters:** `item` – the `OrderItem` to add.  
  **Returns:** void.  
  **Throws:** `ArgumentNullException` if `item` is `null`.

- **`public void RemoveItem(OrderItem item)`**  
  Removes the first occurrence of the specified `item` from the `Items` collection.  
  **Parameters:** `item` – the `OrderItem` to remove.  
  **Returns:** void.  
  **Throws:** `ArgumentNullException` if `item` is `null`. Does nothing if the item is not found.

- **`public void RecalculateTotal()`**  
  Recalculates `TotalAmount`, `TaxAmount`, and `ShippingCost` based on the current `Items` and any applicable business rules (e.g., tax rates, shipping policies). Should be called after adding or removing items, or after modifying item prices.  
  **Parameters:** none.  
  **Returns:** void.  
  **Throws:** none.

- **`public void ConfirmOrder()`**  
  Transitions the order status to `Confirmed`.  
  **Parameters:** none.  
  **Returns:** void.  
  **Throws:** `InvalidOperationException` if the current status is not `Pending`.

- **`public void ShipOrder()`**  
  Transitions the order status to `Shipped`.  
  **Parameters:** none.  
  **Returns:** void.  
  **Throws:** `InvalidOperationException` if the current status is not `Confirmed`.

## Usage

### Example 1: Creating and confirming an order

```csharp
var order = new Order
{
    UserId = 42,
    OrderNumber = "ORD-1001",
    CreatedAt = DateTime.UtcNow,
    ShippingAddress = "123 Main St, Springfield",
    BillingAddress = "123 Main St, Springfield",
    Notes = "Handle with care"
};

order.AddItem(new OrderItem { ProductId = 1, Quantity = 2, UnitPrice = 19.99m });
order.AddItem(new OrderItem { ProductId = 5, Quantity = 1, UnitPrice = 49.99m });
order.RecalculateTotal();

order.ConfirmOrder();
```

### Example 2: Loading an existing order and shipping it

```csharp
// Assume order is retrieved from a repository
Order order = orderRepository.GetById(1001);

if (order.Status == OrderStatus.Confirmed)
{
    order.TrackingNumber = "1Z999AA10123456784";
    order.ShipOrder();
    orderRepository.Update(order);
}
```

## Notes

- **Edge cases:**  
  - Adding a `null` item to `AddItem` throws `ArgumentNullException`.  
  - Removing an item that is not in the `Items` list is a no-op.  
  - `RecalculateTotal` does not automatically enforce minimum or maximum amounts; business rules must be applied externally.  
  - Status transitions are one-way: `Pending` → `Confirmed` → `Shipped` → `Completed`. Calling `ConfirmOrder` on an already confirmed order throws `InvalidOperationException`.  
  - `CompletedAt` is not automatically set by `ShipOrder`; it is expected to be set when the order reaches a completed state (e.g., delivered).

- **Thread safety:**  
  The `Order` class is not thread-safe. Concurrent reads and writes to `Items`, `Status`, or other properties from multiple threads may result in data corruption. If the instance is shared across threads, external synchronization (e.g., a lock) must be used.
