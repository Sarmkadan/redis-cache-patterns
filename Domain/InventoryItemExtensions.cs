#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Domain;

/// <summary>
/// Extension methods for InventoryItem providing common inventory operations
/// </summary>
public static class InventoryItemExtensions
{
    /// <summary>
    /// Determines if the inventory item is overstocked (quantity exceeds max stock)
    /// </summary>
    /// <param name="item">The inventory item</param>
    /// <returns>True if overstocked, false otherwise</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is null</exception>
    public static bool IsOverstocked(this InventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.QuantityOnHand > item.MaxStock;
    }

    /// <summary>
    /// Calculates the percentage of stock that is reserved
    /// </summary>
    /// <param name="item">The inventory item</param>
    /// <returns>Percentage of stock that is reserved (0-100)</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is null</exception>
    public static double GetReservedPercentage(this InventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.QuantityOnHand == 0)
            return 0;
        return (double)item.QuantityReserved / item.QuantityOnHand * 100;
    }

    /// <summary>
    /// Calculates the percentage of available stock relative to max stock
    /// </summary>
    /// <param name="item">The inventory item</param>
    /// <returns>Percentage of max stock that is available (0-100)</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is null</exception>
    public static double GetStockPercentage(this InventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.MaxStock == 0)
            return 0;
        return (double)item.QuantityAvailable / item.MaxStock * 100;
    }

    /// <summary>
    /// Attempts to reserve stock if available, returns success status
    /// </summary>
    /// <param name="item">The inventory item</param>
    /// <param name="quantity">Quantity to reserve (must be positive)</param>
    /// <returns>True if reservation succeeded, false if insufficient stock</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is null</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantity"/> is negative</exception>
    public static bool TryReserve(this InventoryItem item, int quantity)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);

        if (item.CanReserve(quantity))
        {
            item.Reserve(quantity);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a formatted string representing the stock status
    /// </summary>
    /// <param name="item">The inventory item</param>
    /// <returns>Formatted stock status string</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is null</exception>
    public static string GetStockStatus(this InventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.IsLowStock()
            ? "LOW STOCK"
            : item.IsOverstocked()
                ? "OVERSTOCKED"
                : "OK";
    }

    /// <summary>
    /// Calculates days since last inventory count
    /// </summary>
    /// <param name="item">The inventory item</param>
    /// <returns>Days since last count, or null if never counted</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is null</exception>
    public static int? GetDaysSinceLastCount(this InventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.LastCountedAt == default)
            return null;
        return (int)(DateTime.UtcNow - item.LastCountedAt).TotalDays;
    }

    /// <summary>
    /// Calculates days since last movement
    /// </summary>
    /// <param name="item">The inventory item</param>
    /// <returns>Days since last movement, or null if never moved</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is null</exception>
    public static int? GetDaysSinceLastMovement(this InventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.LastMovedAt == null)
            return null;
        return (int)(DateTime.UtcNow - item.LastMovedAt.Value).TotalDays;
    }
}
