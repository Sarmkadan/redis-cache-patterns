# Redis Cache Patterns for .NET 10

**Production-ready Redis caching patterns for .NET applications** implementing cache-aside, write-through, and distributed locking strategies with comprehensive error handling, monitoring, and observability.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Redis](https://img.shields.io/badge/Redis-7.0+-red)](https://redis.io)

## Table of Contents

- [Overview & Motivation](#overview--motivation)
- [Architecture](#architecture)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Usage Examples](#usage-examples)
- [API Reference](#api-reference)
- [CLI Reference](#cli-reference)
- [Configuration Guide](#configuration-guide)
- [Monitoring & Diagnostics](#monitoring--diagnostics)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)

## Overview & Motivation

Redis is one of the most popular caching solutions in production systems, but implementing it correctly across a .NET application requires careful consideration of cache invalidation, consistency, and concurrency. This project provides battle-tested patterns and implementations for:

**Cache-Aside (Lazy Loading)** — The most common pattern where your application checks the cache first and loads from the primary data source on cache miss. Ideal for read-heavy workloads.

**Write-Through (Synchronous)** — Ensures cache and database consistency by updating both atomically. Preferred when data consistency is critical and writes are less frequent.

**Distributed Locks** — Prevents cache stampedes and race conditions across multiple instances using Redis-backed locks with automatic cleanup and deadlock detection.

**Cache Invalidation** — Sophisticated key pattern matching, selective invalidation, and time-based expiration to keep cached data fresh without unnecessary evictions.

**Monitoring & Observability** — Built-in metrics collection, diagnostics, and health checks for production visibility.

The project is designed to be:
- **Production-ready**: Error handling, retry logic, and timeout management
- **Observable**: Built-in logging, metrics, and diagnostics
- **Extensible**: Clean interfaces and dependency injection patterns
- **Well-documented**: Comprehensive examples and architectural documentation

## Architecture

### Layered Design

```
┌─────────────────────────────────────────┐
│   API Layer (Controllers/Endpoints)     │
├─────────────────────────────────────────┤
│   Application Services                  │
│   (UserService, ProductService, etc.)   │
├─────────────────────────────────────────┤
│   Cache Service Layer                   │
│   (RedisCacheService - Pattern Logic)   │
├─────────────────────────────────────────┤
│   Repository Layer (Data Access)        │
│   (UserRepository, ProductRepository)   │
├─────────────────────────────────────────┤
│   Redis Connection (IRedisConnection)   │
├─────────────────────────────────────────┤
│   Domain Models & Utilities              │
└─────────────────────────────────────────┘
```

### Core Components

| Component | Responsibility | Key Files |
|-----------|----------------|-----------|
| **Domain Models** | Entity definitions and contracts | User.cs, Product.cs, Order.cs, CachePolicy.cs |
| **Repository Layer** | Data access with caching integration | IRepository.cs, ProductRepository.cs |
| **Cache Service** | Cache pattern implementation | RedisCacheService.cs, ICacheService.cs |
| **Cache Connection** | Redis connectivity and pooling | RedisConnection.cs, IRedisConnection.cs |
| **Service Layer** | Business logic with caching | UserService.cs, ProductService.cs |
| **Utilities** | Cache keys, locks, serialization | CacheKeyBuilder.cs, DistributedLockHelper.cs |
| **Configuration** | DI setup and cache configuration | DependencyInjectionExtensions.cs, CacheConfiguration.cs |
| **Monitoring** | Metrics collection and diagnostics | CacheMetricsCollector.cs, HealthCheckService.cs |

### Project Structure

```
redis-cache-patterns/
├── Domain/                      # Entity models
│   ├── User.cs                  # User entity with cache policy
│   ├── Product.cs               # Product entity
│   ├── Order.cs                 # Order aggregation
│   ├── OrderItem.cs             # Order line item
│   ├── InventoryItem.cs         # Inventory tracking
│   ├── CachePolicy.cs           # Cache behavior configuration
│   ├── CacheEntry.cs            # Cache metadata
│   ├── DistributedLock.cs       # Lock metadata
│   └── SystemConfiguration.cs   # System-wide settings
├── Infrastructure/
│   ├── Cache/                   # Redis connectivity layer
│   │   ├── IRedisConnection.cs
│   │   └── RedisConnection.cs
│   └── Repositories/            # Data access layer
│       ├── IRepository.cs       # Generic interface
│       ├── Repository.cs        # Base implementation
│       ├── UserRepository.cs    # User-specific access
│       ├── ProductRepository.cs
│       ├── OrderRepository.cs
│       └── InventoryRepository.cs
├── Services/                    # Business logic with caching
│   ├── ICacheService.cs         # Cache pattern interface
│   ├── RedisCacheService.cs     # Implementation
│   ├── UserService.cs           # User business logic
│   ├── ProductService.cs
│   ├── OrderService.cs
│   ├── InventoryService.cs
│   └── [other services]
├── Configuration/               # Dependency injection & config
│   ├── AppConstants.cs
│   ├── CacheConfiguration.cs    # Cache policy defaults
│   ├── CacheConfigurationBuilder.cs
│   ├── DependencyInjectionExtensions.cs
│   ├── ServiceRegistration.cs
│   └── ModuleRegistration.cs
├── Utilities/                   # Helper functions
│   ├── CacheKeyBuilder.cs       # Consistent key generation
│   ├── CacheKeyHelper.cs        # Key pattern utilities
│   ├── DistributedLockHelper.cs # Lock management
│   ├── SerializationHelper.cs
│   ├── ValidationHelper.cs
│   ├── RetryHelper.cs           # Exponential backoff
│   ├── CompressionUtil.cs       # Large value compression
│   └── PerformanceMonitor.cs
├── Extensions/                  # Extension methods
│   ├── StringExtensions.cs
│   ├── CollectionExtensions.cs
│   └── CacheServiceExtensions.cs
├── Exceptions/                  # Custom exceptions
│   ├── CacheException.cs        # Cache-specific errors
│   └── BusinessException.cs
├── Results/                     # Response wrappers
│   └── OperationResult.cs
├── Monitoring/                  # Observability
│   ├── CacheMetricsCollector.cs
│   ├── HealthCheckService.cs
│   ├── DiagnosticsProvider.cs
│   └── CacheMonitor.cs
├── API/                         # HTTP endpoints
│   ├── ApiEndpointBase.cs
│   ├── CacheEndpoint.cs
│   └── ProductEndpoint.cs
├── CLI/                         # Command-line interface
│   ├── CacheCommand.cs
│   ├── ProductCommand.cs
│   └── CommandParser.cs
├── BackgroundWorkers/           # Background tasks
│   ├── CacheCleanupWorker.cs
│   ├── CacheWarmerWorker.cs
│   └── InventoryRebalanceWorker.cs
├── Middleware/                  # HTTP middleware
│   ├── CachingHeaderMiddleware.cs
│   ├── ErrorHandlingMiddleware.cs
│   ├── RateLimitingMiddleware.cs
│   └── [other middleware]
├── Events/                      # Event handling
│   ├── EventPublisher.cs
│   ├── CacheEventListener.cs
│   └── OrderEventHandler.cs
├── Integration/                 # External APIs
│   ├── ExternalApiClient.cs
│   └── WebhookHandler.cs
├── Formatters/                  # Output formatting
│   ├── JsonFormatter.cs
│   ├── CsvFormatter.cs
│   └── XmlFormatter.cs
├── Program.cs                   # Application entry point
├── RedisCachePatterns.csproj
├── docker-compose.yml
├── Dockerfile
├── Makefile
├── .editorconfig
├── CHANGELOG.md
├── LICENSE
├── .gitignore
└── docs/
    ├── ARCHITECTURE.md
    ├── GETTING_STARTED.md
    ├── API_REFERENCE.md
    ├── DEPLOYMENT.md
    └── FAQ.md
```

## Installation

### Prerequisites

- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Redis 7.0+** - [Install](https://redis.io/docs/getting-started/)
- **Docker** (optional) - [Install](https://docs.docker.com/get-docker/)

### Option 1: From Source (Recommended for Development)

```bash
# Clone the repository
git clone https://github.com/Sarmkadan/redis-cache-patterns.git
cd redis-cache-patterns

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run tests (if available)
dotnet test

# Run the application
dotnet run
```

### Option 2: Using Docker Compose (Recommended for Local Development)

```bash
# Start Redis and the application
docker-compose up --build

# View logs
docker-compose logs -f app

# Stop
docker-compose down
```

### Option 3: Docker Build Only

```bash
# Build the Docker image
docker build -t redis-cache-patterns:latest .

# Run with Redis
docker run -d --name redis redis:7-alpine
docker run -p 5000:5000 \
  -e REDIS_CONNECTION_STRING="redis:6379" \
  --link redis \
  redis-cache-patterns:latest

# Stop containers
docker stop $(docker ps -q)
```

### Option 4: NuGet Package (Future)

Once published to NuGet, you can add to your project:

```bash
dotnet add package RedisCachePatterns
```

## Quick Start

### 1. Configure Redis Connection

Update `appsettings.json` (if present) or set environment variables:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "DefaultDatabase": 0,
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000
  },
  "Cache": {
    "DefaultExpiration": 3600,
    "MaxKeyLength": 256
  }
}
```

### 2. Initialize Dependency Injection

In `Program.cs`:

```csharp
using RedisCachePatterns.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add Redis cache services
builder.Services.AddRedisCacheServices(
    builder.Configuration.GetSection("Redis").Value,
    cacheConfig => {
        cacheConfig.DefaultExpirationSeconds = 3600;
        cacheConfig.EnableCompression = true;
        cacheConfig.CompressionThreshold = 1024;
    });

var app = builder.Build();
app.Run();
```

### 3. Inject Cache Service and Use It

```csharp
public class ProductService {
    private readonly ICacheService _cacheService;
    private readonly IProductRepository _repository;

    public ProductService(ICacheService cacheService, IProductRepository repository) {
        _cacheService = cacheService;
        _repository = repository;
    }

    // Cache-Aside Pattern
    public async Task<Product?> GetProductByIdAsync(int id) {
        var key = CacheKeyBuilder.BuildProductKey(id);
        
        var cached = await _cacheService.GetAsync<Product>(key);
        if (cached != null) return cached;

        var product = await _repository.GetByIdAsync(id);
        if (product != null) {
            await _cacheService.SetAsync(key, product, TimeSpan.FromHours(1));
        }
        return product;
    }

    // Write-Through Pattern
    public async Task<Product> UpdateProductAsync(Product product) {
        var updated = await _repository.UpdateAsync(product);
        var key = CacheKeyBuilder.BuildProductKey(product.Id);
        await _cacheService.SetAsync(key, updated, TimeSpan.FromHours(1));
        return updated;
    }
}
```

## Usage Examples

### Example 1: Cache-Aside Pattern with Fallback

```csharp
public async Task<User> GetUserWithFallbackAsync(int userId) {
    var cacheKey = $"user:{userId}";
    
    try {
        // Try cache first
        var cached = await _cacheService.GetAsync<User>(cacheKey);
        if (cached != null) {
            _logger.LogInformation($"Cache HIT for user {userId}");
            return cached;
        }
    } catch (CacheException ex) {
        _logger.LogWarning($"Cache error: {ex.Message}");
        // Continue to fetch from database
    }

    // Load from database
    var user = await _userRepository.GetByIdAsync(userId);
    if (user != null) {
        try {
            await _cacheService.SetAsync(cacheKey, user, TimeSpan.FromMinutes(30));
        } catch (CacheException ex) {
            _logger.LogWarning($"Failed to cache user: {ex.Message}");
        }
    }
    return user;
}
```

### Example 2: Write-Through with Validation

```csharp
public async Task<OperationResult<Product>> UpdateProductAsync(Product product) {
    // Validate before updating
    if (!ValidationHelper.IsValidProduct(product)) {
        return OperationResult<Product>.Failure("Invalid product data");
    }

    try {
        // Update database first
        var updated = await _productRepository.UpdateAsync(product);
        
        // Then update cache
        var key = CacheKeyBuilder.BuildProductKey(product.Id);
        await _cacheService.SetAsync(key, updated, TimeSpan.FromHours(2));
        
        return OperationResult<Product>.Success(updated);
    } catch (Exception ex) {
        _logger.LogError($"Update failed: {ex.Message}");
        return OperationResult<Product>.Failure("Update failed: " + ex.Message);
    }
}
```

### Example 3: Distributed Lock Pattern

```csharp
public async Task<OperationResult> ConfirmOrderAsync(int orderId, string instanceId) {
    var lockKey = $"order-confirm:{orderId}";
    var lockHelper = new DistributedLockHelper(_cacheService);
    
    if (!await lockHelper.AcquireLockAsync(lockKey, instanceId, TimeSpan.FromSeconds(10))) {
        return OperationResult.Failure("Order is being processed by another instance");
    }

    try {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null) {
            return OperationResult.Failure("Order not found");
        }

        // Process order
        order.Status = OrderStatus.Confirmed;
        order.ConfirmedAt = DateTime.UtcNow;
        
        await _orderRepository.UpdateAsync(order);
        await _cacheService.InvalidateAsync($"order:{orderId}*");
        
        return OperationResult.Success();
    } finally {
        await lockHelper.ReleaseLockAsync(lockKey, instanceId);
    }
}
```

### Example 4: Bulk Operations with Cache Invalidation

```csharp
public async Task<OperationResult> BulkUpdateProductsAsync(List<Product> products) {
    try {
        // Update all in database
        await _productRepository.BulkUpdateAsync(products);
        
        // Invalidate all related cache keys
        var tasks = products.Select(p => 
            _cacheService.InvalidateAsync($"product:{p.Id}*")
        );
        await Task.WhenAll(tasks);
        
        // Also invalidate category cache
        await _cacheService.InvalidateAsync("products:category:*");
        
        return OperationResult.Success();
    } catch (Exception ex) {
        return OperationResult.Failure($"Bulk update failed: {ex.Message}");
    }
}
```

### Example 5: Cache Warming

```csharp
public async Task WarmCacheAsync() {
    try {
        // Load frequently accessed data
        var topProducts = await _productRepository.GetTopProductsAsync(100);
        var ttl = TimeSpan.FromHours(4);
        
        var tasks = topProducts.Select(async p => {
            var key = CacheKeyBuilder.BuildProductKey(p.Id);
            await _cacheService.SetAsync(key, p, ttl);
        });
        
        await Task.WhenAll(tasks);
        _logger.LogInformation($"Warmed cache with {topProducts.Count} products");
    } catch (Exception ex) {
        _logger.LogError($"Cache warming failed: {ex.Message}");
    }
}
```

### Example 6: Conditional Cache Update

```csharp
public async Task<Product> GetOrCreateProductAsync(int id, Func<Task<Product>> factory) {
    var key = CacheKeyBuilder.BuildProductKey(id);
    
    var cached = await _cacheService.GetAsync<Product>(key);
    if (cached != null) return cached;

    // Create using factory
    var product = await factory();
    
    // Cache only if valid
    if (product != null && product.IsActive) {
        await _cacheService.SetAsync(key, product, TimeSpan.FromHours(1));
    }
    
    return product;
}
```

### Example 7: Cache with Compression

```csharp
public async Task<OperationResult> CacheLargeDataAsync(string key, byte[] data) {
    try {
        if (data.Length > 1024) {
            var compressed = CompressionUtil.CompressGzip(data);
            await _cacheService.SetAsync(key, 
                Convert.ToBase64String(compressed), 
                TimeSpan.FromHours(1));
        } else {
            await _cacheService.SetAsync(key, 
                Convert.ToBase64String(data), 
                TimeSpan.FromHours(1));
        }
        return OperationResult.Success();
    } catch (Exception ex) {
        return OperationResult.Failure($"Caching failed: {ex.Message}");
    }
}
```

### Example 8: Metrics Collection and Monitoring

```csharp
public async Task<CacheMetrics> GetCacheMetricsAsync() {
    var collector = new CacheMetricsCollector(_cacheService);
    
    return new CacheMetrics {
        HitRate = await collector.GetHitRateAsync(),
        MissRate = await collector.GetMissRateAsync(),
        AverageResponseTime = await collector.GetAverageResponseTimeAsync(),
        TotalKeys = await collector.GetTotalKeysAsync(),
        EstimatedMemory = await collector.GetEstimatedMemoryAsync()
    };
}
```

### Example 9: Health Check Integration

```csharp
public async Task<HealthStatus> CheckCacheHealthAsync() {
    var healthCheck = new HealthCheckService(_cacheService);
    
    var isHealthy = await healthCheck.IsCacheHealthyAsync();
    var responseTime = await healthCheck.MeasureResponseTimeAsync();
    var memoryUsage = await healthCheck.GetMemoryUsageAsync();
    
    return new HealthStatus {
        IsHealthy = isHealthy,
        ResponseTimeMs = responseTime,
        MemoryMb = memoryUsage,
        Timestamp = DateTime.UtcNow
    };
}
```

### Example 10: Key Pattern Invalidation

```csharp
public async Task InvalidateCategoryAsync(int categoryId) {
    // Invalidate all products in category
    await _cacheService.InvalidateAsync($"product:category:{categoryId}:*");
    
    // Invalidate category listing
    await _cacheService.InvalidateAsync($"categories:list");
    
    // Invalidate search results
    await _cacheService.InvalidateAsync("search:*");
}
```

### Example 11: Retry Logic with Exponential Backoff

```csharp
public async Task<T> GetWithRetryAsync<T>(string key, Func<Task<T>> factory) 
    where T : class {
    var policy = new RetryPolicy {
        MaxAttempts = 3,
        InitialDelayMs = 100,
        BackoffMultiplier = 2
    };
    
    return await RetryHelper.ExecuteWithRetryAsync(async () => {
        var cached = await _cacheService.GetAsync<T>(key);
        if (cached != null) return cached;
        
        var data = await factory();
        if (data != null) {
            await _cacheService.SetAsync(key, data, TimeSpan.FromHours(1));
        }
        return data;
    }, policy);
}
```

## API Reference

### ICacheService

```csharp
public interface ICacheService {
    // Retrieve
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    
    // Store
    Task SetAsync<T>(string key, T value, TimeSpan expiration, 
        CancellationToken ct = default);
    Task SetIfNotExistsAsync<T>(string key, T value, TimeSpan expiration,
        CancellationToken ct = default);
    
    // Remove
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task InvalidateAsync(string keyPattern, CancellationToken ct = default);
    
    // Utilities
    Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern,
        CancellationToken ct = default);
    Task<long> GetExpireSecondsAsync(string key, CancellationToken ct = default);
    Task<string> GetInfoAsync(CancellationToken ct = default);
    
    // Transactions
    Task<bool> AcquireLockAsync(string key, TimeSpan duration,
        CancellationToken ct = default);
    Task<bool> ReleaseLockAsync(string key, CancellationToken ct = default);
}
```

### RedisCacheService Methods

**Get Operations**
- `GetAsync<T>(key)` - Retrieve cached value
- `GetAsync<T>(key, default)` - With default value
- `GetAsync<T>(keys)` - Batch retrieve

**Set Operations**
- `SetAsync<T>(key, value, ttl)` - Store with expiration
- `SetIfNotExistsAsync<T>(key, value, ttl)` - Atomic only-if-not-exists
- `IncrementAsync(key)` - Atomic increment

**Removal Operations**
- `RemoveAsync(key)` - Delete single key
- `RemoveAsync(keys)` - Batch delete
- `InvalidateAsync(pattern)` - Pattern-based invalidation

**Utility Methods**
- `ExistsAsync(key)` - Check existence
- `GetKeysByPatternAsync(pattern)` - Find matching keys
- `GetExpireSecondsAsync(key)` - Get TTL
- `GetInfoAsync()` - Redis server info

**Lock Operations**
- `AcquireLockAsync(key, duration)` - Distributed lock
- `ReleaseLockAsync(key)` - Release lock
- `ExtendLockAsync(key, duration)` - Extend lock duration

## CLI Reference

### Cache Commands

```bash
# Get value from cache
dotnet run -- cache get <key>

# Set value in cache
dotnet run -- cache set <key> <value> [ttl-seconds]

# Delete from cache
dotnet run -- cache delete <key>

# Invalidate by pattern
dotnet run -- cache invalidate <pattern>

# Show cache info
dotnet run -- cache info

# Clear entire cache
dotnet run -- cache clear

# Acquire distributed lock
dotnet run -- cache lock acquire <key> [duration-seconds]

# Release distributed lock
dotnet run -- cache lock release <key>
```

### Product Commands

```bash
# List products
dotnet run -- product list

# Get product by ID
dotnet run -- product get <id>

# Create product
dotnet run -- product create <name> <price> [stock]

# Update product
dotnet run -- product update <id> <name> <price> [stock]

# Delete product
dotnet run -- product delete <id>

# Warm cache
dotnet run -- product warm-cache
```

## Configuration Guide

### Redis Configuration

In `appsettings.json`:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379,allowAdmin=true",
    "DefaultDatabase": 0,
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000,
    "MaxPoolSize": 10,
    "AbortOnConnectFail": false,
    "ReconnectInterval": 60000
  }
}
```

### Cache Configuration

```json
{
  "Cache": {
    "DefaultExpirationSeconds": 3600,
    "MaxKeyLength": 256,
    "EnableCompression": true,
    "CompressionThreshold": 1024,
    "EnableMetrics": true,
    "LockTimeoutSeconds": 30,
    "MaxLockWaitMs": 5000
  }
}
```

### Configuration via Code

```csharp
services.AddRedisCacheServices(connectionString, options => {
    options.DefaultExpirationSeconds = 1800;
    options.EnableCompression = true;
    options.CompressionThreshold = 2048;
    options.EnableMetrics = true;
    options.LockTimeoutSeconds = 20;
});
```

## Monitoring & Diagnostics

### Built-in Metrics

The project provides real-time cache metrics:

```csharp
var metrics = await cacheService.GetMetricsAsync();
Console.WriteLine($"Hit Rate: {metrics.HitRate:P}");
Console.WriteLine($"Total Keys: {metrics.TotalKeys}");
Console.WriteLine($"Memory: {metrics.EstimatedMemoryMb} MB");
```

### Health Checks

```csharp
var health = await cacheService.GetHealthAsync();
if (health.IsHealthy) {
    Console.WriteLine("Cache is operational");
    Console.WriteLine($"Response time: {health.ResponseTimeMs}ms");
}
```

### Structured Logging

All cache operations are logged with context:

```
info: RedisCachePatterns.Services.RedisCacheService[0]
      Cache HIT for key 'product:123' (took 2.5ms)
      
warn: RedisCachePatterns.Services.RedisCacheService[0]
      Cache MISS for key 'product:456' - loading from source
      
error: RedisCachePatterns.Services.RedisCacheService[0]
       Cache operation failed: Connection timeout after 5000ms
```

## Troubleshooting

### Issue: "Cannot connect to Redis"

**Cause**: Redis service not running or wrong connection string.

**Solution**:
```bash
# Check if Redis is running
redis-cli ping

# If not running, start Redis
redis-server

# Or use Docker
docker run -d -p 6379:6379 redis:7-alpine
```

### Issue: "Cache key too long"

**Cause**: Key length exceeds configured maximum (default 256).

**Solution**:
```csharp
// Use hashing for long keys
var hash = SHA256.HashData(Encoding.UTF8.GetBytes(longKey));
var shortKey = Convert.ToHexString(hash).Substring(0, 32);
```

### Issue: "Distributed lock timeout"

**Cause**: Lock not released or held by crashed instance.

**Solution**:
```csharp
// Use explicit lock timeout
var lockKey = "my-lock";
var acquired = await cacheService.AcquireLockAsync(
    lockKey, 
    TimeSpan.FromSeconds(30)  // Auto-release after 30s
);
```

### Issue: "Out of memory" in Redis

**Cause**: Cache not evicting or growing too large.

**Solution**:
```json
{
  "Cache": {
    "DefaultExpirationSeconds": 1800,
    "EnableCompression": true,
    "CompressionThreshold": 512
  }
}
```

### Issue: "Slow cache operations"

**Cause**: Network latency or large values.

**Solution**:
- Monitor response times: `GetMetricsAsync()`
- Enable compression for values > 1KB
- Use batch operations where possible
- Check Redis memory usage

## Contributing

### Getting Started

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Make changes with unit tests
4. Submit a pull request

### Code Standards

- Follow [C# Naming Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use nullable reference types: `#nullable enable`
- Add XML comments to public members
- Write unit tests for new features
- Keep methods focused and under 20 lines where possible

### Commit Guidelines

- Use conventional commits: `feat:`, `fix:`, `docs:`, `test:`, `refactor:`
- Example: `feat: add compression support for cached values`

### Testing

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "ClassName"

# Run with code coverage
dotnet test /p:CollectCoverage=true
```

## License

MIT License - Copyright © 2026 Vladyslav Zaiets

See [LICENSE](LICENSE) file for details.

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**

[Portfolio](https://sarmkadan.com) | [GitHub](https://github.com/Sarmkadan) | [Telegram](https://t.me/sarmkadan)
