#nullable enable
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Services;
using RedisCachePatterns.Utilities;
using Xunit;

namespace RedisCachePatterns.Tests;

/// <summary>
/// Integration tests demonstrating end-to-end workflows of the caching library.
/// </summary>
public class CacheAsideIntegrationTests
{
    private readonly Mock<ILogger> _mockLogger = new();

    [Fact]
    public async Task CacheAsidePattern_LoadFromSourceOnFirstCall()
    {
        var mockCache = new MockCacheService();
        var dataLoadCount = 0;

        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Sku = "SKU-001",
            Price = 99.99m,
            StockQuantity = 100,
            ReorderLevel = 20,
            Category = "Electronics",
            IsActive = true
        };

        var result = await mockCache.GetOrLoadAsync(
            "product:1",
            async () =>
            {
                dataLoadCount++;
                await Task.CompletedTask;
                return product;
            });

        result.Should().NotBeNull();
        result?.Id.Should().Be(1);
        dataLoadCount.Should().Be(1);
    }

    [Fact]
    public async Task CacheAsidePattern_ReturnsFromCacheOnSecondCall()
    {
        var mockCache = new MockCacheService();
        var dataLoadCount = 0;

        var product = new Product
        {
            Id = 2,
            Name = "Cached Product",
            Sku = "SKU-002",
            Price = 49.99m,
            StockQuantity = 50,
            ReorderLevel = 10,
            Category = "Gadgets",
            IsActive = true
        };

        await mockCache.GetOrLoadAsync("product:2", async () =>
        {
            dataLoadCount++;
            await Task.CompletedTask;
            return product;
        });

        var cachedResult = await mockCache.GetOrLoadAsync("product:2", async () =>
        {
            dataLoadCount++;
            await Task.CompletedTask;
            return new Product { Id = 99, Name = "Should not load" };
        });

        cachedResult?.Id.Should().Be(2);
        dataLoadCount.Should().Be(1);
    }

    [Fact]
    public async Task MultipleThreads_SimultaneousCacheAccess_AllSucceed()
    {
        var mockCache = new MockCacheService();
        var tasks = new List<Task<Product?>>();

        var product = new Product
        {
            Id = 3,
            Name = "Concurrent Product",
            Sku = "SKU-003",
            Price = 29.99m,
            StockQuantity = 200,
            ReorderLevel = 50,
            Category = "Test",
            IsActive = true
        };

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(mockCache.GetOrLoadAsync("product:3", async () =>
            {
                await Task.Delay(10);
                return product;
            }));
        }

        var results = await Task.WhenAll(tasks);

        results.Should().AllSatisfy(r => r?.Id.Should().Be(3));
        results.Should().HaveCount(10);
    }
}

public class DistributedLockIntegrationTests
{
    [Fact]
    public async Task DistributedLock_ProtectsSharedResource()
    {
        // DistributedLockHelper.ExecuteAsync makes a single acquire attempt and
        // returns false immediately if the lock is held (see OrderService,
        // which treats a failed acquire as "try again later", not "wait").
        // A real caller that must eventually run the action retries acquisition
        // itself, so the test drives that retry loop against a cache fake that
        // enforces genuine mutual exclusion per lock key (MockCacheService),
        // instead of a bare Mock<ICacheService> that always returns true and
        // therefore proves nothing about exclusion.
        var cache = new MockCacheService();
        var lockKey = "shared-resource";
        var sharedCounter = 0;
        var maxConcurrent = 0;
        var currentConcurrent = 0;
        var lockLock = new Lock();

        async Task RunWithLockAsync()
        {
            while (true)
            {
                var lockHelper = new DistributedLockHelper(cache, lockKey, Guid.NewGuid().ToString(), TimeSpan.FromSeconds(5));
                var executed = await lockHelper.ExecuteAsync(async () =>
                {
                    lock (lockLock)
                    {
                        currentConcurrent++;
                        if (currentConcurrent > maxConcurrent)
                            maxConcurrent = currentConcurrent;
                    }

                    sharedCounter++;
                    await Task.Delay(10);

                    lock (lockLock)
                    {
                        currentConcurrent--;
                    }
                });

                if (executed)
                    return;

                await Task.Delay(5);
            }
        }

        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(RunWithLockAsync());
        }

        await Task.WhenAll(tasks);

