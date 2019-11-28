# ServiceCollectionExtensions

Extension methods for registering Redis-based cache patterns in the dependency injection container. These methods configure common cross-cutting concerns such as auditing, batch processing, idempotency, and performance monitoring using Redis as the backing store.

## API

### `AddAuditing`

Registers services required for auditing cache operations. This includes tracking cache hits, misses, and mutations using Redis streams.

- **Parameters**
  - `services` (`IServiceCollection`): The service collection to configure.
  - `configure` (`Action<AuditingOptions>`, optional): Optional configuration for auditing behavior such as stream name and retention policies.

- **Return Value**
  Returns the `IServiceCollection` for method chaining.

- **Exceptions**
  Throws `ArgumentNullException` if `services` is `null`.

### `AddBatchProcessing<T>`

Registers services for batch processing of cache operations using Redis pipelines. The generic type `T` represents the batch request model.

- **Parameters**
  - `services` (`IServiceCollection`): The service collection to configure.
  - `configure` (`Action<BatchProcessingOptions>`, optional): Optional configuration for batch size, timeouts, and retry policies.

- **Return Value**
  Returns the `IServiceCollection` for method chaining.

- **Exceptions**
  Throws `ArgumentNullException` if `services` is `null`.

### `AddIdempotency`

Registers services for ensuring idempotent cache operations. This prevents duplicate processing of the same request by tracking operation identifiers in Redis.

- **Parameters**
  - `services` (`IServiceCollection`): The service collection to configure.
  - `configure` (`Action<IdempotencyOptions>`, optional): Optional configuration for lock duration, retry behavior, and storage key prefixes.

- **Return Value**
  Returns the `IServiceCollection` for method chaining.

- **Exceptions**
  Throws `ArgumentNullException` if `services` is `null`.

### `AddPerformanceMonitoring`

Registers services for monitoring Redis cache performance metrics such as latency, throughput, and error rates.

- **Parameters**
  - `services` (`IServiceCollection`): The service collection to configure.
  - `configure` (`Action<PerformanceMonitoringOptions>`, optional): Optional configuration for metric collection intervals and storage.

- **Return Value**
  Returns the `IServiceCollection` for method chaining.

- **Exceptions**
  Throws `ArgumentNullException` if `services` is `null`.

## Usage

### Example 1: Basic Setup
