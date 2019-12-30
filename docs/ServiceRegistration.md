# ServiceRegistration

Provides extension methods for registering Redis cache patterns and background worker services in the dependency injection container.

## API

### `AddRedisCachePatterns(IServiceCollection services, Action<RedisCachePatternsOptions> configureOptions = null)`

Registers core Redis cache pattern services including distributed cache, cache invalidation, and synchronization primitives.

- **services**: The `IServiceCollection` to add services to.
- **configureOptions**: Optional action to configure `RedisCachePatternsOptions`.
- Returns: The `IServiceCollection` for chaining.
- Throws: `ArgumentNullException` if `services` is `null`.

### `AddRedisCachePatterns(IServiceCollection services, IConfiguration configuration, Action<RedisCachePatternsOptions> configureOptions = null)`

Registers core Redis cache pattern services using configuration values from the provided `IConfiguration`.

- **services**: The `IServiceCollection` to add services to.
- **configuration**: The `IConfiguration` containing Redis connection and pattern settings.
- **configureOptions**: Optional action to configure `RedisCachePatternsOptions`.
- Returns: The `IServiceCollection` for chaining.
- Throws: `ArgumentNullException` if `services` or `configuration` is `null`.

### `AddRedisCachePatterns(IServiceCollection services, string configurationSectionName, Action<RedisCachePatternsOptions> configureOptions = null)`

Registers core Redis cache pattern services using configuration values from the specified section in `IConfiguration`.

- **services**: The `IServiceCollection` to add services to.
- **configurationSectionName**: The name of the configuration section containing Redis connection and pattern settings.
- **configureOptions**: Optional action to configure `RedisCachePatternsOptions`.
- Returns: The `IServiceCollection` for chaining.
- Throws: `ArgumentNullException` if `services` is `null` or `configurationSectionName` is `null` or whitespace.

### `AddBackgroundWorkers(IServiceCollection services)`

Registers background worker services for cache invalidation and synchronization.

- **services**: The `IServiceCollection` to add services to.
- Returns: The `IServiceCollection` for chaining.
- Throws: `ArgumentNullException` if `services` is `null`.

### `AddDistributedInvalidation(IServiceCollection services)`

Registers services for distributed cache invalidation using Redis pub/sub.

- **services**: The `IServiceCollection` to add services to.
- Returns: The `IServiceCollection` for chaining.
- Throws: `ArgumentNullException` if `services` is `null`.

## Usage

### Basic Setup
