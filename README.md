// existing content ...

## IEventPublisher

The `IEventPublisher` interface represents an event publisher for pub-sub pattern supporting async event handling. It provides methods for publishing events, subscribing to events, and unsubscribing from events. This allows for decoupling event producers from consumers using the observer pattern.

### Usage Example
```csharp
public class MyEvent : DomainEvent
{
    public string MyProperty { get; set; }
}

public class MyEventHandler
{
    public async Task HandleMyEvent(MyEvent @event)
    {
        Console.WriteLine($"Received event: MyProperty={@event.MyProperty}");
    }
}

var eventPublisher = new EventPublisher(new NullLogger<EventPublisher>());
var myEventHandler = new MyEventHandler();

eventPublisher.Subscribe<MyEvent>(myEventHandler.HandleMyEvent);

var myEvent = new MyEvent { MyProperty = "Hello, World!" };
await eventPublisher.PublishAsync(myEvent);
```

## CacheHitEvent

The `CacheHitEvent` represents an event triggered when a cache lookup successfully retrieves data. It contains information about the accessed cache key and the size of the retrieved data, which can be used for monitoring cache performance and analyzing hit patterns.

### Usage Example
```csharp
// Create a cache hit event when data is retrieved from cache
var cacheHitEvent = new CacheHitEvent
{
    CacheKey = "user:123:profile",
    DataSize = 1024 // Size in bytes
};

// In a cache service implementation
public class CacheService
{
    private readonly CacheEventListener _listener;
    private readonly ILogger<CacheService> _logger;
    
    public CacheService(CacheEventListener listener, ILogger<CacheService> logger)
    {
        _listener = listener;
        _logger = logger;
    }
    
    public async Task<CacheData> GetDataAsync(string key)
    {
        var cachedData = await _cache.GetAsync(key);
        
        if (cachedData != null)
        {
            // Cache hit occurred
            await _listener.OnCacheHitAsync(new CacheHitEvent
            {
                CacheKey = key,
                DataSize = cachedData.Length
            });
            
            _logger.LogInformation("Cache hit for key: {Key} | Size: {Size} bytes", 
                key, cachedData.Length);
            return cachedData;
        }
        
        // Cache miss would trigger CacheMissEvent instead
        return null;
    }
}

// Usage in application startup
var cacheEventListener = new CacheEventListener(logger);
var cacheService = new CacheService(cacheEventListener, logger);

// Monitor cache statistics
var hitRate = cacheEventListener.GetHitRate();
var totalHits = cacheEventListener.GetTotalHits();
```

## CacheKeyBenchmarks

The `CacheKeyBenchmarks` class provides performance benchmarks for various cache key construction patterns used throughout the Redis Cache Patterns library. These benchmarks measure allocation efficiency and throughput for different key generation strategies that are called on every cache operation, making their performance critical at high throughput scenarios.

### Usage Example
```csharp
using BenchmarkDotNet.Running;
using RedisCachePatterns.Benchmarks;

// Run all benchmarks
var summary = BenchmarkRunner.Run<CacheKeyBenchmarks>();

// Run specific benchmark
var config = ManualConfig.Create(DefaultConfig.Instance)
    .WithOptions(ConfigOptions.DisableOptimizationsValidator);
var summary = BenchmarkRunner.Run<CacheKeyBenchmarks>(config, args: new[] { "UserKey" });

// Example: Using the benchmarked key patterns in application code
public class ProductService
{
    private readonly IDistributedCache _cache;

    public ProductService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<Product?> GetProductAsync(int productId)
    {
        // Using CacheKeyBuilder.Product() - 2 segments
        var cacheKey = CacheKeyBuilder.Product(productId);
        var cachedProduct = await _cache.GetStringAsync(cacheKey);
        
        if (cachedProduct != null)
        {
            return JsonSerializer.Deserialize<Product>(cachedProduct);
        }

        // Cache miss - fetch from database
        var product = await _db.Products.FindAsync(productId);
        if (product != null)
        {
            // Using CacheKeyBuilder.Product() for consistent key naming
            await _cache.SetStringAsync(
                CacheKeyBuilder.Product(product.Id),
                JsonSerializer.Serialize(product),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                });
        }

        return product;
    }

    public async Task<IEnumerable<Product>> GetActiveProductsAsync()
    {
        // Using CacheKeyHelper.BuildCollectionKey() - collection pattern
        var cacheKey = CacheKeyHelper.BuildCollectionKey<Product>("active");
        var cachedProducts = await _cache.GetStringAsync(cacheKey);
        
        if (cachedProducts != null)
        {
            return JsonSerializer.Deserialize<List<Product>>(cachedProducts);
        }

        // Cache miss - fetch from database
        var products = await _db.Products
            .Where(p => p.IsActive)
            .ToListAsync();
        
        if (products.Any())
        {
            // Using CacheKeyHelper.BuildCollectionKey() for consistent collection naming
            await _cache.SetStringAsync(
                CacheKeyHelper.BuildCollectionKey<Product>("active"),
                JsonSerializer.Serialize(products),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
        }

        return products;
    }
}
```