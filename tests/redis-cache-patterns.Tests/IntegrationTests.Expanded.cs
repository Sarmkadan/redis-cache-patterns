#nullable enable
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Events;
using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;
using Xunit;

namespace RedisCachePatterns.Tests;

/// <summary>
/// Expanded integration tests demonstrating comprehensive caching patterns and workflows.
/// </summary>
public class WriteThoughIntegrationTests
{
    [Fact]
    public async Task WriteThroughPattern_UpdatesDataSourceAndCacheSynchronously()
    {
        var mockCache = new MockCacheService();
        var dataSourceUpdated = false;
        var cacheKey = "product:100";
        var product = new Product
        {
            Id = 100,
            Name = "Premium Widget",
            Sku = "PWG-001",
            Price = 199.99m,
            StockQuantity = 100,
            ReorderLevel = 10,
            Category = "Premium",
            IsActive = true
        };

        var result = await mockCache.WriteAsync(
            cacheKey,
            product,
            async () =>
            {
                dataSourceUpdated = true;
                await Task.CompletedTask;
                return product;
            });

        result.Should().NotBeNull();
        dataSourceUpdated.Should().BeTrue();

        var cached = await mockCache.GetAsync<Product>(cacheKey);
        cached.Should().NotBeNull();
        cached?.Id.Should().Be(100);
    }

    [Fact]
    public async Task WriteThroughPattern_FailureInDataSource_PreventsCaching()
    {
        var mockCache = new MockCacheService();
        var product = new Product
        {
            Id = 101,
            Name = "Test",
            Sku = "TEST-001",
            Price = 50m,
            StockQuantity = 50,
            ReorderLevel = 5,
            Category = "Test",
            IsActive = true
        };

        Func<Task> act = async () =>
        {
            await mockCache.WriteAsync(
                "product:101",
                product,
                async () =>
                {
                    await Task.CompletedTask;
                    throw new InvalidOperationException("Database connection failed");
                });
        };

        await act.Should().ThrowAsync<InvalidOperationException>();

        var cached = await mockCache.GetAsync<Product>("product:101");
        cached.Should().BeNull();
    }
}

public class CacheAsideWithConcurrencyIntegrationTests
{
    [Fact]
    public async Task CacheAsidePattern_MultipleThreadsAccessingSameKey_DataLoadedOnceOnly()
    {
        var mockCache = new MockCacheService();
        var loadCount = 0;
        var lockObj = new Lock();

        var product = new Product
        {
            Id = 1,
            Name = "Concurrent Product",
            Sku = "CONC-001",
            Price = 99.99m,
            StockQuantity = 100,
            ReorderLevel = 10,
            Category = "Test",
            IsActive = true
        };

        var tasks = new List<Task<Product?>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(mockCache.GetOrLoadAsync(
                "product:concurrent",
                async () =>
                {
                    lock (lockObj)
                    {
                        loadCount++;
                    }
                    await Task.Delay(10);
                    return product;
                }));
        }

        var results = await Task.WhenAll(tasks);

        results.Should().AllSatisfy(r => r?.Id.Should().Be(1));
        loadCount.Should().Be(1);
    }

    [Fact]
    public async Task CacheAsidePattern_StaleDataRefreshed_OnSubsequentMiss()
    {
        var mockCache = new MockCacheService();
        var loadCount = 0;

        var product1 = new Product
        {
            Id = 1,
            Name = "Original",
            Sku = "SKU-001",
            Price = 50m,
            StockQuantity = 100,
            ReorderLevel = 10,
            Category = "Test",
            IsActive = true
        };

        var product2 = new Product
        {
            Id = 1,
            Name = "Updated",
            Sku = "SKU-001",
            Price = 75m,
            StockQuantity = 50,
            ReorderLevel = 10,
            Category = "Test",
            IsActive = true
        };

        var result1 = await mockCache.GetOrLoadAsync("product:1", async () =>
        {
            loadCount++;
            await Task.CompletedTask;
            return product1;
        });

        result1?.Name.Should().Be("Original");

        await mockCache.RemoveAsync("product:1");

        var result2 = await mockCache.GetOrLoadAsync("product:1", async () =>
        {
            loadCount++;
            await Task.CompletedTask;
            return product2;
        });

        result2?.Name.Should().Be("Updated");
        loadCount.Should().Be(2);
    }
}

