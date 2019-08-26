# Architecture Documentation

## System Design Overview

Redis Cache Patterns is built using a layered architecture with clear separation of concerns, making it easy to understand, extend, and maintain.

```
┌─────────────────────────────────────────────────────┐
│         Application Layer                           │
│    (API Endpoints, Controllers, CLI Commands)       │
└─────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────┐
│         Service Layer                               │
│    (UserService, ProductService, InventoryService) │
│    Business logic with caching integration          │
└─────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────┐
│         Cache Service Layer                         │
│    (RedisCacheService implements ICacheService)     │
│    Cache pattern implementations                    │
│    - Cache-Aside                                    │
│    - Write-Through                                  │
│    - Cache Invalidation                             │
│    - Distributed Locks                              │
└─────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────┐
│         Repository Layer                            │
│    (IRepository, ProductRepository, UserRepository) │
│    Data access abstraction                          │
└─────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────┐
│         Infrastructure Layer                        │
│    (RedisConnection, Database Connection)          │
│    Low-level connectivity                           │
└─────────────────────────────────────────────────────┘
```

## Core Layers

### 1. Domain Layer (`Domain/`)

**Responsibility**: Business entities and value objects

**Key Classes**:
- `User` - User entity with profile information
- `Product` - Product catalog entry
- `Order` - Order aggregation root
- `OrderItem` - Order line items
- `InventoryItem` - Inventory tracking
- `CachePolicy` - Caching strategy definition
- `CacheEntry` - Cache metadata
- `DistributedLock` - Lock state

**Characteristics**:
- No dependencies on infrastructure
- Fully serializable for caching
- Encapsulates business rules

### 2. Infrastructure Layer (`Infrastructure/`)

#### Cache Connectivity (`Infrastructure/Cache/`)

**IRedisConnection Interface**:
```csharp
public interface IRedisConnection
{
    IConnectionMultiplexer GetConnection();
    IDatabase GetDatabase();
    void Close();
    Task<bool> PingAsync();
}
```

**RedisConnection Implementation**:
- Connection pooling
- Automatic reconnection
- Thread-safe operations
- Configuration management

#### Repository Layer (`Infrastructure/Repositories/`)

**IRepository<T> Generic Interface**:
```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> CreateAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
```

**Specialized Repositories**:
- `UserRepository` - User data operations
- `ProductRepository` - Product catalog operations
- `OrderRepository` - Order management
- `InventoryRepository` - Stock tracking

**Design Pattern**: Repository pattern abstracts data access, allows easy testing with mock implementations.

### 3. Cache Service Layer (`Services/RedisCacheService.cs`)

**ICacheService Interface** defines the contract:

```csharp
public interface ICacheService
{
    // Retrieval
    Task<T?> GetAsync<T>(string key);
    Task<bool> ExistsAsync(string key);
    
    // Storage
    Task SetAsync<T>(string key, T value, TimeSpan expiration);
    Task SetIfNotExistsAsync<T>(string key, T value, TimeSpan expiration);
    
    // Removal
    Task RemoveAsync(string key);
    Task InvalidateAsync(string keyPattern);
    
    // Utilities
    Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern);
    Task<long> GetExpireSecondsAsync(string key);
    
    // Locking
    Task<bool> AcquireLockAsync(string key, TimeSpan duration);
    Task ReleaseLockAsync(string key);
}
```

**RedisCacheService Features**:

1. **Serialization**: Uses System.Text.Json for value serialization
2. **Error Handling**: Comprehensive exception handling with custom exceptions
3. **Monitoring**: Tracks operations for metrics collection
4. **Compression**: Optional gzip compression for large values
5. **Distributed Locks**: Redis-backed lock implementation

### 4. Business Logic Layer (`Services/`)

**Application Services** implement business logic:

- `UserService` - User management with caching
- `ProductService` - Product operations with cache invalidation
- `OrderService` - Order processing with distributed locks
- `InventoryService` - Stock management

**Pattern**: Services use ICacheService through dependency injection.

```csharp
public class ProductService
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cache;

    public async Task<Product?> GetProductAsync(int id)
    {
        var key = CacheKeyBuilder.BuildProductKey(id);
        
        // Cache-Aside pattern
        var cached = await _cache.GetAsync<Product>(key);
        if (cached != null) return cached;
        
        var product = await _repository.GetByIdAsync(id);
        if (product != null)
            await _cache.SetAsync(key, product, TimeSpan.FromHours(2));
        
        return product;
    }
}
```

