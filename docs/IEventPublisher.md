# IEventPublisher

The `IEventPublisher` interface defines the contract for a pub/sub messaging component within the `redis-cache-patterns` project, facilitating asynchronous event distribution and subscription management. It provides a standardized mechanism for emitting typed events with metadata regarding their origin and timing, while allowing consumers to dynamically register and unregister interest in specific event types. This abstraction decouples event producers from consumers, enabling scalable, event-driven architectures backed by Redis infrastructure.

## API

### `EventId`
```csharp
public string EventId { get; }
```
Retrieves the unique identifier associated with the current publisher instance or the specific event context. This string is typically used for tracing, logging, and correlating events across distributed systems.

### `OccurredAt`
```csharp
public DateTime OccurredAt { get; }
```
Gets the precise timestamp indicating when the event associated with this publisher instance occurred. This value is typically set at the moment of event creation or publication initiation.

### `Source`
```csharp
public string Source { get; }
```
Returns a string identifier representing the origin of the event. This usually denotes the service name, module, or component responsible for generating the event, aiding in debugging and topology mapping.

### `EventPublisher`
```csharp
public EventPublisher { get; }
```
Exposes the concrete implementation or underlying engine handling the publication logic. This property allows access to low-level configurations or diagnostic tools specific to the `EventPublisher` class if direct interaction is required.

### `PublishAsync<TEvent>`
```csharp
public async Task PublishAsync<TEvent>(TEvent eventPayload)
```
Asynchronously broadcasts an event of type `TEvent` to all active subscribers.
*   **Parameters**:
    *   `eventPayload`: The instance of the event object containing the data to be transmitted.
*   **Return Value**: A `Task` representing the asynchronous operation. The task completes when the event has been successfully handed off to the underlying transport layer (e.g., Redis).
*   **Exceptions**: May throw exceptions if the underlying connection to the Redis server is unavailable, if serialization of `TEvent` fails, or if the payload is null.

### `Subscribe<TEvent>`
```csharp
public void Subscribe<TEvent>(Action<TEvent> handler)
```
Registers a callback method to be invoked whenever an event of type `TEvent` is received.
*   **Parameters**:
    *   `handler`: The delegate to execute upon event reception.
*   **Return Value**: None.
*   **Exceptions**: May throw if the handler is null or if the subscription channel cannot be established due to network constraints.

### `Unsubscribe<TEvent>`
```csharp
public void Unsubscribe<TEvent>(Action<TEvent> handler)
```
Removes a previously registered callback for events of type `TEvent`, stopping further notifications to that specific handler.
*   **Parameters**:
    *   `handler`: The specific delegate instance to remove. It must match the instance passed to `Subscribe<TEvent>`.
*   **Return Value**: None.
*   **Exceptions**: Generally does not throw if the handler was not found, but may throw if the underlying unsubscription process encounters a network error.

## Usage

### Example 1: Publishing a Domain Event
The following example demonstrates how to publish a `OrderCreatedEvent` using the interface, leveraging the asynchronous nature of the API to avoid blocking the main execution thread.

```csharp
public class OrderService
{
    private readonly IEventPublisher _eventPublisher;

    public OrderService(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    public async Task CreateOrderAsync(OrderDetails details)
    {
        // Business logic to create order...
        
        var orderEvent = new OrderCreatedEvent 
        { 
            OrderId = details.Id, 
            Timestamp = DateTime.UtcNow 
        };

        // Publish the event to Redis subscribers
        await _eventPublisher.PublishAsync(orderEvent);
        
        // Log using metadata from the publisher
        Console.WriteLine($"Event { _eventPublisher.EventId } published from { _eventPublisher.Source }");
    }
}
```

### Example 2: Subscribing and Unsubscribing to Events
This example shows a consumer service that subscribes to inventory updates during its lifecycle and cleanly unsubscribes upon disposal to prevent memory leaks or orphaned callbacks.

```csharp
public class InventoryMonitor : IDisposable
{
    private readonly IEventPublisher _eventPublisher;
    private readonly Action<InventoryUpdatedEvent> _handler;

    public InventoryMonitor(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
        _handler = HandleInventoryUpdate;
        
        // Subscribe to specific event type
        _eventPublisher.Subscribe<InventoryUpdatedEvent>(_handler);
    }

    private void HandleInventoryUpdate(InventoryUpdatedEvent evt)
    {
        Console.WriteLine($"Inventory changed for item {evt.ItemId} at {_eventPublisher.OccurredAt}");
        // Update local cache or trigger re-calculation
    }

    public void Dispose()
    {
        // Unsubscribe to stop receiving events
        _eventPublisher.Unsubscribe<InventoryUpdatedEvent>(_handler);
    }
}
```

## Notes

*   **Thread Safety**: While the `PublishAsync` method is asynchronous, the `Subscribe` and `Unsubscribe` methods are synchronous void operations. Implementations should ensure that the internal subscription list is thread-safe if these methods are called concurrently from multiple threads. Callers should avoid modifying subscriptions while iterating over them.
*   **Handler Equality**: The `Unsubscribe<TEvent>` method relies on delegate equality. If an anonymous lambda is passed to `Subscribe`, it cannot be unsubscribed later unless the reference is stored. Always store the `Action<TEvent>` delegate in a variable if future unsubscription is required.
*   **Serialization Constraints**: Since this interface is part of a Redis-based pattern, the generic type `TEvent` used in `PublishAsync` and `Subscribe` must be serializable. Complex object graphs containing non-serializable members (e.g., open file handles, database contexts) will cause runtime failures during publication.
*   **Fire-and-Forget Semantics**: The `PublishAsync` method returns a `Task` that typically completes upon successful write to the Redis stream or channel. It does not guarantee that subscribers have successfully processed the event, only that the event was delivered to the broker.
*   **Metadata Consistency**: The properties `EventId`, `OccurredAt`, and `Source` reflect the state of the publisher instance. In some implementations, these may represent the last published event's context or the static configuration of the publisher; consumers should verify the specific implementation behavior regarding whether these properties update dynamically per event or remain static per instance.
