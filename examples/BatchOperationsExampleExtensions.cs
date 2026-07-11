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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RedisCachePatterns.Examples;

/// <summary>
/// Extension methods for BatchOperationsExample providing additional batch operation utilities
/// for cache management and performance optimization.
/// </summary>
public static class BatchOperationsExampleExtensions
{
    /// <summary>
    /// Gets multiple products with fallback to database for missing items, optimized for read-heavy scenarios.
    /// Returns only products that exist in cache or database (filters out missing IDs).
    /// </summary>
    /// <param name="example">The BatchOperationsExample instance</param>
    /// <param name="productIds">Array of product IDs to retrieve</param>
    /// <param name="skipCache">Skip cache check and load directly from database</param>
    /// <returns>List of found products (empty if none found)</returns>
    /// <exception cref="ArgumentNullException">Thrown if productIds is null</exception>
    public static async Task<List<Product>> GetExistingProductsBatchAsync(this BatchOperationsExample example, int[] productIds, bool skipCache = false)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentNullException.ThrowIfNull(productIds);

        if (productIds.Length == 0)
        {
            return new List<Product>();
        }

        Console.WriteLine($"\n📦 Getting Existing Products (optimized) - {productIds.Length} IDs");
        Console.WriteLine("═════════════════════════════════════════\n");

        var sw = Stopwatch.StartNew();
        var foundProducts = new List<Product>();
        var missingIds = new List<int>();

        if (!skipCache)
        {
            // Phase 1: Check cache for existing products
            Console.WriteLine($"Phase 1: Checking cache for {productIds.Length} products...");
            foreach (var id in productIds)
            {
                var key = CacheKeyBuilder.BuildProductKey(id);
                var cached = await example._cacheService.GetAsync<Product>(key);
                if (cached != null)
                {
                    foundProducts.Add(cached);
                    Console.WriteLine($" ✓ Cache HIT: product {id}");
                }
                else
                {
                    missingIds.Add(id);
                    Console.WriteLine($" ✗ Cache MISS: product {id}");
                }
            }

            var phase1Time = sw.ElapsedMilliseconds;
            Console.WriteLine($"Phase 1 Time: {phase1Time}ms (Found: {foundProducts.Count}, Missing: {missingIds.Count})\n");
        }
        else
        {
            missingIds.AddRange(productIds);
            Console.WriteLine("Cache check skipped - loading all from database\n");
        }

        // Phase 2: Load missing products from database
        if (missingIds.Count > 0)
        {
            Console.WriteLine($"Phase 2: Loading {missingIds.Count} products from database...");
            sw.Restart();

            var dbProducts = await example._productRepository.GetByIdsAsync(missingIds);
            foundProducts.AddRange(dbProducts);

            var phase2Time = sw.ElapsedMilliseconds;
            Console.WriteLine($" ✓ Loaded {dbProducts.Count} products from database");
            Console.WriteLine($"Phase 2 Time: {phase2Time}ms\n");
        }

        sw.Stop();
        Console.WriteLine($"✓ Total operation: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"✓ Found {foundProducts.Count} existing products\n");

        return foundProducts;
    }

    /// <summary>
    /// Conditional batch update - updates products only if they meet specific criteria.
    /// Useful for bulk price adjustments, status updates, or inventory management.
    /// </summary>
    /// <param name="example">The BatchOperationsExample instance</param>
    /// <param name="updatePredicate">Function to determine if product should be updated</param>
    /// <param name="updateAction">Action to apply to qualifying products</param>
    /// <param name="batchSize">Number of products to process at once</param>
    /// <returns>Operation result with count of updated products</returns>
    /// <exception cref="ArgumentNullException">Thrown if updatePredicate or updateAction is null</exception>
    public static async Task<OperationResult> ConditionalBatchUpdateAsync(
        this BatchOperationsExample example,
        Func<Product, bool> updatePredicate,
        Action<Product> updateAction,
        int batchSize = 100)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentNullException.ThrowIfNull(updatePredicate);
        ArgumentNullException.ThrowIfNull(updateAction);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(batchSize, 0);

        Console.WriteLine($"\n🎯 Conditional Batch Update - Batch Size: {batchSize}");
        Console.WriteLine("═════════════════════════════════════════\n");