## Design Patterns

### 1. Cache-Aside (Lazy Loading)

**When to use**: Read-heavy operations, infrequent writes

**Flow**:
```
Client Request
    ↓
Check Cache
    ├─ HIT → Return cached value
    └─ MISS → Load from source
         ↓
        Update Cache
         ↓
      Return value
```

**Example**:
```csharp
var cached = await _cache.GetAsync<Product>(key);
if (cached != null) return cached;

var product = await _repository.GetByIdAsync(id);
await _cache.SetAsync(key, product, ttl);
return product;
```

### 2. Write-Through

**When to use**: Strong consistency requirement, frequent reads, less frequent writes

**Flow**:
```
Update Request
    ↓
Update Source (Database)
    ↓
Update Cache
    ↓
Return updated value
```

**Example**:
```csharp
var updated = await _repository.UpdateAsync(product);
await _cache.SetAsync(key, updated, ttl);
return updated;
```

### 3. Cache Invalidation

**Strategies**:

1. **Time-Based**: TTL-based expiration
2. **Event-Based**: Invalidate on write
3. **Pattern-Based**: Bulk invalidation by key pattern
4. **Conditional**: Only invalidate if conditions met

**Example**:
```csharp
// Direct invalidation
await _cache.RemoveAsync($"product:{id}");

// Pattern-based
await _cache.InvalidateAsync("product:category:5:*");

// TTL-based (automatic)
await _cache.SetAsync(key, value, TimeSpan.FromHours(1));
```

### 4. Distributed Locking

**Purpose**: Prevent cache stampedes and race conditions

**Flow**:
```
Try Acquire Lock
    ├─ ACQUIRED → Process critical section
    └─ NOT ACQUIRED → Wait or fallback

Finally: Release Lock
```

**Example**:
```csharp
var lockKey = $"order-process:{orderId}";
if (!await _cache.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(30)))
    return error;

try
{
    // Critical operation
    await ProcessOrderAsync(orderId);
}
finally
{
    await _cache.ReleaseLockAsync(lockKey);
}
```

## Cross-Cutting Concerns

### 1. Utilities (`Utilities/`)

**CacheKeyBuilder**: Consistent key generation
```csharp
CacheKeyBuilder.BuildProductKey(id)        // "product:{id}"
CacheKeyBuilder.BuildUserKey(userId)       // "user:{userId}"
CacheKeyBuilder.BuildOrderKey(orderId)     // "order:{orderId}"
```

**SerializationHelper**: JSON serialization/deserialization

**ValidationHelper**: Input validation

**DistributedLockHelper**: Lock management

**CompressionUtil**: Gzip compression for large values

### 2. Configuration (`Configuration/`)

**DependencyInjectionExtensions**:
```csharp
services.AddRedisCacheServices(connectionString, options =>
{
    options.DefaultExpirationSeconds = 3600;
    options.EnableCompression = true;
});
```

**CacheConfiguration**: Configuration options
- Connection settings
- Expiration defaults
- Compression thresholds
- Lock timeouts

### 3. Monitoring (`Monitoring/`)

**CacheMetricsCollector**: 
- Hit/miss rates
- Response times
- Memory usage
- Operation counts

**HealthCheckService**:
- Connection status
- Response time measurement
- Memory monitoring

**DiagnosticsProvider**:
- Cache statistics
- Performance metrics
- Error tracking

### 4. Middleware (`Middleware/`)

- `CachingHeaderMiddleware` - HTTP cache headers
- `RateLimitingMiddleware` - Rate limiting
- `ErrorHandlingMiddleware` - Global error handling
- `RequestLoggingMiddleware` - Request/response logging

### 5. Events (`Events/`)

**EventPublisher/CacheEventListener**: Event-based cache invalidation

```csharp
// Publish event when data changes
await _eventPublisher.PublishAsync(
    new ProductUpdatedEvent { ProductId = 123 });

// Listen and invalidate cache
public async Task OnProductUpdatedAsync(ProductUpdatedEvent evt)
{
    await _cache.RemoveAsync($"product:{evt.ProductId}");
}
```

## Data Flow Example: Get Product with Caching

