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
using System.Diagnostics.CodeAnalysis;

namespace RedisCachePatterns.Examples;

/// <summary>
/// Extension methods for <see cref="WriteThroughExample"/> providing additional utility operations
/// and convenience methods for write-through pattern operations.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WriteThroughExampleExtensions
{
    /// <summary>
    /// Retrieves a product from cache if available, otherwise fetches from database
    /// and updates cache with write-through pattern.
    /// </summary>
    /// <param name="example">The <see cref="WriteThroughExample"/> instance.</param>
    /// <param name="productId">The product identifier.</param>
    /// <param name="createDefaultProduct">Function to create a default product when not found in cache or database.</param>
    /// <returns>Operation result containing the product or failure details.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="example"/> or <paramref name="createDefaultProduct"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="productId"/> is less than or equal to zero.</exception>
    public static async Task<OperationResult<Product>> GetOrCreateProductWriteThroughAsync(
        this WriteThroughExample example,
        int productId,
        [DisallowNull] Func<int, Task<Product>> createDefaultProduct)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(productId, 0);
        ArgumentNullException.ThrowIfNull(createDefaultProduct);

        try
        {
            Console.WriteLine($"Getting or creating product {productId} with write-through pattern");

            // Step 1: Try to get from cache first
            var cacheKey = CacheKeyBuilder.BuildProductKey(productId);
            var cachedProduct = await example._cacheService.GetAsync<Product>(cacheKey);

            if (cachedProduct is not null)
            {
                Console.WriteLine(" → Product found in cache");
                return OperationResult<Product>.Success(cachedProduct);
            }

            Console.WriteLine(" → Product not found in cache, fetching from database...");

            // Step 2: Get from database
            var product = await example._productRepository.GetByIdAsync(productId);

            if (product is not null)
            {
                // Cache the existing product
                var ttl = TimeSpan.FromHours(2);
                await example._cacheService.SetAsync(cacheKey, product, ttl);
                Console.WriteLine(" → Product cached from database");
                return OperationResult<Product>.Success(product);
            }

            // Step 3: Product doesn't exist, create default
            Console.WriteLine(" → Product not found, creating default...");
            product = await createDefaultProduct(productId);

            // Create with write-through
            return await example.CreateProductWriteThroughAsync(product);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Get or create failed: {ex.Message}");
            return OperationResult<Product>.Failure($"Get or create failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates multiple products with conditional write-through.
    /// Only updates products that meet validation criteria.
    /// </summary>
    /// <param name="example">The <see cref="WriteThroughExample"/> instance.</param>
    /// <param name="products">The list of products to validate and update.</param>
    /// <param name="validationPredicate">Function to determine if a product should be updated.</param>
    /// <param name="preUpdateAction">Optional action to perform on products before updating.</param>
    /// <returns>Operation result indicating success or failure with validation details.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="example"/> or <paramref name="products"/> or <paramref name="validationPredicate"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="products"/> contains null elements.</exception>
    public static async Task<OperationResult> UpdateValidProductsWriteThroughAsync(
        this WriteThroughExample example,
        [DisallowNull] List<Product> products,
        [DisallowNull] Func<Product, bool> validationPredicate,
        Action<Product>? preUpdateAction = null)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(validationPredicate);

        Console.WriteLine($"Updating valid products from list ({products.Count} total)");

        try
        {
            var validProducts = new List<Product>();
            var invalidProducts = new List<int>();

            // Step 1: Validate all products
            foreach (var product in products)
            {
                ArgumentNullException.ThrowIfNull(product);

                if (validationPredicate(product))
                {
                    preUpdateAction?.Invoke(product);
                    validProducts.Add(product);
                }
                else
                {
                    invalidProducts.Add(product.Id);
                }
            }

            if (validProducts.Count == 0)
            {
                Console.WriteLine($"✓ No valid products to update (invalid: {string.Join(", ", invalidProducts)})");
                return OperationResult.Success();
            }

            Console.WriteLine($" → {validProducts.Count} valid products to update, {invalidProducts.Count} invalid");

            // Step 2: Bulk update valid products with write-through
            var result = await example.BulkUpdateProductsWriteThroughAsync(validProducts);

            if (result.Success && invalidProducts.Count > 0)
            {
                Console.WriteLine($"✓ Updated {validProducts.Count} products (skipped: {string.Join(", ", invalidProducts)})");
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Conditional update failed: {ex.Message}");
            return OperationResult.Failure($"Conditional update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates product price with write-through and price change tracking.
    /// Returns detailed result with old and new price information.
    /// </summary>
    /// <param name="example">The <see cref="WriteThroughExample"/> instance.</param>
    /// <param name="productId">The product identifier.</param>
    /// <param name="newPrice">The new price to set.</param>
    /// <returns>Operation result with price change tracking information.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="example"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="productId"/> is less than or equal to zero.</exception>
    public static async Task<OperationResult<ProductPriceUpdateResult>> UpdateProductPriceWithTrackingAsync(
        this WriteThroughExample example,
        int productId,
        decimal newPrice)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(productId, 0);

        try
        {
            Console.WriteLine($"Updating price with tracking: Product {productId} → ${newPrice}");

            // Step 1: Get current product
            var getResult = await example._productRepository.GetByIdAsync(productId);
            if (getResult is null)
            {
                return OperationResult<ProductPriceUpdateResult>.Failure("Product not found");
            }

            var oldPrice = getResult.Price;
            var priceChanged = oldPrice != newPrice;

            if (!priceChanged)
            {
                Console.WriteLine(" → Price unchanged, no update needed");
                return OperationResult<ProductPriceUpdateResult>.Success(new ProductPriceUpdateResult
                {
                    ProductId = productId,
                    OldPrice = oldPrice,
                    NewPrice = newPrice,
                    PriceChanged = false,
                    Message = "Price unchanged"
                });
            }

            // Step 2: Update price
            var updateResult = await example.UpdateProductPriceAsync(productId, newPrice);

            if (updateResult.Success)
            {
                Console.WriteLine($"✓ Price updated: ${oldPrice} → ${newPrice}");
                return OperationResult<ProductPriceUpdateResult>.Success(new ProductPriceUpdateResult
                {
                    ProductId = productId,
                    OldPrice = oldPrice,
                    NewPrice = newPrice,
                    PriceChanged = true,
                    Message = "Price updated successfully"
                });
            }

            return OperationResult<ProductPriceUpdateResult>.Failure(updateResult.Message ?? "Unknown error");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Price update with tracking failed: {ex.Message}");
            return OperationResult<ProductPriceUpdateResult>.Failure($"Price update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Atomic upsert operation with write-through pattern.
    /// Updates if product exists, creates if it doesn't.
    /// </summary>
    /// <param name="example">The <see cref="WriteThroughExample"/> instance.</param>
    /// <param name="product">The product to upsert.</param>
    /// <returns>Operation result with the upserted product.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="example"/> or <paramref name="product"/> is <see langword="null"/>.</exception>
    public static async Task<OperationResult<Product>> UpsertProductWriteThroughAsync(
        this WriteThroughExample example,
        [DisallowNull] Product product)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentNullException.ThrowIfNull(product);

        try
        {
            Console.WriteLine($"Upserting product {product.Id}: {product.Name}");

            // Step 1: Check if product exists
            var existing = await example._productRepository.GetByIdAsync(product.Id);

            if (existing is not null)
            {
                // Update existing product
                Console.WriteLine(" → Product exists, updating...");
                var updateResult = await example.UpdateProductWriteThroughAsync(product);
                return updateResult;
            }
            else
            {
                // Create new product
                Console.WriteLine(" → Product doesn't exist, creating...");
                var createResult = await example.CreateProductWriteThroughAsync(product);
                return createResult;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Upsert failed: {ex.Message}");
            return OperationResult<Product>.Failure($"Upsert failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Result type for price update operations with tracking information.
/// </summary>
public sealed class ProductPriceUpdateResult
{
    public int ProductId { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public bool PriceChanged { get; set; }
    public string? Message { get; set; }
}