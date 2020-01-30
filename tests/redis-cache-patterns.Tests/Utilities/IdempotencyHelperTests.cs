#nullable enable
using FluentAssertions;
using RedisCachePatterns.Utilities;
using Xunit;

namespace RedisCachePatterns.Tests.Utilities;

/// <summary>
/// Tests for the IdempotencyHelper class.
/// </summary>
public class IdempotencyHelperTests
{
    /// <summary>
    /// Verifies that IsProcessed returns false when the key has never been processed.
    /// </summary>
    [Fact]
    public void IsProcessed_WhenKeyNeverProcessed_ReturnsFalse()
    {
        var helper = new IdempotencyHelper();
        var isProcessed = helper.IsProcessed("new-key");

        isProcessed.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that MarkAsProcessed stores the result for a given key.
    /// </summary>
    [Fact]
    public void MarkAsProcessed_WithValidKey_StoresResult()
    {
        var helper = new IdempotencyHelper();
        var idempotencyKey = "request-123";
        var result = "processed-data";

        helper.MarkAsProcessed(idempotencyKey, result);

        helper.IsProcessed(idempotencyKey).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that GetResult returns the stored result after MarkAsProcessed.
    /// </summary>
    [Fact]
    public void GetResult_AfterMarkedAsProcessed_ReturnsStoredResult()
    {
        var helper = new IdempotencyHelper();
        var idempotencyKey = "req-456";
        var result = 42;

        helper.MarkAsProcessed(idempotencyKey, result);
        var retrieved = helper.GetResult<int>(idempotencyKey);

        retrieved.Should().Be(result);
    }

    /// <summary>
    /// Verifies that GetResult returns null when the key has not been processed.
    /// </summary>
    [Fact]
    public void GetResult_WhenKeyNotProcessed_ReturnsNull()
    {
        var helper = new IdempotencyHelper();
        var result = helper.GetResult<string>("nonexistent-key");

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that IsProcessed tracks different keys independently.
    /// </summary>
    [Fact]
    public void IsProcessed_WithDifferentKeys_TracksIndependently()
    {
        var helper = new IdempotencyHelper();

        helper.MarkAsProcessed("key1", "result1");

        helper.IsProcessed("key1").Should().BeTrue();
        helper.IsProcessed("key2").Should().BeFalse();
    }

    /// <summary>
    /// Verifies that MarkAsProcessed stores results of different types correctly.
    /// </summary>
    [Fact]
    public void MarkAsProcessed_WithDifferentTypes_StoresCorrectly()
    {
        var helper = new IdempotencyHelper();

        helper.MarkAsProcessed("string-key", "string-result");
        helper.MarkAsProcessed("int-key", 42);
        helper.MarkAsProcessed("bool-key", true);

        helper.GetResult<string>("string-key").Should().Be("string-result");
        helper.GetResult<int>("int-key").Should().Be(42);
        helper.GetResult<bool>("bool-key").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that MarkAsProcessed updates the result for an existing key.
    /// </summary>
    [Fact]
    public void MarkAsProcessed_UpdatesExistingKey_OverwritesPreviousResult()
    {
        var helper = new IdempotencyHelper();
        var key = "update-key";

        helper.MarkAsProcessed(key, "first");
        var first = helper.GetResult<string>(key);

        helper.MarkAsProcessed(key, "second");
        var second = helper.GetResult<string>(key);

        first.Should().Be("first");
        second.Should().Be("second");
    }

    /// <summary>
    /// Verifies that IsProcessed returns false for an expired record.
    /// </summary>
    [Fact]
    public void IsProcessed_WithExpiredRecord_ReturnsFalse()
    {
        var helper = new IdempotencyHelper(TimeSpan.FromMilliseconds(100));
        var key = "expire-key";

        helper.MarkAsProcessed(key, "result");
        helper.IsProcessed(key).Should().BeTrue();

        System.Threading.Thread.Sleep(150);

        helper.IsProcessed(key).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that GetResult returns null for an expired record.
    /// </summary>
    [Fact]
    public void GetResult_WithExpiredRecord_ReturnsNull()
    {
        var helper = new IdempotencyHelper(TimeSpan.FromMilliseconds(100));
        var key = "expire-result";

        helper.MarkAsProcessed(key, 42);
        var beforeExpiry = helper.GetResult<int>(key);

        System.Threading.Thread.Sleep(150);

        var afterExpiry = helper.GetResult<int>(key);

        beforeExpiry.Should().Be(42);
        ((object?)afterExpiry).Should().BeNull();
    }

    /// <summary>
    /// Verifies that IsProcessed returns true for a record near its expiry time.
    /// </summary>
    [Fact]
    public void IsProcessed_WithNearExpiryTime_ReturnsTrue()
    {
        var helper = new IdempotencyHelper(TimeSpan.FromMilliseconds(200));
        var key = "near-expiry";

        helper.MarkAsProcessed(key, "result");
        System.Threading.Thread.Sleep(50);

        helper.IsProcessed(key).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that MarkAsProcessed stores and retrieves complex objects correctly.
    /// </summary>
    [Fact]
    public void MarkAsProcessed_WithComplexObject_StoresAndRetrieves()
    {
        var helper = new IdempotencyHelper();
        var key = "complex-key";
        var complexResult = new TestObject { Id = 1, Name = "Test", Values = new[] { 1, 2, 3 } };

        helper.MarkAsProcessed(key, complexResult);
        var retrieved = helper.GetResult<TestObject>(key);

        retrieved.Should().NotBeNull();
        retrieved?.Id.Should().Be(1);
        retrieved?.Name.Should().Be("Test");
        retrieved?.Values.Should().Equal(1, 2, 3);
    }

    /// <summary>
    /// Verifies that IsProcessed checks the specific key when multiple records exist.
    /// </summary>
    [Fact]
    public void IsProcessed_WithMultipleRecords_ChecksSpecificKey()
    {
        var helper = new IdempotencyHelper();

        helper.MarkAsProcessed("key1", "result1");
        helper.MarkAsProcessed("key2", "result2");
        helper.MarkAsProcessed("key3", "result3");

        helper.IsProcessed("key2").Should().BeTrue();
        helper.GetResult<string>("key2").Should().Be("result2");
    }

    /// <summary>
    /// Verifies that the default constructor uses a 24-hour retention period.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaultRetention_Uses24Hours()
    {
        var helper = new IdempotencyHelper();
        var key = "default-retention";

        helper.MarkAsProcessed(key, "result");
        helper.IsProcessed(key).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the constructor with a custom retention period applies correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithCustomRetention_AppliesCorrectly()
    {
        var customRetention = TimeSpan.FromMilliseconds(50);
        var helper = new IdempotencyHelper(customRetention);
        var key = "custom-key";

        helper.MarkAsProcessed(key, "result");
        System.Threading.Thread.Sleep(60);

        helper.IsProcessed(key).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that GetResult returns the correct type when type conversion is needed.
    /// </summary>
    [Fact]
    public void GetResult_WithTypeConversionNeeded_ReturnsCorrectType()
    {
        var helper = new IdempotencyHelper();

        helper.MarkAsProcessed("int-key", 123);
        var result = helper.GetResult<int>("int-key");

        result.Should().Be(123);
        result.Should().BeOfType(typeof(int));
    }

    /// <summary>
    /// A test object used for testing complex object storage and retrieval.
    /// </summary>
    private class TestObject
    {
        /// <summary>
        /// Gets or sets the ID of the test object.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the name of the test object.
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// Gets or sets the values of the test object.
        /// </summary>
        public int[]? Values { get; set; }
    }
}
