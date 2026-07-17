using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Infrastructure.Repositories;

namespace RedisCachePatterns.Infrastructure.Repositories
{
    /// <summary>
    /// Extension methods for <see cref="ProductRepository"/> providing product-specific query operations.
    /// </summary>
    public static class ProductRepositoryExtensions
    {
        /// <summary>
        /// Retrieves all products in the specified category that are currently low on stock.
        /// </summary>
        /// <param name="repository">The product repository instance.</param>
        /// <param name="category">The category to filter products by. Must not be null or empty.</param>
        /// <returns>An asynchronous enumerable of low stock products in the specified category.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="repository"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="category"/> is null or empty.</exception>
        public static async Task<IEnumerable<Product>> GetLowStockProductsInCategoryAsync(this ProductRepository repository, string category)
        {
            ArgumentNullException.ThrowIfNull(repository);
            ArgumentException.ThrowIfNullOrEmpty(category);

            var products = await repository.GetByCategoryAsync(category);
            return products.Where(p => p.IsLowStock());
        }

        /// <summary>
        /// Searches for products in the specified category whose name contains the given search term (case-insensitive).
        /// </summary>
        /// <param name="repository">The product repository instance.</param>
        /// <param name="category">The category to filter products by. Must not be null or empty.</param>
        /// <param name="searchTerm">The term to search for in product names. Must not be null or empty.</param>
        /// <returns>An asynchronous enumerable of products matching the search criteria.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="repository"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="category"/> or <paramref name="searchTerm"/> is null or empty.</exception>
        public static async Task<IEnumerable<Product>> SearchProductsByNameInCategoryAsync(this ProductRepository repository, string category, string searchTerm)
        {
            ArgumentNullException.ThrowIfNull(repository);
            ArgumentException.ThrowIfNullOrEmpty(category);
            ArgumentException.ThrowIfNullOrEmpty(searchTerm);

            var products = await repository.GetByCategoryAsync(category);
            return products.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }
    }
}