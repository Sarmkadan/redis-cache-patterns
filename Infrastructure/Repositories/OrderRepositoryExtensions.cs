using RedisCachePatterns.Domain;
using RedisCachePatterns.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace RedisCachePatterns.Infrastructure.Repositories
{
    /// <summary>
    /// Provides extension methods for <see cref="OrderRepository"/>.
    /// </summary>
    public static class OrderRepositoryExtensions
    {
        /// <summary>
        /// Retrieves a list of orders for a user, filtered by a specific status.
        /// </summary>
        /// <param name="repository">The <see cref="OrderRepository"/> instance.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="status">The status to filter by.</param>
        /// <returns>A list of orders matching the specified criteria.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="repository"/> is null.</exception>
        public static async Task<IReadOnlyList<Order>> GetOrdersByUserAndStatusAsync(this OrderRepository repository, int userId, OrderStatus status)
        {
            ArgumentNullException.ThrowIfNull(repository);

            var orders = await repository.GetByUserIdAsync(userId);
            return orders.Where(o => o.Status == status).ToList();
        }

        /// <summary>
        /// Checks if an order exists for a given order number.
        /// </summary>
        /// <param name="repository">The <see cref="OrderRepository"/> instance.</param>
        /// <param name="orderNumber">The order number to check.</param>
        /// <returns><c>true</c> if an order exists; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="orderNumber"/> is null or empty.</exception>
        public static async Task<bool> OrderExistsAsync(this OrderRepository repository, string orderNumber)
        {
            ArgumentException.ThrowIfNullOrEmpty(orderNumber);

            return await repository.GetByOrderNumberAsync(orderNumber) != null;
        }

        /// <summary>
        /// Retrieves the number of orders within a specific date range.
        /// </summary>
        /// <param name="repository">The <see cref="OrderRepository"/> instance.</param>
        /// <param name="startDate">The start date of the range (inclusive).</param>
        /// <param name="endDate">The end date of the range (inclusive).</param>
        /// <returns>The number of orders within the specified date range.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="startDate"/> is greater than <paramref name="endDate"/>.</exception>
        public static async Task<int> CountOrdersInDateRangeAsync(this OrderRepository repository, DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                throw new ArgumentOutOfRangeException(nameof(startDate), "Start date cannot be greater than end date.");
            }

            var orders = await repository.GetOrdersInDateRangeAsync(startDate, endDate);
            return orders.Count();
        }
    }
}
