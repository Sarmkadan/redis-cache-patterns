#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Services;
using RedisCachePatterns.Infrastructure.Repositories;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Utilities;
using System;
using System.Threading.Tasks;

namespace RedisCachePatterns.Examples;

/// <summary>
/// Demonstrates the Cache-Aside pattern with fallback to database on cache miss.
/// This is the most common caching pattern for read-heavy workloads.
/// </summary>
public class CacheAsideExample
{
    private readonly ICacheService _cacheService;
    private readonly IProductRepository _productRepository;

    public CacheAsideExample(ICacheService cacheService, IProductRepository productRepository)
    {
        _cacheService = cacheService;
        _productRepository = productRepository;
    }

    /// <summary>
    /// Gets product by ID with cache-aside pattern. Checks cache first,
    /// loads from database on miss, then updates cache for future requests.
    /// </summary>
    public async Task<Product?> GetProductWithCacheAsideAsync(int productId)
    {
        var cacheKey = CacheKeyBuilder.BuildProductKey(productId);

        try
        {
            // Step 1: Check cache
            var cached = await _cacheService.GetAsync<Product>(cacheKey).ConfigureAwait(false);
            if (cached != null)
            {
                Console.WriteLine($"✓ Cache HIT for product {productId}");
                return cached;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Cache error: {ex.Message} - continuing to database");
        }

        // Step 2: Cache miss - load from database
        Console.WriteLine($"↓ Cache MISS for product {productId} - loading from database");
        var product = await _productRepository.GetByIdAsync(productId).ConfigureAwait(false);

        // Step 3: Update cache for future requests
        if (product != null)
        {
            try
            {
                var ttl = TimeSpan.FromHours(2);
                await _cacheService.SetAsync(cacheKey, product, ttl).ConfigureAwait(false);
                Console.WriteLine($"✓ Cached product {productId} for 2 hours");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Failed to cache product: {ex.Message}");
            }
        }

        return product;
    }

    /// <summary>
    /// Simulates multiple requests to demonstrate cache hits.
    /// </summary>
    public async Task DemonstrateCacheHitsAsync(int productId, int requestCount)
    {
        Console.WriteLine($"\n=== Cache-Aside Pattern Demo ({requestCount} requests) ===\n");

        for (int i = 1; i <= requestCount; i++)
        {
            Console.WriteLine($"Request #{i}:");
            var product = await GetProductWithCacheAsideAsync(productId).ConfigureAwait(false);
            Console.WriteLine($"  Result: {product?.Name ?? "Not found"}\n");

            if (i < requestCount)
                await Task.Delay(100).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets multiple products efficiently with batch cache operations.
    /// </summary>
    public async Task<List<Product>> GetProductsByCacheAsideAsync(int[] productIds)
    {
        var products = new List<Product>();
        var missedIds = new List<int>();

        // Step 1: Try to get all from cache
        foreach (var id in productIds)
        {
            var key = CacheKeyBuilder.BuildProductKey(id);
            var cached = await _cacheService.GetAsync<Product>(key).ConfigureAwait(false);
            if (cached != null)
            {
                products.Add(cached);
            }
            else
            {
                missedIds.Add(id);
            }
        }

        // Step 2: Load missed products from database
        if (missedIds.Count > 0)
        {
            var fromDb = await _productRepository.GetByIdsAsync(missedIds).ConfigureAwait(false);
            products.AddRange(fromDb);

            // Step 3: Cache the newly loaded products
            var ttl = TimeSpan.FromHours(2);
            foreach (var product in fromDb)
            {
                var key = CacheKeyBuilder.BuildProductKey(product.Id);
                await _cacheService.SetAsync(key, product, ttl).ConfigureAwait(false);
            }
        }

        return products;
    }

    /// <summary>
    /// Implements cache-aside with time-based refresh.
    /// Allows staleness for performance but refreshes periodically.
    /// </summary>
    public async Task<Product?> GetProductWithRefreshAsync(int productId, TimeSpan cacheLifetime)
    {
        var cacheKey = CacheKeyBuilder.BuildProductKey(productId);

        var cached = await _cacheService.GetAsync<Product>(cacheKey).ConfigureAwait(false);
        if (cached != null)
        {
            return cached;
        }

        var product = await _productRepository.GetByIdAsync(productId).ConfigureAwait(false);
        if (product != null)
        {
            await _cacheService.SetAsync(cacheKey, product, cacheLifetime).ConfigureAwait(false);
        }

        return product;
    }
}
