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
/// Service managing inventory with distributed locks to prevent race conditions
/// </summary>
public class InventoryService
{
    private readonly IInventoryRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<InventoryService> _logger;
    private const string INVENTORY_CACHE_KEY = "inventory:{0}";
    private const string PRODUCT_INVENTORY_CACHE_KEY = "inventory:product:{0}";
    private const string LOW_STOCK_CACHE_KEY = "inventory:lowstock";

    public InventoryService(IInventoryRepository repository, ICacheService cache, ILogger<InventoryService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<InventoryItem?> GetInventoryByIdAsync(int inventoryId)
    {
        var cacheKey = string.Format(INVENTORY_CACHE_KEY, inventoryId);
        return await _cache.GetOrLoadAsync(
            cacheKey,
            async () => await _repository.GetByIdAsync(inventoryId),
            TimeSpan.FromMinutes(30)
        );
    }

    public async Task<InventoryItem?> GetByProductAndWarehouseAsync(int productId, string warehouse)
    {
        var cacheKey = $"inventory:product:{productId}:warehouse:{warehouse}";
        return await _cache.GetOrLoadAsync(
            cacheKey,
            async () => await _repository.GetByProductAndWarehouseAsync(productId, warehouse),
            TimeSpan.FromMinutes(30)
        );
    }

    public async Task<IEnumerable<InventoryItem>> GetInventoryByProductAsync(int productId)
    {
        var cacheKey = string.Format(PRODUCT_INVENTORY_CACHE_KEY, productId);
        var result = await _cache.GetOrLoadAsync(
            cacheKey,
            async () => await _repository.GetByProductAsync(productId),
            TimeSpan.FromMinutes(15)
        );
        return result ?? [];
    }

    // Distributed lock pattern: ensure only one instance can reserve inventory
    public async Task<bool> ReserveInventoryAsync(int productId, string warehouse, int quantity, string instanceId)
    {
        var lockKey = $"inventory:lock:{productId}:{warehouse}";
        var lockValue = instanceId;

        // Acquire distributed lock
        var lockAcquired = await _cache.AcquireLockAsync(lockKey, lockValue, TimeSpan.FromSeconds(5));
        if (!lockAcquired)
        {
            _logger.LogWarning("Failed to acquire inventory lock for product: {ProductId}", productId);
            return false;
        }

        try
        {
            var inventory = await GetByProductAndWarehouseAsync(productId, warehouse);
            if (inventory == null)
                throw new NotFoundException($"Inventory for product {productId} in warehouse {warehouse} not found");

            if (!inventory.CanReserve(quantity))
                throw new InsufficientInventoryException(quantity, inventory.QuantityAvailable);

            inventory.Reserve(quantity);
            await _repository.UpdateAsync(inventory);

            // Update cache
            await InvalidateInventoryCachesAsync(productId, warehouse);
            _logger.LogInformation("Inventory reserved: Product {ProductId}, Quantity: {Quantity}", productId, quantity);
            return true;
        }
        finally
        {
            // Release lock
            await _cache.ReleaseLockAsync(lockKey, lockValue);
        }
    }

    /// <summary>
    /// Releases a previously reserved quantity for an inventory item looked up by its own id.
    /// </summary>
    public async Task<bool> ReleaseReservationAsync(int inventoryId, int quantity)
    {
        var inventory = await GetInventoryByIdAsync(inventoryId);
        if (inventory == null)
            return false;

        inventory.ReleaseReservation(quantity);
        await _repository.UpdateAsync(inventory);
        await InvalidateInventoryCachesAsync(inventory.ProductId, inventory.Warehouse);

        _logger.LogInformation("Inventory reservation released: InventoryId {InventoryId}, Quantity: {Quantity}", inventoryId, quantity);
        return true;
    }

    public async Task<bool> ReleaseReservationAsync(int productId, string warehouse, int quantity, string instanceId)
    {
        var lockKey = $"inventory:lock:{productId}:{warehouse}";
        var lockValue = instanceId;

        if (!await _cache.AcquireLockAsync(lockKey, lockValue, TimeSpan.FromSeconds(5)))
            return false;

        try
        {
            var inventory = await GetByProductAndWarehouseAsync(productId, warehouse);
            if (inventory == null)
                return false;

            inventory.ReleaseReservation(quantity);
            await _repository.UpdateAsync(inventory);
            await InvalidateInventoryCachesAsync(productId, warehouse);

            _logger.LogInformation("Inventory reservation released: Product {ProductId}, Quantity: {Quantity}", productId, quantity);
            return true;
        }
        finally
        {
            await _cache.ReleaseLockAsync(lockKey, lockValue);
        }
    }

    public async Task<bool> ReceiveStockAsync(int productId, string warehouse, int quantity, string instanceId)
    {
        var lockKey = $"inventory:lock:{productId}:{warehouse}";
        var lockValue = instanceId;

        if (!await _cache.AcquireLockAsync(lockKey, lockValue, TimeSpan.FromSeconds(5)))
            return false;

        try
        {
            var inventory = await GetByProductAndWarehouseAsync(productId, warehouse);
            if (inventory == null)
                throw new NotFoundException($"Inventory for product {productId} in warehouse {warehouse} not found");

            inventory.ReceiveStock(quantity);
            await _repository.UpdateAsync(inventory);
            await InvalidateInventoryCachesAsync(productId, warehouse);

            _logger.LogInformation("Stock received: Product {ProductId}, Quantity: {Quantity}", productId, quantity);
            return true;
        }
        finally
        {
            await _cache.ReleaseLockAsync(lockKey, lockValue);
        }
    }

    public async Task<bool> DispatchStockAsync(int productId, string warehouse, int quantity, string instanceId)
    {
        var lockKey = $"inventory:lock:{productId}:{warehouse}";
        var lockValue = instanceId;

        if (!await _cache.AcquireLockAsync(lockKey, lockValue, TimeSpan.FromSeconds(5)))
            return false;

        try
        {
            var inventory = await GetByProductAndWarehouseAsync(productId, warehouse);
            if (inventory == null)
                throw new NotFoundException($"Inventory for product {productId} in warehouse {warehouse} not found");

            inventory.DispatchStock(quantity);
            await _repository.UpdateAsync(inventory);
            await InvalidateInventoryCachesAsync(productId, warehouse);

            _logger.LogInformation("Stock dispatched: Product {ProductId}, Quantity: {Quantity}", productId, quantity);
            return true;
        }
        finally
        {
            await _cache.ReleaseLockAsync(lockKey, lockValue);
        }
    }

    public async Task<IEnumerable<InventoryItem>> GetLowStockItemsAsync()
    {
        var result = await _cache.GetOrLoadAsync(
            LOW_STOCK_CACHE_KEY,
            async () => await _repository.GetLowStockItemsAsync(),
            TimeSpan.FromMinutes(10)
        );
        return result ?? [];
    }

    public async Task<int> GetTotalProductQuantityAsync(int productId)
    {
        var cacheKey = $"inventory:total:{productId}";
        return await _cache.GetOrLoadAsync(
            cacheKey,
            async () => await _repository.GetTotalQuantityAsync(productId),
            TimeSpan.FromMinutes(15)
        );
    }

    private async Task InvalidateInventoryCachesAsync(int productId, string warehouse)
    {
        await _cache.RemoveAsync(string.Format(INVENTORY_CACHE_KEY, productId));
        await _cache.RemoveAsync(string.Format(PRODUCT_INVENTORY_CACHE_KEY, productId));
        await _cache.RemoveAsync($"inventory:product:{productId}:warehouse:{warehouse}");
        await _cache.RemoveAsync($"inventory:total:{productId}");
        await _cache.RemoveAsync(LOW_STOCK_CACHE_KEY);
    }
}
