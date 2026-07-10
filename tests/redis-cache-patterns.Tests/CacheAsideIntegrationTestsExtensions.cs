#nullable enable

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;
using Xunit;

namespace RedisCachePatterns.Tests;

/// <summary>
/// Extension methods for CacheAsideIntegrationTests providing additional test scenarios and utilities.
/// </summary>
public static class CacheAsideIntegrationTestsExtensions
{
    /// <summary>
    /// Extension method that verifies cache warming and validation scenarios.
    /// </summary>
    public static async Task CacheWarmup_ValidatesCacheState(this CacheAsideIntegrationTests _, MockCacheService cacheService, Product product)
    {
        // Warm up the cache
        await cacheService.SetAsync("product:warmup", product);

        // Verify the product exists in cache
        var cachedProduct = await cacheService.GetAsync<Product>("product:warmup");
        cachedProduct.Should().NotBeNull();
        cachedProduct?.Id.Should().Be(product.Id);

        // Verify cache statistics
        var stats = await cacheService.GetStatisticsAsync();
        stats.TotalKeys.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Extension method that tests cache expiration and automatic reload scenarios.
    /// </summary>
    public static async Task CacheExpiration_TriggersReloadOnExpiry(this CacheAsideIntegrationTests _, MockCacheService cacheService)
    {
        var loadCount = 0;
        var product = new Product
        {
            Id = 999,
            Name = "Expiring Product",
            Sku = "SKU-EXP",
            Price = 199.99m,
            StockQuantity = 75,
            ReorderLevel = 15,
            Category = "Test",
            IsActive = true
        };

        // Load with very short expiration
        await cacheService.GetOrLoadAsync(
            "product:expiring",
            async () =>
            {
                loadCount++;
                await Task.Delay(10);
                return product;
            },
            TimeSpan.FromMilliseconds(50));

        loadCount.Should().Be(1);

        // Wait for expiration
        await Task.Delay(100);

        // Load again - should reload since cache expired
        await cacheService.GetOrLoadAsync(
            "product:expiring",
            async () =>
            {
                loadCount++;
                await Task.Delay(10);
                return product;
            },
            TimeSpan.FromMilliseconds(50));

        loadCount.Should().Be(2);
    }

    /// <summary>
    /// Extension method that tests bulk operations and batch caching.
    /// </summary>
    public static async Task BulkOperations_CachesMultipleItems(this CacheAsideIntegrationTests _, MockCacheService cacheService)
    {
        var products = new List<Product>();
        for (int i = 0; i < 10; i++)
        {
            products.Add(new Product
            {
                Id = i,
                Name = $"Bulk Product {i}",
                Sku = $"SKU-BULK-{i:D3}",
                Price = 9.99m * (i + 1),
                StockQuantity = 100 * (i + 1),
                ReorderLevel = 10 * (i + 1),
                Category = "Bulk",
                IsActive = true
            });
        }

        // Cache all products
        foreach (var product in products)
        {
            await cacheService.SetAsync($"product:bulk:{product.Id}", product);
        }

        // Verify all products are cached
        foreach (var product in products)
        {
            var cached = await cacheService.GetAsync<Product>($"product:bulk:{product.Id}");
            cached.Should().NotBeNull();
            cached?.Id.Should().Be(product.Id);
        }

        // Verify all keys exist
        var allKeys = await cacheService.GetKeysByPatternAsync("product:bulk:*");
        allKeys.Should().HaveCount(10);
    }

    /// <summary>
    /// Extension method that tests cache invalidation patterns.
    /// </summary>
    public static async Task CacheInvalidation_RemovesRelatedKeys(this CacheAsideIntegrationTests _, MockCacheService cacheService)
    {
        var product = new Product
        {
            Id = 888,
            Name = "Invalidation Test Product",
            Sku = "SKU-INV",
            Price = 299.99m,
            StockQuantity = 200,
            ReorderLevel = 40,
            Category = "Test",
            IsActive = true
        };

        // Cache the product
        await cacheService.SetAsync("product:invalidation", product);

        // Cache related keys
        await cacheService.SetAsync("product:invalidation:details", $"Details for {product.Name}");
        await cacheService.SetAsync("product:invalidation:metadata", $"Metadata for {product.Id}");

        // Verify all keys exist
        (await cacheService.ExistsAsync("product:invalidation")).Should().BeTrue();
        (await cacheService.ExistsAsync("product:invalidation:details")).Should().BeTrue();
        (await cacheService.ExistsAsync("product:invalidation:metadata")).Should().BeTrue();

        // Invalidate by pattern
        await cacheService.RemoveByPatternAsync("product:invalidation:*");

        // Verify all related keys are removed
        (await cacheService.ExistsAsync("product:invalidation")).Should().BeFalse();
        (await cacheService.ExistsAsync("product:invalidation:details")).Should().BeFalse();
        (await cacheService.ExistsAsync("product:invalidation:metadata")).Should().BeFalse();
    }
}
