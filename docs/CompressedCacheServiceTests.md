# CompressedCacheServiceTests

Unit test suite for `CompressedCacheService`, verifying compression behavior, cache operations, and integration with the underlying cache implementation. Tests cover scenarios where values are compressed or uncompressed, cache misses, expiration policies, and delegated operations.

## API

### Methods

#### `GetAsync_WhenCachedValueIsNotCompressed_ReturnsParsedValue`
Ensures that values stored without compression are returned as-is after deserialization. No parameters. Returns the deserialized value. Does not throw under normal test conditions.

#### `GetAsync_WhenCachedValueIsCompressed_DecompressesAndReturnsValue`
Verifies that values stored with compression are correctly decompressed upon retrieval. No parameters. Returns the decompressed and deserialized value. Fails if decompression or deserialization fails.

#### `GetAsync_WhenKeyDoesNotExist_ReturnsNull`
Confirms that querying a non-existent key returns `null`. No parameters. Returns `null`. Does not throw.

#### `SetAsync_WithSmallValue_DoesNotCompress`
Validates that small values are stored without compression to avoid overhead. No parameters. No return value. Fails if compression is incorrectly applied.

#### `SetAsync_WithLargeValue_CompressesBeforeCaching`
Ensures that large values are compressed before storage to reduce memory usage. No parameters. No return value. Fails if compression is omitted or fails.

#### `GetOrLoadAsync_WhenCacheHit_ReturnsValueWithoutLoadingFromSource`
Tests that cached values are returned directly without invoking the load function on cache hits. No parameters. Returns the cached value. Fails if the load function is incorrectly invoked.

#### `GetOrLoadAsync_OnCacheMiss_LoadsAndCaches`
Validates that the load function is invoked and the result is cached on cache misses. No parameters. Returns the loaded value. Fails if the value is not cached or the load function is not called.

#### `GetOrLoadAsync_WhenLoadFnReturnsNull_DoesNotCache`
Ensures that `null` results from the load function are not cached. No parameters. Returns `null`. Fails if a `null` value is cached.

#### `SetAsync_WithExpiration_PassesExpirationToInnerCache`
Confirms that expiration settings are forwarded to the underlying cache. No parameters. No return value. Fails if the expiration is not propagated.

#### `WriteAsync_DelegatesRequestToInnerCache`
Verifies that write operations are forwarded to the inner cache implementation. No parameters. No return value. Fails if the inner cache is not invoked.

#### `GetOrLoadWithSlidingExpirationAsync_DelegatesRequestToInnerCache`
Ensures that sliding expiration cache operations are delegated to the inner cache. No parameters. Returns the cached or loaded value. Fails if the inner cache is not invoked.

#### `GetOrLoadWithEarlyExpirationAsync_DelegatesRequestToInnerCache`
Validates that early expiration cache operations are delegated to the inner cache. No parameters. Returns the cached or loaded value. Fails if the inner cache is not invoked.

#### `RemoveAsync_DelegatesRequestToInnerCache`
Confirms that key removal is delegated to the inner cache. No parameters. No return value. Fails if the inner cache is not invoked.

#### `RemoveByPatternAsync_DelegatesRequestToInnerCache`
Validates that pattern-based removal is delegated to the inner cache. No parameters. No return value. Fails if the inner cache is not invoked.

#### `ExistsAsync_DelegatesRequestToInnerCache`
Ensures that key existence checks are delegated to the inner cache. No parameters. Returns a boolean indicating existence. Fails if the inner cache is not invoked.

#### `GetExpirationAsync_DelegatesRequestToInnerCache`
Validates that expiration retrieval is delegated to the inner cache. No parameters. Returns the expiration time. Fails if the inner cache is not invoked.

#### `GetKeysByPatternAsync_DelegatesRequestToInnerCache`
Ensures that pattern-based key enumeration is delegated to the inner cache. No parameters. Returns a collection of matching keys. Fails if the inner cache is not invoked.

#### `FlushAsync_DelegatesRequestToInnerCache`
Confirms that cache flush operations are delegated to the inner cache. No parameters. No return value. Fails if the inner cache is not invoked.

### Properties

#### `Id`
Gets the unique identifier for the test instance. Read-only. Returns an integer. Value is set during test initialization.

#### `Name`
Gets the optional name of the test instance. Read-only. Returns a nullable string. Value is set during test initialization.

## Usage
