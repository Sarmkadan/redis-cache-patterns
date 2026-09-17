# CLAUDE.md

## Project overview

`RedisCachePatterns` - a .NET 10 class library (NuGet `sarmkadan.redis-cache-patterns`) of production Redis caching patterns on top of StackExchange.Redis: cache-aside, write-through, distributed locks, stampede protection, tagging/invalidation, circuit breaker, compression, metrics.

## Build

SDK pinned in `global.json` (10.0.100, rollForward latestMinor). Solution: `redis-cache-patterns.sln` (library + tests + benchmarks).

```bash
dotnet restore
dotnet build -c Debug --no-restore          # or: make build
dotnet build -c Release                     # or: make release
dotnet publish -c Release -o ./publish      # or: make publish
docker build -t redis-cache-patterns:latest . # or: make docker-build / docker-compose up
```

Notes:
- `RedisCachePatterns.csproj` excludes `tests/**`, `examples/**`, `benchmarks/**`, `Program.cs` and `Program.Web.cs` from compilation. `Program.cs` (console demo) and `Program.Web.cs` (ASP.NET host) are reference entry points, not built by default. ASP.NET packages are referenced only when `-p:DockerMode=true`.
- `Directory.Build.props`: `TreatWarningsAsErrors=false`, XML-doc warnings (1591 etc.) suppressed, `GenerateDocumentationFile=true`.

## Test

xUnit 2.9 + FluentAssertions 7 + Moq 4.20 in `tests/redis-cache-patterns.Tests/`.

```bash
dotnet test                                                  # whole solution
dotnet test tests/redis-cache-patterns.Tests -c Debug        # test project only
dotnet test --filter "FullyQualifiedName~CacheTagServiceTests"
make test        # build + test, verbosity normal
make coverage    # coverlet, opencover -> coverage/coverage.xml
```

Conventions:
- Test folders mirror source folders (`tests/.../Services`, `Monitoring`, `Domain`, `API`, `Utilities`); files named `<Class>Tests.cs`.
- Method names: `Method_Scenario_ExpectedResult` (e.g. `Enqueue_WhenBatchSizeReached_ProcessesImmediately`).
- No real Redis in tests: `MockCacheService` / Moq (`Mock<ILogger<T>>`) are used; FluentAssertions `.Should()` for asserts.
- Benchmarks (BenchmarkDotNet) live in `benchmarks/redis-cache-patterns.Benchmarks/`.

## Lint / Format

```bash
dotnet format                                   # make format
dotnet format --verify-no-changes               # make check
dotnet build -c Release -p:EnableNETAnalyzers=true   # make analyze
```

Style comes from `.editorconfig` (4-space indent, utf-8, final newline, trailing whitespace trimmed).

## Architecture

Single library project, folder = namespace (`RedisCachePatterns.<Folder>`):

| Folder | Role |
|---|---|
| `Services/` | Core: `ICacheService`, `RedisCacheService`, decorators (`StampedeProtectedCacheService`, `CompressedCacheService`, `CircuitBreakerCacheService`, `NegativeCacheService`), `CacheTagService`, `CacheInvalidationService`, `CacheWarmingService`, business services (`ProductService`, `OrderService`, `UserService`, `InventoryService`) |
| `Infrastructure/Cache/` | `IRedisConnection`/`RedisConnection`, cluster variants (StackExchange.Redis multiplexer wrappers) |
| `Infrastructure/Repositories/` | `IRepository<T>` + in-memory/demo repositories |
| `Configuration/` | DI registration (`ServiceRegistration.AddRedisCachePatterns`, `DependencyInjectionExtensions`, `ClusterDependencyInjectionExtensions`), `CacheConfigurationBuilder`, `RedisCachePatternsOptions` (IOptions), `AppConstants` |
| `Domain/` | POCO models (`Product`, `Order`, `User`, `CacheEntry`, `CachePolicy`, `DistributedLock`, ...) |
| `Monitoring/` | metrics collector, statistics aggregator, `HealthCheckService`, diagnostics |
| `API/`, `Middleware/` | endpoint classes (`ApiEndpointBase`) and ASP.NET middleware for the web host |
| `Events/`, `BackgroundWorkers/`, `Integration/`, `Utilities/`, `Extensions/`, `Formatters/` | supporting pieces |
| `Exceptions/`, `Results/` | `CacheException`, `BusinessException` hierarchy, `OperationResult` |
| `CLI/` | `CommandParser`, `CacheCommand`, `ProductCommand` for the console demo |
| `examples/` | numbered usage examples (not compiled) |
| `docs/` | `ARCHITECTURE.md`, `API_REFERENCE.md`, `GETTING_STARTED.md`, `DEPLOYMENT.md`, `FAQ.md` |

Entry points: `Program.cs` (console demo, `RunDemonstrationAsync`), `Program.Web.cs` (minimal API host). Redis address from `REDIS_CONNECTION_STRING` / `REDIS_CONNECTION`, default `localhost:6379`; other `REDIS_*` env vars map to `RedisCachePatternsOptions`.

Patterns: decorator chain over `ICacheService` (registered via `services.Decorate<ICacheService, X>()`), cache-aside via `GetOrLoadAsync(key, loadFn)`, options pattern for config, repository pattern for data sources.

## Conventions

- Every source file starts with `#nullable enable` and the author banner comment (`Author: Vladyslav Zaiets | https://sarmkadan.com`). Keep it on new files.
- File-scoped namespaces, `ImplicitUsings` on, nullable enabled everywhere, `LangVersion latest`.
- One public type per file; companion files `<Type>Extensions.cs`, `<Type>JsonExtensions.cs` (System.Text.Json, camelCase, `JsonSerializerDefaults.Web`), `<Type>Validation.cs` sit next to the type.
- XML `<summary>` docs on public types and members; section separators `// ----` inside large interfaces.
- Async everywhere: `Task`/`Task<T>`, `Async` suffix, `CancellationToken` passed through.
- Guard clauses via `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrEmpty`.
- Error handling: domain errors throw `BusinessException` subclasses (`NotFoundException`, ... with `ErrorCode`); cache failures throw `CacheException`; non-throwing APIs return `OperationResult.Ok()/Fail(message, errorCode)`. Redis connection failures are degraded gracefully (fall back to loader), not propagated.
- DI: constructor injection, `ILogger<T>` in every service, registration through `IServiceCollection` extension methods in `Configuration/`; singletons for connection/cache services.
- Cache keys: `entity:id` (e.g. `product:1`), built via `CacheKeyExtensions` / validated by `CacheKeyValidation`.
- Commit messages: conventional prefixes (`feat:`, `chore:`, `docs:`). Do not add `Co-Authored-By` trailers.
