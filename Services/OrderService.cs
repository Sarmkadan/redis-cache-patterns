#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Domain;
using RedisCachePatterns.Infrastructure.Repositories;
using RedisCachePatterns.Exceptions;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Services;

/// <summary>
/// Service managing orders with distributed lock for concurrent operations and cache invalidation
/// </summary>
public class OrderService
{
    private readonly IOrderRepository _repository;
    private readonly ICacheService _cache;
    private readonly CacheTagService _tagService;
    private readonly ILogger<OrderService> _logger;
    private const string ORDER_CACHE_KEY = "order:{0}";
    private const string USER_ORDERS_CACHE_KEY = "orders:user:{0}";
    private const string STATUS_ORDERS_CACHE_TAG = "orders:status";
    private const string DATE_RANGE_ORDERS_CACHE_TAG = "orders:date-range";
    private const string PENDING_ORDERS_CACHE_KEY = "orders:pending";

    public OrderService(
        IOrderRepository repository,
        ICacheService cache,
        CacheTagService tagService,
        ILogger<OrderService> logger)
    {
        _repository = repository;
        _cache = cache;
        _tagService = tagService;
        _logger = logger;
    }

    public async Task<Order?> GetOrderByIdAsync(int orderId)
    {
        var cacheKey = string.Format(ORDER_CACHE_KEY, orderId);
        return await _cache.GetOrLoadAsync(
            cacheKey,
            async () => await _repository.GetByIdAsync(orderId),
            TimeSpan.FromHours(1)
        );
    }

    public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
    {
        var cacheKey = $"order:number:{orderNumber}";
        return await _cache.GetOrLoadAsync(
            cacheKey,
            async () => await _repository.GetByOrderNumberAsync(orderNumber),
            TimeSpan.FromHours(1)
        );
    }

