#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Domain;

/// <summary>
/// Additional query-style extension methods for <see cref="InventoryItem"/> covering
/// reorder decisions and stock state checks.
/// </summary>
public static class InventoryItemExtensionsQuery
{
    /// <summary>
    /// Determines whether the inventory item has reached its reorder point
    /// (available quantity is at or below <see cref="InventoryItem.ReorderPoint"/>)
    /// </summary>
    /// <param name="item">The inventory item</param>
    /// <returns>True if a replenishment order should be placed, false otherwise</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is null</exception>
    public static bool NeedsReorder(this InventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.QuantityAvailable <= item.ReorderPoint;
    }

    /// <summary>
    /// Determines whether there is no available stock left to sell or reserve
    /// </summary>
    /// <param name="item">The inventory item</param>
    /// <returns>True if available quantity is zero or negative, false otherwise</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is null</exception>
    public static bool IsOutOfStock(this InventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.QuantityAvailable <= 0;
    }

    /// <summary>
    /// Calculates how many units are needed to bring available stock back up to max stock
    /// when the item needs reordering
    /// </summary>
    /// <param name="item">The inventory item</param>
    /// <returns>Suggested reorder quantity (never negative); 0 if the item does not need reordering</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is null</exception>
    public static int GetSuggestedReorderQuantity(this InventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!item.NeedsReorder())
        {
            return 0;
        }

        return Math.Max(0, item.MaxStock - item.QuantityAvailable);
    }

    /// <summary>
    /// Determines whether any units of the inventory item are currently reserved
    /// </summary>
    /// <param name="item">The inventory item</param>
    /// <returns>True if <see cref="InventoryItem.QuantityReserved"/> is greater than zero, false otherwise</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is null</exception>
    public static bool HasReservations(this InventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.QuantityReserved > 0;
    }
}
