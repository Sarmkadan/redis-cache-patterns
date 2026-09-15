# Event Publisher

Documentation for the `EventPublisher` and related types in the `RedisCachePatterns.Events` namespace, implementing the pub-sub pattern for decoupled event handling.

## Interface: `IEventPublisher`

Defines the contract for publishing and subscribing to domain events.

| Method | Description |
|--------|-------------|
| `Task PublishAsync<TEvent>(TEvent @event)` | Publishes an event of type `TEvent` asynchronously, where `TEvent` must inherit from `DomainEvent`. |
| `void Subscribe<TEvent>(Func<TEvent, Task> handler)` | Subscribes an asynchronous handler to events of type `TEvent`. |
| `void Unsubscribe<TEvent>(Func<TEvent, Task> handler)` | Unsubscribes a previously registered handler from events of type `TEvent`. |

## Abstract Class: `DomainEvent`

Base class for all domain events in the system.

| Property | Type | Description |
|----------|------|-------------|
| `EventId` | `string` | Unique identifier for the event, generated as a new GUID upon instantiation. |
| `OccurredAt` | `DateTime` | Timestamp indicating when the event occurred, set to current UTC time upon instantiation. |
| `Source` | `string` | Identifier of the event source (e.g., service name, component). Defaults to empty string. |

## Class: `EventPublisher`

Default implementation of `IEventPublisher` using in-memory subscriber management.

### Constructor

| Parameter | Description |
|-----------|-------------|
| `ILogger<EventPublisher> logger` | Logger instance for publishing diagnostic messages. |

### Methods

#### `PublishAsync<TEvent>(TEvent @event)`

Publishes an event to all subscribed handlers.

**Parameters:**
- `@event`: The domain event to publish.

**Behavior:**
1. Logs the event publication attempt at Information level.
2. Retrieves subscribers for the event type; if none exist, logs a debug message and returns.
3. For each subscriber:
   - Attempts to cast the handler to `Func<TEvent, Task>`.
   - If successful, invokes the handler asynchronously and adds the resulting task to a list.
   - If casting fails or an exception occurs during invocation, logs the error at Error level.
4. Waits for all handler tasks to complete using `Task.WhenAll`.
5. Logs successful publication at Information level with the count of handlers.
6. If any handler throws an exception during execution, logs the error at Error level (but continues waiting for other handlers).

#### `Subscribe<TEvent>(Func<TEvent, Task> handler)`

Registers a handler for events of the specified type.

**Parameters:**
- `handler`: Asynchronous delegate to invoke when an event of type `TEvent` is published.

**Behavior:**
1. Ensures a subscriber list exists for the event type.
2. Adds the handler to the subscriber list.
3. Logs the subscription at Debug level.

#### `Unsubscribe<TEvent>(Func<TEvent, Task> handler)`

Removes a previously registered handler for events of the specified type.

**Parameters:**
- `handler`: The handler to remove.

**Behavior:**
1. Attempts to retrieve the subscriber list for the event type.
2. If found, removes the handler from the list.
3. Logs the unsubscription at Debug level.

### Thread Safety

This implementation is **not thread-safe**. Concurrent modifications to the subscriber list (e.g., subscribing/unsubscribing while publishing) may result in undefined behavior. For thread-safe usage, external synchronization is required.

### Usage Example

```csharp
// Define a domain event
public class OrderCreated : DomainEvent
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
}

// Subscribe to the event
eventPublisher.Subscribe<OrderCreated>(async @event =>
{
    await SendConfirmationEmailAsync(@event.OrderId, @event.CustomerName);
});

// Publish the event
await eventPublisher.PublishAsync(new OrderCreated
{
    OrderId = 123,
    CustomerName = "John Doe"
});
```