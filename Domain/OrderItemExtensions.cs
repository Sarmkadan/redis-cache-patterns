#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace RedisCachePatterns.Domain;

/// <summary>
/// Provides useful extension methods for OrderItem operations
/// </summary>
public static class OrderItemExtensions
{
    /// <summary>
    /// Calculates the total price for this order item including tax calculation
    /// </summary>
    /// <param name="orderItem">The order item</param>
    /// <param name="taxRate">Tax rate as percentage (e.g., 8.25 for 8.25%)</param>
    /// <returns>Total price including tax</returns>
    /// <exception cref="ArgumentNullException"><paramref name="orderItem"/> is null</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="taxRate"/> is negative</exception>
    public static decimal GetTotalPriceWithTax(this OrderItem orderItem, decimal taxRate)
    {
        ArgumentNullException.ThrowIfNull(orderItem);
        ArgumentOutOfRangeException.ThrowIfNegative(taxRate);

        decimal subtotal = orderItem.Subtotal;
        decimal taxAmount = subtotal * (taxRate / 100);
        return subtotal + taxAmount;
    }

    /// <summary>
    /// Calculates the total savings from discount for this order item
    /// </summary>
    /// <param name="orderItem">The order item</param>
    /// <returns>Total savings amount</returns>
    /// <exception cref="ArgumentNullException"><paramref name="orderItem"/> is null</exception>
    public static decimal GetTotalSavings(this OrderItem orderItem)
    {
        ArgumentNullException.ThrowIfNull(orderItem);

        return orderItem.GetDiscountAmount();
    }

    /// <summary>
    /// Creates a formatted price breakdown string for the order item
    /// </summary>
    /// <param name="orderItem">The order item</param>
    /// <param name="includeProductName">Whether to include product name if available</param>
    /// <returns>Formatted price breakdown</returns>
    /// <exception cref="ArgumentNullException"><paramref name="orderItem"/> is null</exception>
    public static string FormatPriceBreakdown(this OrderItem orderItem, bool includeProductName = true)
    {
        ArgumentNullException.ThrowIfNull(orderItem);

        var sb = new System.Text.StringBuilder();

        if (includeProductName && orderItem.Product is { Name: { } name })
        {
            sb.Append($"{name} - ");
        }
        else if (includeProductName)
        {
            sb.Append("Product - ");
        }

        sb.Append($"Unit: ${orderItem.UnitPrice:F2} × {orderItem.Quantity} = ${orderItem.Subtotal:F2}");

        if (orderItem.DiscountPercent > 0)
        {
            decimal discountAmount = orderItem.GetDiscountAmount();
            sb.Append($" (-{orderItem.DiscountPercent}% = -${discountAmount:F2})");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Checks if the order item qualifies for free shipping based on quantity thresholds
    /// </summary>
    /// <param name="orderItem">The order item</param>
    /// <param name="freeShippingThreshold">Minimum quantity required for free shipping</param>
    /// <returns>True if qualifies for free shipping, false otherwise</returns>
    /// <exception cref="ArgumentNullException"><paramref name="orderItem"/> is null</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="freeShippingThreshold"/> is not positive</exception>
    public static bool QualifiesForFreeShipping(this OrderItem orderItem, int freeShippingThreshold = 5)
    {
        ArgumentNullException.ThrowIfNull(orderItem);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(freeShippingThreshold, 0);

        return orderItem.Quantity >= freeShippingThreshold;
    }
}