        sharedCounter.Should().Be(5);
        maxConcurrent.Should().Be(1);
    }

    [Fact]
    public async Task DistributedLock_LockReleaseGuarantee_ReleasesEvenOnException()
    {
        var mockCache = new Mock<ICacheService>();
        var lockKey = "exception-lock";
        var lockValue = Guid.NewGuid().ToString();
        var lockAcquiredCount = 0;
        var lockReleasedCount = 0;

        mockCache.Setup(c => c.AcquireLockAsync(lockKey, lockValue, It.IsAny<TimeSpan>()))
            .Callback(() => lockAcquiredCount++)
            .ReturnsAsync(true);
        mockCache.Setup(c => c.ReleaseLockAsync(lockKey, lockValue))
            .Callback(() => lockReleasedCount++)
            .ReturnsAsync(true);

        var lockHelper = new DistributedLockHelper(mockCache.Object, lockKey, lockValue);

        try
        {
            await lockHelper.ExecuteAsync(async () =>
            {
                await Task.CompletedTask;
                throw new InvalidOperationException("Test exception");
            });
        }
        catch { }

        lockAcquiredCount.Should().Be(1);
        lockReleasedCount.Should().Be(1);
    }
}

public class CompressionIntegrationTests
{
    [Fact]
    public void LargeDataset_CompressionAchievesMeaningfulReduction()
    {
        var largeData = string.Concat(
            Enumerable.Repeat("This is a repeating string for compression testing. ", 100));

        var compressed = CompressionUtil.CompressString(largeData);
        var decompressed = CompressionUtil.DecompressString(compressed);

        decompressed.Should().Be(largeData);

        var ratio = CompressionUtil.GetCompressionRatio(largeData.Length, compressed.Length);
        ratio.Should().BeLessThan(100);
    }

    [Fact]
    public void CompressionRoundTrip_PreservesDataIntegrity()
    {
        var testCases = new[]
        {
            "Simple ASCII text",
            "Unicode: 你好世界 مرحبا بك שלום עולם",
            "Special chars: !@#$%^&*()_+-=[]{}|;:',.<>?/`~",
            new string('A', 10000),
            ""
        };

        foreach (var testData in testCases)
        {
            var compressed = CompressionUtil.CompressString(testData);
            var decompressed = CompressionUtil.DecompressString(compressed);
            decompressed.Should().Be(testData, $"Failed for test case: {testData[..Math.Min(50, testData.Length)]}");
        }
    }
}

public class RetryAndCircuitBreakerIntegrationTests
{
    [Fact]
    public async Task RetryPolicy_RecoverFromTransientFailures()
    {
        var attemptCount = 0;

        var result = await RetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                attemptCount++;
                await Task.CompletedTask;

                if (attemptCount < 3)
                    throw new TimeoutException("Transient timeout");

                return 42;
            },
            maxRetries: 5,
            initialDelayMs: 10);

        result.Should().Be(42);
        attemptCount.Should().Be(3);
    }

    [Fact]
    public async Task CircuitBreaker_ProtectsDownstreamService()
    {
        RetryHelper.CircuitBreaker.Reset("downstream-service");
        var callAttempts = 0;

        for (int i = 0; i < 5; i++)
        {
            try
            {
                await RetryHelper.CircuitBreaker.ExecuteAsync<int>(
                    "downstream-service",
                    async () =>
                    {
                        callAttempts++;
                        await Task.CompletedTask;
                        throw new InvalidOperationException("Service unavailable");
                    },
                    failureThreshold: 3,
                    resetTimeoutSeconds: 60);
            }
            catch { }
        }

        callAttempts.Should().Be(3);
    }
}

public class ValidationIntegrationTests
{
    [Fact]
    public void CompleteProductValidation_Workflow()
    {
        var validProduct = new
        {
            Name = "Premium Widget",
            Price = 99.99m,
            Quantity = 50
        };

        Action validateProduct = () =>
        {
            ValidationHelper.ValidateProductName(validProduct.Name);
            ValidationHelper.ValidatePrice(validProduct.Price);
            ValidationHelper.ValidateQuantity(validProduct.Quantity);
        };

        validateProduct.Should().NotThrow();
    }

    [Fact]
    public void ProductValidationWithErrors_CollectsAllErrors()
    {
        var errors = ValidationHelper.GetValidationErrors(() =>
        {
            ValidationHelper.ValidateProductName("");
            ValidationHelper.ValidatePrice(-50m);
        });

        errors.Should().NotBeEmpty();
    }
}

public class IdempotencyIntegrationTests
{
    [Fact]
    public void IdempotencyHelper_PreventsOrOperationDuplication()
    {
        var helper = new IdempotencyHelper(TimeSpan.FromHours(1));
        var processCount = 0;

        var requestId = "order-123";

        if (!helper.IsProcessed(requestId))
        {
            processCount++;
            var result = new { OrderId = 123, Status = "Created" };
            helper.MarkAsProcessed(requestId, result);
        }

        helper.IsProcessed(requestId).Should().BeTrue();

        if (!helper.IsProcessed(requestId))
        {
            processCount++;
        }

        processCount.Should().Be(1);
    }

