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
using System.Diagnostics;
using System.Threading.Tasks;

namespace RedisCachePatterns.Examples;

/// <summary>
/// Demonstrates efficient batch operations for caching multiple items,
/// including bulk get, set, and invalidation patterns.
/// </summary>
public class BatchOperationsExample
{
    private readonly ICacheService _cacheService;
    private readonly IProductRepository _productRepository;

    public BatchOperationsExample(ICacheService cacheService, IProductRepository productRepository)
    {
        _cacheService = cacheService;
        _productRepository = productRepository;
    }

    /// <summary>
    /// Gets multiple products efficiently - hits cache first, loads misses from DB.
    /// </summary>
    public async Task<List<Product>> GetProductsBatchAsync(int[] productIds)
    {
        Console.WriteLine($"\n📦 Batch Getting {productIds.Length} Products");
        Console.WriteLine("═════════════════════════════════════════\n");

        var sw = Stopwatch.StartNew();
        var cachedProducts = new List<Product>();
        var missingIds = new List<int>();

        // Phase 1: Try to get all from cache
        Console.WriteLine($"Phase 1: Checking cache for {productIds.Length} products...");
        foreach (var id in productIds)
        {
            var key = CacheKeyBuilder.BuildProductKey(id);
            var cached = await _cacheService.GetAsync<Product>(key);
            if (cached != null)
            {
                cachedProducts.Add(cached);
                Console.WriteLine($"  ✓ Cache HIT: product {id}");
            }
            else
            {
                missingIds.Add(id);
                Console.WriteLine($"  ✗ Cache MISS: product {id}");
            }
        }

        var phase1Time = sw.ElapsedMilliseconds;
        Console.WriteLine($"Phase 1 Time: {phase1Time}ms (Cache hits: {cachedProducts.Count}, Misses: {missingIds.Count})\n");

        // Phase 2: Load missing products from database
        var dbProducts = new List<Product>();
        if (missingIds.Count > 0)
        {
            Console.WriteLine($"Phase 2: Loading {missingIds.Count} products from database...");
            sw.Restart();

            dbProducts = await _productRepository.GetByIdsAsync(missingIds);
            Console.WriteLine($"  ✓ Loaded {dbProducts.Count} products from database");

            var phase2Time = sw.ElapsedMilliseconds;
            Console.WriteLine($"Phase 2 Time: {phase2Time}ms\n");

            // Phase 3: Cache the newly loaded products
            Console.WriteLine($"Phase 3: Caching {dbProducts.Count} products...");
            sw.Restart();

            var ttl = TimeSpan.FromHours(2);
            var cacheTasks = dbProducts.Select(async product =>
            {
                var key = CacheKeyBuilder.BuildProductKey(product.Id);
                await _cacheService.SetAsync(key, product, ttl);
            });

            await Task.WhenAll(cacheTasks);
            Console.WriteLine($"  ✓ Cached {dbProducts.Count} products");

            var phase3Time = sw.ElapsedMilliseconds;
            Console.WriteLine($"Phase 3 Time: {phase3Time}ms\n");
        }

        var allProducts = cachedProducts.Concat(dbProducts).ToList();
        sw.Stop();

        Console.WriteLine($"✓ Total batch operation: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"✓ Retrieved {allProducts.Count} total products");
        Console.WriteLine($"  - From cache: {cachedProducts.Count} ({cachedProducts.Count * 100.0 / productIds.Length:F1}%)");
        Console.WriteLine($"  - From database: {dbProducts.Count} ({dbProducts.Count * 100.0 / productIds.Length:F1}%)\n");

        return allProducts;
    }

    /// <summary>
    /// Sets multiple products in cache efficiently.
    /// </summary>
    public async Task<OperationResult> SetProductsBatchAsync(List<Product> products, TimeSpan ttl)
    {
        Console.WriteLine($"\n🔌 Batch Setting {products.Count} Products");
        Console.WriteLine("═════════════════════════════════════════\n");

        try
        {
            var sw = Stopwatch.StartNew();

            var tasks = products.Select(async product =>
            {
                var key = CacheKeyBuilder.BuildProductKey(product.Id);
                await _cacheService.SetAsync(key, product, ttl);
                Console.WriteLine($"  ✓ Set product {product.Id} (TTL: {ttl.TotalMinutes} min)");
            });

            await Task.WhenAll(tasks);
            sw.Stop();

            Console.WriteLine($"\n✓ {products.Count} products cached in {sw.ElapsedMilliseconds}ms\n");
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Batch set failed: {ex.Message}");
            return OperationResult.Failure($"Batch set failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Invalidates multiple products efficiently by pattern.
    /// </summary>
    public async Task<OperationResult> InvalidateProductsBatchAsync(int[] productIds)
    {
        Console.WriteLine($"\n🗑️  Batch Invalidating {productIds.Length} Products");
        Console.WriteLine("═════════════════════════════════════════\n");

        try
        {
            var sw = Stopwatch.StartNew();

            var tasks = productIds.Select(async id =>
            {
                var key = $"product:{id}";
                await _cacheService.RemoveAsync(key);
                Console.WriteLine($"  ✓ Invalidated product {id}");
            });

            await Task.WhenAll(tasks);
            sw.Stop();

            Console.WriteLine($"\n✓ {productIds.Length} products invalidated in {sw.ElapsedMilliseconds}ms\n");
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Batch invalidation failed: {ex.Message}");
            return OperationResult.Failure($"Batch invalidation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Cache warming - preloads frequently accessed data into cache.
    /// </summary>
    public async Task<OperationResult> WarmCacheAsync(int topProductCount)
    {
        Console.WriteLine($"\n🔥 Cache Warming - Loading top {topProductCount} products");
        Console.WriteLine("═════════════════════════════════════════\n");

        try
        {
            var sw = Stopwatch.StartNew();

            // Load top products
            Console.WriteLine($"Phase 1: Loading top {topProductCount} products from database...");
            var topProducts = await _productRepository.GetTopProductsAsync(topProductCount);
            sw.Restart();

            // Cache them
            Console.WriteLine($"Phase 2: Caching {topProducts.Count} products...");
            var ttl = TimeSpan.FromHours(4);

            var tasks = topProducts.Select(async product =>
            {
                var key = CacheKeyBuilder.BuildProductKey(product.Id);
                await _cacheService.SetAsync(key, product, ttl);
            });

            await Task.WhenAll(tasks);
            sw.Stop();

            Console.WriteLine($"\n✓ Cache warmed with {topProducts.Count} products in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  TTL: 4 hours\n");

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Cache warming failed: {ex.Message}");
            return OperationResult.Failure($"Cache warming failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Efficiently updates multiple products and their cache entries.
    /// </summary>
    public async Task<OperationResult> UpdateProductsBatchAsync(List<Product> products)
    {
        Console.WriteLine($"\n✏️  Batch Updating {products.Count} Products");
        Console.WriteLine("═════════════════════════════════════════\n");

        try
        {
            var sw = Stopwatch.StartNew();

            // Update database
            Console.WriteLine($"Phase 1: Updating {products.Count} products in database...");
            await _productRepository.BulkUpdateAsync(products);
            var dbTime = sw.ElapsedMilliseconds;

            // Update cache
            Console.WriteLine($"Phase 2: Updating {products.Count} products in cache...");
            sw.Restart();

            var ttl = TimeSpan.FromHours(2);
            var tasks = products.Select(async product =>
            {
                var key = CacheKeyBuilder.BuildProductKey(product.Id);
                await _cacheService.SetAsync(key, product, ttl);
            });

            await Task.WhenAll(tasks);
            var cacheTime = sw.ElapsedMilliseconds;

            Console.WriteLine($"\n✓ {products.Count} products updated");
            Console.WriteLine($"  Database: {dbTime}ms | Cache: {cacheTime}ms | Total: {dbTime + cacheTime}ms\n");

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Batch update failed: {ex.Message}");
            return OperationResult.Failure($"Batch update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Demonstrates performance difference between sequential and parallel operations.
    /// </summary>
    public async Task<OperationResult> CompareSequentialVsParallelAsync(int productCount)
    {
        Console.WriteLine($"\n⚡ Performance Comparison: Sequential vs Parallel ({productCount} products)");
        Console.WriteLine("═════════════════════════════════════════════════════════════\n");

        var productIds = Enumerable.Range(1, productCount).ToArray();

        // Sequential operations
        Console.WriteLine("Sequential Operations:");
        var sw = Stopwatch.StartNew();

        foreach (var id in productIds)
        {
            var key = CacheKeyBuilder.BuildProductKey(id);
            await _cacheService.RemoveAsync(key);
        }

        sw.Stop();
        var sequentialTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"  Time: {sequentialTime}ms\n");

        // Parallel operations
        Console.WriteLine("Parallel Operations:");
        sw.Restart();

        var parallelTasks = productIds.Select(id =>
        {
            var key = CacheKeyBuilder.BuildProductKey(id);
            return _cacheService.RemoveAsync(key);
        });

        await Task.WhenAll(parallelTasks);
        sw.Stop();
        var parallelTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"  Time: {parallelTime}ms\n");

        var improvement = (sequentialTime - parallelTime) * 100.0 / sequentialTime;
        Console.WriteLine($"✓ Parallel is {improvement:F1}% faster ({sequentialTime - parallelTime}ms saved)\n");

        return OperationResult.Success();
    }
}
