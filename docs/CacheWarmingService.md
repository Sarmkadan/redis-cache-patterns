# CacheWarmingService

The `CacheWarmingService` is a core component within the `redis-cache-patterns` project designed to orchestrate the pre-loading of data into the cache layer before it is requested by end-users. It manages the execution lifecycle of various caching strategies, tracking metrics such as execution duration, success rates, and specific errors encountered during the warming process. By aggregating results from multiple strategies, it provides a comprehensive report on the health and completeness of the cache initialization phase.

## API

### Constructors

#### `public CacheWarmingService()`
Initializes a new instance of the `CacheWarmingService` class. This constructor sets up the internal collections required to track strategies and execution results.

### Methods

#### `public CacheWarmingService AddStrategy(...)`
Registers a new caching strategy to be executed during the warming process.
*   **Purpose**: Adds a strategy implementation to the service's execution queue.
*   **Parameters**: Accepts a strategy instance (specific type inferred from context, likely implementing a strategy interface).
*   **Return Value**: Returns the current `CacheWarmingService` instance to allow for fluent method chaining.
*   **Throws**: May throw an exception if the provided strategy is null or invalid, though specific exception types depend on internal validation logic.

#### `public async Task<CacheWarmingResult> WarmAsync()`
Executes all registered strategies asynchronously and aggregates the results.
*   **Purpose**: Initiates the cache warming process, running each added strategy and compiling statistics.
*   **Parameters**: None.
*   **Return Value**: Returns a `Task<CacheWarmingResult>` containing the final outcome, including counts of successful/failed items and any error messages.
*   **Throws**: May throw exceptions if a critical failure occurs in the underlying infrastructure (e.g., Redis connection loss) that prevents the orchestration itself from completing.

#### `public abstract Task<int> ExecuteAsync()`
Defines the contract for executing a specific warming operation within a derived class or strategy context.
*   **Purpose**: Serves as the base definition for the actual warming logic implementation.
*   **Parameters**: None (implementation details vary in derived classes).
*   **Return Value**: Returns a `Task<int>` representing the number of items successfully warmed by this specific execution unit.
*   **Throws**: Implementations may throw exceptions related to data retrieval or cache insertion failures.

#### `public override async Task<int> ExecuteAsync()`
Provides the concrete implementation of the execution logic for the service or a specific strategy override.
*   **Purpose**: Executes the warming logic defined for this specific instance, overriding the abstract base behavior.
*   **Parameters**: None.
*   **Return Value**: Returns a `Task<int>` indicating the count of items processed successfully.
*   **Throws**: Propagates exceptions occurring during the asynchronous execution of the warming tasks.

#### `public override string ToString()`
Returns a string representation of the current `CacheWarmingService` instance.
*   **Purpose**: Provides a human-readable summary of the service state, typically including the `Name` and key execution metrics.
*   **Parameters**: None.
*   **Return Value**: A `string` representing the object.
*   **Throws**: None.

### Properties

#### `public string Name`
Gets or sets the descriptive name of the cache warming service instance.
*   **Purpose**: Identifies the specific warming job, useful for logging and monitoring.

#### `public DateTime StartedAt`
Gets the timestamp indicating when the `WarmAsync` operation was initiated.
*   **Purpose**: Records the start time for duration calculations and auditing.

#### `public DateTime? CompletedAt`
Gets the timestamp indicating when the `WarmAsync` operation finished.
*   **Purpose**: Records the completion time. This value is `null` if the operation is still in progress or has not started.

#### `public long DurationMs`
Gets the total elapsed time of the warming operation in milliseconds.
*   **Purpose**: Provides a performance metric for the entire warming cycle. Calculated based on `StartedAt` and `CompletedAt`.

#### `public int TotalItemsWarmed`
Gets the cumulative count of cache items successfully populated during the operation.
*   **Purpose**: Indicates the volume of data loaded into the cache.

#### `public int SuccessfulStrategies`
Gets the number of strategies that completed execution without throwing an exception.
*   **Purpose**: Metrics for evaluating the reliability of the configured strategies.

#### `public int FailedStrategies`
Gets the number of strategies that encountered an error during execution.
*   **Purpose**: Metrics for identifying problematic strategies.

