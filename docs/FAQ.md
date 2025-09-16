# Frequently Asked Questions

## Installation & Setup

**Q: What are the system requirements?**

A: You need .NET 10 SDK or later and Redis 7.0 or later. Optional: Docker for containerized development.

**Q: Can I use .NET 8 or 9?**

A: Not recommended. The project targets .NET 10 and uses latest C# features. However, you can modify the .csproj to target an earlier version at your own risk.

**Q: How do I install Redis?**

A: 
- **macOS**: `brew install redis`
- **Ubuntu**: `sudo apt-get install redis-server`
- **Windows**: Download from https://github.com/microsoftarchive/redis/releases
- **Docker**: `docker run -d -p 6379:6379 redis:7-alpine`

**Q: Where do I get the connection string?**

A: For local Redis: `localhost:6379`. For production, it depends on your deployment (Azure, AWS, Redis Cloud, etc.). Always store in configuration/secrets manager, never in code.

## Caching Strategies

**Q: Which caching pattern should I use?**

A: 
- **Cache-Aside**: Most common, read-heavy workloads
- **Write-Through**: Need strong consistency
- **Write-Behind**: High write volume (not implemented)

**Q: What's the difference between cache invalidation and expiration?**

A: 
- **Expiration**: Automatic removal after TTL expires
- **Invalidation**: Immediate removal or pattern-based removal

Use expiration for most cases, invalidation when data changes.

**Q: How long should my cache TTL be?**

A: Depends on data freshness requirements:
- Frequently changing data: 5-15 minutes
- Stable data: 1-4 hours
- Configuration: 24 hours
- Static data: Higher (infinite)

**Q: Should I cache null values?**

A: Yes, but with shorter TTL (e.g., 5 minutes) to avoid "thundering herd" if data becomes available later.

```csharp
var cached = await _cache.GetAsync<Product>(key);
if (cached != null) return cached;

var product = await _repository.GetByIdAsync(id);
if (product != null)
{
    // Cache hit
    await _cache.SetAsync(key, product, TimeSpan.FromHours(1));
}
else
{
    // Cache miss - cache null briefly
    await _cache.SetAsync(key, (Product?)null, TimeSpan.FromMinutes(5));
}
return product;
```

## Performance

**Q: How much faster is caching?**

A: Typically 10-100x faster for cache hits:
- Database query: 50-500ms
- Redis hit: 1-5ms
- Network latency: 1-2ms

**Q: Is serialization a bottleneck?**

A: Usually not. System.Text.Json is fast. If it is, consider:
- Caching serialized strings instead
- Using compression
- Reducing object complexity

**Q: How do I improve cache hit rate?**

A:
1. Increase TTL for stable data
2. Implement cache warming
3. Use appropriate key patterns
4. Monitor hit rate with metrics

```csharp
// Monitor
var hitRate = await _metricsCollector.GetHitRateAsync();
Console.WriteLine($"Hit rate: {hitRate:P}");
```

**Q: When should I enable compression?**

A: Enable when:
- Values exceed 1KB
- Memory is limited
- Network bandwidth is limited

Disable when:
- Values are usually small
- CPU is limited
- Latency is critical

## Distributed Locks

**Q: What if a lock holder crashes?**

A: Locks have a TTL (duration) which automatically releases them. Default is 30 seconds.

**Q: Can I have deadlock with locks?**

A: Not really - locks auto-expire. But ensure your `finally` block releases the lock:

```csharp
if (!await _cache.AcquireLockAsync(key, TimeSpan.FromSeconds(30)))
    return error;

try
{
    // Critical section
}
finally
{
    await _cache.ReleaseLockAsync(key);
}
```

**Q: How long should lock timeout be?**

A: 1.5-2x the expected operation time:
- Fast operation (< 1s): 2 seconds
- Normal operation (1-10s): 15-30 seconds
- Long operation (> 30s): 60+ seconds

