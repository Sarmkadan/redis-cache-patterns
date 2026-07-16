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
## CacheAsideIntegrationTests

The `CacheAsideIntegrationTests` class demonstrates end-to-end integration scenarios for the cache-aside pattern, validating concurrent access, distributed locking, compression, validation, idempotency, retry policies, and circuit breaker patterns. These tests use a mock cache service to simulate Redis behavior without requiring an actual Redis connection, making them fast and deterministic while still exercising the full workflow patterns that applications would encounter in production.

### Usage Example

```csharp
using RedisCachePatterns.Tests;
using RedisCachePatterns.Domain;

// Create a mock cache service for testing
var mockCache = new MockCacheService();

// Simulate a product service using cache-aside pattern
public class ProductService
{
    private readonly ICacheService _cache;
    private readonly IProductRepository _productRepository;
    
    public ProductService(ICacheService cache, IProductRepository productRepository)
    {
        _cache = cache;
        _productRepository = productRepository;
    }
    
    public async Task<Product?> GetProductAsync(int productId)
    {
        // Try to get from cache first (cache-aside pattern)
        var cachedProduct = await _cache.GetOrLoadAsync<Product>(
            $"product:{productId}",
            async () => await _productRepository.GetProductAsync(productId),
            TimeSpan.FromMinutes(30)
        );
        
        return cachedProduct;
    }
    
    public async Task<Product> UpdateProductAsync(Product product)
    {
        // Update product in database
        var updatedProduct = await _productRepository.UpdateProductAsync(product);
        
        // Write back to cache
        await _cache.SetAsync($"product:{product.Id}", updatedProduct);
        
        return updatedProduct;
    }
    
    public async Task<Product> GetOrCreateProductAsync(int productId, Func<Task<Product>> createFn)
    {
        // Use WriteAsync for atomic create-or-update operations
        return await _cache.WriteAsync(
            $"product:{productId}",
            await createFn(),
            async () => await _productRepository.GetProductAsync(productId)
        );
    }
}

// Example usage in a test scenario
var productService = new ProductService(mockCache, new MockProductRepository());

// First call - loads from source
var product = await productService.GetProductAsync(1);

// Second call - returns from cache
var cachedProduct = await productService.GetProductAsync(1);

// Concurrent access is handled safely
var tasks = Enumerable.Range(1, 10)
    .Select(i => productService.GetProductAsync(i))
    .ToList();
var results = await Task.WhenAll(tasks);
```

## CompressionBenchmarks

The `CompressionBenchmarks` class provides performance benchmarks for GZIP compression applied to cache entries of varying sizes. These benchmarks validate that the ArrayPool-based implementation reduces allocations compared to direct byte array allocation, which is critical for high-throughput cache operations where memory pressure must be minimized.

### Usage Example

```csharp
using BenchmarkDotNet.Running;
using RedisCachePatterns.Benchmarks;

// Run all compression benchmarks
var summary = BenchmarkRunner.Run<CompressionBenchmarks>();

// Run specific benchmark
var config = ManualConfig.Create(DefaultConfig.Instance)
    .WithOptions(ConfigOptions.DisableOptimizationsValidator);
var summary = BenchmarkRunner.Run<CompressionBenchmarks>(config, args: new[] { "CompressLarge" });

// Example: Using compression in a real cache service
public class ProductCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<ProductCacheService> _logger;

    public ProductCacheService(IDistributedCache cache, ILogger<ProductCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task CacheProductAsync(Product product)
    {
        // Serialize product to JSON
        var serialized = SerializationHelper.Serialize(product);
        
        // Compress before storing in cache to save memory
        var compressed = CompressionUtil.CompressString(serialized);
        
        await _cache.SetAsync(
            CacheKeyBuilder.Product(product.Id),
            compressed,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

        _logger.LogDebug("Cached product {ProductId} ({OriginalSize} bytes → {CompressedSize} bytes)",
            product.Id,
            serialized.Length,
            compressed.Length);
    }

    public async Task<Product?> GetProductAsync(int productId)
    {
        var cacheKey = CacheKeyBuilder.Product(productId);
        var cachedData = await _cache.GetAsync(cacheKey);

        if (cachedData != null)
        {
            // Decompress then deserialize
            var decompressed = CompressionUtil.DecompressString(cachedData);
            return SerializationHelper.Deserialize<Product>(decompressed);
        }

        return null;
    }

    public async Task CacheProductCollectionAsync(IEnumerable<Product> products)
    {
        // Serialize collection to JSON
        var serialized = SerializationHelper.Serialize(products);
        
        // Compress before storing
        var compressed = CompressionUtil.CompressString(serialized);
        
        await _cache.SetAsync(
            CacheKeyHelper.BuildCollectionKey<Product>("active"),
            compressed,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

        _logger.LogDebug("Cached {Count} products ({OriginalSize} bytes → {CompressedSize} bytes)",
            products.Count(),
            serialized.Length,
            compressed.Length);
    }

    public async Task<List<Product>> GetProductCollectionAsync()
    {
        var cacheKey = CacheKeyHelper.BuildCollectionKey<Product>("active");
        var cachedData = await _cache.GetAsync(cacheKey);

        if (cachedData != null)
        {
            // Decompress then deserialize collection
            var decompressed = CompressionUtil.DecompressString(cachedData);
            return SerializationHelper.Deserialize<List<Product>>(decompressed);
        }

        return new List<Product>();
    }
}

// Usage in application startup
var cacheService = new ProductCacheService(cache, logger);
```

