# AnalyticsEndpointExtensions

Extension methods for interacting with Redis cache analytics endpoints. These methods provide access to key performance indicators, hot/cold key analysis, and efficiency metrics through the Redis cache analytics service.

## API

### `GetHotKeysSnapshotAsync`

Retrieves a snapshot of the most frequently accessed keys in the Redis cache. This method is useful for identifying keys that are under heavy load and may benefit from optimization or partitioning.

- **Parameters**:
  - `connection` (IConnectionMultiplexer): The Redis connection multiplexer.
  - `options` (AnalyticsOptions, optional): Configuration options for the analytics query.
- **Return value**: `Task<ApiResponse<AnalyticsDashboardResponse>>` containing a dashboard response with hot key data.
- **Exceptions**: Throws `ArgumentNullException` if `connection` is null.

### `GetColdKeysSnapshotAsync`

Retrieves a snapshot of the least frequently accessed keys in the Redis cache. This helps identify keys that may be consuming memory without proportional usage, indicating potential candidates for eviction or archival.

- **Parameters**:
  - `connection` (IConnectionMultiplexer): The Redis connection multiplexer.
  - `options` (AnalyticsOptions, optional): Configuration options for the analytics query.
- **Return value**: `Task<ApiResponse<AnalyticsDashboardResponse>>` containing a dashboard response with cold key data.
- **Exceptions**: Throws `ArgumentNullException` if `connection` is null.

### `GetPoorEfficiencyKeysAsync`

Identifies keys with suboptimal memory-to-usage ratios, such as large keys accessed infrequently. Useful for optimizing memory allocation and reducing cache bloat.

- **Parameters**:
  - `connection` (IConnectionMultiplexer): The Redis connection multiplexer.
  - `threshold` (double, optional): Minimum efficiency ratio (usage/memory) to exclude from results. Defaults to 0.1.
  - `options` (AnalyticsOptions, optional): Configuration options for the analytics query.
- **Return value**: `Task<ApiResponse<AnalyticsDashboardResponse>>` containing a dashboard response with poorly efficient keys.
- **Exceptions**: Throws `ArgumentNullException` if `connection` is null. Throws `ArgumentOutOfRangeException` if `threshold` is outside the valid range [0, 1].

### `GetFormattedKeyStatsAsync`

Generates a human-readable summary of key statistics, including size, access frequency, and efficiency metrics. The output is formatted for logging or display purposes.

- **Parameters**:
  - `connection` (IConnectionMultiplexer): The Redis connection multiplexer.
  - `key` (string): The key to analyze.
- **Return value**: `Task<ApiResponse<string>>` containing the formatted statistics as a string.
- **Exceptions**: Throws `ArgumentNullException` if `connection` or `key` is null. Throws `ArgumentException` if `key` is empty or whitespace.

### `ResetWithConfirmationAsync`

Resets the analytics state for the Redis cache and returns a confirmation message. This operation is typically used to clear historical metrics before a new monitoring cycle.

- **Parameters**:
  - `connection` (IConnectionMultiplexer): The Redis connection multiplexer.
  - `confirmationToken` (string): A token or identifier to authorize the reset operation.
- **Return value**: `Task<ApiResponse<string>>` containing a confirmation message upon successful reset.
- **Exceptions**: Throws `ArgumentNullException` if `connection` or `confirmationToken` is null. Throws `InvalidOperationException` if the confirmation token is invalid or unauthorized.

### `GetEfficiencySummaryAsync`

Provides a high-level summary of cache efficiency, including average hit rate, memory usage, and key distribution. Useful for monitoring overall cache health.

- **Parameters**:
  - `connection` (IConnectionMultiplexer): The Redis connection multiplexer.
  - `options` (AnalyticsOptions, optional): Configuration options for the analytics query.
- **Return value**: `Task<ApiResponse<string>>` containing a formatted efficiency summary.
- **Exceptions**: Throws `ArgumentNullException` if `connection` is null.

## Usage
