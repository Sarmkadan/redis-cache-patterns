# ICacheService

`ICacheService` defines a contract for monitoring and reporting high-level cache statistics from a Redis-backed cache implementation. It provides read-only metrics that can be used to track cache performance, memory usage, and hit/miss ratios over time.

## API

### `public int TotalKeys`
Returns the total number of keys currently stored in the cache.

- **Return value**: The count of keys as an `int`. Will never be negative.
- **Thread safety**: Safe to call concurrently from multiple threads.
- **Exceptions**: None.

---

### `public long MemoryUsedBytes`
Returns the estimated memory usage of the cache in bytes.

- **Return value**: The memory usage as a `long`. Represents an approximate value and may not account for all overhead.
- **Thread safety**: Safe to call concurrently from multiple threads.
- **Exceptions**: None.

---
### `public int Hits`
Returns the total number of cache hits since the last reset or service initialization.

- **Return value**: The hit count as an `int`. Will never be negative.
- **Thread safety**: Safe to call concurrently from multiple threads.
- **Exceptions**: None.

---
### `public int Misses`
Returns the total number of cache misses since the last reset or service initialization.

- **Return value**: The miss count as an `int`. Will never be negative.
- **Thread safety**: Safe to call concurrently from multiple threads.
- **Exceptions**: None.

---
### `public DateTime CapturedAt`
Returns the timestamp when the current statistics were captured.

- **Return value**: A `DateTime` representing the moment the metrics were collected. The value is in the local time zone of the capturing system.
- **Thread safety**: Safe to call concurrently from multiple threads.
- **Exceptions**: None.

## Usage
