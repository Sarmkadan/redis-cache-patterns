// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using RedisCachePatterns.Domain;
using Xunit;

namespace RedisCachePatterns.Tests.Domain;

public class ProductTests
{
    private static Product CreateProduct(int stock = 20, int reorderLevel = 10) => new()
    {
        Id = 1,
        Name = "Widget Pro",
        Sku = "WGT-001",
        Price = 49.99m,
        StockQuantity = stock,
        ReorderLevel = reorderLevel,
        Category = "Gadgets",
        IsActive = true
    };

    [Fact]
    public void IsLowStock_WhenStockEqualsReorderLevel_ReturnsTrue()
    {
        var product = CreateProduct(stock: 10, reorderLevel: 10);
        product.IsLowStock().Should().BeTrue();
    }

    [Fact]
    public void IsLowStock_WhenStockExceedsReorderLevel_ReturnsFalse()
    {
        var product = CreateProduct(stock: 25, reorderLevel: 10);
        product.IsLowStock().Should().BeFalse();
    }

    [Fact]
    public void UpdateStock_WhenReductionExceedsAvailable_ThrowsInvalidOperationException()
    {
        var product = CreateProduct(stock: 5);
        Action act = () => product.UpdateStock(-10);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*below zero*");
    }

    [Fact]
    public void UpdateStock_WithPositiveQuantity_IncreasesStockAndSetsTimestamp()
    {
        var product = CreateProduct(stock: 10);
        product.UpdateStock(5);
        product.StockQuantity.Should().Be(15);
        product.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdatePrice_WithNegativeValue_ThrowsArgumentException()
    {
        var product = CreateProduct();
        Action act = () => product.UpdatePrice(-1m);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*negative*");
    }

    [Fact]
    public void UpdatePrice_WithValidValue_UpdatesPriceAndSetsTimestamp()
    {
        var product = CreateProduct();
        product.UpdatePrice(99.95m);
        product.Price.Should().Be(99.95m);
        product.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void CalculateDiscount_With10PercentOff_ReturnsNinetyPercentOfPrice()
    {
        var product = CreateProduct();
        product.Price = 100m;
        product.CalculateDiscount(10m).Should().Be(90m);
    }

    [Fact]
    public void CalculateDiscount_WithZeroPercent_ReturnsOriginalPrice()
    {
        var product = CreateProduct();
        product.Price = 50m;
        product.CalculateDiscount(0m).Should().Be(50m);
    }

    [Fact]
    public void SetRating_WithValueAboveFive_ThrowsArgumentException()
    {
        var product = CreateProduct();
        Action act = () => product.SetRating(5.1, 100);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*between 0 and 5*");
    }

    [Fact]
    public void SetRating_WithNegativeValue_ThrowsArgumentException()
    {
        var product = CreateProduct();
        Action act = () => product.SetRating(-0.1, 10);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsAvailable_WhenDeactivated_ReturnsFalseRegardlessOfStock()
    {
        var product = CreateProduct(stock: 50);
        product.Deactivate();
        product.IsAvailable().Should().BeFalse();
    }

    [Fact]
    public void IsAvailable_WhenActiveWithPositiveStock_ReturnsTrue()
    {
        var product = CreateProduct(stock: 1);
        product.IsAvailable().Should().BeTrue();
    }

    [Fact]
    public void Activate_AfterDeactivate_RestoresAvailability()
    {
        var product = CreateProduct(stock: 5);
        product.Deactivate();
        product.Activate();
        product.IsAvailable().Should().BeTrue();
        product.IsActive.Should().BeTrue();
    }
}
