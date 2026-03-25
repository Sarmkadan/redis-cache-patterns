# Contributing to Redis Cache Patterns

Thank you for your interest in contributing to the Redis Cache Patterns project! We welcome contributions from the community. This document provides guidelines for contributing to the project.

## Getting Started

### Prerequisites

- **.NET 10.0 SDK** or later — [Download here](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Redis 7.0+** — [Installation guide](https://redis.io/download)
- **Git**

### Development Setup

1. **Fork the repository**
   ```bash
   # Visit https://github.com/sarmkadan/redis-cache-patterns and click "Fork"
   ```

2. **Clone your fork**
   ```bash
   git clone https://github.com/YOUR_USERNAME/redis-cache-patterns.git
   cd redis-cache-patterns
   ```

3. **Add upstream remote**
   ```bash
   git remote add upstream https://github.com/sarmkadan/redis-cache-patterns.git
   ```

4. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   # or for bug fixes:
   git checkout -b fix/issue-description
   ```

5. **Set up Redis locally**
   ```bash
   # Using Docker Compose
   docker-compose up -d
   
   # Or install Redis locally and start it
   redis-server
   ```

6. **Restore dependencies and build**
   ```bash
   dotnet restore
   dotnet build
   ```

## Development Workflow

### Before Making Changes

- Create an issue first to discuss larger features
- Check the [architecture documentation](docs/ARCHITECTURE.md) to understand the project structure
- Review existing code to understand conventions and patterns

### Code Style & Conventions

We follow these guidelines:

- **XML Documentation** — All public methods must have XML doc comments (`/// <summary>`, `/// <param>`, `/// <returns>`)
- **Author Headers** — Preserve existing author headers at the top of files
- **Naming** — Follow C# conventions (PascalCase for classes, properties, methods; camelCase for local variables)
- **Async/Await** — Use async patterns consistently; async methods should be suffixed with `Async`
- **Dependency Injection** — Leverage Microsoft.Extensions.DependencyInjection throughout
- **Error Handling** — Use custom exceptions (e.g., `CacheException`, `BusinessException`) with meaningful messages
- **Testing** — Write unit tests for new features; maintain or improve code coverage

### Building & Testing

```bash
# Build the project
dotnet build

# Run tests
dotnet test

# Run specific test file
dotnet test --filter "TestClassName"

# Build with release configuration
dotnet build -c Release
```

### Code Review Checklist

Before submitting a pull request, ensure:

- ✅ Code builds without warnings
- ✅ All tests pass
- ✅ New public APIs have XML documentation
- ✅ Author headers are preserved in modified files
- ✅ No hardcoded values; use `AppConstants` or configuration
- ✅ Proper logging using `ILogger<T>`
- ✅ Exception handling is comprehensive
- ✅ Changes don't break existing functionality

## Submitting Changes

### Commit Messages

Use clear, descriptive commit messages:

```
Add cache invalidation by pattern matching

- Implement KeyPatternMatcher utility
- Add InvalidateByPatternAsync method to RedisCacheService
- Add unit tests for pattern matching scenarios
```

### Creating a Pull Request

1. **Push your branch**
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create a pull request**
   - Go to https://github.com/sarmkadan/redis-cache-patterns/pulls
   - Click "New Pull Request"
   - Select your fork and branch
   - Fill in the PR description with:
     - What problem does this solve?
     - How was this tested?
     - Links to related issues

3. **Address feedback**
   - Push additional commits to address review comments
   - Do not force-push unless requested

## Reporting Issues

### Bug Reports

Use [GitHub Issues](https://github.com/sarmkadan/redis-cache-patterns/issues) to report bugs:

1. Check if the issue already exists
2. Provide a clear title and description
3. Include reproduction steps
4. Provide environment details:
   - .NET SDK version
   - Redis version
   - Operating system

### Feature Requests

When proposing new features:

1. Check if a similar feature is already requested
2. Explain the use case and benefits
3. Provide examples of how it would be used
4. Link to related issues or discussions

### Security Issues

**Do not** open public issues for security vulnerabilities. See [SECURITY.md](SECURITY.md) for responsible disclosure procedures.

## License

By contributing to this project, you agree that your contributions will be licensed under the MIT License. See [LICENSE](LICENSE) for details.

## Questions?

- Open a [GitHub Discussion](https://github.com/sarmkadan/redis-cache-patterns/discussions)
- Check the [FAQ](docs/FAQ.md) and [Getting Started](docs/GETTING_STARTED.md) guides
- Review existing [examples](examples/)

Thank you for contributing to making Redis Cache Patterns better!
