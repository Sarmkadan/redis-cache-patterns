# Order Event Handler

Documentation for the `OrderEventHandler` class in the `RedisCachePatterns.Events` namespace, handling order-related domain events and tracking processed events.

## Class: `OrderEventHandler`

Handles order-related events and performs downstream operations such as logging and tracking processed events.

### Constructor

| Parameter | Description |
|-----------|-------------|
| `ILogger<OrderEventHandler> logger` | Logger instance for publishing diagnostic messages. |

**Behavior:**
1. Validates that the logger parameter is not null using `ArgumentNullException.ThrowIfNull`.
2. Stores the logger instance for use in event handling methods.

### Methods

#### `Task OnOrderCreatedAsync(OrderCreatedEvent @event)`

Handles the `OrderCreatedEvent` by logging the event details and recording it in the processed events list.

**Parameters:**
- `@event`: The order created event containing order information.

**Behavior:**
1. Validates that the event parameter is not null.
2. Logs an information message containing the OrderId, UserId, and TotalAmount.
3. Creates a new `OrderEvent` record with:
   - EventType: "OrderCreated"
   - OrderId: from the event
   - ProcessedAt: current UTC time
4. Adds the record to the internal processed events list.
5. Returns a completed task.

#### `Task OnOrderConfirmedAsync(OrderConfirmedEvent @event)`

Handles the `OrderConfirmedEvent` by logging the event details and recording it in the processed events list.

**Parameters:**
- `@event`: The order confirmed event containing order information.

**Behavior:**
1. Validates that the event parameter is not null.
2. Logs an information message containing the OrderId and ConfirmedAt timestamp.
3. Creates a new `OrderEvent` record with:
   - EventType: "OrderConfirmed"
   - OrderId: from the event
   - ProcessedAt: current UTC time
4. Adds the record to the internal processed events list.
5. Returns a completed task.

#### `Task OnOrderShippedAsync(OrderShippedEvent @event)`

Handles the `OrderShippedEvent` by logging the event details and recording it in the processed events list.

**Parameters:**
- `@event`: The order shipped event containing order information.

**Behavior:**
1. Logs an information message containing the OrderId and TrackingNumber.
2. Creates a new `OrderEvent` record with:
   - EventType: "OrderShipped"
   - OrderId: from the event
   - ProcessedAt: current UTC time
3. Adds the record to the internal processed events list.
4. Returns a completed task.

#### `Task OnInventoryReservedAsync(InventoryReservedEvent @event)`

Handles the `InventoryReservedEvent` by logging the event details and recording it in the processed events list.

**Parameters:**
- `@event`: The inventory reserved event containing order information.

**Behavior:**
1. Validates that the event parameter is not null.
2. Logs an information message containing the ProductId, Quantity, and OrderId.
3. Creates a new `OrderEvent` record with:
   - EventType: "InventoryReserved"
   - OrderId: from the event
   - ProcessedAt: current UTC time
4. Adds the record to the internal processed events list.
5. Returns a completed task.

#### `IEnumerable<OrderEvent> GetProcessedEvents()`

Returns a read-only collection of all processed events.

**Returns:**
- An IEnumerable of OrderEvent objects representing all events that have been processed by this handler.

#### `void ClearProcessedEvents()`

Clears all recorded processed events from the internal list.

### Nested Class: `OrderEvent`

Represents a recorded event that has been processed by the OrderEventHandler.

| Property | Type | Description |
|----------|------|-------------|
| `EventType` | `string` | The type of event that was processed (e.g., "OrderCreated", "OrderConfirmed"). |
| `OrderId` | `int` | The identifier of the order associated with the event. |
| `ProcessedAt` | `DateTime` | The timestamp when the event was processed (in UTC). |

### Thread Safety

This implementation is **not thread-safe**. Concurrent access to the processed events list from multiple threads may result in undefined behavior. For thread-safe usage, external synchronization is required.

### Usage Example

```csharp
// Create handler with logger
var handler = new OrderEventHandler(logger);

// Handle an order created event
await handler.OnOrderCreatedAsync(new OrderCreatedEvent
{
    OrderId = 123,
    UserId = 456,
    TotalAmount = 99.99m
});

// Handle an order confirmed event
await handler.OnOrderConfirmedAsync(new OrderConfirmedEvent
{
    OrderId = 123,
    ConfirmedAt = DateTime.UtcNow
});

// Retrieve processed events
var processedEvents = handler.GetProcessedEvents();
foreach (var evt in processedEvents)
{
    Console.WriteLine($"{evt.EventType} for Order {evt.OrderId} at {evt.ProcessedAt}");
}

// Clear processed events when needed
handler.ClearProcessedEvents();
```