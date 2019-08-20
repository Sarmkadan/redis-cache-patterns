#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Domain;

namespace RedisCachePatterns.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Order entities with order-specific queries
/// </summary>
public class OrderRepository : Repository<Order>, IOrderRepository
{
    public async Task<IEnumerable<Order>> GetByUserIdAsync(int userId)
    {
        lock (_lock)
        {
            return _data.Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).ToList();
        }
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        lock (_lock)
        {
            return _data.FirstOrDefault(o => o.OrderNumber.Equals(orderNumber, StringComparison.OrdinalIgnoreCase));
        }
    }

    public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status)
    {
        lock (_lock)
        {
            return _data.Where(o => o.Status == status).OrderByDescending(o => o.CreatedAt).ToList();
        }
    }

    public async Task<IEnumerable<Order>> GetOrdersInDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        lock (_lock)
        {
            return _data.Where(o =>
                o.CreatedAt >= startDate &&
                o.CreatedAt <= endDate).OrderByDescending(o => o.CreatedAt).ToList();
        }
    }
}
