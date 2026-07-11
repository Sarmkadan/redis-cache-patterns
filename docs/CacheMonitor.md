# CacheMonitor

A utility class for monitoring and analyzing cache performance metrics in Redis-based caching scenarios. It tracks cache entry access patterns, computes hit rates, and provides diagnostic information to help optimize cache usage and identify cold or underperforming entries.

## API

### `CacheMonitor`

The default constructor initializes a new instance of the `CacheMonitor` class with no tracked entries.

### `public async Task<CacheStatistics> GetStatisticsAsync()`

Retrieves aggregated cache statistics including total hits, misses, hit rate, and size metrics.

- **Return value**: A `Task<CacheStatistics>` representing the cache statistics.
- **Exceptions**: May throw if the underlying cache client fails during data retrieval.

### `public async Task PrintStatisticsAsync()`

Asynchronously prints formatted cache statistics to the console, including total hits, misses, hit rate, and size.

- **Exceptions**: May throw if the underlying cache client fails during data retrieval or if console output fails.

### `public void TrackEntry(string key)`

Tracks a cache entry by its key for subsequent monitoring and analysis.

- **Parameters**:
  - `key` (string): The cache key to track.
- **Exceptions**: Throws `ArgumentNullException` if `key` is null.

### `public IEnumerable<CacheEntry> GetTrackedEntries()`

Returns an enumeration of all currently tracked cache entries.

- **Return value**: An `IEnumerable<CacheEntry>` containing tracked entries.

### `public void ClearTracking()`

Clears all currently tracked cache entries.

### `public async Task<double> GetAverageHitRateAsync()`

Computes the average hit rate across all tracked cache entries based on access history.

- **Return value**: A `Task<double>` representing the average hit rate (between 0.0 and 1.0).
- **Exceptions**: May throw if tracking data is inconsistent or cache access logs are unavailable.

### `public long GetTotalCacheSize()`

Returns the total estimated size (in bytes) of all tracked cache entries.

- **Return value**: A `long` representing the total size in bytes.
- **Exceptions**: May throw if size metadata is missing or corrupted for any entry.

### `public IEnumerable<CacheEntry> GetEntriesByHitRate(double minHitRate)`

Filters tracked entries by a minimum hit rate threshold.

- **Parameters**:
  - `minHitRate` (double): The minimum hit rate (inclusive) to filter by.
- **Return value**: An `IEnumerable<CacheEntry>` of entries meeting or exceeding the hit rate.
- **Exceptions**: Throws `ArgumentOutOfRangeException` if `minHitRate` is outside [0.0, 1.0].

### `public IEnumerable<CacheEntry> GetColdEntries(int count)`

Returns the least frequently accessed (coldest) tracked entries.

- **Parameters**:
  - `count` (int): The maximum number of cold entries to return.
- **Return value**: An `IEnumerable<CacheEntry>` of up to `count` coldest entries.
- **Exceptions**: Throws `ArgumentOutOfRangeException` if `count` is negative.

## Usage
