#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Domain;

/// <summary>
/// Represents a customer order containing multiple items
/// </summary>
public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; } = 0;
    public string? Notes { get; set; }
    public string? TrackingNumber { get; set; }

    public List<OrderItem> Items { get; set; } = new();

    public void AddItem(OrderItem item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot add items to a non-pending order");
        Items.Add(item);
        RecalculateTotal();
    }

    public void RemoveItem(int itemId)
    {
        var item = Items.FirstOrDefault(x => x.Id == itemId);
        if (item != null)
        {
            Items.Remove(item);
            RecalculateTotal();
        }
    }

    public void RecalculateTotal()
    {
        TotalAmount = Items.Sum(x => x.Subtotal) + TaxAmount + ShippingCost;
    }

    public void ConfirmOrder()
    {
        if (Items.Count == 0)
            throw new InvalidOperationException("Cannot confirm order with no items");
        Status = OrderStatus.Confirmed;
    }

    public void ShipOrder(string trackingNumber)
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed orders can be shipped");
        Status = OrderStatus.Shipped;
        TrackingNumber = trackingNumber;
    }

    public void CompleteOrder()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException("Only shipped orders can be completed");
        Status = OrderStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void CancelOrder()
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a shipped or completed order");
        Status = OrderStatus.Cancelled;
    }

    public int GetItemCount() => Items.Count;

    public decimal GetSubtotal() => TotalAmount - TaxAmount - ShippingCost;

    public override string ToString() => $"Order #{OrderNumber} - ${TotalAmount:F2} ({Status})";
}

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipped = 2,
    Completed = 3,
    Cancelled = 4
}
