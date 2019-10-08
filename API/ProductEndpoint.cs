#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;

namespace RedisCachePatterns.API;

/// <summary>
/// API endpoint for product operations (list, get, create, update, delete)
/// </summary>
public class ProductEndpoint : ApiEndpointBase
{
    private readonly ProductService _productService;

    public ProductEndpoint(
        ProductService productService,
        ILogger<ProductEndpoint> logger,
        PerformanceMonitor performanceMonitor)
        : base(logger, performanceMonitor)
    {
        _productService = productService;
    }

    public async Task<ApiResponse<Product?>> GetProductByIdAsync(int productId)
    {
        return await ExecuteAsync(
            () => _productService.GetProductByIdAsync(productId),
            $"GetProductById({productId})");
    }

    public async Task<ApiResponse<IEnumerable<Product>>> GetLowStockProductsAsync()
    {
        return await ExecuteAsync(
            () => _productService.GetLowStockProductsAsync(),
            "GetLowStockProducts");
    }

    public async Task<ApiResponse<Product>> CreateProductAsync(string name, string sku, decimal price)
    {
        ValidateRequired(name, nameof(name));
        ValidateRequired(sku, nameof(sku));
        if (price <= 0) throw new ArgumentException("Price must be greater than 0");

        var product = new Product
        {
            Name = name,
            Sku = sku,
            Price = price,
            CreatedAt = DateTime.UtcNow
        };

        return await ExecuteAsync(
            () => _productService.CreateProductAsync(product),
            $"CreateProduct({sku})");
    }

    public async Task<ApiResponse<Product?>> UpdateProductAsync(int id, string? name, decimal? price)
    {
        if (id <= 0) throw new ArgumentException("Invalid product ID");

        var product = await _productService.GetProductByIdAsync(id);
        if (product is null) return ApiResponse<Product?>.NotFound($"Product {id} not found");

        if (!string.IsNullOrEmpty(name)) product.Name = name;
        if (price.HasValue && price > 0) product.UpdatePrice(price.Value);

        var result = await ExecuteAsync(
            () => _productService.UpdateProductAsync(product),
            $"UpdateProduct({id})");
        return new ApiResponse<Product?>
        {
            IsSuccess = result.IsSuccess,
            Data = result.Data,
            Error = result.Error,
            StatusCode = result.StatusCode,
            Timestamp = result.Timestamp,
            RequestId = result.RequestId
        };
    }

    public async Task<ApiResponse<bool>> DeleteProductAsync(int id)
    {
        if (id <= 0) throw new ArgumentException("Invalid product ID");

        return await ExecuteAsync(
            async () =>
            {
                await _productService.DeleteProductAsync(id);
                return true;
            },
            $"DeleteProduct({id})");
    }
}
