// existing content ...

## OrderCreatedEvent

The `OrderCreatedEvent` class represents an event triggered when a new order is created. It contains details such as the order ID, user ID, and total amount of the order. This event can be used to trigger downstream operations, such as inventory reservation or payment processing.

### Usage Example
```csharp
var orderCreatedEvent = new OrderCreatedEvent
{
    OrderId = 123,
    UserId = 456,
    TotalAmount = 99.99m
};

Console.WriteLine($"Order created: OrderId={orderCreatedEvent.OrderId} | UserId={orderCreatedEvent.UserId} | Total=${orderCreatedEvent.TotalAmount}");
```
