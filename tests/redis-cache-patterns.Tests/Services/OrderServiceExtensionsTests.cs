#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
/// Tests for the OrderServiceExtensions batch and query helpers.
/// </summary>
/// <remarks>
/// The extensions target the concrete <see cref="OrderService"/>, so each test wires a real service
/// over the in-memory <see cref="OrderRepository"/> and the in-process <see cref="MockCacheService"/>.
/// Only the failure-propagation tests substitute a Moq cache that throws.
/// </remarks>
public class OrderServiceExtensionsTests
{
    private static readonly DateTime RangeStart = new(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime RangeEnd = new(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

    private readonly OrderRepository _repository = new();
    private readonly MockCacheService _cache = new();
    private readonly Mock<ICacheTagService> _mockTagService = new();
    private readonly Mock<ILogger<OrderService>> _mockLogger = new();

    /// <summary>
    /// Builds a new <see cref="OrderService"/> instance over the supplied cache, backed by the in-memory repository.
    /// </summary>
    /// <param name="cache">The cache service to inject.</param>
    /// <returns>A new order service instance.</returns>
    private OrderService CreateService(ICacheService cache) =>
        new(_repository, cache, _mockTagService.Object, _mockLogger.Object);

    /// <summary>
    /// Creates and persists an order in the in-memory repository.
    /// </summary>
    /// <param name="userId">The owning user identifier.</param>
    /// <param name="status">The order status.</param>
    /// <param name="createdAt">The UTC creation timestamp.</param>
    /// <returns>The persisted order, including its assigned identifier.</returns>
    private async Task<Order> AddOrderAsync(int userId, OrderStatus status, DateTime createdAt) =>
        await _repository.AddAsync(new Order
        {
            UserId = userId,
            OrderNumber = $"ORD-{Guid.NewGuid():N}"[..12],
            Status = status,
            CreatedAt = createdAt,
            TotalAmount = 10m,
            Items = []
        });

    /// <summary>
    /// Tests that TryGetOrderByIdAsync returns null when no order exists with the given identifier.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TryGetOrderByIdAsync_WhenOrderDoesNotExist_ReturnsNull()
    {
        var service = CreateService(_cache);

        var result = await service.TryGetOrderByIdAsync(999);

        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that TryGetOrderByIdAsync returns the populated order when it exists.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TryGetOrderByIdAsync_WhenOrderExists_ReturnsPopulatedOrder()
    {
        var created = await AddOrderAsync(userId: 7, OrderStatus.Confirmed, RangeStart);
        var service = CreateService(_cache);

        var result = await service.TryGetOrderByIdAsync(created.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.UserId.Should().Be(7);
        result.Status.Should().Be(OrderStatus.Confirmed);
        result.OrderNumber.Should().Be(created.OrderNumber);
    }

    /// <summary>
    /// Tests that GetOrdersByStatusWithFormattedStatusAsync returns an empty collection when no order matches the status.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetOrdersByStatusWithFormattedStatusAsync_WhenNoOrderMatches_ReturnsEmptyCollection()
    {
        await AddOrderAsync(userId: 1, OrderStatus.Pending, RangeStart);
        var service = CreateService(_cache);

        var result = await service.GetOrdersByStatusWithFormattedStatusAsync(OrderStatus.Cancelled);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that GetOrdersByStatusPagedAsync returns an empty list when no order matches the status.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetOrdersByStatusPagedAsync_WhenNoOrderMatches_ReturnsEmptyList()
    {
        await AddOrderAsync(userId: 1, OrderStatus.Pending, RangeStart);
        var service = CreateService(_cache);

        var result = await service.GetOrdersByStatusPagedAsync(OrderStatus.Shipped, pageNumber: 1, pageSize: 10);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that an inverted date range (start after end) returns an empty collection rather than throwing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetOrdersInDateRangeFormattedAsync_WhenStartIsAfterEnd_ReturnsEmptyCollection()
    {
        await AddOrderAsync(userId: 1, OrderStatus.Pending, RangeStart.AddDays(5));
        var service = CreateService(_cache);

        var result = await service.GetOrdersInDateRangeFormattedAsync(RangeEnd, RangeStart);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the date range includes orders created exactly at either boundary and excludes orders one tick outside.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetOrdersInDateRangeFormattedAsync_IncludesExactBoundariesAndExcludesOneTickOutside()
    {
        var atStart = await AddOrderAsync(userId: 1, OrderStatus.Pending, RangeStart);
        var atEnd = await AddOrderAsync(userId: 1, OrderStatus.Pending, RangeEnd);
        var middle = await AddOrderAsync(userId: 1, OrderStatus.Pending, RangeStart.AddDays(5));
        await AddOrderAsync(userId: 1, OrderStatus.Pending, RangeStart.AddTicks(-1));
        await AddOrderAsync(userId: 1, OrderStatus.Pending, RangeEnd.AddTicks(1));
        var service = CreateService(_cache);

        var result = await service.GetOrdersInDateRangeFormattedAsync(RangeStart, RangeEnd);

        result.Select(o => o.Id).Should().BeEquivalentTo(new[] { atStart.Id, atEnd.Id, middle.Id });
    }

    /// <summary>
    /// Tests that CountOrdersByStatusAsync returns zero when no order has the requested status.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CountOrdersByStatusAsync_WhenNoOrderHasStatus_ReturnsZero()
    {
        await AddOrderAsync(userId: 1, OrderStatus.Pending, RangeStart);
        var service = CreateService(_cache);

        var count = await service.CountOrdersByStatusAsync(OrderStatus.Completed);

        count.Should().Be(0);
    }

    /// <summary>
    /// Tests that CountOrdersByStatusAsync counts only the orders that match the requested status.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CountOrdersByStatusAsync_WhenOrdersMatch_CountsOnlyMatchingOrders()
    {
        await AddOrderAsync(userId: 1, OrderStatus.Pending, RangeStart);
        await AddOrderAsync(userId: 2, OrderStatus.Pending, RangeStart);
        await AddOrderAsync(userId: 2, OrderStatus.Shipped, RangeStart);
        var service = CreateService(_cache);

        var count = await service.CountOrdersByStatusAsync(OrderStatus.Pending);

        count.Should().Be(2);
    }

    /// <summary>
    /// Tests that CountUserOrdersAsync returns zero for a user who has placed no orders.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CountUserOrdersAsync_WhenUserHasNoOrders_ReturnsZero()
    {
        await AddOrderAsync(userId: 100, OrderStatus.Pending, RangeStart);
        var service = CreateService(_cache);

        var count = await service.CountUserOrdersAsync(userId: 200);

        count.Should().Be(0);
    }

    /// <summary>
    /// Tests that TryGetOrderByIdAsync does not swallow cache failures and propagates the original exception.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task TryGetOrderByIdAsync_WhenCacheThrows_PropagatesCacheException()
    {
        var failingCache = new Mock<ICacheService>();
        failingCache
            .Setup(c => c.GetOrLoadAsync<Order>(It.IsAny<string>(), It.IsAny<Func<Task<Order>>>(), It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new CacheException("Redis unavailable"));
        var service = CreateService(failingCache.Object);

        Func<Task> act = () => service.TryGetOrderByIdAsync(1);

        await act.Should().ThrowAsync<CacheException>().WithMessage("Redis unavailable");
    }

    /// <summary>
    /// Tests that CountOrdersByStatusAsync propagates cache failures instead of returning a zero count.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CountOrdersByStatusAsync_WhenCacheThrows_PropagatesCacheException()
    {
        var failingCache = new Mock<ICacheService>();
        failingCache
            .Setup(c => c.GetOrLoadAsync<IEnumerable<Order>>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<Order>>>>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new CacheException("Redis unavailable"));
        var service = CreateService(failingCache.Object);

        Func<Task> act = () => service.CountOrdersByStatusAsync(OrderStatus.Pending);

        await act.Should().ThrowAsync<CacheException>().WithMessage("Redis unavailable");
    }

    /// <summary>
    /// Tests that CountUserOrdersAsync propagates cache failures instead of returning a zero count.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CountUserOrdersAsync_WhenCacheThrows_PropagatesCacheException()
    {
        var failingCache = new Mock<ICacheService>();
        failingCache
            .Setup(c => c.GetOrLoadAsync<IEnumerable<Order>>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<Order>>>>(),
                It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new CacheException("Redis unavailable"));
        var service = CreateService(failingCache.Object);

        Func<Task> act = () => service.CountUserOrdersAsync(userId: 1);

        await act.Should().ThrowAsync<CacheException>().WithMessage("Redis unavailable");
    }
}
