#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Domain;
using RedisCachePatterns.Infrastructure.Repositories;
using RedisCachePatterns.Exceptions;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Services;

/// <summary>
/// Service managing product catalog with cache-aside and invalidation strategies
/// </summary>
public class ProductService
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<ProductService> _logger;
    private const string PRODUCT_CACHE_KEY = "product:{0}";
    private const string PRODUCTS_CATEGORY_CACHE_KEY = "products:category:{0}";
    private const string LOW_STOCK_CACHE_KEY = "products:lowstock";

    public ProductService(IProductRepository repository, ICacheService cache, ILogger<ProductService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Product?> GetProductByIdAsync(int productId)
    {
        var cacheKey = string.Format(PRODUCT_CACHE_KEY, productId);
        return await _cache.GetOrLoadAsync(
            cacheKey,
            async () => await _repository.GetByIdAsync(productId),
            TimeSpan.FromHours(2)
        );
    }

    public async Task<Product?> GetProductBySkuAsync(string sku)
    {
        var cacheKey = $"product:sku:{sku}";
        return await _cache.GetOrLoadAsync(
            cacheKey,
            async () => await _repository.GetBySkuAsync(sku),
            TimeSpan.FromHours(2)
        );
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category)
    {
        var cacheKey = string.Format(PRODUCTS_CATEGORY_CACHE_KEY, category);
        var result = await _cache.GetOrLoadAsync(
            cacheKey,
            async () => await _repository.GetByCategoryAsync(category),
            TimeSpan.FromMinutes(30)
        );
        return result ?? [];
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        var existing = await GetProductBySkuAsync(product.Sku);
        if (existing is not null)
            throw new ValidationException("Product with this SKU already exists");

        var created = await _repository.AddAsync(product);
        await _cache.SetAsync(string.Format(PRODUCT_CACHE_KEY, created.Id), created, TimeSpan.FromHours(2));

        await InvalidateProductCachesAsync();
        _logger.LogInformation("Product created: {ProductId} - {ProductName}", created.Id, created.Name);
        return created;
    }

    public async Task<Product> UpdateProductAsync(Product product)
    {
        var existing = await GetProductByIdAsync(product.Id);
        if (existing is null)
            throw new NotFoundException(nameof(Product), product.Id);

        var updated = await _repository.UpdateAsync(product);
        await _cache.SetAsync(string.Format(PRODUCT_CACHE_KEY, updated.Id), updated, TimeSpan.FromHours(2));

        // Invalidate category cache if category changed
        if (existing.Category != product.Category)
        {
            await _cache.RemoveAsync(string.Format(PRODUCTS_CATEGORY_CACHE_KEY, existing.Category));
            await _cache.RemoveAsync(string.Format(PRODUCTS_CATEGORY_CACHE_KEY, product.Category));
        }

        await _cache.RemoveAsync(LOW_STOCK_CACHE_KEY);
        _logger.LogInformation("Product updated: {ProductId}", product.Id);
        return updated;
    }

    public async Task<bool> DeleteProductAsync(int productId)
    {
        var product = await GetProductByIdAsync(productId);
        if (product is null)
            return false;

        var deleted = await _repository.DeleteAsync(productId);
        if (deleted)
        {
            await _cache.RemoveAsync(string.Format(PRODUCT_CACHE_KEY, productId));
            await _cache.RemoveAsync($"product:sku:{product.Sku}");
            await InvalidateProductCachesAsync();
            _logger.LogInformation("Product deleted: {ProductId}", productId);
        }

        return deleted;
    }

    public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
    {
        var result = await _cache.GetOrLoadAsync(
            LOW_STOCK_CACHE_KEY,
            async () => await _repository.GetLowStockProductsAsync(),
            TimeSpan.FromMinutes(15)
        );
        return result ?? [];
    }

    public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
    {
        var cacheKey = $"products:search:{searchTerm}";
        var result = await _cache.GetOrLoadAsync(
            cacheKey,
            async () => await _repository.SearchByNameAsync(searchTerm),
            TimeSpan.FromMinutes(10)
        );
        return result ?? [];
    }

    public async Task UpdateProductPriceAsync(int productId, decimal newPrice)
    {
        var product = await GetProductByIdAsync(productId);
        if (product is null)
            throw new NotFoundException(nameof(Product), productId);

        product.UpdatePrice(newPrice);
        await UpdateProductAsync(product);
        _logger.LogInformation("Product price updated: {ProductId} - ${Price}", productId, newPrice);
    }

    public async Task UpdateProductStockAsync(int productId, int quantity)
    {
        var product = await GetProductByIdAsync(productId);
        if (product is null)
            throw new NotFoundException(nameof(Product), productId);

        product.UpdateStock(quantity);
        await UpdateProductAsync(product);

        if (product.IsLowStock())
            await _cache.RemoveAsync(LOW_STOCK_CACHE_KEY);

        _logger.LogInformation("Product stock updated: {ProductId}, New Quantity: {Quantity}", productId, product.StockQuantity);
    }

    private async Task InvalidateProductCachesAsync()
    {
        await _cache.RemoveAsync(LOW_STOCK_CACHE_KEY);
        await _cache.RemoveByPatternAsync(PRODUCTS_CATEGORY_CACHE_KEY.Replace("{0}", "*"));
        await _cache.RemoveByPatternAsync("products:search:*");
    }
}
