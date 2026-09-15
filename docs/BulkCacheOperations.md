# Bulk Cache Operations

Documentation for the `BulkCacheOperations` domain models, which handle request and response structures for bulk cache retrieval and storage operations.

## Classes

### `BulkGetRequest`
Request model for bulk get operations.

| Property | Type | Description |
|----------|------|-------------|
| `Keys` | `List<string>` | List of cache keys to retrieve |
| `ReturnNullForMissing` | `bool` | Whether to return null for missing keys or omit them from response |

### `BulkGetResult<T>`
Response model for individual bulk get operation result.

| Property | Type | Description |
|----------|------|-------------|
| `Key` | `string` | Cache key |
| `Value` | `T?` | Retrieved value, or null if key not found |
| `Found` | `bool` | Whether the key was found in cache |
| `Error` | `string?` | Error message if operation failed for this key |

### `BulkGetResponse<T>`
Response model for bulk get operations.

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether the bulk operation succeeded |
| `Results` | `List<BulkGetResult<T>>` | Individual results for each requested key |
| `TotalKeys` | `int` | Total number of keys requested |
| `RetrievedCount` | `int` | Number of keys successfully retrieved |
| `NotFoundCount` | `int` | Number of keys not found in cache |
| `FailedCount` | `int` | Number of keys that failed to retrieve |

### `BulkSetRequest<T>`
Request model for bulk set operations.

| Property | Type | Description |
|----------|------|-------------|
| `Entries` | `List<CacheEntry>` | List of cache entries to set |
| `DefaultExpiration` | `TimeSpan` | Default TTL to apply if not specified in individual entries |

### `BulkSetResult`
Response model for individual bulk set operation result.

| Property | Type | Description |
|----------|------|-------------|
| `Key` | `string` | Cache key that was set |
| `Success` | `bool` | Whether the set operation succeeded |
| `Error` | `string?` | Error message if operation failed |
| `SizeBytes` | `long` | Size of the cached value in bytes |

### `BulkSetResponse`
Response model for bulk set operations.

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether the bulk operation succeeded |
| `Results` | `List<BulkSetResult>` | Individual results for each entry |
| `TotalEntries` | `int` | Total number of entries attempted |
| `SuccessCount` | `int` | Number of entries successfully set |
| `FailedCount` | `int` | Number of entries that failed to set |
| `TotalSizeBytes` | `long` | Total size of all successfully cached entries in bytes |
