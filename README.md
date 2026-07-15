// existing content ...

## IEventPublisher

The `IEventPublisher` interface represents an event publisher for pub-sub pattern supporting async event handling. It provides methods for publishing events, subscribing to events, and unsubscribing from events. This allows for decoupling event producers from consumers using the observer pattern.

### Usage Example
```csharp
public class MyEvent : DomainEvent
{
    public string MyProperty { get; set; }
}

public class MyEventHandler
{
    public async Task HandleMyEvent(MyEvent @event)
    {
        Console.WriteLine($"Received event: MyProperty={@event.MyProperty}");
    }
}

var eventPublisher = new EventPublisher(new NullLogger<EventPublisher>());
var myEventHandler = new MyEventHandler();

eventPublisher.Subscribe<MyEvent>(myEventHandler.HandleMyEvent);

var myEvent = new MyEvent { MyProperty = "Hello, World!" };
await eventPublisher.PublishAsync(myEvent);
```
