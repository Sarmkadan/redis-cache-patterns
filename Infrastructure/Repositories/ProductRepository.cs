#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Domain;

namespace RedisCachePatterns.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Product entities with product-specific queries
/// </summary>
public class ProductRepository : Repository<Product>, IProductRepository
{
    public async Task<IEnumerable<Product>> GetByCategoryAsync(string category)
    {
        lock (_lock)
        {
            return _data.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase) && p.IsActive).ToList();
        }
    }

    public async Task<Product?> GetBySkuAsync(string sku)
    {
        lock (_lock)
        {
            return _data.FirstOrDefault(p => p.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));
        }
    }

    public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
    {
        lock (_lock)
        {
            return _data.Where(p => p.IsLowStock() && p.IsActive).ToList();
        }
    }

    public async Task<IEnumerable<Product>> SearchByNameAsync(string searchTerm)
    {
        lock (_lock)
        {
            var term = searchTerm.ToLower();
            return _data.Where(p =>
                (p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                 p.Description.Contains(term, StringComparison.OrdinalIgnoreCase)) &&
                p.IsActive).ToList();
        }
    }
}
