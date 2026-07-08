#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Domain;

/// <summary>
/// Tracks inventory movements and stock levels across warehouses
/// </summary>
public class InventoryItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Warehouse { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public int QuantityAvailable { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime LastCountedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastMovedAt { get; set; }
    public DateTime? LastUpdated { get; set; }
    public int MinimumLevel { get; set; }
    public int ReorderPoint { get; set; }
    public int MaxStock { get; set; }

    public bool IsLowStock() => QuantityAvailable <= MinimumLevel;

    public bool CanReserve(int quantity) => QuantityAvailable >= quantity;

    public void Reserve(int quantity)
    {
        if (!CanReserve(quantity))
            throw new InvalidOperationException($"Cannot reserve {quantity} units. Available: {QuantityAvailable}");
        QuantityReserved += quantity;
        LastMovedAt = DateTime.UtcNow;
        LastUpdated = LastMovedAt;
        SyncQuantityAvailable();
    }

    public void ReleaseReservation(int quantity)
    {
        if (QuantityReserved < quantity)
            throw new InvalidOperationException("Cannot release more than reserved");
        QuantityReserved -= quantity;
        LastMovedAt = DateTime.UtcNow;
        LastUpdated = LastMovedAt;
        SyncQuantityAvailable();
    }

    public void ReceiveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive");
        QuantityOnHand += quantity;
        LastMovedAt = DateTime.UtcNow;
        LastUpdated = LastMovedAt;
        SyncQuantityAvailable();
    }

    public void DispatchStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive");
        if (QuantityOnHand < quantity)
            throw new InvalidOperationException("Insufficient stock");
        QuantityOnHand -= quantity;
        QuantityReserved = Math.Max(0, QuantityReserved - quantity);
        LastMovedAt = DateTime.UtcNow;
        LastUpdated = LastMovedAt;
        SyncQuantityAvailable();
    }

    public void AdjustCount(int newCount)
    {
        QuantityOnHand = Math.Max(0, newCount);
        LastCountedAt = DateTime.UtcNow;
        LastMovedAt = DateTime.UtcNow;
        LastUpdated = LastMovedAt;
        SyncQuantityAvailable();
    }

    /// <summary>
    /// Recomputes <see cref="QuantityAvailable"/> from <see cref="QuantityOnHand"/> and
    /// <see cref="QuantityReserved"/>. Kept in sync explicitly (rather than computed) so that
    /// callers may also set it directly when hydrating from cache/serialization.
    /// </summary>
    private void SyncQuantityAvailable()
    {
        QuantityAvailable = QuantityOnHand - QuantityReserved;
    }

    public override string ToString() => $"{Warehouse}/{Location}: {QuantityAvailable} available ({QuantityOnHand} on hand, {QuantityReserved} reserved)";
}
