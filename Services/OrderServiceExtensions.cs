#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using RedisCachePatterns.Domain;
using RedisCachePatterns.Exceptions;
using System.Globalization;

namespace RedisCachePatterns.Services;

/// <summary>
/// Extension methods for <see cref="OrderService"/> providing additional convenience and batch operations
/// </summary>
public static class OrderServiceExtensions
{
    /// <summary>
    /// Attempts to get an order by its ID, returning null if not found instead of throwing
    /// </summary>
    /// <param name="service">The order service instance</param>
    /// <param name="orderId">The order identifier</param>
    /// <returns>The order if found, otherwise null</returns>
    public static async Task<Order?> TryGetOrderByIdAsync(this OrderService service, int orderId)
    {
        ArgumentNullException.ThrowIfNull(service);

        try
        {
            return await service.GetOrderByIdAsync(orderId);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets orders by their status with pagination support
    /// </summary>
    /// <param name="service">The order service instance</param>
    /// <param name="status">The order status to filter by</param>
    /// <param name="pageNumber">The page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paged collection of orders</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when pageNumber or pageSize is less than 1</exception>
    public static async Task<IReadOnlyList<Order>> GetOrdersByStatusPagedAsync(
        this OrderService service,
        OrderStatus status,
        int pageNumber,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var allOrders = await service.GetOrdersByStatusAsync(status);
        return allOrders.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
    }

    /// <summary>
    /// Gets the total count of orders matching the specified status
    /// </summary>
    /// <param name="service">The order service instance</param>
    /// <param name="status">The order status to count</param>
    /// <returns>The count of orders with the specified status</returns>
    public static async Task<int> CountOrdersByStatusAsync(this OrderService service, OrderStatus status)
    {
        ArgumentNullException.ThrowIfNull(service);

        var orders = await service.GetOrdersByStatusAsync(status);
        return orders.Count();
    }

    /// <summary>
    /// Gets orders created within the last N days
    /// </summary>
    /// <param name="service">The order service instance</param>
    /// <param name="days">Number of days to look back from today</param>
    /// <returns>Collection of orders created within the time period</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when days is less than 1</exception>
    public static async Task<IEnumerable<Order>> GetOrdersFromLastDaysAsync(this OrderService service, int days)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentOutOfRangeException.ThrowIfLessThan(days, 1);

        var startDate = DateTime.UtcNow.AddDays(-days);
        var endDate = DateTime.UtcNow;

        return await service.GetOrdersInDateRangeAsync(startDate, endDate);
    }

    /// <summary>
    /// Gets the total order count for a specific user
    /// </summary>
    /// <param name="service">The order service instance</param>
    /// <param name="userId">The user identifier</param>
    /// <returns>The count of orders for the user</returns>
    public static async Task<int> CountUserOrdersAsync(this OrderService service, int userId)
    {
        ArgumentNullException.ThrowIfNull(service);

        var orders = await service.GetUserOrdersAsync(userId);
        return orders.Count();
    }

    /// <summary>
    /// Attempts to get an order by its order number, returning null if not found
    /// </summary>
    /// <param name="service">The order service instance</param>
    /// <param name="orderNumber">The order number</param>
    /// <returns>The order if found, otherwise null</returns>
    public static async Task<Order?> TryGetOrderByNumberAsync(this OrderService service, string orderNumber)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(orderNumber);

        try
        {
            return await service.GetOrderByNumberAsync(orderNumber);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets orders by status
    /// </summary>
    /// <param name="service">The order service instance</param>
    /// <param name="status">The order status to filter by</param>
    /// <returns>Collection of orders with the specified status</returns>
    public static async Task<IEnumerable<Order>> GetOrdersByStatusWithFormattedStatusAsync(
        this OrderService service,
        OrderStatus status)
    {
        ArgumentNullException.ThrowIfNull(service);

        return await service.GetOrdersByStatusAsync(status);
    }

    /// <summary>
    /// Gets orders in a specific date range
    /// </summary>
    /// <param name="service">The order service instance</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <returns>Collection of orders in the date range</returns>
    public static async Task<IEnumerable<Order>> GetOrdersInDateRangeFormattedAsync(
        this OrderService service,
        DateTime startDate,
        DateTime endDate)
    {
        ArgumentNullException.ThrowIfNull(service);

        return await service.GetOrdersInDateRangeAsync(startDate, endDate);
    }
}