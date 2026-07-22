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
/// Demonstrates the GetManyAsync batch retrieval method for efficiently
/// fetching multiple cached values in a single Redis operation.
/// </summary>
public class BatchGetOperationsExample
{
    private readonly ICacheService _cacheService;
    private readonly IProductRepository _productRepository;

    public BatchGetOperationsExample(ICacheService cacheService, IProductRepository productRepository)
    {
        _cacheService = cacheService;
        _productRepository = productRepository;
    }

    /// <summary>
    /// Gets multiple products efficiently using GetManyAsync - single Redis operation.
    /// This demonstrates the new batch retrieval method that reduces network round-trips.
    /// </summary>
    public async Task<List<Product>> GetProductsBatchAsync(int[] productIds)
    {
        Console.WriteLine($"\n📦 Batch Getting {productIds.Length} Products (Using GetManyAsync)");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        var sw = Stopwatch.StartNew();
        var productKeys = productIds.Select(id => CacheKeyBuilder.BuildProductKey(id)).ToArray();

        // Phase 1: Try to get all from cache in a SINGLE operation
        Console.WriteLine($"Phase 1: Checking cache for {productIds.Length} products (single Redis operation)...");
        var cachedResults = await _cacheService.GetManyAsync<Product>(productKeys);

        var cachedProducts = new List<Product>();
        var missingKeys = new List<string>();

        foreach (var kvp in cachedResults)
        {
            if (kvp.Value != null)
            {
                cachedProducts.Add(kvp.Value);
                Console.WriteLine($" ✓ Cache HIT: product {kvp.Key.Split(':').Last()}");
            }
            else
            {
                missingKeys.Add(kvp.Key);
                Console.WriteLine($" ✗ Cache MISS: product {kvp.Key.Split(':').Last()}");
            }
        }

        var phase1Time = sw.ElapsedMilliseconds;
        Console.WriteLine($"Phase 1 Time: {phase1Time}ms (Cache hits: {cachedProducts.Count}, Misses: {missingKeys.Count})\n");

        // Phase 2: Load missing products from database
        var dbProducts = new List<Product>();
        if (missingKeys.Count > 0)
        {
            Console.WriteLine($"Phase 2: Loading {missingKeys.Count} products from database...");
            sw.Restart();

            // Extract IDs from missing keys
            var missingIds = missingKeys.Select(key => int.Parse(key.Split(':').Last())).ToArray();
            dbProducts = await _productRepository.GetByIdsAsync(missingIds);
            Console.WriteLine($" ✓ Loaded {dbProducts.Count} products from database");

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
            Console.WriteLine($" ✓ Cached {dbProducts.Count} products");

            var phase3Time = sw.ElapsedMilliseconds;
            Console.WriteLine($"Phase 3 Time: {phase3Time}ms\n");
        }

        var allProducts = cachedProducts.Concat(dbProducts).ToList();
        sw.Stop();

        Console.WriteLine($"✓ Total batch operation: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"✓ Retrieved {allProducts.Count} total products");
        Console.WriteLine($" - From cache: {cachedProducts.Count} ({cachedProducts.Count * 100.0 / productIds.Length:F1}%)");
        Console.WriteLine($" - From database: {dbProducts.Count} ({dbProducts.Count * 100.0 / productIds.Length:F1}%)");
        Console.WriteLine($"\n💡 Performance Note: GetManyAsync uses Redis pipelining for a single network round-trip!");
        Console.WriteLine("   This is much more efficient than individual GET operations.\n");

        return allProducts;
    }

    /// <summary>
    /// Demonstrates pure cache-aside with GetManyAsync - loads all missing items in parallel.
    /// </summary>
    public async Task<Dictionary<string, Product?>> GetProductsWithLoadAsync(int[] productIds, Func<int, Task<Product?>> loadFn)
    {
        Console.WriteLine($"\n🔄 Cache-Aside with GetManyAsync - Loading Missing Items");
        Console.WriteLine("═════════════════════════════════════════════════════════════\n");

        var productKeys = productIds.Select(id => CacheKeyBuilder.BuildProductKey(id)).ToArray();

        // Try to get all from cache first
        Console.WriteLine($"Step 1: Checking cache for {productIds.Length} products...");
        var cachedResults = await _cacheService.GetManyAsync<Product>(productKeys);

        var missingKeys = new List<string>();
        var resultDict = new Dictionary<string, Product?>();

        foreach (var kvp in cachedResults)
        {
            if (kvp.Value != null)
            {
                resultDict[kvp.Key] = kvp.Value;
                Console.WriteLine($" ✓ Cache HIT: {kvp.Key}");
            }
            else
            {
                missingKeys.Add(kvp.Key);
                Console.WriteLine($" ✗ Cache MISS: {kvp.Key}");
            }
        }

        // Load missing items in parallel
        if (missingKeys.Count > 0)
        {
            Console.WriteLine($"\nStep 2: Loading {missingKeys.Count} missing products...");

            var loadTasks = missingKeys.Select(async key =>
            {
                var id = int.Parse(key.Split(':').Last());
                var product = await loadFn(id);
                if (product != null)
                {
                    await _cacheService.SetAsync(key, product, TimeSpan.FromHours(2));
                    resultDict[key] = product;
                }
                return (key, product);
            });

            await Task.WhenAll(loadTasks);
            Console.WriteLine($" ✓ Loaded and cached {missingKeys.Count} products\n");
        }

        Console.WriteLine($"✓ Final result: {resultDict.Count(kvp => kvp.Value != null)} products retrieved\n");
        return resultDict;
    }

    /// <summary>
    /// Performance comparison: GetManyAsync vs individual GET operations.
    /// </summary>
    public async Task<OperationResult> CompareBatchVsIndividualAsync(int productCount)
    {
        Console.WriteLine($"\n⚡ Performance Comparison: GetManyAsync vs Individual GETs ({productCount} products)");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════\n");

        var productIds = Enumerable.Range(1, productCount).ToArray();
        var productKeys = productIds.Select(id => CacheKeyBuilder.BuildProductKey(id)).ToArray();

        // First, cache some products
        Console.WriteLine("Pre-caching products...");
        var products = await _productRepository.GetByIdsAsync(productIds);
        var ttl = TimeSpan.FromHours(1);

        var cacheTasks = products.Select(async product =>
        {
            var key = CacheKeyBuilder.BuildProductKey(product.Id);
            await _cacheService.SetAsync(key, product, ttl);
        });
        await Task.WhenAll(cacheTasks);
        Console.WriteLine($"✓ Pre-cached {products.Count} products\n");

        // Method 1: GetManyAsync (single Redis operation)
        Console.WriteLine("Method 1: GetManyAsync (single Redis operation)");
        var sw = Stopwatch.StartNew();
        var batchResults = await _cacheService.GetManyAsync<Product>(productKeys);
        sw.Stop();
        var batchTime = sw.ElapsedMilliseconds;
        Console.WriteLine($" Time: {batchTime}ms");
        Console.WriteLine($" Network round-trips: 1 (pipelined)");
        Console.WriteLine($" Results: {batchResults.Count(kvp => kvp.Value != null)} products retrieved\n");

        // Method 2: Individual GET operations (sequential)
        Console.WriteLine("Method 2: Individual GET operations (sequential)");
        sw.Restart();
        var individualResults = new Dictionary<string, Product?>();
        foreach (var key in productKeys)
        {
            var product = await _cacheService.GetAsync<Product>(key);
            individualResults[key] = product;
        }
        sw.Stop();
        var individualTime = sw.ElapsedMilliseconds;
        Console.WriteLine($" Time: {individualTime}ms");
        Console.WriteLine($" Network round-trips: {productKeys.Length}");
        Console.WriteLine($" Results: {individualResults.Count(kvp => kvp.Value != null)} products retrieved\n");

        // Method 3: Individual GET operations (parallel)
        Console.WriteLine("Method 3: Individual GET operations (parallel)");
        sw.Restart();
        var parallelTasks = productKeys.Select(async key =>
        {
            var product = await _cacheService.GetAsync<Product>(key);
            return (key, product);
        });
        var parallelResults = (await Task.WhenAll(parallelTasks)).ToDictionary(x => x.key, x => x.product);
        sw.Stop();
        var parallelTime = sw.ElapsedMilliseconds;
        Console.WriteLine($" Time: {parallelTime}ms");
        Console.WriteLine($" Network round-trips: {productKeys.Length}");
        Console.WriteLine($" Results: {parallelResults.Count(kvp => kvp.Value != null)} products retrieved\n");

        // Summary
        Console.WriteLine("📊 Summary:");
        Console.WriteLine("═════════");
        Console.WriteLine($"GetManyAsync:      {batchTime}ms | 1 network call");
        Console.WriteLine($"Individual Seq:     {individualTime}ms | {productKeys.Length} network calls");
        Console.WriteLine($"Individual Par:     {parallelTime}ms | {productKeys.Length} network calls");

        var vsSeq = (individualTime - batchTime) * 100.0 / individualTime;
        var vsPar = (parallelTime - batchTime) * 100.0 / parallelTime;

        Console.WriteLine($"\n💡 GetManyAsync is {vsSeq:F1}% faster than sequential individual GETs");
        Console.WriteLine($"💡 GetManyAsync is {vsPar:F1}% faster than parallel individual GETs");
        Console.WriteLine("\n📝 Note: The performance difference grows with more items and network latency!");

        return OperationResult.Success();
    }
}