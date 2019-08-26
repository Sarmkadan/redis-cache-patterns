# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-05-04

### Added
- Comprehensive documentation suite (GETTING_STARTED, ARCHITECTURE, API_REFERENCE, DEPLOYMENT, FAQ)
- 7 complete example programs demonstrating all patterns
- Docker and docker-compose configuration for local development
- GitHub Actions CI/CD workflow with security scanning
- Health check endpoints for monitoring
- Batch operation examples for improved performance
- Distributed lock timeout configuration
- Cache compression support for large values
- Cache metrics collection and reporting
- Error handling with graceful degradation examples
- Resilience patterns (circuit breaker, bulkhead isolation)

### Changed
- Improved documentation with 2000+ word README
- Enhanced API reference with complete method signatures
- Updated examples to show production-ready patterns
- Refined error messages for better debugging

### Fixed
- Connection string validation in configuration
- Lock release in exception scenarios
- Compression threshold handling

## [1.1.0] - 2026-04-20

### Added
- Distributed lock implementation with timeout
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

### Changed
- Upgraded StackExchange.Redis to 2.8.0
- Improved serialization performance with custom options
- Enhanced error handling with custom exceptions
- Refactored service layer for better testability

### Fixed
- Handle null values in cache properly
- Prevent cache stampede with distributed locks
- Connection pooling improvements

## [1.0.0] - 2026-04-05

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

## [0.9.0] - 2026-03-25

### Added
- Initial project structure
- Basic Redis connection management
- Dependency injection setup
- Configuration management

### Changed
- Transitioned from POC to production-ready codebase

## [0.1.0] - 2026-03-10

### Added
- Project initialization
- .NET 10 target framework setup
- Basic Redis connectivity
- README and LICENSE

## Upgrade Guide

### From 1.1.0 to 1.2.0

No breaking changes. Just update the NuGet package:

```bash
dotnet add package RedisCachePatterns --version 1.2.0
```

### From 1.0.0 to 1.1.0

New features (backward compatible):

```csharp
// Distributed locks now support timeout
var acquired = await cache.AcquireLockAsync(key, TimeSpan.FromSeconds(30));

// New batch operations available
var items = new[] { "key1", "key2", "key3" };
var results = await cache.GetAsync<MyType>(items);

// New metrics available
var hitRate = await metrics.GetHitRateAsync();
```

### From 0.9.0 to 1.0.0

Breaking changes:
- `CacheService` renamed to `ICacheService` interface
- `SetAsync` now requires `TimeSpan expiration` parameter
- Configuration API changed - use `AddRedisCacheServices`

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

## Version Policy

- **Patch versions (x.y.Z)**: Bug fixes and minor improvements
- **Minor versions (x.Y.z)**: New features, backward compatible
- **Major versions (X.y.z)**: Breaking changes, significant overhauls

## Support Matrix

| Version | Status | .NET | Released | EOL |
|---------|--------|------|----------|-----|
| 1.2.0 | Current | 10.0 | 2026-05-04 | 2027-05-04 |
| 1.1.0 | Maintained | 10.0 | 2026-04-20 | 2027-04-20 |
| 1.0.0 | Maintained | 10.0 | 2026-04-05 | 2027-04-05 |
| 0.9.0 | EOL | 10.0 | 2026-03-25 | 2026-06-25 |
| 0.1.0 | EOL | 10.0 | 2026-03-10 | 2026-06-10 |

## Security Fixes

### Version 1.2.0
- Improved connection string validation
- Enhanced lock management
- Better error message handling

### Version 1.1.0
- Connection pooling security improvements
- Serialization safety enhancements

### Version 1.0.0
- Initial security review passed
- No known vulnerabilities

## Known Issues

### Version 1.2.0
- None reported

### Version 1.1.0
- None reported

### Version 1.0.0
- None reported

## Roadmap

### Planned for 1.3.0
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
3. Create git tag: `git tag v1.2.0`
4. Push tag: `git push origin v1.2.0`
5. GitHub Actions builds and publishes NuGet package

## References

- [Keep a Changelog](https://keepachangelog.com)
- [Semantic Versioning](https://semver.org)
- [Redis Documentation](https://redis.io/docs)
- [.NET Release Cycle](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support)
