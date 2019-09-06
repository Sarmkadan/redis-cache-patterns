#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Services;
using RedisCachePatterns.Infrastructure.Repositories;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Results;
using RedisCachePatterns.Utilities;
using System;
using System.Threading.Tasks;

namespace RedisCachePatterns.Examples;

/// <summary>
/// Demonstrates cache invalidation strategies including pattern-based,
/// time-based, and selective invalidation approaches.
/// </summary>
public class CacheInvalidationExample
{
    private readonly ICacheService _cacheService;
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CacheInvalidationExample(
        ICacheService cacheService,
        IProductRepository productRepository,
        ICategoryRepository categoryRepository)
    {
        _cacheService = cacheService;
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    /// <summary>
    /// Invalidates all products in a specific category using pattern matching.
    /// </summary>
    public async Task<OperationResult> InvalidateCategoryProductsAsync(int categoryId)
    {
        try
        {
            Console.WriteLine($"Invalidating all products in category {categoryId}");

            // Invalidate all product keys for this category
            var pattern = $"product:category:{categoryId}:*";
            await _cacheService.InvalidateAsync(pattern).ConfigureAwait(false);
            Console.WriteLine($"✓ Invalidated pattern: {pattern}");

            // Invalidate category listing
            var listingPattern = $"products:list:*";
            await _cacheService.InvalidateAsync(listingPattern).ConfigureAwait(false);
            Console.WriteLine($"✓ Invalidated pattern: {listingPattern}");

            // Invalidate search results
            var searchPattern = $"search:*";
            await _cacheService.InvalidateAsync(searchPattern).ConfigureAwait(false);
            Console.WriteLine($"✓ Invalidated pattern: {searchPattern}");

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Invalidation failed: {ex.Message}");
            return OperationResult.Failure($"Invalidation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Selective invalidation - only removes specific keys without affecting others.
    /// </summary>
    public async Task<OperationResult> InvalidateSpecificProductAsync(int productId)
    {
        try
        {
            Console.WriteLine($"Selectively invalidating product {productId}");

            var productKey = $"product:{productId}";
            var byNameKey = $"product:byname:{productId}";
            var detailsKey = $"product:details:{productId}";

            var tasks = new[]
            {
                _cacheService.RemoveAsync(productKey),
                _cacheService.RemoveAsync(byNameKey),
                _cacheService.RemoveAsync(detailsKey)
            };

            await Task.WhenAll(tasks).ConfigureAwait(false);

            Console.WriteLine($"✓ Removed {tasks.Length} product-specific cache entries");
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Invalidation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Invalidates all cache entries - nuclear option for data consistency issues.
    /// Use sparingly and with caution.
    /// </summary>
    public async Task<OperationResult> InvalidateAllCacheAsync()
    {
        try
        {
            Console.WriteLine("⚠ Performing complete cache clear (nuclear option)");

            await _cacheService.InvalidateAsync("*").ConfigureAwait(false);

            Console.WriteLine("✓ All cache entries cleared");
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Clear failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Cascading invalidation - when a product changes, invalidate related caches.
    /// </summary>
    public async Task<OperationResult> UpdateProductWithCascadingInvalidationAsync(Product product)
    {
        try
        {
            Console.WriteLine($"Updating product {product.Id} with cascading invalidation");

            // Update in database
            Console.WriteLine("  → Writing to database");
            var updated = await _productRepository.UpdateAsync(product).ConfigureAwait(false);

            // Invalidate all related caches
            var invalidatePatterns = new[]
            {
                $"product:{product.Id}",                    // Direct product cache
                $"product:category:{product.CategoryId}:*", // Category listings
                $"products:all:*",                          // All products listings
                $"search:*",                                // Search results
                $"related:*",                               // Related products
                $"popular:*"                                // Popular products
            };

            Console.WriteLine("  → Invalidating related cache patterns");
            foreach (var pattern in invalidatePatterns)
            {
                await _cacheService.InvalidateAsync(pattern).ConfigureAwait(false);
            }

            Console.WriteLine($"✓ Product {product.Id} updated, {invalidatePatterns.Length} cache patterns invalidated");
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Smart invalidation - uses TTL-based expiration instead of immediate invalidation.
    /// Reduces write pressure while maintaining eventual consistency.
    /// </summary>
    public async Task<OperationResult> UpdateProductWithTTLInvalidationAsync(Product product)
    {
        try
        {
            Console.WriteLine($"Updating product {product.Id} with TTL-based invalidation");

            // Update database
            await _productRepository.UpdateAsync(product).ConfigureAwait(false);

            // Instead of immediate invalidation, set a short TTL
            var cacheKey = $"product:{product.Id}";
            var shortTTL = TimeSpan.FromMinutes(1); // Cache for 1 min then auto-expire

            await _cacheService.SetAsync(cacheKey, product, shortTTL).ConfigureAwait(false);

            Console.WriteLine($"✓ Product {product.Id} will auto-invalidate in 1 minute");
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Conditional invalidation - only invalidates if certain conditions are met.
    /// </summary>
    public async Task<OperationResult> UpdateProductWithConditionalInvalidationAsync(
        Product product,
        string[] priceChangeThreshold)
    {
        try
        {
            var oldProduct = await _productRepository.GetByIdAsync(product.Id).ConfigureAwait(false);
            if (oldProduct == null)
                return OperationResult.Failure("Product not found");

            // Calculate price change percentage
            var priceChange = Math.Abs(product.Price - oldProduct.Price) / oldProduct.Price * 100;

            Console.WriteLine($"Price changed by {priceChange:F1}%");

            // Update database
            await _productRepository.UpdateAsync(product).ConfigureAwait(false);

            // Only invalidate if significant price change
            if (priceChange > 10)
            {
                Console.WriteLine("  → Significant price change detected - invalidating cache");
                var key = $"product:{product.Id}";
                await _cacheService.RemoveAsync(key).ConfigureAwait(false);

                // Also invalidate related searches
                await _cacheService.InvalidateAsync($"search:*").ConfigureAwait(false);
                await _cacheService.InvalidateAsync($"price:*").ConfigureAwait(false);
            }
            else
            {
                Console.WriteLine("  → Minor price change - updating cache with new data");
                var key = $"product:{product.Id}";
                await _cacheService.SetAsync(key, product, TimeSpan.FromHours(2)).ConfigureAwait(false);
            }

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Batch invalidation - efficiently clears multiple specific keys.
    /// </summary>
    public async Task<OperationResult> InvalidateProductsAsync(int[] productIds)
    {
        try
        {
            Console.WriteLine($"Invalidating {productIds.Length} products");

            var tasks = productIds.Select(id =>
                _cacheService.RemoveAsync($"product:{id}")
            );

            await Task.WhenAll(tasks).ConfigureAwait(false);

            Console.WriteLine($"✓ {productIds.Length} products invalidated");
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Batch invalidation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Time-based invalidation using scheduled cleanup worker.
    /// Removes stale entries that haven't been accessed.
    /// </summary>
    public async Task<OperationResult> InvalidateStaleEntriesAsync(TimeSpan minAge)
    {
        try
        {
            Console.WriteLine($"Invalidating entries older than {minAge.TotalMinutes} minutes");

            // This would typically be done by a background worker
            // Get all cache keys and check their age
            var keys = await _cacheService.GetKeysByPatternAsync("product:*").ConfigureAwait(false);
            var invalidatedCount = 0;

            foreach (var key in keys)
            {
                var ttl = await _cacheService.GetExpireSecondsAsync(key).ConfigureAwait(false);

                // If TTL indicates it's approaching expiration, remove it
                if (ttl >= 0 && ttl < 60)
                {
                    await _cacheService.RemoveAsync(key).ConfigureAwait(false);
                    invalidatedCount++;
                }
            }

            Console.WriteLine($"✓ Invalidated {invalidatedCount} stale entries");
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Cleanup failed: {ex.Message}");
        }
    }
}
