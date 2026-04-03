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
/// Demonstrates error handling and resilience patterns for production-grade
/// cache implementations including fallbacks, retries, and graceful degradation.
/// </summary>
public class ErrorHandlingAndResilienceExample
{
    private readonly ICacheService _cacheService;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ErrorHandlingAndResilienceExample> _logger;

    public ErrorHandlingAndResilienceExample(
        ICacheService cacheService,
        IProductRepository productRepository,
        ILogger<ErrorHandlingAndResilienceExample> logger)
    {
        _cacheService = cacheService;
        _productRepository = productRepository;
        _logger = logger;
    }

    /// <summary>
    /// Implements graceful degradation - uses database if cache is unavailable.
    /// </summary>
    public async Task<Product?> GetProductWithGracefulDegradationAsync(int productId)
    {
        var cacheKey = CacheKeyBuilder.BuildProductKey(productId);

        try
        {
            // Try cache first
            var cached = await _cacheService.GetAsync<Product>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation($"Cache HIT for product {productId}");
                return cached;
            }
        }
        catch (Exception ex)
        {
            // Cache error - log and continue
            _logger.LogWarning($"Cache error (product {productId}): {ex.Message}");
            _logger.LogInformation("Degrading to database-only read");
        }

        try
        {
            // Load from database
            var product = await _productRepository.GetByIdAsync(productId);

            // Try to cache for next time
            if (product != null)
            {
                try
                {
                    await _cacheService.SetAsync(cacheKey, product, TimeSpan.FromHours(1));
                    _logger.LogInformation($"Successfully cached product {productId} after database load");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Could not cache product {productId}: {ex.Message}");
                }
            }

            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Database error for product {productId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Implements exponential backoff retry logic for transient failures.
    /// </summary>
    public async Task<Product?> GetProductWithRetryAsync(int productId, int maxAttempts = 3)
    {
        var cacheKey = CacheKeyBuilder.BuildProductKey(productId);
        var attempt = 1;
        var delayMs = 100; // Initial delay: 100ms

        while (attempt <= maxAttempts)
        {
            try
            {
                _logger.LogInformation($"Attempt {attempt}/{maxAttempts} to get product {productId}");

                var cached = await _cacheService.GetAsync<Product>(cacheKey);
                if (cached != null)
                {
                    return cached;
                }

                var product = await _productRepository.GetByIdAsync(productId);
                if (product != null)
                {
                    try
                    {
                        await _cacheService.SetAsync(cacheKey, product, TimeSpan.FromHours(1));
                    }
                    catch
                    {
                        _logger.LogWarning($"Cache write failed (attempt {attempt})");
                    }
                }

                return product;
            }
            catch (TimeoutException ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning($"Timeout on attempt {attempt}: {ex.Message}. Retrying in {delayMs}ms");
                await Task.Delay(delayMs);
                delayMs *= 2; // Exponential backoff
                attempt++;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error on attempt {attempt}: {ex.Message}");
                return null;
            }
        }

        _logger.LogError($"Failed to get product {productId} after {maxAttempts} attempts");
        return null;
    }

    /// <summary>
    /// Circuit breaker pattern - stops calling failing service temporarily.
    /// </summary>
    public class CacheCircuitBreaker
    {
        private int _failureCount;
        private DateTime _lastFailureTime;
        private readonly int _failureThreshold = 5;
        private readonly TimeSpan _resetTimeout = TimeSpan.FromMinutes(1);

        public bool IsClosed => _failureCount == 0 ||
                               DateTime.UtcNow - _lastFailureTime > _resetTimeout;

        public void RecordSuccess()
        {
            _failureCount = 0;
        }

        public void RecordFailure()
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
        }

        public bool IsOpen => _failureCount >= _failureThreshold &&
                             DateTime.UtcNow - _lastFailureTime <= _resetTimeout;
    }

    /// <summary>
    /// Gets product using circuit breaker pattern.
    /// </summary>
    public async Task<Product?> GetProductWithCircuitBreakerAsync(int productId, CacheCircuitBreaker breaker)
    {
        var cacheKey = CacheKeyBuilder.BuildProductKey(productId);

        // If circuit is open, go directly to database
        if (breaker.IsOpen)
        {
            _logger.LogWarning("Circuit breaker OPEN - bypassing cache");
            return await _productRepository.GetByIdAsync(productId);
        }

        try
        {
            var cached = await _cacheService.GetAsync<Product>(cacheKey);
            if (cached != null)
            {
                breaker.RecordSuccess();
                return cached;
            }

            var product = await _productRepository.GetByIdAsync(productId);
            if (product != null)
            {
                try
                {
                    await _cacheService.SetAsync(cacheKey, product, TimeSpan.FromHours(1));
                    breaker.RecordSuccess();
                }
                catch (Exception ex)
                {
                    breaker.RecordFailure();
                    _logger.LogWarning($"Cache operation failed: {ex.Message}");
                }
            }

            return product;
        }
        catch (Exception ex)
        {
            breaker.RecordFailure();
            _logger.LogError($"Cache operation failed: {ex.Message}");
            return await _productRepository.GetByIdAsync(productId);
        }
    }

    /// <summary>
    /// Bulkhead isolation - separates cache operations by category to prevent
    /// one failure from affecting others.
    /// </summary>
    public class BulkheadIsolation
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly int _maxConcurrent;

        public BulkheadIsolation(int maxConcurrent = 10)
        {
            _maxConcurrent = maxConcurrent;
            _semaphore = new SemaphoreSlim(maxConcurrent);
        }

        public async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            await _semaphore.WaitAsync();
            try
            {
                return await operation();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    /// <summary>
    /// Updates product with comprehensive error handling.
    /// </summary>
    public async Task<OperationResult<Product>> UpdateProductWithErrorHandlingAsync(Product product)
    {
        try
        {
            _logger.LogInformation($"Updating product {product.Id}");

            // Validate
            if (!ValidationHelper.IsValidProduct(product))
            {
                _logger.LogWarning($"Invalid product data: {product.Id}");
                return OperationResult<Product>.Failure("Invalid product data");
            }

            // Update database
            var updated = await _productRepository.UpdateAsync(product);

            // Update cache (best effort)
            var cacheKey = CacheKeyBuilder.BuildProductKey(product.Id);
            try
            {
                await _cacheService.SetAsync(cacheKey, updated, TimeSpan.FromHours(2));
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning($"Failed to update cache: {cacheEx.Message}");
                // Don't fail the overall operation
            }

            return OperationResult<Product>.Success(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Update failed: {ex.Message}");
            return OperationResult<Product>.Failure($"Update failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles cache timeout gracefully with fallback.
    /// </summary>
    public async Task<Product?> GetProductWithTimeoutAsync(int productId, int timeoutMs = 5000)
    {
        var cacheKey = CacheKeyBuilder.BuildProductKey(productId);

        try
        {
            // Create cancellation token with timeout
            using var cts = new CancellationTokenSource(timeoutMs);

            var cached = await _cacheService.GetAsync<Product>(cacheKey, cts.Token);
            if (cached != null)
            {
                return cached;
            }

            return await _productRepository.GetByIdAsync(productId);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning($"Cache operation timeout (>{timeoutMs}ms): {ex.Message}");
            return await _productRepository.GetByIdAsync(productId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting product: {ex.Message}");
            return await _productRepository.GetByIdAsync(productId);
        }
    }

    /// <summary>
    /// Validates cache consistency with database.
    /// </summary>
    public async Task<OperationResult> ValidateCacheConsistencyAsync(int productId)
    {
        try
        {
            var cacheKey = CacheKeyBuilder.BuildProductKey(productId);

            // Get from both sources
            var cached = await _cacheService.GetAsync<Product>(cacheKey);
            var fromDb = await _productRepository.GetByIdAsync(productId);

            // Compare
            if (cached == null && fromDb == null)
            {
                _logger.LogInformation($"Product {productId}: Consistent (both null)");
                return OperationResult.Success();
            }

            if (cached == null && fromDb != null)
            {
                _logger.LogWarning($"Product {productId}: Inconsistent (cache miss, in DB)");
                return OperationResult.Failure("Cache miss for existing product");
            }

            if (cached != null && fromDb == null)
            {
                _logger.LogWarning($"Product {productId}: Inconsistent (in cache, deleted from DB)");
                await _cacheService.RemoveAsync(cacheKey);
                return OperationResult.Failure("Cache entry for deleted product");
            }

            if (cached!.Price != fromDb!.Price || cached.Stock != fromDb.Stock)
            {
                _logger.LogWarning($"Product {productId}: Data mismatch detected");
                return OperationResult.Failure("Cache data mismatch");
            }

            _logger.LogInformation($"Product {productId}: Consistent");
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Consistency check failed: {ex.Message}");
            return OperationResult.Failure($"Check failed: {ex.Message}");
        }
    }
}
