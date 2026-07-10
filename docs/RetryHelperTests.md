# RetryHelperTests

Unit test class for `RetryHelper`, verifying retry logic, circuit breaker behavior, and exception handling in asynchronous operations.

## API

### `ExecuteWithRetryAsync_WhenOperationSucceedsOnFirstAttempt_ReturnsResult`
Verifies that when an operation succeeds on the first attempt, the retry helper returns the result immediately without additional retries.

### `ExecuteWithRetryAsync_WhenOperationFailsThenSucceeds_EventuallyReturnsResult`
Ensures that when an operation fails once and then succeeds, the retry helper eventually returns the result after one retry.

### `ExecuteWithRetryAsync_WhenOperationExceedsMaxRetries_ThrowsInvalidOperationException`
Confirms that when an operation fails on every attempt up to the maximum retry count, the retry helper throws an `InvalidOperationException`.

### `ExecuteWithRetryAsync_WithCustomInitialDelay_RespectsDelayBetweenAttempts`
Validates that when a custom initial delay is specified, the retry helper waits the specified duration between retry attempts.

### `ExecuteWithRetryAsync_WithExponentialBackoff_IncreaseDelayBetweenAttempts`
Checks that when exponential backoff is enabled, the delay between retry attempts increases according to the backoff factor.

### `ExecuteWithRetryAsync_LogsWarningOnRetry`
Ensures that when a retry occurs, the retry helper logs a warning message indicating the retry attempt.

### `ExecuteWithRetryAsync_OnFinalAttempt_DoesNotRetry`
Confirms that when the maximum retry count is reached, the retry helper does not perform an additional retry after the final attempt.

### `ExecuteWithRetryAsync_PreservesInnerExceptionAsInnerException`
Verifies that when an operation fails, the retry helper preserves the original exception as the inner exception of the thrown `InvalidOperationException`.

### `CircuitBreaker_WhenOperationSucceeds_AllowsSubsequentCalls`
Ensures that when an operation succeeds, the circuit breaker remains closed and allows subsequent calls to proceed.

### `CircuitBreaker_WhenFailureThresholdIsReached_OpensCircuit`
Confirms that when the failure threshold is reached, the circuit breaker opens and prevents further calls.

### `CircuitBreaker_WhenCircuitOpens_RejectsNewRequests`
Validates that when the circuit is open, the circuit breaker rejects new requests immediately without invoking the operation.

### `CircuitBreaker_AfterSuccessfulCall_ResetsFailureCount`
Checks that after a successful call following a failure, the circuit breaker resets the failure count.

### `CircuitBreaker_MultipleCircuits_AreIndependent`
Ensures that multiple circuit breaker instances operate independently and do not interfere with each other.

### `CircuitBreaker_AfterResetTimeout_AllowsRetry`
Confirms that after the reset timeout elapses, the circuit breaker allows a retry attempt.

### `CircuitBreaker_Reset_ClearsCircuitState`
Verifies that calling `Reset` on the circuit breaker clears its state and allows new calls to proceed.

## Usage