## SerializationBenchmarks

The `SerializationBenchmarks` class provides performance benchmarks for JSON serialization and deserialization operations used in Redis cache read/write paths. These benchmarks measure the per-operation cost of converting domain objects like `Product` and `Order` to/from JSON strings, which is a critical performance factor for cache throughput at scale.



### Usage Example
```csharp
using BenchmarkDotNet.Running;
using RedisCachePatterns.Benchmarks;

// Run all benchmarks
var summary = BenchmarkRunner.Run<SerializationBenchmarks>();

// Run specific benchmark
var config = ManualConfig.Create(DefaultConfig.Instance)
  .WithOptions(ConfigOptions.DisableOptimizationsValidator);
var summary = BenchmarkRunner.Run<SerializationBenchmarks>(config, args: new[] { "SerializeProduct" });

// Example: Using serialization in a real cache service
public class CacheService
{
  private readonly IDistributedCache _cache;
  private readonly ILogger<CacheService> _logger;

  public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
  {
    _cache = cache;
    _logger = logger;
  }

  public async Task CacheProductAsync(Product product)
  {
    // Serialize product to store in cache
    var serialized = SerializationHelper.Serialize(product);
    
    await _cache.SetStringAsync(
      CacheKeyBuilder.Product(product.Id),
      serialized,
      new DistributedCacheEntryOptions
      {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
      });
    
    _logger.LogDebug("Cached product {ProductId} ({Size} bytes)", 
      product.Id, serialized.Length);
  }

  public async Task<Product?> GetProductAsync(int productId)
  {
    var cacheKey = CacheKeyBuilder.Product(productId);
    var cachedData = await _cache.GetStringAsync(cacheKey);

    if (cachedData != null)
    {
      // Deserialize product from cache
      return SerializationHelper.Deserialize<Product>(cachedData);
    }

    return null;
  }

  public async Task CacheOrderAsync(Order order)
  {
    // Serialize order with multiple items to store in cache
    var serialized = SerializationHelper.Serialize(order);
    
    await _cache.SetStringAsync(
      CacheKeyBuilder.Order(order.Id),
      serialized,
      new DistributedCacheEntryOptions
      {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
      });
    
    _logger.LogDebug("Cached order {OrderNumber} ({Size} bytes, {ItemCount} items)", 
      order.OrderNumber, serialized.Length, order.Items.Count);
  }

  public async Task<Order?> GetOrderAsync(int orderId)
  {
    var cacheKey = CacheKeyBuilder.Order(orderId);
    var cachedData = await _cache.GetStringAsync(cacheKey);

    if (cachedData != null)
    {
      // Deserialize order with items from cache
      return SerializationHelper.Deserialize<Order>(cachedData);
    }

    return null;
  }
}

// Usage in application startup
var cacheService = new CacheService(cache, logger);
```
