# OrderCreatedEvent

Event object representing order lifecycle events in a Redis-backed cache pattern. Used to track order creation, confirmation, shipping, and inventory reservation events with timestamps and associated metadata.

## API

### Properties

- **`EventType`** (string)
  Identifies the type of order event. Expected values include `"Created"`, `"Confirmed"`, `"Shipped"`, and `"InventoryReserved"`.

- **`OrderId`** (int)
  Unique identifier for the order. Present in all event types.

- **`UserId`** (int)
  Identifier of the user who placed the order. Only populated for `"Created"` events.

- **`TotalAmount`** (decimal)
  Total monetary value of the order. Only populated for `"Created"` events.

- **`ConfirmedAt`** (DateTime)
  Timestamp when the order was confirmed. Only populated for `"Confirmed"` events.

- **`TrackingNumber`** (string)
  Shipping tracking number. Only populated for `"Shipped"` events.

- **`ProductId`** (int)
  Identifier of the product involved in the event. Only populated for `"InventoryReserved"` events.

- **`Quantity`** (int)
  Quantity of the product reserved or ordered. Only populated for `"InventoryReserved"` events.

- **`ProcessedAt`** (DateTime)
  Timestamp when the event was processed by the event handler.

### Methods

- **`GetProcessedEvents()`** → `IEnumerable<OrderEvent>`
  Returns a read-only collection of all processed order events for the current order. Never returns `null`.

- **`ClearProcessedEvents()`**
  Removes all processed events from the internal tracking collection. Safe to call even if no events are present.

- **`OnOrderCreatedAsync(OrderCreatedEvent)`** → `Task`
  Handles an order creation event. Publishes the event to the Redis stream and updates internal state. Throws `ArgumentNullException` if the event is `null`.

- **`OnOrderConfirmedAsync(OrderCreatedEvent)`** → `Task`
  Handles an order confirmation event. Updates the `ConfirmedAt` timestamp and publishes the event to Redis. Throws `ArgumentNullException` if the event is `null`.

- **`OnOrderShippedAsync(OrderCreatedEvent)`** → `Task`
  Handles an order shipped event. Sets the `TrackingNumber` and publishes the event to Redis. Throws `ArgumentNullException` if the event is `null`.

- **`OnInventoryReservedAsync(OrderCreatedEvent)`** → `Task`
  Handles an inventory reservation event. Records the `ProductId` and `Quantity`, then publishes the event to Redis. Throws `ArgumentNullException` if the event is `null`.

## Usage

### Example 1: Processing an order creation event
