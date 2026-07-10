# OperationResult

A set of result types used to encapsulate the outcome of operations, especially those interacting with Redis or other cache layers. The types provide success/failure state, diagnostic information, and optionally typed data or paged results. They are designed to simplify error handling and logging in cache-heavy workflows.

## API

### `public bool Success`
Indicates whether the operation completed successfully. `true` when the operation succeeded; `false` when it failed.

### `public string? Message`
A human-readable message describing the outcome (e.g., "Cache miss", "Value deserialized"). May be `null` for success or when no message is relevant.

### `public string? ErrorCode`
A machine-readable error identifier (e.g., "CACHE_MISS", "DESERIALIZE_FAIL"). `null` when the operation succeeded.

### `public DateTime Timestamp`
The UTC time when the operation result was created.

### `public static OperationResult Ok()`
Creates a successful result with default values: `Success = true`, `Message = null`, `ErrorCode = null`, and `Timestamp = DateTime.UtcNow`.

### `public static OperationResult Fail(string? message = null, string? errorCode = null)`
Creates a failed result with `Success = false`, the provided `message` and `errorCode`, and `Timestamp = DateTime.UtcNow`.

### `public T? Data`
Gets or sets the typed payload of the operation. `null` when no data is present.

### `public static OperationResult<T> Ok(T? data = null, string? message = null)`
Creates a successful typed result with `Success = true`, `Data = data`, `Message = message`, `ErrorCode = null`, and `Timestamp = DateTime.UtcNow`.

### `public new static OperationResult<T> Fail(string? message = null, string? errorCode = null)`
Creates a failed typed result with `Success = false`, the provided `message` and `errorCode`, `Data = default`, and `Timestamp = DateTime.UtcNow`.

### `public IEnumerable<T> Items`
Gets or sets the collection of items returned by a paged operation. Empty when no items are present.

### `public int PageNumber`
Gets or sets the 1-based page number of the paged result. Defaults to `1`.

### `public int PageSize`
Gets or sets the number of items per page. Defaults to `0` (indicating no paging).

### `public int TotalCount`
Gets or sets the total number of items available across all pages. Defaults to `0`.

### `public static PagedResult<T> Create(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount, string? message = null)`
Creates a paged result with the provided `items`, `pageNumber`, `pageSize`, `totalCount`, and optional `message`. Sets `Success = true`, `ErrorCode = null`, and `Timestamp = DateTime.UtcNow`.

### `public T? Data`
(In `CacheOperationResult<T>`) Gets or sets the typed payload retrieved from cache.

### `public bool CacheHit`
Indicates whether the data was retrieved from cache (`true`) or freshly computed (`false`).

### `public long ElapsedMilliseconds`
The duration of the cache operation in milliseconds, measured from start to completion.

### `public string Source`
Identifies the cache source (e.g., "Redis", "MemoryCache") that fulfilled the request.

### `public DateTime ExecutedAt`
The UTC time when the cache operation was executed.

### `public static CacheOperationResult<T> FromCache(T data, string source, long elapsedMilliseconds, bool cacheHit = true, string? message = null)`
Creates a cache operation result with `Success = true`, `Data = data`, `Source = source`, `ElapsedMilliseconds = elapsedMilliseconds`, `CacheHit = cacheHit`, `Message = message`, `ErrorCode = null`, and `Timestamp = DateTime.UtcNow`.

## Usage
