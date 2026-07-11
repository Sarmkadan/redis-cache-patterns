#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using RedisCachePatterns.Services;
using RedisCachePatterns.Infrastructure.Repositories;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Results;
using RedisCachePatterns.Utilities;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RedisCachePatterns.Examples;

/// <summary>
/// Extension methods for <see cref="CacheInvalidationExample"/> providing additional cache invalidation strategies
/// and utility methods for common scenarios.
/// </summary>
public static class CacheInvalidationExampleExtensions
{
    /// <summary>
    /// Bulk invalidation with progress tracking - invalidates multiple categories and their products in a single operation.
    /// </summary>
    /// <param name="example">The <see cref="CacheInvalidationExample"/> instance</param>
    /// <param name="categoryIds">Array of category IDs to invalidate</param>
    /// <exception cref="ArgumentNullException"><paramref name="example"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="categoryIds"/> is <see langword="null"/></exception>
    /// <returns>OperationResult indicating success or failure</returns>
    public static async Task<OperationResult> InvalidateCategoriesBulkAsync(
        this CacheInvalidationExample example,
        int[] categoryIds)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentNullException.ThrowIfNull(categoryIds);

        try
        {
            Console.WriteLine($"Bulk invalidating {categoryIds.Length} categories");

            var tasks = new List<Task<OperationResult>>();

            foreach (var categoryId in categoryIds)
            {
                tasks.Add(example.InvalidateCategoryProductsAsync(categoryId));
            }

            var results = await Task.WhenAll(tasks);

            var failedCount = results.Count(r => !r.Success);
            if (failedCount > 0)
            {
                return OperationResult.Failure($"{failedCount} category invalidations failed");
            }

            Console.WriteLine($"✓ Bulk invalidated {categoryIds.Length} categories successfully");
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Bulk invalidation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Product update with versioning - uses cache versioning to prevent stale reads during updates.
    /// </summary>
    /// <param name="example">The <see cref="CacheInvalidationExample"/> instance</param>
    /// <param name="product">Product to update</param>
    /// <param name="version">Cache version identifier</param>
    /// <exception cref="ArgumentNullException"><paramref name="example"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="product"/> is <see langword="null"/></exception>
    /// <returns>OperationResult indicating success or failure</returns>
    public static async Task<OperationResult> UpdateProductWithVersioningAsync(
        this CacheInvalidationExample example,
        Product product,
        string version)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentNullException.ThrowIfNull(product);
        ArgumentException.ThrowIfNullOrEmpty(version);

        try
        {
            Console.WriteLine($"Updating product {product.Id} with version {version}");

            // Update database
            await example.UpdateProductWithCascadingInvalidationAsync(product);

            // Set new versioned cache entry
            var versionedKey = $"product:{product.Id}:v{version}";
            await example._cacheService.SetAsync(versionedKey, product, TimeSpan.FromHours(1));

            // Invalidate old versioned entries
            var oldPattern = $"product:{product.Id}:v*";
            await example._cacheService.InvalidateAsync(oldPattern);

            Console.WriteLine($"✓ Product {product.Id} updated with version {version}");
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Versioned update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Selective category invalidation - only removes category-specific cache entries without affecting products.
    /// </summary>
    /// <param name="example">The <see cref="CacheInvalidationExample"/> instance</param>
    /// <param name="categoryId">Category ID to invalidate</param>
    /// <exception cref="ArgumentNullException"><paramref name="example"/> is <see langword="null"/></exception>
    /// <returns>OperationResult indicating success or failure</returns>
    public static async Task<OperationResult> InvalidateCategoryOnlyAsync(
        this CacheInvalidationExample example,
        int categoryId)
    {
        ArgumentNullException.ThrowIfNull(example);

        try
        {
            Console.WriteLine($"Invalidating category {categoryId} metadata only");

            // Invalidate category-specific keys only
            var categoryKeys = new[]
            {
                $"category:{categoryId}",
                $"category:{categoryId}:products",
                $"category:{categoryId}:details",
                $"category:{categoryId}:stats"
            };

            var tasks = new List<Task>();
            foreach (var key in categoryKeys)
            {
                tasks.Add(example._cacheService.RemoveAsync(key));
            }

            await Task.WhenAll(tasks);
            Console.WriteLine($"✓ Invalidated {categoryKeys.Length} category-specific cache entries");
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Category-only invalidation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Scheduled cache cleanup - performs regular maintenance on cache entries.
    /// </summary>
    /// <param name="example">The <see cref="CacheInvalidationExample"/> instance</param>
    /// <param name="olderThan">Minimum age for entries to be considered stale</param>
    /// <param name="batchSize">Number of entries to process per batch</param>
    /// <exception cref="ArgumentNullException"><paramref name="example"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="batchSize"/> is less than 1</exception>
    /// <returns>OperationResult indicating success or failure</returns>
    public static async Task<OperationResult> PerformScheduledCleanupAsync(
        this CacheInvalidationExample example,
        TimeSpan olderThan,
        int batchSize = 100)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(batchSize, 0);

        try
        {
            Console.WriteLine($"Performing scheduled cleanup (older than {olderThan.TotalMinutes} minutes, batch size: {batchSize})");

            var allKeys = await example._cacheService.GetKeysByPatternAsync("product:*");
            var staleKeys = new List<string>();

            foreach (var key in allKeys)
            {
                var ttl = await example._cacheService.GetExpireSecondsAsync(key);
                if (ttl >= 0 && ttl < (int)olderThan.TotalSeconds)
                {
                    staleKeys.Add(key);

                    if (staleKeys.Count >= batchSize)
                    {
                        break;
                    }
                }
            }

            if (staleKeys.Count == 0)
            {
                Console.WriteLine("✓ No stale entries found in this batch");
                return OperationResult.Success();
            }

            var tasks = staleKeys.Select(key => example._cacheService.RemoveAsync(key));
            await Task.WhenAll(tasks);

            Console.WriteLine($"✓ Cleaned up {staleKeys.Count} stale entries");
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Scheduled cleanup failed: {ex.Message}");
        }
    }
}