#nullable enable
using FluentAssertions;
using RedisCachePatterns.Utilities;
using Xunit;

namespace RedisCachePatterns.Tests.Utilities;

public class IdempotencyHelperTests
{
    [Fact]
    public void IsProcessed_WhenKeyNeverProcessed_ReturnsFalse()
    {
        var helper = new IdempotencyHelper();
        var isProcessed = helper.IsProcessed("new-key");

        isProcessed.Should().BeFalse();
    }

    [Fact]
    public void MarkAsProcessed_WithValidKey_StoresResult()
    {
        var helper = new IdempotencyHelper();
        var idempotencyKey = "request-123";
        var result = "processed-data";

        helper.MarkAsProcessed(idempotencyKey, result);

        helper.IsProcessed(idempotencyKey).Should().BeTrue();
    }

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

    [Fact]
    public void GetResult_WhenKeyNotProcessed_ReturnsNull()
    {
        var helper = new IdempotencyHelper();
        var result = helper.GetResult<string>("nonexistent-key");

        result.Should().BeNull();
    }

    [Fact]
    public void IsProcessed_WithDifferentKeys_TracksIndependently()
    {
        var helper = new IdempotencyHelper();

        helper.MarkAsProcessed("key1", "result1");

        helper.IsProcessed("key1").Should().BeTrue();
        helper.IsProcessed("key2").Should().BeFalse();
    }

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
        afterExpiry.Should().BeNull();
    }

    [Fact]
    public void IsProcessed_WithNearExpiryTime_ReturnsTrue()
    {
        var helper = new IdempotencyHelper(TimeSpan.FromMilliseconds(200));
        var key = "near-expiry";

        helper.MarkAsProcessed(key, "result");
        System.Threading.Thread.Sleep(50);

        helper.IsProcessed(key).Should().BeTrue();
    }

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

    [Fact]
    public void Constructor_WithDefaultRetention_Uses24Hours()
    {
        var helper = new IdempotencyHelper();
        var key = "default-retention";

        helper.MarkAsProcessed(key, "result");
        helper.IsProcessed(key).Should().BeTrue();
    }

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

    [Fact]
    public void GetResult_WithTypeConversionNeeded_ReturnsCorrectType()
    {
        var helper = new IdempotencyHelper();

        helper.MarkAsProcessed("int-key", 123);
        var result = helper.GetResult<int>("int-key");

        result.Should().Be(123);
        result.Should().BeOfType<int>();
    }

    private class TestObject
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int[]? Values { get; set; }
    }
}
