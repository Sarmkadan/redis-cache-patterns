#nullable enable
using FluentAssertions;
using RedisCachePatterns.Utilities;
using Xunit;

/// <summary>
/// Tests for the CompressionUtil class.
/// </summary>
namespace RedisCachePatterns.Tests.Utilities;

public class CompressionUtilTests
{
    /// <summary>
    /// Tests that CompressString with a small string returns compressed bytes.
    /// </summary>
    [Fact]
    public void CompressString_WithSmallString_ReturnsCompressedBytes()
    {
        var data = "Hello, World!";
        var compressed = CompressionUtil.CompressString(data);

        compressed.Should().NotBeNull();
        compressed.Length.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that CompressString with a large repetitive string achieves compression.
    /// </summary>
    [Fact]
    public void CompressString_WithLargeRepetitiveString_AchievesCompression()
    {
        var data = string.Concat(Enumerable.Repeat("This is a long repeating string for compression test. ", 100));
        var compressed = CompressionUtil.CompressString(data);

        compressed.Length.Should().BeLessThan(data.Length);
    }

    /// <summary>
    /// Tests that CompressString with an empty string returns compressed bytes.
    /// </summary>
    [Fact]
    public void CompressString_WithEmptyString_ReturnsCompressedBytes()
    {
        var data = "";
        var compressed = CompressionUtil.CompressString(data);

        compressed.Should().NotBeNull();
        compressed.Length.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that DecompressString after Compress returns the original data.
    /// </summary>
    [Fact]
    public void DecompressString_AfterCompress_ReturnsOriginalData()
    {
        var original = "The quick brown fox jumps over the lazy dog";
        var compressed = CompressionUtil.CompressString(original);
        var decompressed = CompressionUtil.DecompressString(compressed);

        decompressed.Should().Be(original);
    }

    /// <summary>
    /// Tests that CompressBytes with a byte array returns compressed data.
    /// </summary>
    [Fact]
    public void CompressBytes_WithByteArray_ReturnsCompressedData()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("Test data");
        var compressed = CompressionUtil.CompressBytes(data);

        compressed.Should().NotBeNull();
        compressed.Length.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that CompressBytes with a span returns compressed data.
    /// </summary>
    [Fact]
    public void CompressBytes_WithSpan_ReturnsCompressedData()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("Test span data");
        var compressed = CompressionUtil.CompressBytes(data.AsSpan());

        compressed.Should().NotBeNull();
        compressed.Length.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that DecompressBytes after Compress returns the original data.
    /// </summary>
    [Fact]
    public void DecompressBytes_AfterCompress_ReturnsOriginalData()
    {
        var original = System.Text.Encoding.UTF8.GetBytes("Original byte array data");
        var compressed = CompressionUtil.CompressBytes(original);
        var decompressed = CompressionUtil.DecompressBytes(compressed);

        decompressed.Should().Equal(original);
    }

    /// <summary>
    /// Tests that CompressAndDecompress with a Unicode string preserves characters.
    /// </summary>
    [Fact]
    public void CompressAndDecompress_WithUnicodeString_PreservesCharacters()
    {
        var original = "Hello 世界 مرحبا мир 🚀";
        var compressed = CompressionUtil.CompressString(original);
        var decompressed = CompressionUtil.DecompressString(compressed);

        decompressed.Should().Be(original);
    }

    /// <summary>
    /// Tests that CompressAndDecompress with a multiline string preserves line breaks.
    /// </summary>
    [Fact]
    public void CompressAndDecompress_WithMultilineString_PreservesLineBreaks()
    {
        var original = "Line 1\nLine 2\r\nLine 3\rLine 4";
        var compressed = CompressionUtil.CompressString(original);
        var decompressed = CompressionUtil.DecompressString(compressed);

        decompressed.Should().Be(original);
    }

    /// <summary>
    /// Tests that GetCompressionRatio with compressed data returns a percentage.
    /// </summary>
    [Fact]
    public void GetCompressionRatio_WithCompressedData_ReturnsPercentage()
    {
        var original = "A very long string that should compress well!";
        var compressed = CompressionUtil.CompressString(original);

        var ratio = CompressionUtil.GetCompressionRatio(original.Length, compressed.Length);

        ratio.Should().BeGreaterThan(0);
        ratio.Should().BeLessThanOrEqualTo(100);
    }

    /// <summary>
    /// Tests that GetCompressionRatio with zero original size returns zero.
    /// </summary>
    [Fact]
    public void GetCompressionRatio_WithZeroOriginalSize_ReturnsZero()
    {
        var ratio = CompressionUtil.GetCompressionRatio(0, 100);
        ratio.Should().Be(0);
    }

    /// <summary>
    /// Tests that GetCompressionRatio with identical sizes returns 100.
    /// </summary>
    [Fact]
    public void GetCompressionRatio_WithIdenticalSizes_ReturnsOneHundred()
    {
        var ratio = CompressionUtil.GetCompressionRatio(100, 100);
        ratio.Should().Be(100);
    }

    /// <summary>
    /// Tests that IsCompressionWorthwhile with high compression returns true.
    /// </summary>
    [Fact]
    public void IsCompressionWorthwhile_WithHighCompression_ReturnsTrue()
    {
        var repetitiveData = string.Concat(Enumerable.Repeat("A", 1000));
        var compressed = CompressionUtil.CompressString(repetitiveData);

        var worthwhile = CompressionUtil.IsCompressionWorthwhile(
            repetitiveData.Length, compressed.Length, minSavingsPercent: 10);

        worthwhile.Should().BeTrue();
    }

    /// <summary>
    /// Tests that IsCompressionWorthwhile with low compression returns false.
    /// </summary>
    [Fact]
    public void IsCompressionWorthwhile_WithLowCompression_ReturnsFalse()
    {
        var data = "ABC";
        var compressed = CompressionUtil.CompressString(data);

        var worthwhile = CompressionUtil.IsCompressionWorthwhile(
            data.Length, compressed.Length, minSavingsPercent: 50);

        worthwhile.Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsCompressionWorthwhile with exact threshold returns true.
    /// </summary>
    [Fact]
    public void IsCompressionWorthwhile_WithExactThreshold_ReturnsTrue()
    {
        var original = 100;
        var compressed = 90; // Exactly 10% savings

        var worthwhile = CompressionUtil.IsCompressionWorthwhile(original, compressed, minSavingsPercent: 10);

        worthwhile.Should().BeTrue();
    }

    /// <summary>
    /// Tests that CompressBytes with an empty array returns compressed data.
    /// </summary>
    [Fact]
    public void CompressBytes_WithEmptyArray_ReturnsCompressedData()
    {
        var data = Array.Empty<byte>();
        var compressed = CompressionUtil.CompressBytes(data);

        compressed.Should().NotBeNull();
        compressed.Length.Should().BeGreaterThan(0);

        var decompressed = CompressionUtil.DecompressBytes(compressed);
        decompressed.Should().Equal(data);
    }

    /// <summary>
    /// Tests that CompressString with multiple compressions are consistent.
    /// </summary>
    [Fact]
    public void CompressString_MultipleCompressions_AreConsistent()
    {
        var data = "Consistency test string";
        var compressed1 = CompressionUtil.CompressString(data);
        var compressed2 = CompressionUtil.CompressString(data);

        var decompressed1 = CompressionUtil.DecompressString(compressed1);
        var decompressed2 = CompressionUtil.DecompressString(compressed2);

        decompressed1.Should().Be(decompressed2);
        decompressed1.Should().Be(data);
    }

    /// <summary>
    /// Tests that DecompressString with large data successfully decompresses.
    /// </summary>
    [Fact]
    public void DecompressString_WithLargeData_SuccessfullyDecompresses()
    {
        var largeData = string.Concat(Enumerable.Repeat("This is test data for large compression. ", 500));
        var compressed = CompressionUtil.CompressString(largeData);
        var decompressed = CompressionUtil.DecompressString(compressed);

        decompressed.Should().Be(largeData);
        decompressed.Length.Should().Be(largeData.Length);
    }
}