**Q: Is there a more efficient way than polling for locks?**

A: In a true messaging scenario, yes (RabbitMQ, Service Bus). For simple cases, the TTL-based approach is sufficient.

## Error Handling

**Q: What happens if Redis is down?**

A: By default, it throws `CacheException`. Handle with graceful degradation:

```csharp
try
{
    var cached = await _cache.GetAsync<T>(key);
    if (cached != null) return cached;
}
catch (CacheException ex)
{
    _logger.LogWarning($"Cache unavailable: {ex.Message}");
    // Continue to database
}

return await _repository.GetByIdAsync(id);
```

**Q: How do I retry failed cache operations?**

A: Use `RetryHelper`:

```csharp
var result = await RetryHelper.ExecuteWithRetryAsync(
    () => _cache.GetAsync<T>(key),
    maxAttempts: 3,
    initialDelayMs: 100);
```

**Q: What if serialization fails?**

A: Make sure your type is JSON-serializable:

```csharp
// ❌ Won't serialize
public class Product
{
    private int _id;  // Private field
}

// ✓ Will serialize
public class Product
{
    public int Id { get; set; }  // Public property
}
```

## Monitoring

**Q: What metrics should I track?**

A: Key metrics:
- Hit rate (target: > 80%)
- Response time (target: < 10ms)
- Memory usage
- Error rate
- Key count

**Q: How do I monitor cache health?**

A:

```csharp
var health = await _healthCheck.IsCacheHealthyAsync();
if (!health)
    _alerting.Alert("Cache unhealthy");
```

**Q: Can I export metrics to Prometheus/Grafana?**

