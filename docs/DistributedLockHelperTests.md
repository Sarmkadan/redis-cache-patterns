# DistributedLockHelperTests

Unit tests for `DistributedLockHelper`, verifying the correct behavior of distributed lock acquisition, execution, and release patterns using Redis as the backing store.

## API

### `AcquireAsync_WhenLockCanBeAcquired_ReturnsTrue`
Verifies that `DistributedLockHelper.AcquireAsync` returns `true` when the lock can be acquired without contention.

- **Parameters**: None
- **Return value**: `Task<bool>` that resolves to `true` when the lock is acquired.
- **Throws**: Only if the underlying Redis operation fails.

### `AcquireAsync_WhenLockCannotBeAcquired_ReturnsFalse`
Ensures that `DistributedLockHelper.AcquireAsync` returns `false` when the lock is already held by another process.

- **Parameters**: None
- **Return value**: `Task<bool>` that resolves to `false` when the lock is unavailable.
- **Throws**: Only if the underlying Redis operation fails.

### `ReleaseAsync_WhenLockIsHeld_CallsReleaseLockAsync`
Confirms that `DistributedLockHelper.ReleaseAsync` invokes the underlying `ReleaseLockAsync` method when the lock is currently held.

- **Parameters**: None
- **Return value**: `Task<bool>` that resolves to `true` on successful release.
- **Throws**: Only if the underlying Redis operation fails.

### `ReleaseAsync_WhenLockIsNotHeld_ReturnsFalse`
Validates that `DistributedLockHelper.ReleaseAsync` returns `false` when attempting to release a lock that is not currently held.

- **Parameters**: None
- **Return value**: `Task<bool>` that resolves to `false`.
- **Throws**: Only if the underlying Redis operation fails.

### `ExecuteAsync_WithAction_AcquiresLockExecutesActionAndReleases`
Tests that `DistributedLockHelper.ExecuteAsync` acquires the lock, executes the provided action, and releases the lock upon completion.

- **Parameters**: None
- **Return value**: `Task<bool>` that resolves to `true` if the action completes successfully.
- **Throws**: Propagates any exception thrown by the action.

### `ExecuteAsync_WhenLockCannotBeAcquired_ReturnsFalse`
Ensures that `DistributedLockHelper.ExecuteAsync` returns `false` when the lock cannot be acquired due to contention.

- **Parameters**: None
- **Return value**: `Task<bool>` that resolves to `false`.
- **Throws**: Only if the underlying Redis operation fails.

### `ExecuteAsyncGeneric_ReturnsActionResult`
Verifies that `DistributedLockHelper.ExecuteAsync<T>` returns the result of the provided function after acquiring and releasing the lock.

- **Parameters**: None
- **Return value**: `Task<T>` containing the result of the function.
- **Throws**: Propagates any exception thrown by the function.

### `ExecuteAsyncGeneric_WhenLockCannotBeAcquired_ThrowsInvalidOperationException`
Confirms that `DistributedLockHelper.ExecuteAsync<T>` throws an `InvalidOperationException` when the lock cannot be acquired.

- **Parameters**: None
- **Return value**: `Task<T>` that throws `InvalidOperationException`.
- **Throws**: `InvalidOperationException` when the lock is unavailable.

### `ExecuteAsync_WhenActionThrows_ReleasesLockAndThrows`
Ensures that `DistributedLockHelper.ExecuteAsync` releases the lock and rethrows the original exception when the action throws.

- **Parameters**: None
- **Return value**: `Task<bool>` that throws the original exception.
- **Throws**: The original exception thrown by the action.

### `DisposeAsync_WhenLocked_ReleasesLock`
Validates that `DistributedLockHelper.DisposeAsync` releases the lock when called on an instance that currently holds a lock.

- **Parameters**: None
- **Return value**: `ValueTask` completing when the lock is released.
- **Throws**: Only if the underlying Redis operation fails.

### `LockValue_ReturnsCorrectValue`
Checks that the `LockValue` property returns the expected unique identifier for the current lock.

- **Parameters**: None
- **Return value**: `string` representing the lock value.
- **Throws**: None.

### `Constructor_WithoutExplicitLockValue_GeneratesGuid`
Ensures that the constructor generates a unique `Guid` as the lock value when none is provided.

- **Parameters**: None
- **Return value**: Instance of `DistributedLockHelper` with a generated `Guid`-based lock value.
- **Throws**: None.

### `Constructor_WithDefaultDuration_UsesDefaultTimespan`
Verifies that the constructor uses the default `TimeSpan` duration when none is explicitly provided.

- **Parameters**: None
- **Return value**: Instance of `DistributedLockHelper` with the default lock duration.
- **Throws**: None.

## Usage

### Basic lock acquisition and release
