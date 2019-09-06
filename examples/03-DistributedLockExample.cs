#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using RedisCachePatterns.Services;
using RedisCachePatterns.Infrastructure.Repositories;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Results;
using RedisCachePatterns.Utilities;
using System;
using System.Threading.Tasks;

namespace RedisCachePatterns.Examples;

/// <summary>
/// Demonstrates distributed locking to prevent cache stampedes and
/// race conditions across multiple instances.
/// </summary>
public class DistributedLockExample
{
    private readonly ICacheService _cacheService;
    private readonly IOrderRepository _orderRepository;
    private readonly string _instanceId;

    public DistributedLockExample(ICacheService cacheService, IOrderRepository orderRepository)
    {
        _cacheService = cacheService;
        _orderRepository = orderRepository;
        _instanceId = Environment.MachineName;
    }

    /// <summary>
    /// Acquires a distributed lock before processing critical operation.
    /// Automatically releases lock in finally block.
    /// </summary>
    public async Task<OperationResult> ProcessOrderWithLockAsync(int orderId)
    {
        var lockKey = $"order-processing:{orderId}";
        var lockDuration = TimeSpan.FromSeconds(30);

        Console.WriteLine($"[{_instanceId}] Attempting to acquire lock for order {orderId}");

        var acquired = await _cacheService.AcquireLockAsync(lockKey, lockDuration).ConfigureAwait(false);

        if (!acquired)
        {
            Console.WriteLine($"[{_instanceId}] ✗ Lock not acquired - order is being processed elsewhere");
            return OperationResult.Failure("Order is being processed by another instance");
        }

        Console.WriteLine($"[{_instanceId}] ✓ Lock acquired for order {orderId}");

        try
        {
            // Perform critical operation
            var order = await _orderRepository.GetByIdAsync(orderId).ConfigureAwait(false);
            if (order == null)
            {
                return OperationResult.Failure("Order not found");
            }

            Console.WriteLine($"[{_instanceId}] Processing order {orderId}...");
            order.Status = OrderStatus.Processing;
            order.ProcessedAt = DateTime.UtcNow;

            // Simulate processing
            await Task.Delay(1000).ConfigureAwait(false);

            await _orderRepository.UpdateAsync(order).ConfigureAwait(false);
            Console.WriteLine($"[{_instanceId}] ✓ Order {orderId} processed successfully");

            return OperationResult.Success();
        }
        finally
        {
            // Always release the lock
            await _cacheService.ReleaseLockAsync(lockKey).ConfigureAwait(false);
            Console.WriteLine($"[{_instanceId}] Lock released for order {orderId}");
        }
    }

