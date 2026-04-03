#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Utilities;

namespace RedisCachePatterns.Benchmarks;

/// <summary>
/// Benchmarks for JSON serialization used in every cache read/write path.
/// Measures the per-operation cost of converting domain objects to/from JSON.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class SerializationBenchmarks
{
    private Product _product = null!;
    private Order _order = null!;
    private string _productJson = null!;
    private string _orderJson = null!;

    [GlobalSetup]
    public void Setup()
    {
        _product = new Product
        {
            Id = 1,
            Name = "Wireless Noise-Cancelling Headphones",
            Description = "Premium over-ear headphones with 30-hour battery and active noise cancellation",
            Sku = "WNC-HDPH-BLK-001",
            Price = 299.99m,
            StockQuantity = 150,
            ReorderLevel = 20,
            Category = "Electronics",
            IsActive = true,
            Rating = 4.7,
            ReviewCount = 2341,
        };

        _order = new Order
        {
            Id = 42,
            UserId = 7,
            OrderNumber = "ORD-2024-0042",
            Status = OrderStatus.Confirmed,
            ShippingAddress = "123 Main St, Springfield, IL 62701",
            BillingAddress = "123 Main St, Springfield, IL 62701",
            TotalAmount = 329.94m,
            TaxAmount = 29.95m,
            ShippingCost = 0m,
            Items = Enumerable.Range(1, 3).Select(i => new OrderItem
            {
                Id = i,
                OrderId = 42,
                ProductId = i * 10,
                Quantity = i,
                UnitPrice = 99.98m,
            }).ToList(),
        };

        _productJson = SerializationHelper.Serialize(_product);
        _orderJson = SerializationHelper.Serialize(_order);
    }

    [Benchmark(Baseline = true, Description = "Serialize Product (~280 B)")]
    public string SerializeProduct() => SerializationHelper.Serialize(_product);

    [Benchmark(Description = "Deserialize Product")]
    public Product? DeserializeProduct() => SerializationHelper.Deserialize<Product>(_productJson);

    [Benchmark(Description = "Serialize Order with 3 items (~620 B)")]
    public string SerializeOrder() => SerializationHelper.Serialize(_order);

    [Benchmark(Description = "Deserialize Order with 3 items")]
    public Order? DeserializeOrder() => SerializationHelper.Deserialize<Order>(_orderJson);
}
