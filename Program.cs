#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Configuration;
using RedisCachePatterns.Infrastructure.Cache;
using RedisCachePatterns.Monitoring;
using RedisCachePatterns.Services;
using RedisCachePatterns.Domain;

// Configure dependency injection
var services = new ServiceCollection();

// Logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Configure RedisCachePatternsOptions from environment variables
services.Configure<RedisCachePatternsOptions>(options =>
{
    options.ConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379";
    options.DatabaseId = int.TryParse(Environment.GetEnvironmentVariable("REDIS_DATABASE"), out var db) ? db : 0;
    options.ConnectTimeoutMs = int.TryParse(Environment.GetEnvironmentVariable("REDIS_CONNECT_TIMEOUT"), out var ct) ? ct : 5000;
    options.SyncTimeoutMs = int.TryParse(Environment.GetEnvironmentVariable("REDIS_SYNC_TIMEOUT"), out var st) ? st : 5000;
    options.EnableCompression = bool.TryParse(Environment.GetEnvironmentVariable("REDIS_COMPRESSION"), out var comp) && comp;
    options.MaxCacheSizeBytes = int.TryParse(Environment.GetEnvironmentVariable("REDIS_MAX_SIZE"), out var size) ? size : 104857600;
    options.EvictionPolicy = Environment.GetEnvironmentVariable("REDIS_EVICTION_POLICY") ?? "allkeys-lru";
});

// Register Redis cache patterns services using IOptions pattern
// Registers connection, cache service, repositories and business services -
// no need to duplicate those registrations here
services.AddRedisCachePatterns();

// Decorate the ICacheService with stampede protection
services.Decorate<ICacheService, StampedeProtectedCacheService>();

services.AddSingleton<HealthCheckService>();

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

// API mode lives in Program.Web.cs (ASP.NET host); this entry point is the console demo
if (Environment.GetEnvironmentVariable("API_MODE")?.ToLower() == "true")
{
    logger.LogWarning("API_MODE is set, but the HTTP host is a separate entry point (Program.Web.cs). Running the console demonstration instead.");
}

logger.LogInformation("Running in demonstration mode");
await RunDemonstrationAsync(serviceProvider, logger);

async Task RunDemonstrationAsync(IServiceProvider sp, ILogger logger)
{
    logger.LogInformation("=== Redis Cache Patterns Demonstration ===");

    try
    {
        var redisConnection = sp.GetRequiredService<IRedisConnection>();
        var isConnected = await redisConnection.IsConnectedAsync();

        if (!isConnected)
        {
            logger.LogWarning("Redis connection failed. Using in-memory repositories for demonstration.");
        }
        else
        {
            logger.LogInformation("Connected to Redis successfully");
        }

        // Create sample data
        var userService = sp.GetRequiredService<UserService>();
        var productService = sp.GetRequiredService<ProductService>();
        var orderService = sp.GetRequiredService<OrderService>();
        var inventoryService = sp.GetRequiredService<InventoryService>();

        // 1. Create and cache a user
        logger.LogInformation("\n--- Creating User (Write-Through) ---");
        var user = new User
        {
            Username = "john_doe",
            Email = "john@example.com",
            FirstName = "John",
            LastName = "Doe",
            PasswordHash = "hashed_password_123"
        };
        var createdUser = await userService.CreateUserAsync(user);
        logger.LogInformation("User created: {UserId} - {Username}", createdUser.Id, createdUser.Username);

        // 2. Retrieve user (Cache-Aside pattern)
        logger.LogInformation("\n--- Retrieving User (Cache-Aside) ---");
        var retrievedUser = await userService.GetUserByIdAsync(createdUser.Id);
        logger.LogInformation("User retrieved: {Username} - {Email}", retrievedUser?.Username, retrievedUser?.Email);

        // 3. Create products
        logger.LogInformation("\n--- Creating Products ---");
        var product1 = new Product
        {
            Name = "Laptop",
            Description = "High-performance laptop",
            Sku = "LAPTOP-001",
            Price = 1299.99m,
            StockQuantity = 50,
            ReorderLevel = 10,
            Category = "Electronics"
        };

        var product2 = new Product
        {
            Name = "Mouse",
            Description = "Wireless mouse",
            Sku = "MOUSE-001",
            Price = 29.99m,
            StockQuantity = 5,
            ReorderLevel = 20,
            Category = "Accessories"
        };

        var createdProduct1 = await productService.CreateProductAsync(product1);
        var createdProduct2 = await productService.CreateProductAsync(product2);
        logger.LogInformation("Products created: {Product1} (${Price1}), {Product2} (${Price2})",
            createdProduct1.Name, createdProduct1.Price, createdProduct2.Name, createdProduct2.Price);

        // 4. Get low stock products
        logger.LogInformation("\n--- Low Stock Products ---");
        var lowStockProducts = await productService.GetLowStockProductsAsync();
        foreach (var p in lowStockProducts)
        {
            logger.LogWarning("Low stock: {ProductName} - {Stock} units", p.Name, p.StockQuantity);
        }

        // 5. Create an order
        logger.LogInformation("\n--- Creating Order ---");
        var order = new Order
        {
            UserId = createdUser.Id,
            ShippingAddress = "123 Main St, City, State 12345",
            BillingAddress = "123 Main St, City, State 12345"
        };

        var orderItem = new OrderItem
        {
            ProductId = createdProduct1.Id,
            Quantity = 2,
            UnitPrice = createdProduct1.Price
        };

        order.AddItem(orderItem);
        order.RecalculateTotal();

        var createdOrder = await orderService.CreateOrderAsync(order);
        logger.LogInformation("Order created: {OrderNumber} - {Items} items, Total: ${Total}",
            createdOrder.OrderNumber, createdOrder.GetItemCount(), createdOrder.TotalAmount);

        // 6. Confirm order with distributed lock
        logger.LogInformation("\n--- Confirming Order (Distributed Lock) ---");
        var instanceId = Guid.NewGuid().ToString();
        var orderConfirmed = await orderService.ConfirmOrderAsync(createdOrder.Id, instanceId);
        logger.LogInformation("Order confirmed: {Result}", orderConfirmed ? "Success" : "Failed");

        // 7. Get cache statistics
        logger.LogInformation("\n--- Cache Statistics ---");
        var cacheService = sp.GetRequiredService<ICacheService>();
        var stats = await cacheService.GetStatisticsAsync();
        logger.LogInformation("Total Keys: {Keys}, Memory: {Memory}KB, Hit Rate: {HitRate}%",
            stats.TotalKeys, stats.MemoryUsedBytes / 1024, stats.HitRate.ToString("F2"));

        logger.LogInformation("\n=== Demonstration Complete ===");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during demonstration");
    }
}
