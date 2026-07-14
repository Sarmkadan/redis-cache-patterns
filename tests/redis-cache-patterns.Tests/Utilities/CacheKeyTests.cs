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

/// <summary>
/// Unit tests for <see cref="CacheKeyBuilder"/>.
/// </summary>
public class CacheKeyBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="CacheKeyBuilder.BuildKey(string, object[])"/> 
    /// returns a colon-separated string when given multiple parts.
    /// </summary>
    [Fact]
    public void BuildKey_WithMultipleParts_ReturnsColonSeparatedString()
    {
        CacheKeyBuilder.BuildKey("entity", "type", 42).Should().Be("entity:type:42");
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyBuilder.BuildKey(string, object[])"/> 
    /// substitutes a null literal for null parts.
    /// </summary>
    [Fact]
    public void BuildKey_WithNullPart_SubstitutesNullLiteral()
    {
        CacheKeyBuilder.BuildKey("prefix", null, "suffix").Should().Be("prefix:null:suffix");
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyBuilder.User(int)"/> 
    /// returns a user-prefixed key.
    /// </summary>
    [Fact]
    public void User_ReturnsUserPrefixedKey()
    {
        CacheKeyBuilder.User(7).Should().Be("user:7");
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyBuilder.Product(int)"/> 
    /// returns a product-prefixed key.
    /// </summary>
    [Fact]
    public void Product_ReturnsProductPrefixedKey()
    {
        CacheKeyBuilder.Product(42).Should().Be("product:42");
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyBuilder.ProductBySku(string)"/> 
    /// returns a SKU-scoped product key.
    /// </summary>
    [Fact]
    public void ProductBySku_ReturnsSkuScopedKey()
    {
        CacheKeyBuilder.ProductBySku("PRD-007").Should().Be("product:sku:PRD-007");
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyBuilder.OrdersByUser(int)"/> 
    /// returns a user-scoped order key.
    /// </summary>
    [Fact]
    public void OrdersByUser_ReturnsUserScopedOrderKey()
    {
        CacheKeyBuilder.OrdersByUser(3).Should().Be("orders:user:3");
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyBuilder.InventoryByProductAndWarehouse(int, string)"/> 
    /// returns a fully qualified inventory key.
    /// </summary>
    [Fact]
    public void InventoryByProductAndWarehouse_ReturnsFullyQualifiedKey()
    {
        var key = CacheKeyBuilder.InventoryByProductAndWarehouse(5, "WH-East");
        key.Should().Be("inventory:product:5:warehouse:WH-East");
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyBuilder.DistributedLock(string)"/> 
    /// returns a lock-prefixed key.
    /// </summary>
    [Fact]
    public void DistributedLock_ReturnsLockPrefixedKey()
    {
        CacheKeyBuilder.DistributedLock("order-create").Should().Be("lock:order-create");
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyBuilder.GeneratePattern(string)"/> 
    /// appends a separator and wildcard to the input string.
    /// </summary>
    [Fact]
    public void GeneratePattern_AppendsSeparatorAndWildcard()
    {
        CacheKeyBuilder.GeneratePattern("product").Should().Be("product:*");
    }
}

/// <summary>
/// Unit tests for <see cref="CacheKeyHelper"/>.
/// </summary>
public class CacheKeyHelperTests
{
    /// <summary>
    /// Verifies that <see cref="CacheKeyHelper.IsValidKey(string)"/> 
    /// returns true for a well-formed key.
    /// </summary>
    [Fact]
    public void IsValidKey_WithWellFormedKey_ReturnsTrue()
    {
        CacheKeyHelper.IsValidKey("product:42").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyHelper.IsValidKey(string)"/> 
    /// returns false for an empty string.
    /// </summary>
    [Fact]
    public void IsValidKey_WithEmptyString_ReturnsFalse()
    {
        CacheKeyHelper.IsValidKey(string.Empty).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyHelper.IsValidKey(string)"/> 
    /// returns false for a string containing only whitespace.
    /// </summary>
    [Fact]
    public void IsValidKey_WithWhitespaceOnly_ReturnsFalse()
    {
        CacheKeyHelper.IsValidKey("   ").Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyHelper.IsValidKey(string)"/> 
    /// returns false for a string containing a newline character.
    /// </summary>
    [Fact]
    public void IsValidKey_WithNewlineCharacter_ReturnsFalse()
    {
        CacheKeyHelper.IsValidKey("bad\nkey").Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyHelper.IsValidKey(string)"/> 
    /// returns false for a string containing a carriage return.
    /// </summary>
    [Fact]
    public void IsValidKey_WithCarriageReturn_ReturnsFalse()
    {
        CacheKeyHelper.IsValidKey("bad\rkey").Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyHelper.NormalizeKey(string)"/> 
    /// converts a key to lowercase and trims whitespace.
    /// </summary>
    [Fact]
    public void NormalizeKey_ConvertsToLowercaseAndTrims()
    {
        CacheKeyHelper.NormalizeKey("  PRODUCT:42  ").Should().Be("product:42");
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyHelper.ParseKey(string)"/> 
    /// splits a key into segments on colons.
    /// </summary>
    [Fact]
    public void ParseKey_SplitsSegmentsOnColon()
    {
        CacheKeyHelper.ParseKey("product:entity:7").Should().Equal("product", "entity", "7");
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyHelper.GetPrefix(string)"/> 
    /// returns the first segment of a key.
    /// </summary>
    [Fact]
    public void GetPrefix_ReturnsFirstSegmentOnly()
    {
        CacheKeyHelper.GetPrefix("order:number:ORD-001").Should().Be("order");
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyHelper.BuildEntityKey{T}(int)"/> 
    /// builds an entity key using the type name and entity ID.
    /// </summary>
    [Fact]
    public void BuildEntityKey_UsesLowercaseTypeNameWithEntitySegment()
    {
        CacheKeyHelper.BuildEntityKey<Product>(99).Should().Be("product:entity:99");
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyHelper.BuildLockKey(string)"/> 
    /// builds a lock key by prepending a lock prefix.
    /// </summary>
    [Fact]
    public void BuildLockKey_PrependsLockPrefix()
    {
        CacheKeyHelper.BuildLockKey("checkout-flow").Should().Be("lock:checkout-flow");
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyHelper.BuildPattern(string)"/> 
    /// builds a pattern by appending a wildcard.
    /// </summary>
    [Fact]
    public void BuildPattern_WithNoParameters_AppendsWildcard()
    {
        CacheKeyHelper.BuildPattern("orders").Should().Be("orders:*");
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyHelper.BuildCollectionKey{T}(string?)"/> 
    /// builds a collection key without a filter.
    /// </summary>
    [Fact]
    public void BuildCollectionKey_WithoutFilter_OmitsFilterSegment()
    {
        var key = CacheKeyHelper.BuildCollectionKey<Product>();
        key.Should().Be("product:collection");
    }

    /// <summary>
    /// Verifies that <see cref="CacheKeyHelper.BuildCollectionKey{T}(string?)"/> 
    /// builds a collection key with a filter.
    /// </summary>
    [Fact]
    public void BuildCollectionKey_WithFilter_AppendsFilter()
    {
        var key = CacheKeyHelper.BuildCollectionKey<Product>("electronics");
        key.Should().Be("product:collection:electronics");
    }
}
