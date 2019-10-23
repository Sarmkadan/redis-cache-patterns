#nullable enable

using System.Text;

namespace RedisCachePatterns.Domain;

/// <summary>
/// Extension methods for the Product class providing additional functionality
/// </summary>
public static class ProductExtensions
{
    /// <summary>
    /// Calculates the total value of the product in stock based on current price and stock quantity
    /// </summary>
    /// <param name="product">The product instance</param>
    /// <returns>The total inventory value (Price * StockQuantity)</returns>
    public static decimal CalculateInventoryValue(this Product product)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        return product.Price * product.StockQuantity;
    }

    /// <summary>
    /// Generates a formatted product description suitable for display in catalogs or receipts
    /// </summary>
    /// <param name="product">The product instance</param>
    /// <param name="includeRating">Whether to include rating information</param>
    /// <param name="includeCategory">Whether to include category information</param>
    /// <returns>Formatted product description string</returns>
    public static string FormatForDisplay(this Product product, bool includeRating = true, bool includeCategory = true)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        var sb = new StringBuilder();
        sb.AppendLine(product.Name);
        sb.AppendLine($"SKU: {product.Sku}");

        if (includeCategory && !string.IsNullOrEmpty(product.Category))
        {
            sb.AppendLine($"Category: {product.Category}");
        }

        sb.AppendLine($"Price: ${product.Price:F2}");

        if (includeRating && product.ReviewCount > 0)
        {
            sb.AppendLine($"Rating: {product.Rating:F1}/5 ({product.ReviewCount} reviews)");
        }

        if (!string.IsNullOrEmpty(product.Description))
        {
            sb.AppendLine();
            sb.AppendLine(product.Description);
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Determines if the product is eligible for reorder based on current stock and reorder level
    /// </summary>
    /// <param name="product">The product instance</param>
    /// <param name="includePendingOrders">Whether to consider pending orders in the calculation</param>
    /// <returns>True if reorder is needed, false otherwise</returns>
    public static bool NeedsReorder(this Product product, bool includePendingOrders = false)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        var stockToConsider = product.StockQuantity;

        if (includePendingOrders && product.OrderItems != null)
        {
            var pendingOrders = product.OrderItems.Sum(oi => oi.Quantity);
            stockToConsider -= pendingOrders;
        }

        return stockToConsider <= product.ReorderLevel;
    }

    /// <summary>
    /// Calculates the potential revenue if all available stock were sold at current price
    /// </summary>
    /// <param name="product">The product instance</param>
    /// <param name="includePendingOrders">Whether to include pending orders in available stock calculation</param>
    /// <returns>Potential revenue amount</returns>
    public static decimal CalculatePotentialRevenue(this Product product, bool includePendingOrders = false)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        var availableStock = product.StockQuantity;

        if (includePendingOrders && product.OrderItems != null)
        {
            var pendingOrders = product.OrderItems.Sum(oi => oi.Quantity);
            availableStock = Math.Max(0, availableStock - pendingOrders);
        }

        return product.Price * availableStock;
    }
}