public class DistributedLockConcurrencyIntegrationTests
{
    [Fact]
    public async Task DistributedLock_ProtectsSharedResourceFromConcurrentAccess()
    {
        var mockCache = new MockCacheService();
        var lockKey = "critical-section";
        var sharedCounter = 0;
        var maxConcurrent = 0;
        var currentConcurrent = 0;
        var lockLock = new Lock();
        var accessLog = new List<string>();

        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            var workerId = i;
            tasks.Add(ProcessCriticalSection(workerId));
        }

        await Task.WhenAll(tasks);

        sharedCounter.Should().Be(5);
        maxConcurrent.Should().Be(1);
        accessLog.Should().HaveCount(5);

        async Task ProcessCriticalSection(int id)
        {
            var lockValue = Guid.NewGuid().ToString();
            var acquired = await mockCache.AcquireLockAsync(lockKey, lockValue, TimeSpan.FromSeconds(5));

            if (acquired)
            {
                try
                {
                    lock (lockLock)
                    {
                        currentConcurrent++;
                        accessLog.Add($"Worker {id} acquired lock");
                        if (currentConcurrent > maxConcurrent)
                            maxConcurrent = currentConcurrent;
                    }

                    sharedCounter++;
                    await Task.Delay(20);
                }
                finally
                {
                    lock (lockLock)
                    {
                        currentConcurrent--;
                    }
                    await mockCache.ReleaseLockAsync(lockKey, lockValue);
                }
            }
        }
    }

    [Fact]
    public async Task DistributedLock_MultipleCompetingWorkersWithFailover()
    {
        var mockCache = new MockCacheService();
        var lockKey = "resource:1";
        var successfulLocks = 0;

        var tasks = new List<Task>();
        for (int i = 0; i < 3; i++)
        {
            var workerId = i;
            tasks.Add(TryAcquireAndProcess(workerId));
        }

        await Task.WhenAll(tasks);

        successfulLocks.Should().Be(1);

        async Task TryAcquireAndProcess(int id)
        {
            var lockValue = $"worker-{id}";
            var acquired = await mockCache.AcquireLockAsync(lockKey, lockValue, TimeSpan.FromSeconds(5));

            if (acquired)
            {
                try
                {
                    Interlocked.Increment(ref successfulLocks);
                    await Task.Delay(10);
                }
                finally
                {
                    await mockCache.ReleaseLockAsync(lockKey, lockValue);
                }
            }
        }
    }
}