    [Fact]
    public void IdempotencyHelper_RetrievesStoredResult()
    {
        var helper = new IdempotencyHelper();
        var requestId = "payment-456";
        var paymentResult = new { TransactionId = "TXN-789", Amount = 99.99m, Status = "Completed" };

        helper.MarkAsProcessed(requestId, paymentResult);
        object? retrieved = helper.GetResult<dynamic>(requestId);

        retrieved.Should().NotBeNull();
    }
}

/// <summary>
/// Mock cache service for integration testing without Redis dependency.
/// </summary>
public class MockCacheService : ICacheService
{
    private readonly Dictionary<string, (string Value, DateTime ExpiresAt)> _store = new();
    private readonly Lock _lock = new();

    public async Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan? expiration = null)
    {
        lock (_lock)
        {
            if (_store.TryGetValue(key, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(cached.Value);
            }
        }

        var value = await loadFn();
        if (value != null)
        {
            await SetAsync(key, value, expiration);
        }
        return value;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        lock (_lock)
        {
            if (_store.TryGetValue(key, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(cached.Value);
            }
        }
        return await Task.FromResult<T?>(default);
    }

public async Task<T?> GetWithSlidingExpirationAsync<T>(string key, TimeSpan slidingExpiration)
{
    lock (_lock)
    {
        if (_store.TryGetValue(key, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            // Reset the expiration on every access (sliding expiration)
            _store[key] = (cached.Value, DateTime.UtcNow.Add(slidingExpiration));
            return System.Text.Json.JsonSerializer.Deserialize<T>(cached.Value);
        }
    }
    return await Task.FromResult<T?>(default);
}

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        var expiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : DateTime.MaxValue;

        lock (_lock)
        {
            _store[key] = (json, expiresAt);
        }
        return Task.CompletedTask;
    }

    public Task<T?> GetOrLoadWithSlidingExpirationAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan slidingExpiration)
        => GetOrLoadAsync(key, loadFn, slidingExpiration);

    public Task<T?> GetOrLoadWithEarlyExpirationAsync<T>(string key, Func<Task<T>> loadFn, TimeSpan expiration, double beta = 1.0)
        => GetOrLoadAsync(key, loadFn, expiration);

    public Task<CacheKeyMetadata?> GetKeyMetadataAsync(string key) => Task.FromResult<CacheKeyMetadata?>(null);

    public async Task<T> WriteAsync<T>(string key, T value, Func<Task<T>> persistFn, TimeSpan? expiration = null)
    {
        var persisted = await persistFn();
        await SetAsync(key, persisted, expiration);
        return persisted;
    }

    public Task RemoveAsync(string key)
    {
        lock (_lock)
        {
            _store.Remove(key);
        }
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        lock (_lock)
        {
            var keysToRemove = _store.Keys.Where(k => MatchPattern(k, pattern)).ToList();
            foreach (var key in keysToRemove)
            {
                _store.Remove(key);
            }
        }
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key)
    {
        lock (_lock)
        {
            return _store.ContainsKey(key) && _store[key].ExpiresAt > DateTime.UtcNow;
        }
    }

    public async Task<TimeSpan?> GetExpirationAsync(string key)
    {
        lock (_lock)
        {
            if (_store.TryGetValue(key, out var cached))
            {
                var remaining = cached.ExpiresAt - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : null;
            }
        }
        return null;
    }

    public async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern)
    {
        lock (_lock)
        {
            return _store.Keys.Where(k => MatchPattern(k, pattern)).ToList();
        }
    }

    public async Task<bool> AcquireLockAsync(string lockKey, string lockValue, TimeSpan duration)
    {
        lock (_lock)
        {
            if (!_store.ContainsKey(lockKey))
            {
                _store[lockKey] = (lockValue, DateTime.UtcNow.Add(duration));
                return true;
            }
        }
        return false;
    }

    public async Task<bool> ReleaseLockAsync(string lockKey, string lockValue)
    {
        lock (_lock)
        {
            if (_store.TryGetValue(lockKey, out var cached) && cached.Value == lockValue)
            {
                _store.Remove(lockKey);
                return true;
            }
        }
        return false;
    }

    public async Task<bool> RenewLockAsync(string lockKey, string lockValue, TimeSpan newDuration)
    {
        lock (_lock)
        {
            if (_store.TryGetValue(lockKey, out var cached) && cached.Value == lockValue)
            {
                _store[lockKey] = (lockValue, DateTime.UtcNow.Add(newDuration));
                return true;
            }
        }
        return false;
    }

    public Task FlushAsync()
    {
        lock (_lock)
        {
            _store.Clear();
        }
        return Task.CompletedTask;
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
        => new() { TotalKeys = _store.Count };

    public ValueTask SetPolicyAsync(CachePolicy policy) => ValueTask.CompletedTask;

    public ValueTask<CachePolicy?> GetPolicyAsync(string key) => ValueTask.FromResult<CachePolicy?>(null);

    private static bool MatchPattern(string key, string pattern)
    {
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(key, regexPattern);
    }
}
