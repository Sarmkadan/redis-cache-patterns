# BulkGetRequest

Represents a request to retrieve multiple keys from the Redis cache in a single operation. The type encapsulates the list of keys to fetch, options for handling missing values, and the resulting cache entries or error information.

## API

| Member | Type | Purpose | Remarks |
|--------|------|---------|---------|
| `Keys` | `List<string>` | The collection of cache keys to retrieve. | Must not be `null` when the request is executed; an empty list results in no operations. |
| `ReturnNullForMissing` | `bool` | Indicates whether missing keys should yield a `null` value in the results (`true`) or be omitted (`false`). | Defaults to `false`. |
| `Key` | `string` | A single key convenience property; when set, overrides `Keys` to contain only this key. | Setting this property clears any existing entries in `Keys` and adds the provided key. |
| `Value` | `T?` | Placeholder for a value associated with the request; used internally during deserialization. | Not intended for direct user assignment. |
| `Found` | `bool` | Indicates whether at least one key was successfully retrieved from the cache. | Set after the request is processed. |
| `Error` | `string?` | Contains an error message if the bulk operation failed; otherwise `null`. | Check alongside `Success` to diagnose failures. |
| `Success` | `bool` | Indicates whether the bulk get operation completed without throwing an exception. | `false` when `Error` is non‑null. |
| `Results` | `List<BulkGetResult<T>>` | Detailed outcome for each key, including value, flags, and per‑key errors. | Populated only when `Success` is `true`. |
| `TotalKeys` | `int` | Total number of keys requested (size of `Keys` after any `Key` assignment). | Read‑only; computed from `Keys`. |
| `RetrievedCount` | `int` | Number of keys for which a value was successfully returned. | Updated after processing. |
| `NotFoundCount` | `int` | Number of keys that were not present in the cache. | Updated after processing; respects `ReturnNullForMissing`. |
| `FailedCount` | `int` | Number of keys that caused an error during retrieval (e.g., serialization failure). | Updated after processing. |
| `Entries` | `List<CacheEntry>` | Raw cache entries retrieved from Redis, including metadata such as expiration. | Used internally; may be empty if no entries were read. |
| `DefaultExpiration` | `TimeSpan` | The expiration time to apply to any missing keys when using write‑through patterns. | Ignored for pure get operations; relevant when the request is part of a combined get‑or‑set flow. |
| `SizeBytes` | `long` | Approximate total size in bytes of the retrieved values. | Calculated after deserialization; `0` if nothing retrieved. |

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

### Single‑key shortcut

```csharp
var request = new BulkGetRequest<int> { Key = "counter:visits" };

var response = await cache.BulkGetAsync(request);

if (response.Success && response.Results[0].Found)
{
    int count = response.Results[0].Value ?? 0;
    // Use the counter value...
}
```

## Notes

- The `Keys` property must be initialized before execution; assigning `null` will cause an `ArgumentNullException` in the underlying cache implementation.
- Setting `Key` after `Keys` has been populated replaces the entire list with a single‑item list containing the provided key.
- `ReturnNullForMissing` only affects the `Found` flag and presence of a value in `BulkGetResult<T>` when a key is absent; it does not change `NotFoundCount`.
- All numeric counters (`TotalKeys`, `RetrievedCount`, `NotFoundCount`, `FailedCount`, `SizeBytes`) are updated atomically by the cache client after the operation completes; reading them before the request finishes yields stale or default values.
- The type is not thread‑safe for concurrent modifications. Multiple threads should not alter `Keys`, `ReturnNullForMissing`, or `Key` on the same instance while a bulk operation is in progress. Immutable usage (configure once, then invoke) is safe.
