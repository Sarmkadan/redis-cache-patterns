# Getting Started with Redis Cache Patterns

This guide will help you quickly integrate Redis caching into your .NET 10 application.

## Prerequisites

- .NET 10 SDK or later
- Redis 7.0 or later
- Basic C# knowledge

## 5-Minute Setup

### 1. Install .NET 10

```bash
# On macOS
brew install dotnet@10

# On Ubuntu/Debian
curl https://dot.net/v1/dotnet-install.sh | bash
export PATH="$PATH:/root/.dotnet"

# On Windows
# Download from https://dotnet.microsoft.com/download/dotnet/10.0
```

### 2. Install Redis

**Option A: Using Docker (Easiest)**

```bash
docker run -d \
  --name redis \
  -p 6379:6379 \
  redis:7-alpine
```

**Option B: Using Homebrew (macOS)**

```bash
brew install redis
redis-server
```

**Option C: Using APT (Ubuntu/Debian)**

```bash
sudo apt-get install redis-server
redis-server
```

### 3. Clone the Repository

```bash
git clone https://github.com/Sarmkadan/redis-cache-patterns.git
cd redis-cache-patterns
```

### 4. Restore and Build

```bash
dotnet restore
dotnet build
```

### 5. Run the Application

```bash
dotnet run
```

You should see the application starting with Redis connection established.

## Create Your First Cache-Enabled Service

### Step 1: Create a Domain Model

```csharp
namespace MyApp.Domain;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
```

### Step 2: Create a Repository

```csharp
using MyApp.Domain;

namespace MyApp.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);
}

public class UserRepository : IUserRepository
{
    // Your database implementation
    public async Task<User?> GetByIdAsync(int id)
    {
        // TODO: Fetch from database
        return null;
    }

    // ... other methods
}
```

### Step 3: Create a Cached Service

```csharp
using MyApp.Domain;
using MyApp.Repositories;
using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;

namespace MyApp.Services;

public class UserService
{
    private readonly IUserRepository _repository;
    private readonly ICacheService _cache;

    public UserService(IUserRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    // Cache-Aside Pattern
    public async Task<User?> GetUserAsync(int id)
    {
        var cacheKey = $"user:{id}";
        
        // Try cache first
        var cached = await _cache.GetAsync<User>(cacheKey);
        if (cached != null) return cached;
        
        // Cache miss - load from database
        var user = await _repository.GetByIdAsync(id);
        if (user != null)
        {
            // Store in cache for 1 hour
            await _cache.SetAsync(cacheKey, user, TimeSpan.FromHours(1));
        }
        
        return user;
    }

    // Write-Through Pattern
    public async Task<User> CreateUserAsync(User user)
    {
        // Create in database
        var created = await _repository.CreateAsync(user);
        
        // Store in cache
        var cacheKey = $"user:{created.Id}";
        await _cache.SetAsync(cacheKey, created, TimeSpan.FromHours(1));
        
        // Invalidate user list cache
        await _cache.InvalidateAsync("users:*");
        
        return created;
    }

    public async Task UpdateUserAsync(User user)
    {
        // Update database
        await _repository.UpdateAsync(user);
        
        // Update cache
        var cacheKey = $"user:{user.Id}";
        await _cache.SetAsync(cacheKey, user, TimeSpan.FromHours(1));
        
        // Invalidate related caches
        await _cache.InvalidateAsync("users:*");
    }

    public async Task DeleteUserAsync(int id)
    {
        // Delete from database
        await _repository.DeleteAsync(id);
        
        // Remove from cache
        var cacheKey = $"user:{id}";
        await _cache.RemoveAsync(cacheKey);
        
        // Invalidate list caches
        await _cache.InvalidateAsync("users:*");
    }
}
```

### Step 4: Register Services in Dependency Injection

```csharp
// Program.cs
using MyApp.Repositories;
using MyApp.Services;
using RedisCachePatterns.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add Redis cache services
builder.Services.AddRedisCacheServices(
    builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379",
    options =>
    {
        options.DefaultExpirationSeconds = 3600;
        options.EnableCompression = true;
        options.CompressionThreshold = 1024;
    });

// Register your services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserService>();

var app = builder.Build();

app.Run();
```