public class EndToEndWorkflowIntegrationTests
{
    [Fact]
    public async Task CompleteOrderWorkflow_CreateRetrieveUpdateDelete()
    {
        var mockCache = new MockCacheService();

        var user = new User
        {
            Id = 1,
            Username = "customer1",
            Email = "customer@example.com",
            FullName = "John Doe",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = 1,
            UserId = user.Id,
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-001",
            Status = OrderStatus.Pending,
            TotalAmount = 299.99m,
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        await mockCache.SetAsync("user:1", user, TimeSpan.FromHours(1));
        var cachedUser = await mockCache.GetAsync<User>("user:1");
        cachedUser.Should().NotBeNull();
        cachedUser?.Username.Should().Be("customer1");

        await mockCache.SetAsync($"order:{order.Id}", order, TimeSpan.FromHours(1));
        var cachedOrder = await mockCache.GetAsync<Order>($"order:{order.Id}");
        cachedOrder.Should().NotBeNull();
        cachedOrder?.OrderNumber.Should().Be(order.OrderNumber);

        order.Status = OrderStatus.Confirmed;
        await mockCache.SetAsync($"order:{order.Id}", order, TimeSpan.FromHours(1));
        var updatedOrder = await mockCache.GetAsync<Order>($"order:{order.Id}");
        updatedOrder?.Status.Should().Be(OrderStatus.Confirmed);

        await mockCache.RemoveAsync($"order:{order.Id}");
        var deletedOrder = await mockCache.GetAsync<Order>($"order:{order.Id}");
        deletedOrder.Should().BeNull();
    }

    [Fact]
    public async Task InventoryWorkflow_ReserveAndRelease()
    {
        var mockCache = new MockCacheService();
        var lockKey = "inventory:reserve:100";
        var lockValue = Guid.NewGuid().ToString();

        var inventory = new InventoryItem
        {
            Id = 1,
            ProductId = 100,
            Warehouse = "WH-US-East",
            QuantityOnHand = 500,
            QuantityReserved = 0,
            QuantityAvailable = 500,
            ReorderPoint = 50,
            MaxStock = 1000,
            LastUpdated = DateTime.UtcNow
        };

        await mockCache.SetAsync("inventory:100", inventory);

        var acquired = await mockCache.AcquireLockAsync(lockKey, lockValue, TimeSpan.FromSeconds(5));
        acquired.Should().BeTrue();

        var reservedInventory = await mockCache.GetAsync<InventoryItem>("inventory:100");
        reservedInventory?.QuantityAvailable.Should().Be(500);

        reservedInventory!.QuantityReserved += 50;
        reservedInventory.QuantityAvailable -= 50;
        await mockCache.SetAsync("inventory:100", reservedInventory);

        await mockCache.ReleaseLockAsync(lockKey, lockValue);

        var finalInventory = await mockCache.GetAsync<InventoryItem>("inventory:100");
        finalInventory?.QuantityReserved.Should().Be(50);
        finalInventory?.QuantityAvailable.Should().Be(450);
    }
}

public class CacheInvalidationPatternIntegrationTests
{
    [Fact]
    public async Task TagBasedInvalidation_RemovesAllRelatedEntries()
    {
        var mockCache = new MockCacheService();
        var invalidationService = new CacheInvalidationService(
            mockCache,
            new Mock<IEventPublisher>().Object,
            new Mock<ILogger<CacheInvalidationService>>().Object);

        invalidationService.RegisterKeyWithTags("product:1", "catalog");
        invalidationService.RegisterKeyWithTags("product:2", "catalog");
        invalidationService.RegisterKeyWithTags("product:3", "featured");

        await mockCache.SetAsync("product:1", new { Id = 1, Name = "Product 1" });
        await mockCache.SetAsync("product:2", new { Id = 2, Name = "Product 2" });
        await mockCache.SetAsync("product:3", new { Id = 3, Name = "Product 3" });

        await invalidationService.InvalidateByTagAsync("catalog");

        var removed1 = await mockCache.GetAsync<dynamic>("product:1");
        var removed2 = await mockCache.GetAsync<dynamic>("product:2");
        var remained3 = await mockCache.GetAsync<dynamic>("product:3");

        removed1.Should().BeNull();
        removed2.Should().BeNull();
        remained3.Should().NotBeNull();
    }

    [Fact]
    public async Task PatternBasedRemoval_MatchesAndRemovesKeys()
    {
        var mockCache = new MockCacheService();

        await mockCache.SetAsync("product:1", new { Id = 1 });
        await mockCache.SetAsync("product:2", new { Id = 2 });
        await mockCache.SetAsync("user:1", new { Id = 1 });

        await mockCache.RemoveByPatternAsync("product:*");

        var product1 = await mockCache.GetAsync<dynamic>("product:1");
        var product2 = await mockCache.GetAsync<dynamic>("product:2");
        var user1 = await mockCache.GetAsync<dynamic>("user:1");

        product1.Should().BeNull();
        product2.Should().BeNull();
        user1.Should().NotBeNull();
    }
}

public class HighVolumeOperationsIntegrationTests
{
    [Fact]
    public async Task BulkCacheOperations_ThousandKeysStoreAndRetrieve()
    {
        var mockCache = new MockCacheService();
        var itemCount = 1000;

        for (int i = 0; i < itemCount; i++)
        {
            await mockCache.SetAsync($"item:{i}", new { Id = i, Value = $"Data-{i}" });
        }

        var statistics = await mockCache.GetStatisticsAsync();
        statistics.TotalKeys.Should().Be(itemCount);

        for (int i = 0; i < itemCount; i++)
        {
            var item = await mockCache.GetAsync<dynamic>($"item:{i}");
            item.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task ConcurrentReadWriteOperations_StressTest()
    {
        var mockCache = new MockCacheService();
        var operationCount = 500;
        var tasks = new List<Task>();

        for (int i = 0; i < operationCount; i++)
        {
            var id = i;
            tasks.Add(Task.Run(async () =>
            {
                await mockCache.SetAsync($"item:{id}", new { Id = id });
            }));
        }

        for (int i = 0; i < operationCount; i++)
        {
            var id = i;
            tasks.Add(Task.Run(async () =>
            {
                await mockCache.GetAsync<dynamic>($"item:{id}");
            }));
        }

        await Task.WhenAll(tasks);

        var stats = await mockCache.GetStatisticsAsync();
        stats.TotalKeys.Should().Be(operationCount);
    }
}
