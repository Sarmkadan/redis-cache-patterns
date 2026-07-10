using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Infrastructure.Repositories;

namespace RedisCachePatterns.Infrastructure.Repositories
{
    public static class ProductRepositoryExtensions
    {
        public static async Task<IEnumerable<Product>> GetLowStockProductsInCategoryAsync(this ProductRepository repository, string category)
        {
            var products = await repository.GetByCategoryAsync(category);
            return products.Where(p => p.IsLowStock());
        }

        public static async Task<IEnumerable<Product>> SearchProductsByNameInCategoryAsync(this ProductRepository repository, string category, string searchTerm)
        {
            var products = await repository.GetByCategoryAsync(category);
            return products.Where(p => p.Name.Contains(searchTerm, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
