#nullable enable

using System.Text;

namespace RedisCachePatterns.Domain;

/// <summary>
/// Provides extension methods for <see cref="Product"/> to enhance inventory management and display capabilities.
/// </summary>
public static class ProductExtensions
{
    /// <summary>
    /// Calculates the total inventory value based on the product's current price and available stock quantity.
    /// </summary>
    /// <param name="product">The product instance. Cannot be <see langword="null"/>.</param>
    /// <returns>The total inventory value calculated as <c>Price * StockQuantity</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="product"/> is <see langword="null"/>.</exception>
    public static decimal CalculateInventoryValue(this Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        return product.Price * product.StockQuantity;
    }

    /// <summary>
    /// Generates a human-readable product description suitable for display in catalogs, receipts, or UI elements.
    /// </summary>
    /// <param name="product">The product instance. Cannot be <see langword="null"/>.</param>
    /// <param name="includeRating">Whether to include rating information in the output. Defaults to <see langword="true"/>.</param>
    /// <param name="includeCategory">Whether to include category information in the output. Defaults to <see langword="true"/>.</param>
    /// <returns>A formatted product description string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="product"/> is <see langword="null"/>.</exception>
    public static string FormatForDisplay(this Product product, bool includeRating = true, bool includeCategory = true)
    {
        ArgumentNullException.ThrowIfNull(product);

        var sb = new StringBuilder();
        sb.Append(product.Name);
        sb.AppendLine($"\nSKU: {product.Sku}");

        if (includeCategory && !string.IsNullOrWhiteSpace(product.Category))
        {
            sb.AppendLine($"Category: {product.Category}");
        }

        sb.AppendLine($"Price: ${product.Price:F2}");

        if (includeRating && product.ReviewCount > 0)
        {
            sb.AppendLine($"Rating: {product.Rating:F1}/5 ({product.ReviewCount} reviews)");
        }

        if (!string.IsNullOrWhiteSpace(product.Description))
        {
            sb.AppendLine();
            sb.Append(product.Description.Trim());
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Determines whether the product requires reordering based on current stock levels and reorder threshold.
    /// </summary>
    /// <param name="product">The product instance. Cannot be <see langword="null"/>.</param>
    /// <param name="includePendingOrders">Whether to subtract pending orders from available stock when calculating reorder eligibility.
    /// Pending orders are retrieved from <see cref="Product.OrderItems"/> if available.</param>
    /// <returns><see langword="true"/> if stock quantity is at or below the reorder level; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="product"/> is <see langword="null"/>.</exception>
    public static bool NeedsReorder(this Product product, bool includePendingOrders = false)
    {
        ArgumentNullException.ThrowIfNull(product);

        var stockToConsider = product.StockQuantity;

        if (includePendingOrders && product.OrderItems.Count > 0)
        {
            var pendingOrders = product.OrderItems.Sum(oi => oi.Quantity);
            stockToConsider -= pendingOrders;
        }

        return stockToConsider <= product.ReorderLevel;
    }

    /// <summary>
    /// Calculates the potential revenue if all available stock were sold at the current price.
    /// </summary>
    /// <param name="product">The product instance. Cannot be <see langword="null"/>.</param>
    /// <param name="includePendingOrders">Whether to subtract pending orders from available stock when calculating potential revenue.
    /// Pending orders are retrieved from <see cref="Product.OrderItems"/> if available.</param>
    /// <returns>The potential revenue amount calculated as <c>Price * AvailableStock</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="product"/> is <see langword="null"/>.</exception>
    public static decimal CalculatePotentialRevenue(this Product product, bool includePendingOrders = false)
    {
        ArgumentNullException.ThrowIfNull(product);

        var availableStock = product.StockQuantity;

        if (includePendingOrders && product.OrderItems.Count > 0)
        {
            var pendingOrders = product.OrderItems.Sum(oi => oi.Quantity);
            availableStock = Math.Max(0, availableStock - pendingOrders);
        }

        return product.Price * availableStock;
    }
}