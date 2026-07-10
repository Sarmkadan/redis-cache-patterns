# CompressionUtilTests

Unit test suite for `CompressionUtil` that validates compression and decompression behavior for strings and byte arrays, including edge cases and consistency checks. Tests cover compression ratios, threshold-based decisions, and preservation of data integrity across various input types and sizes.

## API

### `CompressionUtilTests.CompressString_WithSmallString_ReturnsCompressedBytes()`
Validates that compressing a small string (< 100 bytes) produces a non-empty byte array. Does not assert compression ratio, only that the operation completes successfully.

### `CompressionUtilTests.CompressString_WithLargeRepetitiveString_AchievesCompression()`
Ensures that compressing a string with high repetition (e.g., `"aaaaaaaaaa"`) results in a compressed output smaller than the original UTF-8 byte representation. Throws if compression fails or does not reduce size.

### `CompressionUtilTests.CompressString_WithEmptyString_ReturnsCompressedBytes()`
Confirms that compressing an empty string returns a valid (non-null) compressed byte array. Does not require the compressed output to be smaller.

### `CompressionUtilTests.DecompressString_AfterCompress_ReturnsOriginalData()`
Compresses a sample string, then decompresses the result and asserts that the decompressed string matches the original input exactly. Throws if decompression fails or output mismatches.

### `CompressionUtilTests.CompressBytes_WithByteArray_ReturnsCompressedData()`
Validates that compressing a byte array via `byte[]` input produces a non-empty compressed byte array. No assumptions about compression ratio are made.

### `CompressionUtilTests.CompressBytes_WithSpan_ReturnsCompressedData()`
Ensures that compressing a byte array via `ReadOnlySpan<byte>` input produces a valid compressed byte array. Tests overload compatibility with span-based APIs.

### `CompressionUtilTests.DecompressBytes_AfterCompress_ReturnsOriginalData()`
Compresses a byte array, then decompresses it and asserts that the decompressed bytes match the original input exactly. Throws on mismatch or failure.

### `CompressionUtilTests.CompressAndDecompress_WithUnicodeString_PreservesCharacters()`
Tests round-trip compression and decompression of a Unicode string containing non-ASCII characters (e.g., emoji, accented letters). Asserts exact character preservation.

### `CompressionUtilTests.CompressAndDecompress_WithMultilineString_PreservesLineBreaks()`
Ensures that strings containing `\r\n` or `\n` line endings are preserved exactly after compression and decompression. Validates newline integrity across platforms.

### `CompressionUtilTests.GetCompressionRatio_WithCompressedData_ReturnsPercentage()`
Computes the compression ratio (original size / compressed size) as a percentage. Returns a value between 0 and 100. Throws if compressed size exceeds original size unexpectedly.

### `CompressionUtilTests.GetCompressionRatio_WithZeroOriginalSize_ReturnsZero()`
Handles edge case where original data size is zero. Returns `0` without throwing.

### `CompressionUtilTests.GetCompressionRatio_WithIdenticalSizes_ReturnsOneHundred()`
When compressed size equals original size, returns `100`. Used to validate no-op or failed compression scenarios.

### `CompressionUtilTests.IsCompressionWorthwhile_WithHighCompression_ReturnsTrue()`
Evaluates whether compression is beneficial based on a configurable threshold (e.g., 20% size reduction). Returns `true` when savings are significant.

### `CompressionUtilTests.IsCompressionWorthwhile_WithLowCompression_ReturnsFalse()`
Returns `false` when compression yields minimal or no size reduction, indicating that compression may not be beneficial.

### `CompressionUtilTests.IsCompressionWorthwhile_WithExactThreshold_ReturnsTrue()`
Tests boundary condition where compression achieves exactly the configured threshold (e.g., 20%). Returns `true` when equal to threshold.

### `CompressionUtilTests.CompressBytes_WithEmptyArray_ReturnsCompressedData()`
Ensures that compressing an empty byte array produces a valid compressed output (non-null, possibly zero-length). Validates graceful handling of zero-length inputs.

### `CompressionUtilTests.CompressString_MultipleCompressions_AreConsistent()`
Compresses the same string multiple times and asserts that all compressed outputs are identical. Validates deterministic behavior across runs.

### `CompressionUtilTests.DecompressString_WithLargeData_SuccessfullyDecompresses()`
Tests decompression of a large compressed string (> 1MB) to ensure no out-of-memory or timeout issues. Validates successful round-trip.

## Usage
