#nullable enable
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Exceptions;
using RedisCachePatterns.Infrastructure.Repositories;
using RedisCachePatterns.Services;
using Xunit;

namespace RedisCachePatterns.Tests.Services;

/// <summary>
/// Contains unit tests for <see cref="InventoryService"/>.
/// </summary>
public class InventoryServiceTests
{
    private readonly Mock<IInventoryRepository> _mockRepo = new();
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly Mock<ILogger<InventoryService>> _mockLogger = new();
    private readonly InventoryService _sut;

    /// <summary>
    /// Initializes mock dependencies and creates an instance of <see cref="InventoryService"/> for testing.
    /// </summary>
    public InventoryServiceTests()
    {
        _sut = new InventoryService(_mockRepo.Object, _mockCache.Object, _mockLogger.Object);
    }

    private static InventoryItem MakeInventory(
        int id = 1,
        int productId = 100,
        string warehouse = "WH-US-East",
        int quantity = 500) => new()
    {
        Id = id,
        ProductId = productId,
        Warehouse = warehouse,
        QuantityOnHand = quantity,
        QuantityReserved = 0,
        QuantityAvailable = quantity,
        ReorderPoint = 50,
        MaxStock = 1000,
        LastUpdated = DateTime.UtcNow
    };

    /// <summary>
    /// Verifies that when the inventory item is found in the cache, the repository is not called and the cached item is returned.
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution.</returns>
    [Fact]
    public async Task GetInventoryByIdAsync_WhenCacheHit_ReturnsWithoutRepositoryCall()
    {
        var inventory = MakeInventory(id: 1);
        _mockCache
            .Setup(c => c.GetOrLoadAsync<InventoryItem>(
                "inventory:1",
                It.IsAny<Func<Task<InventoryItem>>>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(inventory);

        var result = await _sut.GetInventoryByIdAsync(1);

        result.Should().BeEquivalentTo(inventory);
        _mockRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    /// <summary>
    /// Ensures that the cache key used for retrieving an inventory item by ID is correctly formatted.
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution.</returns>
    [Fact]
    public async Task GetInventoryByIdAsync_UsesCorrectCacheKey()
    {
        _mockCache
            .Setup(c => c.GetOrLoadAsync<InventoryItem>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<InventoryItem>>>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((InventoryItem?)null);

        await _sut.GetInventoryByIdAsync(42);

        _mockCache.Verify(c => c.GetOrLoadAsync<InventoryItem>(
            "inventory:42",
            It.IsAny<Func<Task<InventoryItem>>>(),
            It.IsAny<TimeSpan?>()), Times.Once);
    }

    /// <summary>
    /// Checks that the service retrieves an inventory item for a specific product and warehouse using the expected cache key.
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution.</returns>
    [Fact]
    public async Task GetByProductAndWarehouseAsync_RetrievesInventoryByProductAndWarehouse()
    {
        var inventory = MakeInventory(productId: 100, warehouse: "WH-US-West");
        _mockCache
            .Setup(c => c.GetOrLoadAsync<InventoryItem>(
                "inventory:product:100:warehouse:WH-US-West",
                It.IsAny<Func<Task<InventoryItem>>>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(inventory);

        var result = await _sut.GetByProductAndWarehouseAsync(100, "WH-US-West");

        result.Should().BeEquivalentTo(inventory);
    }

    /// <summary>
    /// Confirms that retrieving inventory by product ID returns items from multiple warehouses.
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution.</returns>
    [Fact]
    public async Task GetInventoryByProductAsync_ReturnsMultipleWarehouses()
    {
        var inventories = new List<InventoryItem>
        {
            MakeInventory(productId: 50, warehouse: "WH-US-East"),
            MakeInventory(productId: 50, warehouse: "WH-US-West"),
            MakeInventory(productId: 50, warehouse: "WH-EU-Central")
        };

        _mockCache
            .Setup(c => c.GetOrLoadAsync<IEnumerable<InventoryItem>>(
                "inventory:product:50",
                It.IsAny<Func<Task<IEnumerable<InventoryItem>>>>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(inventories);

        var result = await _sut.GetInventoryByProductAsync(50);

        result.Should().HaveCount(3);
        result.Should().AllSatisfy(i => i.ProductId.Should().Be(50));
    }

    /// <summary>
    /// Tests that when a lock is successfully acquired and sufficient stock exists, the inventory is reserved and the method returns true.
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution.</returns>
    [Fact]
    public async Task ReserveInventoryAsync_WhenLockAcquiredAndStockAvailable_ReservesAndReturnsTrue()
    {
        var inventory = MakeInventory(productId: 10, warehouse: "WH-East", quantity: 100);
        var instanceId = "instance-1";

        _mockCache.Setup(c => c.GetOrLoadAsync<InventoryItem>(
            "inventory:product:10:warehouse:WH-East",
            It.IsAny<Func<Task<InventoryItem>>>(),
            It.IsAny<TimeSpan?>()))
            .ReturnsAsync(inventory);
        _mockCache.Setup(c => c.AcquireLockAsync(
            "inventory:lock:10:WH-East", instanceId, It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<InventoryItem>()))
            .ReturnsAsync(inventory);
        _mockCache.Setup(c => c.ReleaseLockAsync("inventory:lock:10:WH-East", instanceId))
            .ReturnsAsync(true);
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.ReserveInventoryAsync(10, "WH-East", 30, instanceId);

        result.Should().BeTrue();
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<InventoryItem>()), Times.Once);
    }

    /// <summary>
    /// Verifies that if the lock cannot be acquired, the method returns false and does not update the inventory.
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution.</returns>
    [Fact]
    public async Task ReserveInventoryAsync_WhenLockNotAcquired_ReturnsFalseWithoutModifyingInventory()
    {
        var instanceId = "instance-1";

        _mockCache.Setup(c => c.AcquireLockAsync(
            "inventory:lock:10:WH-East", instanceId, It.IsAny<TimeSpan>()))
            .ReturnsAsync(false);

        var result = await _sut.ReserveInventoryAsync(10, "WH-East", 30, instanceId);

        result.Should().BeFalse();
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<InventoryItem>()), Times.Never);
    }

    /// <summary>
    /// Ensures that attempting to reserve more inventory than available throws <see cref="InsufficientInventoryException"/>.
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution.</returns>
    [Fact]
    public async Task ReserveInventoryAsync_WhenStockInsufficient_ThrowsException()
    {
        var inventory = MakeInventory(productId: 10, warehouse: "WH-East", quantity: 10);
        var instanceId = "instance-1";

        _mockCache.Setup(c => c.GetOrLoadAsync<InventoryItem>(
            "inventory:product:10:warehouse:WH-East",
            It.IsAny<Func<Task<InventoryItem>>>(),
            It.IsAny<TimeSpan?>()))
            .ReturnsAsync(inventory);
        _mockCache.Setup(c => c.AcquireLockAsync(
            "inventory:lock:10:WH-East", instanceId, It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);
        _mockCache.Setup(c => c.ReleaseLockAsync("inventory:lock:10:WH-East", instanceId))
            .ReturnsAsync(true);

        Func<Task> act = () => _sut.ReserveInventoryAsync(10, "WH-East", 50, instanceId);

        await act.Should().ThrowAsync<InsufficientInventoryException>();
    }

    /// <summary>
    /// Confirms that the lock is released even when an exception occurs during inventory reservation.
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution.</returns>
    [Fact]
    public async Task ReserveInventoryAsync_ReleasesLockEvenOnException()
    {
        var inventory = MakeInventory(productId: 10);
        var instanceId = "instance-1";

        _mockCache.Setup(c => c.GetOrLoadAsync<InventoryItem>(
            It.IsAny<string>(),
            It.IsAny<Func<Task<InventoryItem>>>(),
            It.IsAny<TimeSpan?>()))
            .ReturnsAsync(inventory);
        _mockCache.Setup(c => c.AcquireLockAsync(
            It.IsAny<string>(), instanceId, It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<InventoryItem>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));
        _mockCache.Setup(c => c.ReleaseLockAsync(It.IsAny<string>(), instanceId))
            .ReturnsAsync(true);

        Func<Task> act = () => _sut.ReserveInventoryAsync(10, "WH-East", 10, instanceId);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _mockCache.Verify(c => c.ReleaseLockAsync(It.IsAny<string>(), instanceId), Times.Once);
    }

    /// <summary>
    /// Checks that after a successful reservation, related cache entries are invalidated.
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution.</returns>
    [Fact]
    public async Task ReserveInventoryAsync_InvalidatesInventoryCaches()
    {
        var inventory = MakeInventory(productId: 10, warehouse: "WH-East", quantity: 100);
        var instanceId = "instance-1";

        _mockCache.Setup(c => c.GetOrLoadAsync<InventoryItem>(
            It.IsAny<string>(),
            It.IsAny<Func<Task<InventoryItem>>>(),
            It.IsAny<TimeSpan?>()))
            .ReturnsAsync(inventory);
        _mockCache.Setup(c => c.AcquireLockAsync(It.IsAny<string>(), instanceId, It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<InventoryItem>()))
            .ReturnsAsync(inventory);
        _mockCache.Setup(c => c.ReleaseLockAsync(It.IsAny<string>(), instanceId))
            .ReturnsAsync(true);
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _sut.ReserveInventoryAsync(10, "WH-East", 10, instanceId);

        _mockCache.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.AtLeast(2));
    }

    /// <summary>
    /// Tests that releasing a reservation updates the inventory and returns true when the inventory exists.
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution.</returns>
    [Fact]
    public async Task ReleaseReservationAsync_WhenInventoryExists_ReleasesReservation()
    {
        var inventory = MakeInventory(id: 1, quantity: 100);
        inventory.QuantityReserved = 20;
        inventory.QuantityAvailable = 80;

        _mockCache.Setup(c => c.GetOrLoadAsync<InventoryItem>(
            "inventory:1",
            It.IsAny<Func<Task<InventoryItem>>>(),
            It.IsAny<TimeSpan?>()))
            .ReturnsAsync(inventory);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<InventoryItem>()))
            .ReturnsAsync(inventory);
        _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<InventoryItem>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.ReleaseReservationAsync(1, 10);

        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the service returns inventory items whose quantity is below the reorder point.
    /// </summary>
    /// <returns>A task that represents the asynchronous test execution.</returns>
    [Fact]
    public async Task GetLowStockItemsAsync_ReturnsItemsBelowReorderPoint()
    {
        var lowStockItems = new List<InventoryItem>
        {
            MakeInventory(id: 1, quantity: 30),
            MakeInventory(id: 2, quantity: 40)
        };

        _mockCache.Setup(c => c.GetOrLoadAsync<IEnumerable<InventoryItem>>(
            "inventory:lowstock",
            It.IsAny<Func<Task<IEnumerable<InventoryItem>>>>(),
            It.IsAny<TimeSpan?>()))
            .ReturnsAsync(lowStockItems);

        var result = await _sut.GetLowStockItemsAsync();

        result.Should().HaveCount(2);
    }
}