#### `public List<string> Errors`
Gets a collection of error messages captured during the execution of strategies.
*   **Purpose**: Provides detailed diagnostic information for failed strategies.

### Nested Types / Related

#### `public PredefinedKeyStrategy`
Represents a specific implementation or type of strategy that targets predefined keys for warming.
*   **Purpose**: Likely a concrete class or enum used in conjunction with `AddStrategy` to define standard warming patterns.

## Usage

### Example 1: Basic Initialization and Execution
This example demonstrates how to instantiate the service, register a strategy, and execute the warming process asynchronously.

```csharp
using System;
using System.Threading.Tasks;
using RedisCachePatterns;

public class Program
{
    public static async Task Main()
    {
        // Initialize the service
        var warmingService = new CacheWarmingService
        {
            Name = "ProductCatalogWarmer"
        };

        // Add a predefined strategy for product keys
        warmingService.AddStrategy(new PredefinedKeyStrategy("products:*"));

        try
        {
            // Execute the warming process
            var result = await warmingService.WarmAsync();

            Console.WriteLine($"Warming completed in {result.DurationMs}ms");
            Console.WriteLine($"Items warmed: {result.TotalItemsWarmed}");
            Console.WriteLine($"Failed strategies: {result.FailedStrategies}");

            if (result.Errors.Count > 0)
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"Error: {error}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Critical failure during warming: {ex.Message}");
        }
    }
}
```

### Example 2: Chaining Strategies and Monitoring State
This example illustrates the fluent interface for adding multiple strategies and inspecting the service state properties after execution.

```csharp
using System;
using System.Threading.Tasks;
using RedisCachePatterns;

public class AdvancedWarmingScenario
{
    public async Task RunWarming()
    {
        var service = new CacheWarmingService()
            .AddStrategy(new PredefinedKeyStrategy("users:active"))
            .AddStrategy(new PredefinedKeyStrategy("sessions:recent"));

        // Record start time manually if needed, though StartedAt is handled internally
        var result = await service.WarmAsync();

        // Inspect detailed properties
        Console.WriteLine($"Service: {service.Name}");
        Console.WriteLine($"Started: {service.StartedAt:HH:mm:ss}");
        Console.WriteLine($"Completed: {service.CompletedAt?.ToString("HH:mm:ss") ?? "N/A"}");
        Console.WriteLine($"Duration: {service.DurationMs}ms");
        Console.WriteLine($"Success Count: {service.SuccessfulStrategies}");
        Console.WriteLine($"Failure Count: {service.FailedStrategies}");
        
        // Output summary via overridden ToString
        Console.WriteLine(service.ToString());
    }
}
```

## Notes

### Thread Safety
The `CacheWarmingService` exposes mutable properties (`Name`, `StartedAt`, `CompletedAt`, etc.) and a mutable list (`Errors`). While the `WarmAsync` method handles the asynchronous coordination of strategies, external modification of these properties during an active execution cycle may lead to race conditions or inconsistent state reporting. It is recommended to configure the service (e.g., setting `Name` and adding strategies) before calling `WarmAsync` and to treat the instance as read-only once execution has commenced.

### Edge Cases
*   **Empty Strategy List**: If `WarmAsync` is called without any strategies added via `AddStrategy`, the `TotalItemsWarmed` will be 0, and `SuccessfulStrategies` will be 0. The `DurationMs` will reflect only the overhead of the orchestration logic.
*   **Partial Failures**: If some strategies fail while others succeed, `WarmAsync` will not throw an exception by default; instead, it will populate the `Errors` list and increment `FailedStrategies`. The caller must inspect these properties to determine if the warming state is acceptable.
*   **Long-Running Tasks**: The `DurationMs` property is calculated upon completion. If the service is inspected while `CompletedAt` is null, `DurationMs` may return 0 or the elapsed time up to that point depending on internal implementation details; consumers should verify `CompletedAt` before relying on final duration metrics.
*   **Inheritance**: The class contains both `abstract` and `override` implementations of `ExecuteAsync`. When deriving from this class or using strategies that inherit from it, ensure the correct override is implemented to avoid `NotImplementedException` from the abstract base.