### Step 5: Use Your Service

```csharp
// In your controller or endpoint
public class UserController
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _userService.GetUserAsync(id);
        if (user == null)
            return NotFound();
        
        return Ok(user);
    }
}
```

## Configuration Guide

### Basic Configuration (appsettings.json)

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "DefaultDatabase": 0,
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000
  },
  "Cache": {
    "DefaultExpirationSeconds": 3600,
    "EnableCompression": true,
    "CompressionThreshold": 1024,
    "EnableMetrics": true
  }
}
```

### Advanced Configuration

```csharp
builder.Services.AddRedisCacheServices(
    connectionString: "localhost:6379,allowAdmin=true",
    configureOptions: options =>
    {
        // Expiration
        options.DefaultExpirationSeconds = 1800;
        options.MaxExpirationSeconds = 86400;
        
        // Compression
        options.EnableCompression = true;
        options.CompressionThreshold = 512; // Compress values > 512 bytes
        
        // Monitoring
        options.EnableMetrics = true;
        options.EnableDiagnostics = true;
        
        // Locking
        options.LockTimeoutSeconds = 30;
        options.MaxLockWaitMs = 5000;
        
        // Naming
        options.KeyPrefix = "myapp:";
        options.MaxKeyLength = 256;
    });
```

## Environment-Specific Configuration

### Development

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "ConnectTimeout": 10000,
    "SyncTimeout": 10000
  },
  "Cache": {
    "DefaultExpirationSeconds": 300,
    "EnableMetrics": true
  }
}
```

### Production

```json
{
  "Redis": {
    "ConnectionString": "redis-prod.example.com:6379,password=securepassword",
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000,
    "MaxPoolSize": 50
  },
  "Cache": {
    "DefaultExpirationSeconds": 3600,
    "EnableCompression": true,
    "CompressionThreshold": 1024,
    "EnableMetrics": true,
    "KeyPrefix": "prod:"
  }
}
```

## Testing Your Setup

### 1. Verify Redis Connection

```bash
redis-cli ping
# Expected output: PONG
```

### 2. Run the Application

```bash
dotnet run
# Should show: "✓ Redis connection established"
```

### 3. Test Cache Operations

Create a test file:

```csharp
// Test.cs
var services = new ServiceCollection();
services.AddRedisCacheServices("localhost:6379");
var provider = services.BuildServiceProvider();
var cache = provider.GetRequiredService<ICacheService>();

// Test SET
await cache.SetAsync("test-key", "test-value", TimeSpan.FromSeconds(60));
Console.WriteLine("✓ SET successful");

// Test GET
var value = await cache.GetAsync<string>("test-key");
Console.WriteLine($"✓ GET successful: {value}");

// Test REMOVE
await cache.RemoveAsync("test-key");
Console.WriteLine("✓ REMOVE successful");
```

Run: `dotnet run`

## Common Issues

### "Connection refused"

**Problem**: Cannot connect to Redis

**Solution**:
```bash
# Check if Redis is running
redis-cli ping

# If not running:
redis-server
# or
docker run -p 6379:6379 redis:7-alpine
```

### "Timeout waiting for connection"

**Problem**: Redis is slow or unreachable

**Solution**:
```csharp
// Increase timeout in configuration
options.ConnectTimeout = 10000; // 10 seconds
options.SyncTimeout = 10000;
```

### "Serialization error"

**Problem**: Cannot serialize object to cache

**Solution**:
```csharp
// Make your class JSON serializable
[Serializable]
public class MyClass
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}
```

## Next Steps

1. **Read the Architecture Guide** - Understand the layered design
2. **Review Examples** - Check `examples/` directory for patterns
3. **Explore API Reference** - See complete ICacheService documentation
4. **Deploy** - Follow deployment guide for production setup

## Resources

- [Redis Documentation](https://redis.io/docs/)
- [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/)
- [.NET Dependency Injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)

## Getting Help

- Check [FAQ.md](FAQ.md) for common questions
- Review [examples/](../examples/) for working code
- Open an issue on GitHub
