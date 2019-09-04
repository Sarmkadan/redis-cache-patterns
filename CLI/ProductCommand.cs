#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using RedisCachePatterns.Services;
using RedisCachePatterns.Domain;

namespace RedisCachePatterns.CLI;

/// <summary>
/// Implements product management CLI commands including inventory operations and reporting
/// </summary>
public class ProductCommand
{
    private readonly ProductService _productService;
    private readonly ILogger<ProductCommand> _logger;

    public ProductCommand(ProductService productService, ILogger<ProductCommand> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(Dictionary<string, string> options)
    {
        if (!options.TryGetValue("subcommand", out var subcommand))
        {
            subcommand = "list";
        }

        return subcommand.ToLower() switch
        {
            "list" => await ListProductsAsync(options),
            "low-stock" => await ListLowStockAsync(options),
            "create" => await CreateProductAsync(options),
            "update" => await UpdateProductAsync(options),
            "delete" => await DeleteProductAsync(options),
            _ => InvalidCommand(subcommand)
        };
    }

    private async Task<int> ListProductsAsync(Dictionary<string, string> options)
    {
        try
        {
            var allProducts = new List<Product>();
            Console.WriteLine("=== All Products ===");
            Console.WriteLine($"{"ID",-5} {"Name",-20} {"SKU",-12} {"Price",-10} {"Stock",-8} {"Status"}");
            Console.WriteLine(new string('-', 75));

            // In real implementation, would paginate through cached products
            Console.WriteLine("(Cached product listing)");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list products");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> ListLowStockAsync(Dictionary<string, string> options)
    {
        try
        {
            var lowStockProducts = await _productService.GetLowStockProductsAsync();
            Console.WriteLine("=== Low Stock Products ===");
            Console.WriteLine($"{"ID",-5} {"Name",-20} {"Current",-10} {"Reorder",-10}");
            Console.WriteLine(new string('-', 50));

            foreach (var product in lowStockProducts)
            {
                Console.WriteLine($"{product.Id,-5} {product.Name,-20} {product.StockQuantity,-10} {product.ReorderLevel,-10}");
            }

            Console.WriteLine($"\nTotal low stock items: {lowStockProducts.Count()}");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve low stock products");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> CreateProductAsync(Dictionary<string, string> options)
    {
        if (!options.TryGetValue("name", out var name) ||
            !options.TryGetValue("sku", out var sku) ||
            !decimal.TryParse(options.GetValueOrDefault("price", "0"), out var price))
        {
            Console.Error.WriteLine("Required: --name, --sku, --price");
            return 1;
        }

        try
        {
            var product = new Product
            {
                Name = name,
                Sku = sku,
                Price = price,
                Description = options.GetValueOrDefault("description", ""),
                Category = options.GetValueOrDefault("category", ""),
                StockQuantity = int.TryParse(options.GetValueOrDefault("stock", "0"), out var stock) ? stock : 0,
                ReorderLevel = int.TryParse(options.GetValueOrDefault("reorder", "10"), out var reorder) ? reorder : 10
            };

            var created = await _productService.CreateProductAsync(product);
            Console.WriteLine($"Product created: ID={created.Id}, SKU={created.Sku}");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create product");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> UpdateProductAsync(Dictionary<string, string> options)
    {
        if (!int.TryParse(options.GetValueOrDefault("id", ""), out var id))
        {
            Console.Error.WriteLine("--id parameter required");
            return 1;
        }

        try
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                Console.WriteLine($"Product {id} not found");
                return 1;
            }

            if (options.TryGetValue("price", out var priceStr) && decimal.TryParse(priceStr, out var price))
            {
                product.UpdatePrice(price);
            }

            if (options.TryGetValue("stock", out var stockStr) && int.TryParse(stockStr, out var stock))
            {
                product.UpdateStock(stock - product.StockQuantity);
            }

            var updated = await _productService.UpdateProductAsync(product);
            Console.WriteLine("Product updated successfully");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update product");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> DeleteProductAsync(Dictionary<string, string> options)
    {
        if (!int.TryParse(options.GetValueOrDefault("id", ""), out var id))
        {
            Console.Error.WriteLine("--id parameter required");
            return 1;
        }

        try
        {
            await _productService.DeleteProductAsync(id);
            Console.WriteLine($"Product {id} deleted");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete product");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private int InvalidCommand(string command)
    {
        Console.Error.WriteLine($"Unknown product subcommand: {command}");
        return 1;
    }
}
