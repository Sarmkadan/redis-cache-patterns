// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Infrastructure.Cache;
using RedisCachePatterns.Infrastructure.Repositories;
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

// Redis
var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379";
services.AddSingleton<IRedisConnection>(sp =>
    new RedisConnection(redisConnectionString, sp.GetRequiredService<ILogger<RedisConnection>>()));

// Cache service
services.AddSingleton<ICacheService, RedisCacheService>();

// Repositories
services.AddSingleton<IUserRepository, UserRepository>();
services.AddSingleton<IProductRepository, ProductRepository>();
services.AddSingleton<IOrderRepository, OrderRepository>();
services.AddSingleton<IInventoryRepository, InventoryRepository>();

// Services
services.AddSingleton<UserService>();
services.AddSingleton<ProductService>();
services.AddSingleton<OrderService>();
services.AddSingleton<InventoryService>();

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

// Demonstrate caching patterns
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
