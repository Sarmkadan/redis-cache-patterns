# CacheAsideExample

Implements the Cache-Aside pattern for product data retrieval using Redis as the distributed cache layer. This class coordinates lookups against a cache store and a fallback database, handles cache population on misses, and provides explicit cache-refresh semantics to keep cached representations aligned with the authoritative data source.

## API

### `CacheAsideExample`

Constructor. Initializes a new instance wired to the underlying Redis multiplexer and product repository. The constructor is responsible for establishing the connection and injecting the required data-access dependency; no public configuration parameters are exposed on the type itself.

### `async Task<Product?> GetProductWithCacheAsideAsync`

Retrieves a single product by its identifier using the Cache-Aside strategy.

- **Purpose**: Checks Redis for a cached serialized product. On a hit, deserializes and returns it immediately. On a miss, queries the database, writes the result into the cache with an appropriate TTL, and returns the product. Returns `null` when the product does not exist in the database.
- **Parameters**: A product identifier (typically a string or integer key, depending on the internal signature).
- **Returns**: `Task<Product?>` — the product instance if found; otherwise `null`.
- **Throws**: May surface `RedisConnectionException` or `RedisTimeoutException` when the cache is unreachable. Database-level exceptions (e.g., `SqlException`) propagate if the fallback query fails.

### `async Task DemonstrateCacheHitsAsync`

Executes a controlled demonstration that illustrates cache-hit and cache-miss behaviour for the Cache-Aside flow.

- **Purpose**: Performs repeated lookups for the same product key, logging or outputting timing and source information (cache vs. database) so callers can observe the latency difference between a cold miss and a warm hit.
- **Parameters**: None exposed publicly; the demonstration operates against a predetermined product key or keys.
- **Returns**: `Task` representing the completed demonstration.
- **Throws**: Same cache and database exceptions as `GetProductWithCacheAsideAsync`; additionally may throw if the demonstration’s internal assumptions about key existence are violated.

### `async Task<List<Product>> GetProductsByCacheAsideAsync`

Bulk variant that retrieves multiple products using the Cache-Aside pattern.

- **Purpose**: Accepts a collection of product identifiers, checks the cache for each, and for any misses batches a database query to fetch the missing products. Populates the cache for the newly fetched items and returns the combined result set in the original request order.
- **Parameters**: A collection of product identifiers.
- **Returns**: `Task<List<Product>>` — a list of product instances. Positions corresponding to identifiers that do not exist in the database will contain `null` entries (or the list may omit them, depending on the internal implementation).
- **Throws**: Aggregate-style failures are possible if the underlying batch database query throws; individual cache operations may fail independently and should be handled gracefully by the implementation.

### `async Task<Product?> GetProductWithRefreshAsync`

Retrieves a product while forcing a cache refresh from the database, regardless of whether a cached entry already exists.

- **Purpose**: Bypasses the cache-hit fast path. Always queries the authoritative database, overwrites the cached representation with the fresh result (or removes the cache key if the product no longer exists), and returns the current state. Useful when the caller knows the data may be stale or after a known write.
- **Parameters**: A product identifier.
- **Returns**: `Task<Product?>` — the current product from the database, or `null` if absent.
- **Throws**: Same cache and database exception surface as `GetProductWithCacheAsideAsync`. A cache-write failure after a successful database read may result in a stale cache on subsequent calls but does not prevent the fresh product from being returned.

## Usage

### Example 1: Single Product with Transparent Caching

```csharp
var cacheAside = new CacheAsideExample(redisConnection, productRepository);

// First call — cache miss, hits database, populates cache.
Product? product = await cacheAside.GetProductWithCacheAsideAsync("prod:42");
Console.WriteLine(product?.Name);

// Second call — cache hit, returns deserialized copy without touching database.
Product? cachedProduct = await cacheAside.GetProductWithCacheAsideAsync("prod:42");
Console.WriteLine(cachedProduct?.Name);
```

### Example 2: Bulk Fetch Followed by Forced Refresh

```csharp
var cacheAside = new CacheAsideExample(redisConnection, productRepository);
var ids = new List<string> { "prod:10", "prod:20", "prod:30" };

// Bulk Cache-Aside: cold start fetches missing products from DB, warms cache.
List<Product> products = await cacheAside.GetProductsByCacheAsideAsync(ids);
foreach (var p in products.Where(p => p is not null))
{
    Console.WriteLine($"{p.Id}: {p.Name}");
}

// Later, after an inventory update, force-refresh a specific product.
Product? refreshed = await cacheAside.GetProductWithRefreshAsync("prod:20");
Console.WriteLine($"Refreshed stock: {refreshed?.Stock}");
```

## Notes

- **Cache invalidation responsibility**: This class implements the read side of Cache-Aside. It does not automatically invalidate or update cached entries on writes; the caller must either use `GetProductWithRefreshAsync` after a mutation or explicitly remove keys from Redis.
- **Stale data window**: Between a database write and a subsequent refresh, `GetProductWithCacheAsideAsync` may return a stale cached copy. The TTL governs the maximum staleness, but applications requiring strong consistency should call `GetProductWithRefreshAsync` or perform cache deletion at write time.
- **Thread safety**: The instance is safe for concurrent use provided the injected Redis multiplexer and repository are themselves thread-safe (which is the case for standard `StackExchange.Redis` connections and typical EF Core / ADO.NET repositories). The class holds no mutable shared state beyond these dependencies.
- **Null handling**: When a product does not exist in the database, `GetProductWithCacheAsideAsync` and `GetProductWithRefreshAsync` return `null`. The implementation may cache a sentinel value to avoid repeated database lookups for known-missing keys; callers should not assume that every `null` return corresponds to a fresh database check.
- **Bulk partial failures**: `GetProductsByCacheAsideAsync` may encounter transient Redis errors for a subset of keys while the database query succeeds. The implementation should return whatever products were obtainable; callers should not rely on perfect cache population for every item in the batch.
- **Refresh semantics**: `GetProductWithRefreshAsync` always queries the database, even if the cache holds a recent copy. This increases load on the database and should be used sparingly, typically in response to a known update event rather than in the hot path.
