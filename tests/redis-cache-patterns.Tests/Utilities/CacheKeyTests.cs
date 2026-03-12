#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Utilities;
using Xunit;

namespace RedisCachePatterns.Tests.Utilities;

public class CacheKeyBuilderTests
{
    [Fact]
    public void BuildKey_WithMultipleParts_ReturnsColonSeparatedString()
    {
        CacheKeyBuilder.BuildKey("entity", "type", 42).Should().Be("entity:type:42");
    }

    [Fact]
    public void BuildKey_WithNullPart_SubstitutesNullLiteral()
    {
        CacheKeyBuilder.BuildKey("prefix", null, "suffix").Should().Be("prefix:null:suffix");
    }

    [Fact]
    public void User_ReturnsUserPrefixedKey()
    {
        CacheKeyBuilder.User(7).Should().Be("user:7");
    }

    [Fact]
    public void Product_ReturnsProductPrefixedKey()
    {
        CacheKeyBuilder.Product(42).Should().Be("product:42");
    }

    [Fact]
    public void ProductBySku_ReturnsSkuScopedKey()
    {
        CacheKeyBuilder.ProductBySku("PRD-007").Should().Be("product:sku:PRD-007");
    }

    [Fact]
    public void OrdersByUser_ReturnsUserScopedOrderKey()
    {
        CacheKeyBuilder.OrdersByUser(3).Should().Be("orders:user:3");
    }

    [Fact]
    public void InventoryByProductAndWarehouse_ReturnsFullyQualifiedKey()
    {
        var key = CacheKeyBuilder.InventoryByProductAndWarehouse(5, "WH-East");
        key.Should().Be("inventory:product:5:warehouse:WH-East");
    }

    [Fact]
    public void DistributedLock_ReturnsLockPrefixedKey()
    {
        CacheKeyBuilder.DistributedLock("order-create").Should().Be("lock:order-create");
    }

    [Fact]
    public void GeneratePattern_AppendsSeparatorAndWildcard()
    {
        CacheKeyBuilder.GeneratePattern("product").Should().Be("product:*");
    }
}

public class CacheKeyHelperTests
{
    [Fact]
    public void IsValidKey_WithWellFormedKey_ReturnsTrue()
    {
        CacheKeyHelper.IsValidKey("product:42").Should().BeTrue();
    }

    [Fact]
    public void IsValidKey_WithEmptyString_ReturnsFalse()
    {
        CacheKeyHelper.IsValidKey(string.Empty).Should().BeFalse();
    }

    [Fact]
    public void IsValidKey_WithWhitespaceOnly_ReturnsFalse()
    {
        CacheKeyHelper.IsValidKey("   ").Should().BeFalse();
    }

    [Fact]
    public void IsValidKey_WithNewlineCharacter_ReturnsFalse()
    {
        CacheKeyHelper.IsValidKey("bad\nkey").Should().BeFalse();
    }

    [Fact]
    public void IsValidKey_WithCarriageReturn_ReturnsFalse()
    {
        CacheKeyHelper.IsValidKey("bad\rkey").Should().BeFalse();
    }

    [Fact]
    public void NormalizeKey_ConvertsToLowercaseAndTrims()
    {
        CacheKeyHelper.NormalizeKey("  PRODUCT:42  ").Should().Be("product:42");
    }

    [Fact]
    public void ParseKey_SplitsSegmentsOnColon()
    {
        CacheKeyHelper.ParseKey("product:entity:7").Should().Equal("product", "entity", "7");
    }

    [Fact]
    public void GetPrefix_ReturnsFirstSegmentOnly()
    {
        CacheKeyHelper.GetPrefix("order:number:ORD-001").Should().Be("order");
    }

    [Fact]
    public void BuildEntityKey_UsesLowercaseTypeNameWithEntitySegment()
    {
        CacheKeyHelper.BuildEntityKey<Product>(99).Should().Be("product:entity:99");
    }

    [Fact]
    public void BuildLockKey_PrependsLockPrefix()
    {
        CacheKeyHelper.BuildLockKey("checkout-flow").Should().Be("lock:checkout-flow");
    }

    [Fact]
    public void BuildPattern_WithNoParameters_AppendsWildcard()
    {
        CacheKeyHelper.BuildPattern("orders").Should().Be("orders:*");
    }

    [Fact]
    public void BuildCollectionKey_WithoutFilter_OmitsFilterSegment()
    {
        var key = CacheKeyHelper.BuildCollectionKey<Product>();
        key.Should().Be("product:collection");
    }

    [Fact]
    public void BuildCollectionKey_WithFilter_AppendsFilter()
    {
        var key = CacheKeyHelper.BuildCollectionKey<Product>("electronics");
        key.Should().Be("product:collection:electronics");
    }
}
