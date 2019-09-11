#nullable enable
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Infrastructure.Repositories;
using RedisCachePatterns.Services;
using Xunit;

namespace RedisCachePatterns.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _mockRepo = new();
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly Mock<ILogger<OrderService>> _mockLogger = new();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _sut = new OrderService(_mockRepo.Object, _mockCache.Object, _mockLogger.Object);
    }

    private static Order MakeOrder(int id = 1, int userId = 100, string status = "Pending") => new()
    {
        Id = id,
        UserId = userId,
        OrderNumber = $"ORD-{Guid.NewGuid().ToString().Substring(0, 8)}",
        Status = status,
        TotalAmount = 99.99m,
        CreatedAt = DateTime.UtcNow,
        Items = new List<OrderItem>()
    };

    [Fact]
    public async Task GetOrderByIdAsync_WhenCacheHit_ReturnsOrderWithoutHittingRepository()
    {
        var order = MakeOrder(id: 1);
        _mockCache
            .Setup(c => c.GetOrLoadAsync<Order>(
                "order:1",
                It.IsAny<Func<Task<Order>>>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(order);

        var result = await _sut.GetOrderByIdAsync(1);

        result.Should().BeEquivalentTo(order);
        _mockRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetOrderByIdAsync_UsesCorrectCacheKey()
    {
        _mockCache
            .Setup(c => c.GetOrLoadAsync<Order>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<Order>>>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((Order?)null);

        await _sut.GetOrderByIdAsync(42);

        _mockCache.Verify(c => c.GetOrLoadAsync<Order>(
            "order:42",
            It.IsAny<Func<Task<Order>>>(),
            It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetOrderByNumberAsync_RetrievesOrderByOrderNumber()
    {
        var order = MakeOrder(id: 5);
        _mockCache
            .Setup(c => c.GetOrLoadAsync<Order>(
                "order:number:ORD-12345678",
                It.IsAny<Func<Task<Order>>>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(order);

        var result = await _sut.GetOrderByNumberAsync("ORD-12345678");

        result.Should().BeEquivalentTo(order);
    }

    [Fact]
    public async Task GetUserOrdersAsync_ReturnsUserOrdersFromCache()
    {
        var orders = new List<Order>
        {
            MakeOrder(id: 1, userId: 100),
            MakeOrder(id: 2, userId: 100)
        };

        _mockCache
            .Setup(c => c.GetOrLoadAsync<IEnumerable<Order>>(
                "orders:user:100",
                It.IsAny<Func<Task<IEnumerable<Order>>>>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(orders);

        var result = await _sut.GetUserOrdersAsync(100);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(o => o.UserId.Should().Be(100));
    }

    [Fact]
    public async Task CreateOrderAsync_GeneratesOrderNumberAndCachesResult()
    {
        var newOrder = MakeOrder(id: 0, userId: 50);
        var createdOrder = MakeOrder(id: 10, userId: 50);

        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .ReturnsAsync(createdOrder);
        _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<Order>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateOrderAsync(newOrder);

        result.Id.Should().Be(10);
        result.OrderNumber.Should().StartWith("ORD-");
        _mockCache.Verify(c => c.SetAsync("order:10", createdOrder, It.IsAny<TimeSpan?>()), Times.Once);
        _mockCache.Verify(c => c.RemoveAsync("orders:user:50"), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_InvalidatesUserOrdersCache()
    {
        var order = MakeOrder(id: 0, userId: 75);
        var created = MakeOrder(id: 99, userId: 75);

        _mockRepo.Setup(r => r.AddAsync(order)).ReturnsAsync(created);
        _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<Order>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _sut.CreateOrderAsync(order);

        _mockCache.Verify(c => c.RemoveAsync("orders:user:75"), Times.Once);
    }

    [Fact]
    public async Task ConfirmOrderAsync_WhenLockAcquired_ConfirmsOrderAndReturnsTrue()
    {
        var order = MakeOrder(id: 1, status: "Pending");
        var instanceId = "instance-1";

        _mockCache.Setup(c => c.GetOrLoadAsync<Order>(
            "order:1",
            It.IsAny<Func<Task<Order>>>(),
            It.IsAny<TimeSpan?>()))
            .ReturnsAsync(order);
        _mockCache.Setup(c => c.AcquireLockAsync("order:lock:1", instanceId, It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(order);
        _mockCache.Setup(c => c.ReleaseLockAsync("order:lock:1", instanceId))
            .ReturnsAsync(true);
        _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<Order>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.ConfirmOrderAsync(1, instanceId);

        result.Should().BeTrue();
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmOrderAsync_WhenLockNotAcquired_ReturnsFalseWithoutConfirming()
    {
        var instanceId = "instance-1";

        _mockCache.Setup(c => c.AcquireLockAsync("order:lock:1", instanceId, It.IsAny<TimeSpan>()))
            .ReturnsAsync(false);

        var result = await _sut.ConfirmOrderAsync(1, instanceId);

        result.Should().BeFalse();
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmOrderAsync_ReleasesLockEvenOnException()
    {
        var order = MakeOrder(id: 1);
        var instanceId = "instance-1";

        _mockCache.Setup(c => c.GetOrLoadAsync<Order>(
            "order:1",
            It.IsAny<Func<Task<Order>>>(),
            It.IsAny<TimeSpan?>()))
            .ReturnsAsync(order);
        _mockCache.Setup(c => c.AcquireLockAsync("order:lock:1", instanceId, It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));
        _mockCache.Setup(c => c.ReleaseLockAsync("order:lock:1", instanceId))
            .ReturnsAsync(true);

        Func<Task> act = () => _sut.ConfirmOrderAsync(1, instanceId);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _mockCache.Verify(c => c.ReleaseLockAsync("order:lock:1", instanceId), Times.Once);
    }

    [Fact]
    public async Task CancelOrderAsync_WhenOrderExists_CancelsAndInvalidatesCache()
    {
        var order = MakeOrder(id: 1, status: "Pending");

        _mockCache.Setup(c => c.GetOrLoadAsync<Order>(
            "order:1",
            It.IsAny<Func<Task<Order>>>(),
            It.IsAny<TimeSpan?>()))
            .ReturnsAsync(order);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(order);
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<Order>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.CancelOrderAsync(1);

        result.Should().BeTrue();
        _mockCache.Verify(c => c.RemoveAsync("orders:user:" + order.UserId), Times.Once);
    }

    [Fact]
    public async Task CancelOrderAsync_WhenOrderNotFound_ReturnsFalse()
    {
        _mockCache.Setup(c => c.GetOrLoadAsync<Order>(
            "order:1",
            It.IsAny<Func<Task<Order>>>(),
            It.IsAny<TimeSpan?>()))
            .ReturnsAsync((Order?)null);

        var result = await _sut.CancelOrderAsync(1);

        result.Should().BeFalse();
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task GetPendingOrdersAsync_ReturnsPendingOrdersFromCache()
    {
        var pendingOrders = new List<Order>
        {
            MakeOrder(id: 1, status: "Pending"),
            MakeOrder(id: 2, status: "Pending")
        };

        _mockCache.Setup(c => c.GetOrLoadAsync<IEnumerable<Order>>(
            "orders:pending",
            It.IsAny<Func<Task<IEnumerable<Order>>>>(),
            It.IsAny<TimeSpan?>()))
            .ReturnsAsync(pendingOrders);

        var result = await _sut.GetPendingOrdersAsync();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(o => o.Status.Should().Be("Pending"));
    }
}
