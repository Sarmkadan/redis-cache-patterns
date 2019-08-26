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
/// Demonstrates the Write-Through pattern where both cache and database
/// are updated atomically. Preferred for consistency-critical operations.
/// </summary>
public class WriteThroughExample
{
    private readonly ICacheService _cacheService;
    private readonly IProductRepository _productRepository;

    public WriteThroughExample(ICacheService cacheService, IProductRepository productRepository)
    {
        _cacheService = cacheService;
        _productRepository = productRepository;
    }

    /// <summary>
    /// Updates a product in database first, then updates cache.
    /// Ensures cache and database are always consistent.
    /// </summary>
    public async Task<OperationResult<Product>> UpdateProductWriteThroughAsync(Product product)
    {
        try
        {
            Console.WriteLine($"Updating product {product.Id} with write-through pattern");

            // Step 1: Validate input
            if (!ValidationHelper.IsValidProduct(product))
            {
                return OperationResult<Product>.Failure("Invalid product data");
            }

            // Step 2: Update in database
            Console.WriteLine("  → Writing to database...");
            var updated = await _productRepository.UpdateAsync(product);

            // Step 3: Update in cache
            Console.WriteLine("  → Writing to cache...");
            var cacheKey = CacheKeyBuilder.BuildProductKey(product.Id);
            var ttl = TimeSpan.FromHours(2);
            await _cacheService.SetAsync(cacheKey, updated, ttl);

            Console.WriteLine($"✓ Product {product.Id} updated in both database and cache");
            return OperationResult<Product>.Success(updated);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Update failed: {ex.Message}");
            return OperationResult<Product>.Failure($"Update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a new product with write-through. If database write fails,
    /// cache is not updated. If cache write fails, database update still stands.
    /// </summary>
    public async Task<OperationResult<Product>> CreateProductWriteThroughAsync(Product product)
    {
        try
        {
            Console.WriteLine($"Creating product: {product.Name}");

            // Step 1: Insert into database
            Console.WriteLine("  → Inserting into database...");
            var created = await _productRepository.CreateAsync(product);

            // Step 2: Add to cache
            Console.WriteLine("  → Adding to cache...");
            var cacheKey = CacheKeyBuilder.BuildProductKey(created.Id);
            var ttl = TimeSpan.FromHours(2);

            try
            {
                await _cacheService.SetAsync(cacheKey, created, ttl);
                Console.WriteLine($"✓ Product {created.Id} created and cached");
            }
            catch (Exception cacheEx)
            {
                Console.WriteLine($"⚠ Cache write failed (database is correct): {cacheEx.Message}");
            }

            return OperationResult<Product>.Success(created);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Creation failed: {ex.Message}");
            return OperationResult<Product>.Failure($"Creation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a product and its cache entry atomically.
    /// </summary>
    public async Task<OperationResult> DeleteProductWriteThroughAsync(int productId)
    {
        try
        {
            Console.WriteLine($"Deleting product {productId}");

            // Step 1: Delete from database
            Console.WriteLine("  → Removing from database...");
            var success = await _productRepository.DeleteAsync(productId);

            if (!success)
            {
                return OperationResult.Failure("Product not found");
            }

            // Step 2: Delete from cache
            Console.WriteLine("  → Removing from cache...");
            var cacheKey = CacheKeyBuilder.BuildProductKey(productId);
            await _cacheService.RemoveAsync(cacheKey);

            Console.WriteLine($"✓ Product {productId} deleted from both database and cache");
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Deletion failed: {ex.Message}");
            return OperationResult.Failure($"Deletion failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates product price with write-through and validation.
    /// Rolls back if either write fails.
    /// </summary>
    public async Task<OperationResult> UpdateProductPriceAsync(int productId, decimal newPrice)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
        {
            return OperationResult.Failure("Product not found");
        }

        // Validate new price
        if (newPrice <= 0)
        {
            return OperationResult.Failure("Price must be positive");
        }

        try
        {
            var oldPrice = product.Price;
            product.Price = newPrice;

            Console.WriteLine($"Updating price: ${oldPrice} → ${newPrice}");

            // Step 1: Update database
            Console.WriteLine("  → Persisting to database...");
            await _productRepository.UpdateAsync(product);

            // Step 2: Update cache
            Console.WriteLine("  → Updating cache...");
            var cacheKey = CacheKeyBuilder.BuildProductKey(productId);
            var ttl = TimeSpan.FromHours(2);
            await _cacheService.SetAsync(cacheKey, product, ttl);

            Console.WriteLine($"✓ Price updated in both systems");
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Price update failed: {ex.Message}");
            return OperationResult.Failure($"Update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Batch update with write-through. Updates multiple products atomically.
    /// </summary>
    public async Task<OperationResult> BulkUpdateProductsWriteThroughAsync(List<Product> products)
    {
        Console.WriteLine($"Bulk updating {products.Count} products");

        try
        {
            // Step 1: Update all in database
            Console.WriteLine("  → Bulk writing to database...");
            await _productRepository.BulkUpdateAsync(products);

            // Step 2: Update all in cache
            Console.WriteLine("  → Bulk writing to cache...");
            var ttl = TimeSpan.FromHours(2);
            var cacheTasks = products.Select(async p =>
            {
                var key = CacheKeyBuilder.BuildProductKey(p.Id);
                await _cacheService.SetAsync(key, p, ttl);
            });

            await Task.WhenAll(cacheTasks);

            Console.WriteLine($"✓ {products.Count} products updated in both systems");
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Bulk update failed: {ex.Message}");
            return OperationResult.Failure($"Bulk update failed: {ex.Message}");
        }
    }
}
