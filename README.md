# Redis Cache Patterns - .NET 10

Production-ready Redis caching patterns for .NET applications, implementing cache-aside, write-through, and distributed locking strategies with comprehensive error handling and monitoring.

## Overview

This project provides a robust foundation for implementing Redis caching in .NET applications. It includes:

- **Cache-Aside Pattern**: Check cache, load from source on miss, store in cache
- **Write-Through Pattern**: Update cache and database atomically
- **Distributed Locks**: Prevent race conditions across multiple instances
- **Cache Invalidation**: Sophisticated key pattern matching and selective invalidation
- **Monitoring & Diagnostics**: Real-time cache statistics and performance tracking

## Architecture

### Core Components

- **Domain Models**: User, Product, Order, OrderItem, InventoryItem, CachePolicy, DistributedLock
- **Repository Layer**: Generic repository with specialized implementations
- **Service Layer**: Business logic with integrated caching strategies
- **Cache Service**: Redis-backed implementation of cache patterns
- **Utilities**: Validation, serialization, distributed lock helpers, cache monitoring

### Directory Structure

```
redis-cache-patterns/
├── Domain/                      # Entity models
│   ├── User.cs
│   ├── Product.cs
│   ├── Order.cs
│   ├── OrderItem.cs
│   ├── InventoryItem.cs
│   ├── CachePolicy.cs
│   ├── CacheEntry.cs
│   ├── DistributedLock.cs
│   └── SystemConfiguration.cs
├── Infrastructure/
│   ├── Cache/                   # Redis connectivity
│   │   ├── IRedisConnection.cs
│   │   └── RedisConnection.cs
│   └── Repositories/            # Data access layer
│       ├── IRepository.cs
│       ├── Repository.cs
│       ├── UserRepository.cs
│       ├── ProductRepository.cs
│       ├── OrderRepository.cs
│       └── InventoryRepository.cs
├── Services/                    # Business logic layer
│   ├── ICacheService.cs
│   ├── RedisCacheService.cs
│   ├── UserService.cs
│   ├── ProductService.cs
│   ├── OrderService.cs
│   └── InventoryService.cs
├── Configuration/               # Setup & configuration
│   ├── AppConstants.cs
│   ├── CacheConfiguration.cs
│   └── DependencyInjectionExtensions.cs
├── Utilities/                   # Helpers & tools
│   ├── CacheKeyBuilder.cs
│   ├── SerializationHelper.cs
│   ├── ValidationHelper.cs
│   ├── DistributedLockHelper.cs
│   └── CacheMonitor.cs
├── Extensions/                  # Extension methods
│   ├── StringExtensions.cs
│   ├── CollectionExtensions.cs
│   └── CacheServiceExtensions.cs
├── Exceptions/                  # Custom exceptions
│   ├── CacheException.cs
│   └── BusinessException.cs
├── Results/                     # Response wrappers
│   └── OperationResult.cs
├── Program.cs                   # Application entry point
└── RedisCachePatterns.csproj
```

## Features

### Cache Patterns

**Cache-Aside Pattern**
```csharp
// Check cache first, load from database on miss
var user = await userService.GetUserByIdAsync(userId);
```

**Write-Through Pattern**
```csharp
// Update database and cache atomically
var updatedUser = await userService.UpdateUserAsync(user);
```

**Distributed Locks**
```csharp
// Prevent concurrent modifications
await orderService.ConfirmOrderAsync(orderId, instanceId);
```

### Advanced Features

- **Automatic Cache Invalidation**: Pattern-based key deletion
- **Cache Policies**: Configurable TTL and behavior per key
- **Serialization**: JSON with proper null handling
- **Monitoring**: Real-time statistics and hit rate tracking
- **Lock Renewal**: Automatic background renewal of distributed locks
- **Error Handling**: Specific exceptions for cache operations
- **Retry Logic**: Exponential backoff with configurable attempts

## Usage

### Basic Setup

```csharp
var services = new ServiceCollection();
services.AddLogging();
services.AddRedisCachePatterns("localhost:6379");
var serviceProvider = services.BuildServiceProvider();

var userService = serviceProvider.GetRequiredService<UserService>();
var user = await userService.GetUserByIdAsync(1);
```

### Configuration

Set Redis connection via environment variable:
```bash
export REDIS_CONNECTION="redis.example.com:6379"
```

Or configure programmatically:
```csharp
var config = new CacheConfiguration
{
    ConnectionString = "redis.example.com:6379",
    DatabaseId = 0,
    ConnectTimeoutMs = 5000
};
services.AddRedisCache(config);
```

### Cache Monitoring

```csharp
var cacheService = serviceProvider.GetRequiredService<ICacheService>();
var stats = await cacheService.GetStatisticsAsync();
Console.WriteLine($"Hit Rate: {stats.HitRate}%");
Console.WriteLine($"Keys: {stats.TotalKeys}");
```

## Requirements

- .NET 10
- Redis 6.0+
- StackExchange.Redis 2.8.0+

## NuGet Dependencies

```xml
<PackageReference Include="StackExchange.Redis" Version="2.8.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.0" />
```

## License

MIT License - Copyright (c) 2026 Vladyslav Zaiets

## Author

**Vladyslav Zaiets** - CTO & Software Architect
- Website: https://sarmkadan.com
- Email: rutova2@gmail.com
