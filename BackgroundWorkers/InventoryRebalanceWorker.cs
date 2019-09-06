// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

#nullable enable

using Microsoft.Extensions.Logging;
using RedisCachePatterns.Services;
using RedisCachePatterns.Events;

namespace RedisCachePatterns.BackgroundWorkers;

/// <summary>
/// Background worker monitoring inventory levels and alerting on low stock conditions
/// Rebalances inventory across warehouse locations (simulated)
/// </summary>
public class InventoryRebalanceWorker : IDisposable
{
    private readonly InventoryService _inventoryService;
    private readonly ProductService _productService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<InventoryRebalanceWorker> _logger;
    private readonly TimeSpan _interval;
    private Timer? _timer;
    private bool _isRunning;

    public InventoryRebalanceWorker(
        InventoryService inventoryService,
        ProductService productService,
        IEventPublisher eventPublisher,
        ILogger<InventoryRebalanceWorker> logger,
        TimeSpan? interval = null)
    {
        _inventoryService = inventoryService;
        _productService = productService;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _interval = interval ?? TimeSpan.FromMinutes(30);
    }

    public void Start()
    {
        if (_isRunning)
        {
            _logger.LogWarning("Inventory rebalance worker is already running");
            return;
        }

        _isRunning = true;
        _timer = new Timer(ExecuteRebalance, null, TimeSpan.FromMinutes(1), _interval);
        _logger.LogInformation("Inventory rebalance worker started with interval: {IntervalSeconds}s", _interval.TotalSeconds);
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _timer?.Dispose();
        _timer = null;
        _logger.LogInformation("Inventory rebalance worker stopped");
    }

    private async void ExecuteRebalance(object? state)
    {
        try
        {
            _logger.LogInformation("Starting inventory rebalance check");

            var lowStockProducts = await _productService.GetLowStockProductsAsync().ConfigureAwait(false);

            foreach (var product in lowStockProducts)
            {
                _logger.LogWarning(
                    "Low stock alert: {ProductName} | Current: {Current} | Reorder: {Reorder}",
                    product.Name, product.StockQuantity, product.ReorderLevel);

                // Publish event for low stock
                await _eventPublisher.PublishAsync(new InventoryLowStockEvent
                {
                    Source = "InventoryRebalanceWorker",
                    ProductId = product.Id,
                    CurrentStock = product.StockQuantity,
                    ReorderLevel = product.ReorderLevel
                });
            }

            _logger.LogInformation("Inventory rebalance check completed: {AlertCount} products with low stock",
                lowStockProducts.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during inventory rebalance");
        }
    }

    public void Dispose()
    {
        Stop();
        _timer?.Dispose();
    }
}

/// <summary>
/// Domain event for low inventory situations
/// </summary>
public class InventoryLowStockEvent : DomainEvent
{
    public int ProductId { get; set; }
    public int CurrentStock { get; set; }
    public int ReorderLevel { get; set; }
}
