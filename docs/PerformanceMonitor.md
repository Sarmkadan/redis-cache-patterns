# PerformanceMonitor

The `PerformanceMonitor` class provides a mechanism for tracking execution times and statistical metrics of specific operations within the `redis-cache-patterns` project. It supports both manual recording of operation durations and automatic timing via an IDisposable scope, allowing developers to aggregate data such as total count, minimum, maximum, and average execution times for performance analysis and diagnostics.

## API

### Constructors

#### `public PerformanceMonitor()`
Initializes a new instance of the `PerformanceMonitor` class.
*   **Parameters**: None.
*   **Return Value**: A new `PerformanceMonitor` instance.
*   **Exceptions**: None.

### Properties

#### `public string OperationName`
Gets the name associated with this monitor instance, used to identify the operation in logs or aggregated reports.
*   **Return Value**: The name of the operation.
*   **Exceptions**: None.

#### `public int Count`
Gets the total number of operations recorded by this monitor.
*   **Return Value**: The count of recorded operations.
*   **Exceptions**: None.

#### `public long TotalMs`
Gets the cumulative duration in milliseconds of all recorded operations.
*   **Return Value**: The sum of all operation durations in milliseconds.
*   **Exceptions**: None.

#### `public long MinMs`
Gets the shortest duration in milliseconds among all recorded operations.
*   **Return Value**: The minimum recorded duration in milliseconds. Returns 0 if no operations have been recorded.
*   **Exceptions**: None.

#### `public long MaxMs`
Gets the longest duration in milliseconds among all recorded operations.
*   **Return Value**: The maximum recorded duration in milliseconds. Returns 0 if no operations have been recorded.
*   **Exceptions**: None.

#### `public OperationTimer OperationTimer`
Gets an `OperationTimer` instance associated with this monitor, typically used to start timing an operation manually if not using the `MeasureOperation` scope.
*   **Return Value**: An `OperationTimer` instance.
*   **Exceptions**: None.

### Methods

#### `public IDisposable MeasureOperation()`
Starts a timed scope for an operation. When the returned `IDisposable` is disposed (e.g., at the end of a `using` block), the elapsed time is automatically recorded.
*   **Parameters**: None.
*   **Return Value**: An `IDisposable` object that, when disposed, records the operation metrics.
*   **Exceptions**: None.

#### `public void RecordOperation(long elapsedMs)`
Manually records an operation with the specified duration.
*   **Parameters**:
    *   `elapsedMs`: The duration of the operation in milliseconds.
*   **Return Value**: None.
*   **Exceptions**: May throw `ArgumentOutOfRangeException` if `elapsedMs` is negative (depending on internal validation logic implied by metric aggregation).

#### `public OperationMetrics? GetMetrics()`
Retrieves the current aggregated metrics for this specific monitor instance.
*   **Parameters**: None.
*   **Return Value**: An `OperationMetrics` object containing current statistics, or `null` if no operations have been recorded.
*   **Exceptions**: None.

#### `public IEnumerable<OperationMetrics> GetAllMetrics()`
Retrieves a collection of metrics. In the context of a single monitor instance, this typically returns a sequence containing the current metrics if available.
*   **Parameters**: None.
*   **Return Value**: An enumerable collection of `OperationMetrics`.
*   **Exceptions**: None.

#### `public void ResetMetrics()`
Clears all accumulated statistical data (Count, TotalMs, MinMs, MaxMs) without affecting the `OperationName`.
*   **Parameters**: None.
*   **Return Value**: None.
*   **Exceptions**: None.

#### `public void ResetOperation()`
Resets the state of the current operation tracking, potentially clearing active timers or temporary states associated with an in-flight operation.
*   **Parameters**: None.
*   **Return Value**: None.
*   **Exceptions**: None.

#### `public void Dispose()`
Releases resources used by the `PerformanceMonitor`. If an `OperationTimer` or active `MeasureOperation` scope is pending, this may finalize or discard it.
*   **Parameters**: None.
*   **Return Value**: None.
*   **Exceptions**: None.

#### `public override string ToString()`
Returns a string representation of the current metrics, typically including the operation name and key statistics.
*   **Parameters**: None.
*   **Return Value**: A formatted string summarizing the performance data.
*   **Exceptions**: None.

## Usage

### Example 1: Automatic Timing with Scope
Using the `MeasureOperation` method within a `using` statement ensures that the operation duration is recorded automatically when the scope exits, even if an exception occurs.

```csharp
using (var monitor = new PerformanceMonitor())
{
    // The operation name is typically set via constructor or property in real usage
    // assuming a hypothetical setter or constructor overload for brevity here
    // or relying on default naming if applicable.
    
    using (monitor.MeasureOperation())
    {
        // Simulate work being done
        Thread.Sleep(150);
    }

    // Metrics are now updated
    Console.WriteLine(monitor.ToString());
}
```

### Example 2: Manual Recording and Aggregation
Manually calculating elapsed time and recording it allows for more granular control, such as excluding specific setup/teardown code from the metrics.

```csharp
var monitor = new PerformanceMonitor();
var stopwatch = System.Diagnostics.Stopwatch.StartNew();

// Perform complex logic
ExecuteCacheRetrieval();

stopwatch.Stop();
monitor.RecordOperation(stopwatch.ElapsedMilliseconds);

// Retrieve specific metrics
var metrics = monitor.GetMetrics();
if (metrics.HasValue)
{
    Console.WriteLine($"Operations: {metrics.Value.Count}");
    Console.WriteLine($"Average MS: {(double)metrics.Value.TotalMs / metrics.Value.Count}");
}

monitor.Dispose();
```

## Notes

*   **Thread Safety**: The provided signatures do not explicitly indicate internal synchronization mechanisms (e.g., locks). If `PerformanceMonitor` instances are shared across multiple threads calling `RecordOperation`, `ResetMetrics`, or accessing properties like `Count` and `TotalMs` concurrently, external synchronization is required to prevent race conditions and data corruption.
*   **Zero-Count State**: When no operations have been recorded, `MinMs` and `MaxMs` typically return 0. Consumers should check the `Count` property before calculating averages to avoid division by zero errors.
*   **Disposal Behavior**: Calling `Dispose` on the monitor itself cleans up resources. If an `IDisposable` returned by `MeasureOperation` is not disposed before the parent monitor is disposed, the pending operation may not be recorded, or the timer may be invalidated.
*   **Reset Semantics**: `ResetMetrics` clears historical data but preserves the identity (`OperationName`) of the monitor. `ResetOperation` appears intended to cancel or reset a currently active timing session rather than clearing historical aggregates.
