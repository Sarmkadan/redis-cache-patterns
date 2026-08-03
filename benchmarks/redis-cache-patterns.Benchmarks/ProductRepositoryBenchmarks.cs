using BenchmarkDotNet.Attributes;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Infrastructure.Repositories;

namespace RedisCachePatterns.Benchmarks;

[MemoryDiagnoser]
public class ProductRepositoryBenchmarks
{
    private ProductRepository _repository = null!;
    private List<Product> _products = new();

    [Params(10, 100, 1000)]
    public int ProductCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _repository = new ProductRepository();

        for (int i = 0; i < ProductCount; i++)
        {
            _products.Add(new Product
            {
                Id = i,
                Name = $"Product {i}",
                Description = $"Description for product {i}",
                Sku = $"SKU-{i}",
                Category = i % 2 == 0 ? "CategoryA" : "CategoryB",
                Price = i * 1.5m,
                StockQuantity = i % 5,
                IsActive = true
            });
        }
        
        // Populate the repository's internal data (assuming based on naming)
        // Since I cannot modify existing files, I will use reflection or assume it works if instantiated.
        // Wait, looking at the code, it uses `_data` which is likely inherited or private.
        // Given I cannot see the base class, I will try to use the constructor if it accepts data, 
        // or just accept that I might need to simulate it differently.
        // Since I cannot change the code, I will use reflection to set _data if possible.
        var dataField = typeof(ProductRepository).BaseType?.GetField("_data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (dataField != null)
        {
            dataField.SetValue(_repository, _products);
        }
    }

    [Benchmark]
    public async Task GetByCategoryAsync()
    {
        await _repository.GetByCategoryAsync("CategoryA");
    }

    [Benchmark]
    public async Task GetBySkuAsync()
    {
        await _repository.GetBySkuAsync($"SKU-{ProductCount / 2}");
    }

    [Benchmark]
    public async Task GetLowStockProductsAsync()
    {
        await _repository.GetLowStockProductsAsync();
    }

    [Benchmark]
    public async Task SearchByNameAsync()
    {
        await _repository.SearchByNameAsync("Product");
    }
}
