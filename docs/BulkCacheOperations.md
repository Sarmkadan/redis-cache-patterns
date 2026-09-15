# BulkCacheOperations

Contains request and response models for bulk cache operations enabling efficient retrieval and storage of multiple cache entries in a single round-trip to Redis.

## API

### BulkGetRequest

| Member | Type | Purpose | Remarks |
|--------|------|---------|---------|
| `Keys` | `List<string>` | The collection of cache keys to retrieve. | Must not be `null` when the request is executed; an empty list results in no operations. |
| `ReturnNullForMissing` | `bool` | Indicates whether missing keys should yield a `null` value in the results (`true`) or be omitted (`false`). | Defaults to `false`. |

### BulkGetResult<T>

| Member | Type | Purpose | Remarks |
|--------|------|---------|---------|
| `Key` | `string` | Cache key | |
| `Value` | `T?` | Retrieved value, or null if key not found | |
| `Found` | `bool` | Whether the key was found in cache | |
| `Error` | `string?` | Error message if operation failed for this key | |

### BulkGetResponse<T>

| Member | Type | Purpose | Remarks |
|--------|------|---------|---------|
| `Success` | `bool` | Whether the bulk operation succeeded | Defaults to `true`. |
| `Results` | `List<BulkGetResult<T>>` | Individual results for each requested key | |
| `TotalKeys` | `int` | Total number of keys requested | |
| `RetrievedCount` | `int` | Number of keys successfully retrieved | |
| `NotFoundCount` | `int` | Number of keys not found in cache | |
| `FailedCount` | `int` | Number of keys that failed to retrieve | |

### BulkSetRequest<T>

| Member | Type | Purpose | Remarks |
|--------|------|---------|---------|
| `Entries` | `List<CacheEntry>` | List of cache entries to set | |
| `DefaultExpiration` | `TimeSpan` | Default TTL to apply if not specified in individual entries | Defaults to 30 minutes. |

### BulkSetResult

| Member | Type | Purpose | Remarks |
|--------|------|---------|---------|
| `Key` | `string` | Cache key that was set | |
| `Success` | `bool` | Whether the set operation succeeded | |
| `Error` | `string?` | Error message if operation failed | |
| `SizeBytes` | `long` | Size of the cached value in bytes | |

### BulkSetResponse

| Member | Type | Purpose | Remarks |
|--------|------|---------|---------|
| `Success` | `bool` | Whether the bulk operation succeeded | Defaults to `true`. |
| `Results` | `List<BulkSetResult>` | Individual results for each entry | |
| `TotalEntries` | `int` | Total number of entries attempted | |
| `SuccessCount` | `int` | Number of entries successfully set | |
| `FailedCount` | `int` | Number of entries that failed to set | |
| `TotalSizeBytes` | `long` | Total size of all successfully cached entries in bytes | |

## Usage

### Basic bulk get

```csharp
var request = new BulkGetRequest<string>
{
    Keys = new List<string> { "user:1000:profile", "user:1001:profile", "user:1002:profile" },
    ReturnNullForMissing = true
};

var response = await cache.BulkGetAsync(request);

if (response.Success)
{
    foreach (var result in response.Results)
    {
        if (result.Found)
        {
            Console.WriteLine($"{result.Key}: {result.Value}");
        }
        else
        {
            Console.WriteLine($"{result.Key}: missing");
        }
    }
}
else
{
    Console.Error.WriteLine($"Bulk get failed: {response.Error}");
}
```

### Basic bulk set

```csharp
var request = new BulkSetRequest<string>
{
    DefaultExpiration = TimeSpan.FromHours(1),
    Entries = new List<CacheEntry>
    {
        new CacheEntry("user:1000:profile", "json", Encoding.UTF8.GetBytes("{\"id\":1000,\"name\":\"John\"}")),
        new CacheEntry("user:1001:profile", "json", Encoding.UTF8.GetBytes("{\"id\":1001,\"name\":\"Jane\"}")),
        new CacheEntry("user:1002:profile", "json", Encoding.UTF8.GetBytes("{\"id\":1002,\"name\":\"Bob\"}"))
    }
};

var response = await cache.BulkSetAsync(request);

if (response.Success)
{
    Console.WriteLine($"Successfully cached {response.SuccessCount} entries");
    Console.WriteLine($"Total size: {response.TotalSizeBytes} bytes");
}
else
{
    Console.Error.WriteLine($"Bulk set failed: {response.Error}");
}
```

### Mixed bulk operations

```csharp
// Perform bulk get and set in sequence for cache warming
var getRequest = new BulkGetRequest<string>
{
    Keys = new List<string> { "product:1001", "product:1002", "product:1003" }
};

var getResponse = await cache.BulkGetAsync(getRequest);

if (getResponse.Success)
{
    var setRequest = new BulkSetRequest<string>
    {
        DefaultExpiration = TimeSpan.FromMinutes(30),
        Entries = new List<CacheEntry>()
    };

    // Only set entries that were found (cache warming pattern)
    foreach (var result in getResponse.Results)
    {
        if (result.Found && result.Value != null)
        {
            var entry = new CacheEntry(
                result.Key, 
                "string", 
                Encoding.UTF8.GetBytes(result.Value.ToString()!)
            );
            setRequest.Entries.Add(entry);
        }
    }

    if (setRequest.Entries.Count > 0)
    {
        var setResponse = await cache.BulkSetAsync(setRequest);
        // Handle set response...
    }
}
```

## Notes

- Bulk operations significantly reduce network round-trips compared to individual get/set calls, improving performance especially in high-latency environments.
- All numeric counters (`TotalKeys`, `RetrievedCount`, `NotFoundCount`, `FailedCount`, etc.) are updated atomically by the cache client after the operation completes.
- The types are not thread-safe for concurrent modifications. Multiple threads should not alter properties on the same instance while a bulk operation is in progress. Immutable usage (configure once, then invoke) is safe.
- For `BulkGetRequest`, setting `ReturnNullForMissing = true` ensures that all requested keys appear in the results (with `Found = false` and `Value = null` for missing keys), while `false` omits missing keys entirely from results.
- Error handling in bulk operations is per-item; a failure in one key does not cause the entire operation to fail unless the underlying Redis operation itself throws an exception.