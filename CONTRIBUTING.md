# Contributing

Thanks for helping improve Redis Cache Patterns. This guide covers the essentials; see [docs/GETTING_STARTED.md](docs/GETTING_STARTED.md) and [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for background.

## Prerequisites

- **.NET 10 SDK** — the version is pinned in `global.json` (`10.0.100`, `rollForward: latestMinor`).
- **Redis 7+** — needed to run the demo and any manual checks against a live server. The unit tests use mocks and do not connect to Redis. CI runs with Redis on `localhost:6379` and `REDIS_CONNECTION_STRING=localhost:6379`.
  - Docker, bound to localhost only:
    ```bash
    docker run --rm -d --name redis-dev -p 127.0.0.1:6379:6379 redis:7-alpine
    ```
    Stop it with `docker stop redis-dev` when you are done.
  - Or install Redis locally and run `redis-server`.

## Build

```bash
dotnet restore
dotnet build            # or: make build
```

## Test

```bash
dotnet test                                                   # whole solution
dotnet test --filter "FullyQualifiedName~CacheTagServiceTests" # one class
make coverage           # coverage report in coverage/coverage.xml
```

New behaviour needs tests. Put them in the folder that mirrors the source folder, name the file `<Class>Tests.cs`, and use the `Method_Scenario_ExpectedResult` naming pattern. Use `MockCacheService` or Moq for dependencies, and FluentAssertions for asserts.

## Coding conventions

- **Formatting:** follow `.editorconfig` (4-space indent, UTF-8, final newline, no trailing whitespace). Run `dotnet format` before committing.
- **Nullable reference types** are enabled everywhere. Do not suppress nullability warnings with `!` unless you can explain why the value is non-null.
- **Warnings:** do not add new compiler or analyzer warnings. Note that `TreatWarningsAsErrors` is currently `false` in `Directory.Build.props`, so the build will not fail on them.
- **Async:** methods that return `Task` or `Task<T>` end in `Async` and take a `CancellationToken` that is passed through.
- **ConfigureAwait:** library code (everything outside `tests/`, `examples/`, and `benchmarks/`) must call `.ConfigureAwait(false)` on every `await`. Do not omit it.
- **Guard clauses:** use `ArgumentNullException.ThrowIfNull` and `ArgumentException.ThrowIfNullOrEmpty`.
- **Errors:** throw `CacheException` for cache failures and `BusinessException` subclasses for domain errors.
- **File headers:** every source file starts with `#nullable enable` and the author banner. Keep it on new files.
- **Public API:** add XML `<summary>` docs to public types and members.

## Branching and pull requests

1. Branch from an up-to-date `main`: `git switch -c feat/short-description` (or `fix/`, `docs/`, `chore/`).
2. Keep each PR small and focused on one change. Split unrelated work into separate PRs.
3. Use conventional commit prefixes (`feat:`, `fix:`, `docs:`, `chore:`).
4. Before opening the PR, run locally:
   ```bash
   dotnet build -c Release
   dotnet test -c Release
   dotnet format --verify-no-changes
   ```
5. Open the PR against `main`. The **Build and Test** workflow must be green before merge. Do not merge with failing checks.
6. In the PR description, state:
   - **What** changed and **why**.
   - **How it was tested**: the commands you ran and any manual checks against Redis.

Do not force-push to a PR branch while it is under review unless a reviewer asks for it.

## Reporting issues

- Bugs and feature requests: [GitHub Issues](https://github.com/sarmkadan/redis-cache-patterns/issues). Include the .NET SDK version, the Redis version, and the OS, plus reproduction steps for bugs.
- Security vulnerabilities: do not open a public issue. Follow [SECURITY.md](SECURITY.md).

## License

By contributing, you agree that your contributions are licensed under the MIT License. See [LICENSE](LICENSE).
