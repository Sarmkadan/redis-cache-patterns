#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Domain;

/// <summary>
/// Represents an individual item in an order
/// </summary>
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; } = 0;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public decimal Subtotal => (UnitPrice * Quantity) * (1 - DiscountPercent / 100);

    public decimal GetDiscountAmount() => UnitPrice * Quantity * (DiscountPercent / 100);

    public void ApplyDiscount(decimal discountPercent)
    {
        if (discountPercent < 0 || discountPercent > 100)
            throw new ArgumentException("Discount must be between 0 and 100");
        DiscountPercent = discountPercent;
    }

    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero");
        Quantity = quantity;
    }

    public override string ToString() => $"Product #{ProductId} x {Quantity} @ ${UnitPrice:F2}";
}