```
HTTP GET /products/123
         ↓
   ProductController
         ↓
   ProductService.GetProductAsync(123)
         ↓
   CacheKeyBuilder.BuildProductKey(123)
         → "product:123"
         ↓
   ICacheService.GetAsync<Product>("product:123")
         ↓
    ┌─ Cache HIT
    │   ↓
    │ Return cached Product
    │
    └─ Cache MISS
        ↓
        IProductRepository.GetByIdAsync(123)
         ↓
        Database Query
         ↓
        Product entity loaded
         ↓
        ICacheService.SetAsync("product:123", product, 2hrs)
         ↓
        Cache updated
         ↓
        Return Product
         ↓
   ProductController returns JSON
         ↓
    HTTP 200 OK
```

## Scalability Considerations

### Horizontal Scaling

**Distributed Cache**:
- Redis cluster for high availability
- Connection pooling for efficiency
- Consistent hashing for key distribution

**Load Distribution**:
- Multiple application instances
- Distributed locks prevent conflicts
- Event-based invalidation across instances

### Caching Strategy

**Cache Warming**: Pre-load frequently accessed data

```csharp
await _cacheWarmerService.WarmCacheAsync();
```

**Selective Caching**: Cache only valuable data

```csharp
if (value.Size > threshold)
    await _cache.SetAsync(key, value, ttl);
```

**Compression**: Reduce memory footprint

```csharp
options.CompressionThreshold = 1024; // Compress > 1KB
```

## Error Handling Strategy

**Graceful Degradation**:
```csharp
try
{
    var cached = await _cache.GetAsync<T>(key);
    if (cached != null) return cached;
}
catch (CacheException ex)
{
    _logger.LogWarning($"Cache error: {ex.Message}");
    // Continue to database
}

return await _repository.GetByIdAsync(id);
```

**Retry Logic**:
```csharp
var result = await RetryHelper.ExecuteWithRetryAsync(
    () => _cache.GetAsync<T>(key),
    maxAttempts: 3,
    initialDelayMs: 100,
    backoffMultiplier: 2.0);
```

## Testing Architecture

### Unit Tests
- Mock ICacheService and IRepository
- Test business logic independently
- No external dependencies

### Integration Tests
- Real Redis instance
- Test cache and database together
- Verify serialization

### Performance Tests
- Measure cache hit rates
- Monitor response times
- Load testing

## Directory Structure Rationale

```
redis-cache-patterns/
├── Domain/              # Pure business logic
├── Infrastructure/      # Data access and connectivity
│   ├── Cache/          # Redis connectivity
│   └── Repositories/   # Data repositories
├── Services/           # Application logic with caching
├── Configuration/      # DI and setup
├── Utilities/          # Reusable helpers
├── Monitoring/         # Observability
├── Middleware/         # HTTP pipeline
├── API/                # HTTP endpoints
├── CLI/                # Command-line interface
├── Events/             # Event handling
├── Integration/        # External APIs
└── Tests/              # Unit and integration tests
```

**Rationale**:
- Clear separation of concerns
- Easy to locate functionality
- Scalable structure for growth
- Follows DDD principles
- Supports testing strategies

## Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| Runtime | .NET | 10.0 |
| Language | C# | 12.0+ |
| Redis Client | StackExchange.Redis | 2.8.0+ |
| DI Container | Microsoft.Extensions.DependencyInjection | 9.0.0+ |
| Logging | Microsoft.Extensions.Logging | 9.0.0+ |
| Serialization | System.Text.Json | 9.0.0+ |

## Performance Characteristics

**Cache-Aside**:
- Cache hit: <5ms
- Cache miss: Database latency + 5ms
- Optimal for read-heavy workloads

**Write-Through**:
- Write time: Database + Cache time
- Strong consistency guarantee
- ~10-15ms for both operations

**Distributed Locks**:
- Acquire: <10ms
- Release: <5ms
- Prevents race conditions

## Monitoring and Observability

**Metrics Tracked**:
- Hit rate (%)
- Miss rate (%)
- Average response time (ms)
- Memory usage (MB)
- Key count
- Error count

**Health Checks**:
- Redis connectivity
- Response time baseline
- Memory thresholds
- Eviction policy status

## Future Enhancements

1. **Multi-tier Caching**: L1 (in-memory), L2 (Redis)
2. **Cache Preheating**: Automatic population on startup
3. **Advanced Metrics**: Percentile latencies, throughput
4. **Event Sourcing**: Complete audit trail
5. **Sharding**: Manual sharding support
6. **Replication**: Cross-region replication