    /// <summary>
    /// Prevents cache stampede by using a lock to serialize database reads
    /// when a cache miss occurs.
    /// </summary>
    public async Task<Order?> GetOrderWithStampedeProtectionAsync(int orderId)
    {
        var cacheKey = $"order:{orderId}";
        var lockKey = $"order-load:{orderId}";
        var lockDuration = TimeSpan.FromSeconds(10);

        // Try to get from cache
        var cached = await _cacheService.GetAsync<Order>(cacheKey).ConfigureAwait(false);
        if (cached != null)
        {
            return cached;
        }

        // Cache miss - try to acquire lock for loading
        Console.WriteLine($"Cache miss for order {orderId} - attempting load lock");

        var acquiredLock = await _cacheService.AcquireLockAsync(lockKey, lockDuration).ConfigureAwait(false);

        try
        {
            if (acquiredLock)
            {
                // We have the lock - load from database and cache
                Console.WriteLine($"  → Loading from database...");
                var order = await _orderRepository.GetByIdAsync(orderId).ConfigureAwait(false);

                if (order != null)
                {
                    await _cacheService.SetAsync(cacheKey, order, TimeSpan.FromHours(1)).ConfigureAwait(false);
                    Console.WriteLine($"  → Cached order {orderId}");
                }

                return order;
            }
            else
            {
                // Another instance is loading - wait for cache to be populated
                Console.WriteLine($"  → Waiting for another instance to populate cache...");
                for (int i = 0; i < 5; i++)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                    var cached2 = await _cacheService.GetAsync<Order>(cacheKey).ConfigureAwait(false);
                    if (cached2 != null)
                    {
                        Console.WriteLine($"  → Got order from cache (populated by other instance)");
                        return cached2;
                    }
                }

                // Fallback to direct load if cache not populated
                Console.WriteLine($"  → Cache not populated by other instance - loading directly");
                return await _orderRepository.GetByIdAsync(orderId).ConfigureAwait(false);
            }
        }
        finally
        {
            if (acquiredLock)
            {
                await _cacheService.ReleaseLockAsync(lockKey).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Distributed lock with timeout - automatically released after duration.
    /// Prevents deadlocks from crashed instances.
    /// </summary>
    public async Task<OperationResult> RefundOrderWithTimeoutLockAsync(int orderId)
    {
        var lockKey = $"order-refund:{orderId}";
        var lockDuration = TimeSpan.FromSeconds(60); // Auto-release after 60 seconds

        Console.WriteLine($"Acquiring refund lock for order {orderId} (timeout: 60s)");

        var acquired = await _cacheService.AcquireLockAsync(lockKey, lockDuration).ConfigureAwait(false);
        if (!acquired)
        {
            return OperationResult.Failure("Another instance is already refunding this order");
        }

        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId).ConfigureAwait(false);
            if (order == null)
                return OperationResult.Failure("Order not found");

            // Process refund
            Console.WriteLine($"  → Processing refund for order {orderId}...");
            order.Status = OrderStatus.Refunded;
            order.RefundedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order).ConfigureAwait(false);

            // Invalidate cache
            var cacheKey = $"order:{orderId}";
            await _cacheService.RemoveAsync(cacheKey).ConfigureAwait(false);

            Console.WriteLine($"✓ Order {orderId} refunded");
            return OperationResult.Success();
        }
        finally
        {
            await _cacheService.ReleaseLockAsync(lockKey).ConfigureAwait(false);
            Console.WriteLine("Refund lock released");
        }
    }

    /// <summary>
    /// Multiple sequential locks with timeout detection.
    /// Useful for complex multi-step operations.
    /// </summary>
    public async Task<OperationResult> ConfirmAndShipOrderWithLocksAsync(int orderId)
    {
        var confirmLockKey = $"order-confirm:{orderId}";
        var shipLockKey = $"order-ship:{orderId}";
        var lockDuration = TimeSpan.FromSeconds(20);

        Console.WriteLine($"Starting order confirmation and shipping for {orderId}");

        // Step 1: Confirm order
        var confirmLock = await _cacheService.AcquireLockAsync(confirmLockKey, lockDuration).ConfigureAwait(false);
        if (!confirmLock)
        {
            return OperationResult.Failure("Order is being confirmed by another instance");
        }

        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId).ConfigureAwait(false);
            if (order?.Status != OrderStatus.Pending)
            {
                return OperationResult.Failure("Order cannot be confirmed");
            }

            order.Status = OrderStatus.Confirmed;
            await _orderRepository.UpdateAsync(order).ConfigureAwait(false);
            Console.WriteLine($"✓ Order {orderId} confirmed");
        }
        finally
        {
            await _cacheService.ReleaseLockAsync(confirmLockKey).ConfigureAwait(false);
        }

        // Step 2: Ship order
        var shipLock = await _cacheService.AcquireLockAsync(shipLockKey, lockDuration).ConfigureAwait(false);
        if (!shipLock)
        {
            return OperationResult.Failure("Order is being shipped by another instance");
        }

        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId).ConfigureAwait(false);
            if (order?.Status != OrderStatus.Confirmed)
            {
                return OperationResult.Failure("Order is not confirmed");
            }

            order.Status = OrderStatus.Shipped;
            order.ShippedAt = DateTime.UtcNow;
            await _orderRepository.UpdateAsync(order).ConfigureAwait(false);
            Console.WriteLine($"✓ Order {orderId} shipped");

            // Invalidate cache
            await _cacheService.RemoveAsync($"order:{orderId}").ConfigureAwait(false);

            return OperationResult.Success();
        }
        finally
        {
            await _cacheService.ReleaseLockAsync(shipLockKey).ConfigureAwait(false);
        }
    }
}
