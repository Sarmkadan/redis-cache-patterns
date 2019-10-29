# OrderServiceTests

The `OrderServiceTests` class serves as the comprehensive test suite for validating the behavior of the `OrderService` within the `redis-cache-patterns` project. It specifically verifies the correctness of caching strategies, cache key generation, cache invalidation logic, and distributed locking mechanisms used during order lifecycle operations such as creation, confirmation, and cancellation. Each test method isolates a specific scenario to ensure that the service interacts with the Redis cache and the underlying repository exactly as intended, maintaining data consistency and performance optimization.

## API

### `public OrderServiceTests()`
Initializes a new instance of the `OrderServiceTests` class. This constructor typically sets up the necessary mocks, fakes, or test containers required to simulate the Redis cache and database repository dependencies before each test execution.

### `public async Task GetOrderByIdAsync_WhenCacheHit_ReturnsOrderWithoutHittingRepository()`
Verifies that when an order exists in the cache, the service returns the cached entity without invoking the underlying repository.
*   **Parameters**: None (test context is internal).
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws an assertion exception if the repository is accessed or if the returned order does not match the cached data.

### `public async Task GetOrderByIdAsync_UsesCorrectCacheKey()`
Ensures that the service constructs and utilizes the correct cache key format when retrieving an order by its unique identifier.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws an assertion exception if the cache key passed to the cache provider does not match the expected pattern.

### `public async Task GetOrderByNumberAsync_RetrievesOrderByOrderNumber()`
Validates the retrieval logic for orders based on their human-readable order number rather than their internal ID.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws an assertion exception if the order returned does not correspond to the requested order number.

### `public async Task GetUserOrdersAsync_ReturnsUserOrdersFromCache()`
Confirms that the list of orders associated with a specific user is retrieved directly from the cache when available.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws an assertion exception if the repository is queried unnecessarily or if the order list is incomplete.

### `public async Task CreateOrderAsync_GeneratesOrderNumberAndCachesResult()`
Tests that creating a new order results in the generation of a unique order number and that the newly created order is immediately stored in the cache.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws an assertion exception if the order number is null/empty or if the cache set operation is not invoked.

### `public async Task CreateOrderAsync_InvalidatesUserOrdersCache()`
Ensures that upon successful creation of an order, the cached list of orders for the associated user is invalidated to prevent stale data.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws an assertion exception if the specific user orders cache key is not removed or updated.

### `public async Task ConfirmOrderAsync_WhenLockAcquired_ConfirmsOrderAndReturnsTrue()`
Validates the happy path for order confirmation where the distributed lock is successfully acquired, allowing the order status to be updated and the method to return `true`.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation (resulting in a boolean verification within the test).
*   **Exceptions**: Throws an assertion exception if the order status is not updated or the return value indicates failure.

### `public async Task ConfirmOrderAsync_WhenLockNotAcquired_ReturnsFalseWithoutConfirming()`
Verifies that if the distributed lock cannot be acquired (indicating a concurrent operation), the method returns `false` and does not modify the order status.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws an assertion exception if the repository update method is called despite lock failure.

### `public async Task ConfirmOrderAsync_ReleasesLockEvenOnException()`
Ensures robustness by verifying that the distributed lock is released even if an unexpected exception occurs during the confirmation process.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws an assertion exception if the lock release mechanism is not triggered in the exception handler.

### `public async Task CancelOrderAsync_WhenOrderExists_CancelsAndInvalidatesCache()`
Tests the cancellation workflow for an existing order, ensuring the status is updated to "Cancelled" and relevant cache entries are invalidated.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws an assertion exception if the order is not marked as cancelled or cache invalidation fails.

### `public async Task CancelOrderAsync_WhenOrderNotFound_ReturnsFalse()`
Validates that attempting to cancel a non-existent order results in a `false` return value and no side effects.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws an assertion exception if the method returns `true` or attempts to update a non-existent record.

### `public async Task GetPendingOrdersAsync_ReturnsPendingOrdersFromCache()`
Confirms that the collection of pending orders is served from the cache to optimize read performance for listing operations.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws an assertion exception if the repository is accessed or the filtered list is incorrect.

## Usage

The following examples demonstrate how these tests might be structured using xUnit and Moq to verify the caching and locking behaviors.

### Example 1: Verifying Cache Hit Behavior
This example illustrates the arrangement and assertion logic for ensuring the repository is bypassed when data is present in the cache.

```csharp
[Fact]
public async Task GetOrderByIdAsync_WhenCacheHit_ReturnsOrderWithoutHittingRepository()
{
    // Arrange
    var orderId = Guid.NewGuid();
    var cachedOrder = new Order { Id = orderId, Status = OrderStatus.Pending };
    
    _cacheMock.Setup(c => c.GetAsync<Order>(It.IsAny<string>()))
              .ReturnsAsync(cachedOrder);

    var service = new OrderService(_cacheMock.Object, _repoMock.Object);

    // Act
    var result = await service.GetOrderByIdAsync(orderId);

    // Assert
    Assert.Equal(cachedOrder.Id, result.Id);
    _repoMock.Verify(r => r.GetByIdAsync(orderId), Times.Never);
}
```

### Example 2: Verifying Distributed Lock Release on Exception
This example demonstrates testing the resilience of the locking mechanism during error conditions.

```csharp
[Fact]
public async Task ConfirmOrderAsync_ReleasesLockEvenOnException()
{
    // Arrange
    var orderId = Guid.NewGuid();
    _lockMock.Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
             .ReturnsAsync(true);
    
    _repoMock.Setup(r => r.UpdateStatusAsync(orderId, OrderStatus.Confirmed))
             .ThrowsAsync(new InvalidOperationException("Simulated failure"));

    var service = new OrderService(_cacheMock.Object, _repoMock.Object, _lockMock.Object);

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmOrderAsync(orderId));
    
    // Verify lock release was called despite the exception
    _lockMock.Verify(l => l.ReleaseAsync(It.IsAny<string>()), Times.Once);
}
```

## Notes

*   **Cache Key Consistency**: The tests strictly validate cache key formats (`GetOrderByIdAsync_UsesCorrectCacheKey`). Any deviation in the production service's key generation strategy will cause these tests to fail, ensuring that cache collisions do not occur between different entities or environments.
*   **Concurrency and Locking**: The `ConfirmOrderAsync` tests highlight the critical nature of distributed locking. The logic assumes that if a lock cannot be acquired, the operation must fail fast (`ReturnsFalseWithoutConfirming`) to prevent race conditions. Furthermore, the guarantee that locks are released even during exceptions (`ReleasesLockEvenOnException`) is vital to prevent deadlocks in a multi-instance deployment.
*   **Cache Invalidation Strategy**: The suite enforces a strict invalidation policy. Actions that modify state (`CreateOrderAsync`, `CancelOrderAsync`) must explicitly invalidate dependent cache entries (such as user-specific order lists) to maintain data integrity. There is no reliance on time-based expiration for immediate consistency in these scenarios.
*   **Thread Safety**: While the tests themselves run sequentially in most test runners by default, the logic they verify (locking and atomic cache updates) is designed for high-concurrency environments. The tests do not simulate multi-threaded access within a single test method but rather verify that the service's internal synchronization primitives are invoked correctly.
