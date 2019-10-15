#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using RedisCachePatterns.Domain;
using Xunit;

/// <summary>
/// Tests for the Product class.
/// </summary>
namespace RedisCachePatterns.Tests.Domain;

public class ProductTests
{
    /// <summary>
    /// Creates a new Product instance with the specified stock and reorder level.
    /// </summary>
    /// <param name="stock">The initial stock quantity.</param>
    /// <param name="reorderLevel">The reorder level.</param>
    /// <returns>A new Product instance.</returns>
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

    /// <summary>
    /// Verifies that IsLowStock returns true when the stock equals the reorder level.
    /// </summary>
    [Fact]
    public void IsLowStock_WhenStockEqualsReorderLevel_ReturnsTrue()
    {
        var product = CreateProduct(stock: 10, reorderLevel: 10);
        product.IsLowStock().Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsLowStock returns false when the stock exceeds the reorder level.
    /// </summary>
    [Fact]
    public void IsLowStock_WhenStockExceedsReorderLevel_ReturnsFalse()
    {
        var product = CreateProduct(stock: 25, reorderLevel: 10);
        product.IsLowStock().Should().BeFalse();
    }

    /// <summary>
    /// Verifies that UpdateStock throws an InvalidOperationException when the reduction exceeds the available stock.
    /// </summary>
    [Fact]
    public void UpdateStock_WhenReductionExceedsAvailable_ThrowsInvalidOperationException()
    {
        var product = CreateProduct(stock: 5);
        Action act = () => product.UpdateStock(-10);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*below zero*");
    }

    /// <summary>
    /// Verifies that UpdateStock increases the stock and sets the UpdatedAt timestamp when a positive quantity is provided.
    /// </summary>
    [Fact]
    public void UpdateStock_WithPositiveQuantity_IncreasesStockAndSetsTimestamp()
    {
        var product = CreateProduct(stock: 10);
        product.UpdateStock(5);
        product.StockQuantity.Should().Be(15);
        product.UpdatedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that UpdatePrice throws an ArgumentException when a negative value is provided.
    /// </summary>
    [Fact]
    public void UpdatePrice_WithNegativeValue_ThrowsArgumentException()
    {
        var product = CreateProduct();
        Action act = () => product.UpdatePrice(-1m);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*negative*");
    }

    /// <summary>
    /// Verifies that UpdatePrice updates the price and sets the UpdatedAt timestamp when a valid value is provided.
    /// </summary>
    [Fact]
    public void UpdatePrice_WithValidValue_UpdatesPriceAndSetsTimestamp()
    {
        var product = CreateProduct();
        product.UpdatePrice(99.95m);
        product.Price.Should().Be(99.95m);
        product.UpdatedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that CalculateDiscount returns the price minus the discount percentage when 10% off is applied.
    /// </summary>
    [Fact]
    public void CalculateDiscount_With10PercentOff_ReturnsNinetyPercentOfPrice()
    {
        var product = CreateProduct();
        product.Price = 100m;
        product.CalculateDiscount(10m).Should().Be(90m);
    }

    /// <summary>
    /// Verifies that CalculateDiscount returns the original price when 0% off is applied.
    /// </summary>
    [Fact]
    public void CalculateDiscount_WithZeroPercent_ReturnsOriginalPrice()
    {
        var product = CreateProduct();
        product.Price = 50m;
        product.CalculateDiscount(0m).Should().Be(50m);
    }

    /// <summary>
    /// Verifies that SetRating throws an ArgumentException when a value above 5 is provided.
    /// </summary>
    [Fact]
    public void SetRating_WithValueAboveFive_ThrowsArgumentException()
    {
        var product = CreateProduct();
        Action act = () => product.SetRating(5.1, 100);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*between 0 and 5*");
    }

    /// <summary>
    /// Verifies that SetRating throws an ArgumentException when a negative value is provided.
    /// </summary>
    [Fact]
    public void SetRating_WithNegativeValue_ThrowsArgumentException()
    {
        var product = CreateProduct();
        Action act = () => product.SetRating(-0.1, 10);
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifies that IsAvailable returns false when the product is deactivated, regardless of the stock quantity.
    /// </summary>
    [Fact]
    public void IsAvailable_WhenDeactivated_ReturnsFalseRegardlessOfStock()
    {
        var product = CreateProduct(stock: 50);
        product.Deactivate();
        product.IsAvailable().Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsAvailable returns true when the product is active and has a positive stock quantity.
    /// </summary>
    [Fact]
    public void IsAvailable_WhenActiveWithPositiveStock_ReturnsTrue()
    {
        var product = CreateProduct(stock: 1);
        product.IsAvailable().Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Activate restores the product's availability after it has been deactivated.
    /// </summary>
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
