#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Domain;

namespace RedisCachePatterns.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for InventoryItem entities
/// </summary>
public class InventoryRepository : Repository<InventoryItem>, IInventoryRepository
{
    public async Task<InventoryItem?> GetByProductAndWarehouseAsync(int productId, string warehouse)
    {
        lock (_lock)
        {
            return _data.FirstOrDefault(i =>
                i.ProductId == productId &&
                i.Warehouse.Equals(warehouse, StringComparison.OrdinalIgnoreCase));
        }
    }

    public async Task<IEnumerable<InventoryItem>> GetByProductAsync(int productId)
    {
        lock (_lock)
        {
            return _data.Where(i => i.ProductId == productId).ToList();
        }
    }

    public async Task<IEnumerable<InventoryItem>> GetLowStockItemsAsync()
    {
        lock (_lock)
        {
            return _data.Where(i => i.IsLowStock()).ToList();
        }
    }

    public async Task<int> GetTotalQuantityAsync(int productId)
    {
        lock (_lock)
        {
            return _data.Where(i => i.ProductId == productId)
                .Sum(i => i.QuantityOnHand);
        }
    }
}