    public async Task<IEnumerable<Order>> GetUserOrdersAsync(int userId)
    {
        var cacheKey = string.Format(USER_ORDERS_CACHE_KEY, userId);
        var result = await _cache.GetOrLoadAsync(
            cacheKey,
            async () => await _repository.GetByUserIdAsync(userId),
            TimeSpan.FromMinutes(30)
        );
        return result ?? [];
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        order.OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 8)}";

        var created = await _repository.AddAsync(order);
        await _cache.SetAsync(string.Format(ORDER_CACHE_KEY, created.Id), created, TimeSpan.FromHours(1));

        await InvalidateOrderCachesAsync(created.UserId);
        _logger.LogInformation("Order created: {OrderId} - {OrderNumber}", created.Id, created.OrderNumber);
        return created;
    }

    // Distributed lock pattern: ensure only one instance can confirm an order
    public async Task<bool> ConfirmOrderAsync(int orderId, string instanceId)
    {
        var lockKey = $"order:lock:{orderId}";
        var lockValue = instanceId;

        // Attempt to acquire lock
        var lockAcquired = await _cache.AcquireLockAsync(lockKey, lockValue, TimeSpan.FromSeconds(10));
        if (!lockAcquired)
        {
            _logger.LogWarning("Failed to acquire lock for order: {OrderId}", orderId);
            return false;
        }

        try
        {
            var order = await GetOrderByIdAsync(orderId);
            if (order == null)
                throw new NotFoundException(nameof(Order), orderId);

            order.ConfirmOrder();
            await _repository.UpdateAsync(order);
            await _cache.SetAsync(string.Format(ORDER_CACHE_KEY, order.Id), order, TimeSpan.FromHours(1));

            await InvalidateOrderCachesAsync(order.UserId);
            _logger.LogInformation("Order confirmed: {OrderId}", orderId);
            return true;
        }
        finally
        {
            // Release lock
            await _cache.ReleaseLockAsync(lockKey, lockValue);
        }
    }

    public async Task<bool> ShipOrderAsync(int orderId, string trackingNumber)
    {
        var order = await GetOrderByIdAsync(orderId);
        if (order == null)
            throw new NotFoundException(nameof(Order), orderId);

        order.ShipOrder(trackingNumber);
        await _repository.UpdateAsync(order);
        await _cache.SetAsync(string.Format(ORDER_CACHE_KEY, order.Id), order, TimeSpan.FromHours(1));

        await InvalidateOrderCachesAsync(order.UserId);
        _logger.LogInformation("Order shipped: {OrderId} - Tracking: {TrackingNumber}", orderId, trackingNumber);
        return true;
    }

    public async Task<bool> CompleteOrderAsync(int orderId)
    {
        var order = await GetOrderByIdAsync(orderId);
        if (order == null)
            throw new NotFoundException(nameof(Order), orderId);

        order.CompleteOrder();
        await _repository.UpdateAsync(order);
        await _cache.SetAsync(string.Format(ORDER_CACHE_KEY, order.Id), order, TimeSpan.FromHours(1));

        await InvalidateOrderCachesAsync(order.UserId);
        _logger.LogInformation("Order completed: {OrderId}", orderId);
        return true;
    }

    public async Task<bool> CancelOrderAsync(int orderId)
    {
        var order = await GetOrderByIdAsync(orderId);
        if (order == null)
            throw new NotFoundException(nameof(Order), orderId);

        order.CancelOrder();
        await _repository.UpdateAsync(order);
        await _cache.SetAsync(string.Format(ORDER_CACHE_KEY, order.Id), order, TimeSpan.FromHours(1));

        await InvalidateOrderCachesAsync(order.UserId);
        _logger.LogInformation("Order cancelled: {OrderId}", orderId);
        return true;
    }

    public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status)
    {
        var cacheKey = $"orders:status:{status}";
        var result = await _cache.GetOrLoadAsync(
            cacheKey,
            async () => await _repository.GetByStatusAsync(status),
            TimeSpan.FromMinutes(15)
        );

        // Tag the cache entry so it can be invalidated when any order's status changes
        if (result != null)
        {
            await _tagService.TagKeyAsync(cacheKey, STATUS_ORDERS_CACHE_TAG);
        }

        return result ?? [];
    }

    public async Task<IEnumerable<Order>> GetPendingOrdersAsync()
    {
        var result = await _cache.GetOrLoadAsync(
            PENDING_ORDERS_CACHE_KEY,
            async () => await _repository.GetByStatusAsync(OrderStatus.Pending),
            TimeSpan.FromMinutes(15)
        );

        // Tag the cache entry so it can be invalidated when any order's status changes
        if (result != null)
        {
            await _tagService.TagKeyAsync(PENDING_ORDERS_CACHE_KEY, STATUS_ORDERS_CACHE_TAG);
        }

        return result ?? [];
    }

    public async Task<IEnumerable<Order>> GetOrdersInDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        // Normalize dates to ISO-8601 UTC format with invariant culture to ensure consistent cache keys
        // regardless of server culture settings or DST transitions
        var normalizedStartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        var normalizedEndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
        var cacheKey = $"orders:date:{normalizedStartDate:yyyy-MM-ddTHH:mm:ssZ}-{normalizedEndDate:yyyy-MM-ddTHH:mm:ssZ}";

        var result = await _cache.GetOrLoadAsync(
            cacheKey,
            async () => await _repository.GetOrdersInDateRangeAsync(startDate, endDate),
            TimeSpan.FromHours(4)
        );

        // Tag the cache entry so it can be invalidated when any order in this date range changes
        if (result != null)
        {
            await _tagService.TagKeyAsync(cacheKey, DATE_RANGE_ORDERS_CACHE_TAG);
        }

        return result ?? [];
    }

    private async Task InvalidateOrderCachesAsync(int userId)
    {
        await _cache.RemoveAsync(string.Format(USER_ORDERS_CACHE_KEY, userId));
        await _cache.RemoveAsync(PENDING_ORDERS_CACHE_KEY);

        // Invalidate all status-based query caches using tag-based invalidation
        await _tagService.InvalidateTagAsync(STATUS_ORDERS_CACHE_TAG);

        // Invalidate all date-range query caches using tag-based invalidation
        await _tagService.InvalidateTagAsync(DATE_RANGE_ORDERS_CACHE_TAG);
    }
}