        try
        {
            var sw = Stopwatch.StartNew();
            var totalUpdated = 0;
            var batchNumber = 0;

            // Get all products in batches
            var allProducts = new List<Product>();
            var batchIds = new List<int>();

            Console.WriteLine("Loading products for conditional update...");

            // Load products in batches to avoid memory issues
            var productIds = await example._productRepository.GetAllIdsAsync();
            for (int i = 0; i < productIds.Count; i += batchSize)
            {
                batchNumber++;
                var batch = productIds.Skip(i).Take(batchSize).ToList();
                batchIds.AddRange(batch);

                Console.WriteLine($"Processing batch {batchNumber}...");

                var products = await example._productRepository.GetByIdsAsync(batch);
                allProducts.AddRange(products);
            }

            Console.WriteLine($"Loaded {allProducts.Count} total products across {batchNumber} batches\n");

            // Apply conditional updates
            var productsToUpdate = allProducts.Where(updatePredicate).ToList();
            Console.WriteLine($"Found {productsToUpdate.Count} products matching criteria...");

            if (productsToUpdate.Count > 0)
            {
                // Update database
                Console.WriteLine($"Phase 1: Updating {productsToUpdate.Count} products in database...");
                sw.Restart();
                await example._productRepository.BulkUpdateAsync(productsToUpdate);
                var dbTime = sw.ElapsedMilliseconds;

                // Update cache
                Console.WriteLine($"Phase 2: Updating {productsToUpdate.Count} products in cache...");
                sw.Restart();
                var ttl = TimeSpan.FromHours(2);
                var cacheTasks = productsToUpdate.Select(async product =>
                {
                    var key = CacheKeyBuilder.BuildProductKey(product.Id);
                    await example._cacheService.SetAsync(key, product, ttl);
                });

                await Task.WhenAll(cacheTasks);
                var cacheTime = sw.ElapsedMilliseconds;

                totalUpdated = productsToUpdate.Count;
                Console.WriteLine($"\n✓ Updated {totalUpdated} products");
                Console.WriteLine($" Database: {dbTime}ms | Cache: {cacheTime}ms");
            }
            else
            {
                Console.WriteLine("No products matched the update criteria");
            }

            var totalTime = sw.ElapsedMilliseconds;
            Console.WriteLine($"\n✓ Conditional batch update completed in {totalTime}ms");
            Console.WriteLine($" Total products updated: {totalUpdated}\n");

            return OperationResult.Success(totalUpdated);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Conditional batch update failed: {ex.Message}");
            return OperationResult.Failure($"Conditional batch update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Batch cache invalidation with pattern matching for related products.
    /// Invalidates all cache entries matching a specific pattern (e.g., all products in a category).
    /// </summary>
    /// <param name="example">The BatchOperationsExample instance</param>
    /// <param name="pattern">Cache key pattern to match (e.g., "product:*" or "category:electronics:*")</param>
    /// <returns>Operation result with count of invalidated entries</returns>
    /// <exception cref="ArgumentNullException">Thrown if pattern is null</exception>
    /// <exception cref="ArgumentException">Thrown if pattern is empty or whitespace</exception>
    public static async Task<OperationResult> InvalidateCachePatternAsync(this BatchOperationsExample example, string pattern)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern, nameof(pattern));

        Console.WriteLine($"\n🗑️ Batch Invalidating Cache Pattern: {pattern}");
        Console.WriteLine("═════════════════════════════════════════\n");

        try
        {
            var sw = Stopwatch.StartNew();
            var invalidatedCount = 0;

            // For Redis, we can use KEYS command (in production, use SCAN for large datasets)
            Console.WriteLine("Scanning for matching cache keys...");
            var keys = await example._cacheService.GetKeysByPatternAsync(pattern);

            Console.WriteLine($"Found {keys.Count} keys matching pattern '{pattern}'");

            if (keys.Count > 0)
            {
                Console.WriteLine($"Invalidating {keys.Count} cache entries...");
                var tasks = keys.Select(key => example._cacheService.RemoveAsync(key));
                await Task.WhenAll(tasks);
                invalidatedCount = keys.Count;
                Console.WriteLine($"✓ Invalidated {invalidatedCount} cache entries");
            }
            else
            {
                Console.WriteLine("No matching cache entries found");
            }

            sw.Stop();
            Console.WriteLine($"\n✓ Pattern invalidation completed in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($" Total entries invalidated: {invalidatedCount}\n");

            return OperationResult.Success(invalidatedCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Pattern invalidation failed: {ex.Message}");
            return OperationResult.Failure($"Pattern invalidation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Batch cache warming with product filtering - preloads only products matching specific criteria.
    /// Useful for warming cache with bestsellers, featured items, or seasonal products.
    /// </summary>
    /// <param name="example">The BatchOperationsExample instance</param>
    /// <param name="filterFunc">Function to filter which products to warm</param>
    /// <param name="topCount">Maximum number of products to warm</param>
    /// <param name="ttlHours">TTL in hours for warmed cache entries</param>
    /// <returns>Operation result with count of warmed products</returns>
    /// <exception cref="ArgumentNullException">Thrown if filterFunc is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if topCount or ttlHours is invalid</exception>
    public static async Task<OperationResult> WarmFilteredCacheAsync(
        this BatchOperationsExample example,
        Func<Product, bool> filterFunc,
        int topCount = 50,
        int ttlHours = 4)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentNullException.ThrowIfNull(filterFunc);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(topCount, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ttlHours, 0);

        Console.WriteLine($"\n🔥 Warm Filtered Cache - Top {topCount} products (TTL: {ttlHours}h)");
        Console.WriteLine("═════════════════════════════════════════\n");

        try
        {
            var sw = Stopwatch.StartNew();

            // Load products from database
            Console.WriteLine($"Phase 1: Loading top {topCount} products from database...");
            var allProducts = await example._productRepository.GetTopProductsAsync(topCount);
            var filteredProducts = allProducts.Where(filterFunc).ToList();

            Console.WriteLine($" ✓ Loaded {allProducts.Count} total products, {filteredProducts.Count} match filter");

            // Cache filtered products
            Console.WriteLine($"Phase 2: Warming {filteredProducts.Count} filtered products...");
            var ttl = TimeSpan.FromHours(ttlHours);
            var tasks = filteredProducts.Select(async product =>
            {
                var key = CacheKeyBuilder.BuildProductKey(product.Id);
                await example._cacheService.SetAsync(key, product, ttl);
            });

            await Task.WhenAll(tasks);
            sw.Stop();

            Console.WriteLine($"\n✓ Filtered cache warmed with {filteredProducts.Count} products in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($" TTL: {ttlHours} hours\n");

            return OperationResult.Success(filteredProducts.Count);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Filtered cache warming failed: {ex.Message}");
            return OperationResult.Failure($"Filtered cache warming failed: {ex.Message}");
        }
    }
}