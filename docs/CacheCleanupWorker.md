# CacheCleanupWorker

A background worker that periodically cleans up expired or stale entries in a distributed Redis cache. It ensures cache consistency by removing entries that have exceeded their TTL or are marked for eviction, while minimizing Redis memory pressure.

## API

### `public CacheCleanupWorker`

Initializes a new instance of the `CacheCleanupWorker` with the specified Redis connection and cleanup interval.

### `public void Start`

Starts the cleanup worker asynchronously. The worker will begin scanning and removing stale cache entries at the configured interval.

- **Parameters**: None
- **Return value**: None
- **Exceptions**: Throws `ArgumentNullException` if the Redis connection is null.
- **Exceptions**: Throws `InvalidOperationException` if the worker is already running.

### `public void Stop`

Stops the cleanup worker gracefully. The worker will complete the current cleanup cycle before terminating.

- **Parameters**: None
- **Return value**: None
- **Exceptions**: Throws `InvalidOperationException` if the worker is not running.

### `public void Dispose`

Releases all resources used by the `CacheCleanupWorker`, including stopping the worker if it is running. This method is idempotent and safe to call multiple times.

- **Parameters**: None
- **Return value**: None

## Usage

### Example 1: Basic Usage with Start/Stop
