# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-10-14

### Added
- Core ICacheService interface
- RedisCacheService implementation with:
  - Cache-Aside pattern support
  - Write-Through pattern support
  - Distributed locking
  - Pattern-based invalidation
  - Automatic serialization/deserialization
- Repository pattern for data access
- Generic Repository<T> base implementation
- Specialized repositories:
  - UserRepository
  - ProductRepository
  - OrderRepository
  - InventoryRepository
- Domain models:
  - User, Product, Order, OrderItem
  - InventoryItem, CachePolicy, CacheEntry
  - DistributedLock, SystemConfiguration
- Business logic services:
  - UserService
  - ProductService
  - OrderService
  - InventoryService
- Distributed lock implementation with configurable timeout
- Cache invalidation with pattern matching
- Batch cache operations (get multiple, set multiple)
- Cache warming service for pre-loading data
- Monitoring and diagnostics providers
- Health check service
- Cache metrics collector
- Compression utility for large values
- Retry helper with exponential backoff
- Rate limiting middleware
- Caching headers middleware
- Event-based cache invalidation
- Idempotency helper for safe retries
- Utility helpers:
  - CacheKeyBuilder
  - SerializationHelper
  - ValidationHelper
  - CompressionUtil
  - DistributedLockHelper
- Configuration:
  - CacheConfiguration class
  - CacheConfigurationBuilder
  - DependencyInjectionExtensions
  - ServiceRegistration
- Custom exceptions:
  - CacheException
  - BusinessException
- Extension methods:
  - StringExtensions
  - CollectionExtensions
  - ServiceCollectionExtensions
  - CacheServiceExtensions
- Result wrappers:
  - OperationResult<T>
  - OperationResult
- Middleware:
  - ErrorHandlingMiddleware
  - AuthenticationMiddleware
  - RateLimitingMiddleware
- CLI interface with cache and product commands
- API endpoints with cache support
- Background workers for cache cleanup and warming
- Event publishing and handling
- External API integration support
- Comprehensive documentation suite (GETTING_STARTED, ARCHITECTURE, API_REFERENCE, DEPLOYMENT, FAQ)
- 7 complete example programs demonstrating all patterns
- Docker and docker-compose configuration for local development
- GitHub Actions CI/CD workflow with security scanning and NuGet publish
- Health check endpoints for monitoring

### Changed
- Upgraded StackExchange.Redis to 2.8.0
- Improved serialization performance with custom options
- Enhanced error handling with custom exceptions
- Refactored service layer for better testability
- Improved connection string validation in configuration

### Fixed
- Handle null values in cache properly
- Prevent cache stampede with distributed locks
- Connection pooling improvements
- Lock release in exception scenarios
- Compression threshold handling

## [0.9.0] - 2025-09-02

### Added
- Distributed lock prototype
- Cache invalidation by key pattern
- Basic monitoring hooks
- Retry logic with exponential backoff

### Changed
- Transitioned from proof-of-concept to structured layered architecture
- Moved Redis connection management behind IRedisConnection interface
- Improved dependency injection setup

### Fixed
- Serialization edge case for nullable types
- Connection not being re-established after timeout

## [0.5.0] - 2025-07-28

### Added
- Write-Through caching pattern implementation
- CacheKeyBuilder for consistent key generation
- CachePolicy domain model for per-entity TTL configuration
- Basic middleware pipeline (error handling, logging)
- Initial unit test project with xUnit and Moq

### Changed
- ICacheService interface stabilised; breaking change from 0.1.0 prototype
- SetAsync now requires explicit TimeSpan expiration

### Fixed
- Cache miss no longer throws when key does not exist

## [0.1.0] - 2025-06-10

### Added
- Project initialization
- .NET 10 target framework setup
- Basic Redis connectivity via StackExchange.Redis
- Cache-Aside pattern prototype
- README and LICENSE

## Upgrade Guide

### From 0.9.0 to 1.0.0

Breaking changes:
- `CacheService` renamed to `ICacheService` interface
- `SetAsync` now requires `TimeSpan expiration` parameter
- Configuration API changed — use `AddRedisCacheServices`

Migration:

```csharp
// Old (0.9.0)
var cache = new CacheService(connection);
await cache.SetAsync(key, value);

// New (1.0.0)
services.AddRedisCacheServices("localhost:6379");
var cache = serviceProvider.GetRequiredService<ICacheService>();
await cache.SetAsync(key, value, TimeSpan.FromHours(1));
```

### From 0.5.0 to 0.9.0

New features (backward compatible):

```csharp
// Distributed locks now available
var acquired = await cache.AcquireLockAsync(key, TimeSpan.FromSeconds(30));

// New batch operations available
var items = new[] { "key1", "key2", "key3" };
var results = await cache.GetAsync<MyType>(items);
```

## Version Policy

- **Patch versions (x.y.Z)**: Bug fixes and minor improvements
- **Minor versions (x.Y.z)**: New features, backward compatible
- **Major versions (X.y.z)**: Breaking changes, significant overhauls

## Support Matrix

| Version | Status | .NET | Released | EOL |
|---------|--------|------|----------|-----|
| 1.0.0 | Current | 10.0 | 2025-10-14 | 2026-10-14 |
| 0.9.0 | EOL | 10.0 | 2025-09-02 | 2026-03-02 |
| 0.5.0 | EOL | 10.0 | 2025-07-28 | 2026-01-28 |
| 0.1.0 | EOL | 10.0 | 2025-06-10 | 2025-12-10 |

## Security Fixes

### Version 1.0.0
- Improved connection string validation
- Enhanced distributed lock management to prevent orphaned locks
- Serialization safety enhancements
- Initial security review passed

### Version 0.9.0
- Connection pooling security improvements

## Known Issues

### Version 1.0.0
- None reported

## Roadmap

### Planned for 1.1.0
- Redis Streams support for event sourcing
- Multi-tier caching (L1 in-memory + L2 Redis)
- Advanced sharding strategies
- Redis Module integration (RedisBloom, RedisSearch)

### Under Consideration
- Pub/Sub integration
- Geo-spatial operations
- Time series data support
- Cluster topology management

## Contributing

We welcome contributions! See [README.md](README.md#contributing) for guidelines.

### Release Process

1. Update CHANGELOG.md
2. Update version in RedisCachePatterns.csproj
3. Create git tag: `git tag v1.0.0`
4. Push tag: `git push origin v1.0.0`
5. GitHub Actions builds and publishes NuGet package

## References

- [Keep a Changelog](https://keepachangelog.com)
- [Semantic Versioning](https://semver.org)
- [Redis Documentation](https://redis.io/docs)
- [.NET Release Cycle](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support)
