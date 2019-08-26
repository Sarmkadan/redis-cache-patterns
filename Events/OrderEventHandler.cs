// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Events;

/// <summary>
/// Domain events related to order operations
/// </summary>
public class OrderCreatedEvent : DomainEvent
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public decimal TotalAmount { get; set; }
}

public class OrderConfirmedEvent : DomainEvent
{
    public int OrderId { get; set; }
    public DateTime ConfirmedAt { get; set; }
}

public class OrderShippedEvent : DomainEvent
{
    public int OrderId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
}

public class InventoryReservedEvent : DomainEvent
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int OrderId { get; set; }
}

/// <summary>
/// Handles order-related events and performs downstream operations
/// </summary>
public class OrderEventHandler
{
    private readonly ILogger<OrderEventHandler> _logger;
    private readonly List<OrderEvent> _processedEvents = new();

    public OrderEventHandler(ILogger<OrderEventHandler> logger)
    {
        _logger = logger;
    }

    public Task OnOrderCreatedAsync(OrderCreatedEvent @event)
    {
        _logger.LogInformation(
            "Order created: OrderId={OrderId} | UserId={UserId} | Total=${Amount}",
            @event.OrderId, @event.UserId, @event.TotalAmount);

        _processedEvents.Add(new OrderEvent
        {
            EventType = "OrderCreated",
            OrderId = @event.OrderId,
            ProcessedAt = DateTime.UtcNow
        });

        return Task.CompletedTask;
    }

    public Task OnOrderConfirmedAsync(OrderConfirmedEvent @event)
    {
        _logger.LogInformation("Order confirmed: OrderId={OrderId} | ConfirmedAt={Time}",
            @event.OrderId, @event.ConfirmedAt);

        _processedEvents.Add(new OrderEvent
        {
            EventType = "OrderConfirmed",
            OrderId = @event.OrderId,
            ProcessedAt = DateTime.UtcNow
        });

        return Task.CompletedTask;
    }

    public Task OnOrderShippedAsync(OrderShippedEvent @event)
    {
        _logger.LogInformation("Order shipped: OrderId={OrderId} | TrackingNumber={Tracking}",
            @event.OrderId, @event.TrackingNumber);

        _processedEvents.Add(new OrderEvent
        {
            EventType = "OrderShipped",
            OrderId = @event.OrderId,
            ProcessedAt = DateTime.UtcNow
        });

        return Task.CompletedTask;
    }

    public Task OnInventoryReservedAsync(InventoryReservedEvent @event)
    {
        _logger.LogInformation("Inventory reserved: ProductId={ProductId} | Qty={Quantity} | OrderId={OrderId}",
            @event.ProductId, @event.Quantity, @event.OrderId);

        return Task.CompletedTask;
    }

    public IEnumerable<OrderEvent> GetProcessedEvents() => _processedEvents.AsReadOnly();

    public void ClearProcessedEvents() => _processedEvents.Clear();

    public class OrderEvent
    {
        public string EventType { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}