A: Yes, via Application Insights or custom metrics endpoint. See [DEPLOYMENT.md](DEPLOYMENT.md#monitoring--observability).

## Scaling

**Q: How do I scale the cache?**

A:
- **Vertical**: Increase Redis memory/CPU
- **Horizontal**: Redis Cluster, read replicas
- **Application**: More instances with cache warming

**Q: Should each app instance have its own Redis cache?**

A: No. Use one centralized Redis instance accessed by all app instances. This ensures consistency.

**Q: What's the maximum cache size?**

A: Limited by Redis configuration (`maxmemory`). Typical range: 1GB - 100GB depending on hardware.

**Q: How do I handle eviction?**

A: Configure policy in Redis:

```conf
maxmemory 2gb
maxmemory-policy allkeys-lru  # Evict least recently used keys
```

Options:
- `noeviction`: Error when full
- `allkeys-lru`: Evict any key
- `allkeys-lfu`: Evict least frequently used
- `volatile-*`: Evict only keys with TTL

## Development

**Q: How do I run tests with caching?**

A: Mock `ICacheService`:

```csharp
var mockCache = new Mock<ICacheService>();
mockCache
    .Setup(x => x.GetAsync<T>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync((T?)null);  // Always cache miss

var service = new ProductService(_repository.Object, mockCache.Object);
```

**Q: How do I test distributed locks?**

A:

```csharp
[Fact]
public async Task AcquireLock_SucceedsFirstTime()
{
    var acquired = await _cache.AcquireLockAsync("test-lock", TimeSpan.FromSeconds(10));
    Assert.True(acquired);
    
    // Second acquire should fail
    var acquired2 = await _cache.AcquireLockAsync("test-lock", TimeSpan.FromSeconds(10));
    Assert.False(acquired2);
}
```

**Q: Can I use Redis Cache Patterns in a CLI application?**

A: Yes, it's designed to work with any .NET application type. See CLI examples in `CLI/` directory.

## Troubleshooting

**Q: "Cannot connect to Redis"**

A: Check:
1. Redis is running: `redis-cli ping`
2. Connection string is correct
3. Firewall allows port 6379
4. Redis is accessible from your network

**Q: "Cache key too long"**

A: Keys are limited to 256 chars by default. Either:
- Use shorter keys
- Hash long keys: `SHA256(longKey).Substring(0, 32)`
- Increase max length in config

**Q: "Out of memory" in Redis**

A: Solutions:
1. Reduce TTL for less important data
2. Implement cache eviction policy
3. Add compression
4. Upgrade Redis memory

**Q: "Slow cache operations"**

A: Check:
1. Network latency (should be < 5ms)
2. Redis CPU usage
3. Large values (use compression)
4. Key count (scan performance)

**Q: "Cache stampede" (thundering herd)**

A: Multiple requests hit database simultaneously on cache miss:

```csharp
// ✗ Bad - every request loads from database
var cached = await _cache.GetAsync<T>(key);
if (cached == null)
{
    var value = await _database.GetAsync(id);  // Many concurrent
    await _cache.SetAsync(key, value, ttl);
}

// ✓ Good - use lock to serialize
var lockKey = $"load:{key}";
if (await _cache.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(10)))
{
    try
    {
        var value = await _database.GetAsync(id);  // Only one
        await _cache.SetAsync(key, value, ttl);
    }
    finally
    {
        await _cache.ReleaseLockAsync(lockKey);
    }
}
```

**Q: How do I clear all cache?**

A: ⚠ Use with caution:

```csharp
// Clear all keys
await _cache.InvalidateAsync("*");

// Or from redis-cli
redis-cli FLUSHDB
```

## License & Support

**Q: Can I use this in production?**

A: Yes, it's production-ready. MIT license allows commercial use.

**Q: Can I modify the source?**

A: Yes, MIT license permits modifications. Please contribute improvements back.

**Q: Where do I report bugs?**

A: Open an issue on GitHub with:
- Steps to reproduce
- Expected vs actual behavior
- .NET version
- Redis version
- Full stack trace

**Q: How do I contribute?**

A: See [README.md](../README.md#contributing):
1. Fork repository
2. Create feature branch
3. Commit with conventional commits
4. Submit pull request with tests

## Migration

**Q: Can I migrate from other caching libraries?**

A: Yes. ICacheService provides a standard interface. You can:

1. Keep existing code using old library
2. Gradually migrate methods to use ICacheService
3. Run both in parallel during transition

**Q: How do I migrate from StackExchange.Redis directly?**

A: RedisCacheService wraps StackExchange.Redis, so you're essentially doing that. Advantages:
- Unified interface
- Built-in serialization
- Distributed locks
- Monitoring

**Q: Can I use this with Entity Framework Core?**

A: Yes! Cache database queries:

```csharp
public async Task<User?> GetUserAsync(int id)
{
    var key = $"user:{id}";
    var cached = await _cache.GetAsync<User>(key);
    if (cached != null) return cached;

    var user = await _dbContext.Users.FindAsync(id);
    if (user != null)
        await _cache.SetAsync(key, user, TimeSpan.FromHours(1));
    
    return user;
}
```

## Related Questions

**Q: What about Redis Pub/Sub?**

A: Not currently implemented, but can be added. Use for:
- Real-time notifications
- Cache invalidation events
- Chat/messaging

**Q: Does this support Redis Streams?**

A: Not currently. Streams are useful for:
- Event sourcing
- Audit logs
- Message queues

Contributions welcome for these features.

**Q: Can I use Redis for sessions?**

A: Yes. Cache a `SessionState` object:

```csharp
public class SessionState
{
    public int UserId { get; set; }
    public Dictionary<string, object> Data { get; set; }
}

// Store
await _cache.SetAsync($"session:{sessionId}", state, TimeSpan.FromHours(8));

// Retrieve
var state = await _cache.GetAsync<SessionState>($"session:{sessionId}");
```

## Still Have Questions?

- Check [GETTING_STARTED.md](GETTING_STARTED.md)
- Review [ARCHITECTURE.md](ARCHITECTURE.md)
- Check [examples/](../examples/) directory
- Open an issue on GitHub

---

Last updated: 2026-05-04
