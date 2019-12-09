# CacheWarmerWorker

The `CacheWarmerWorker` is a dedicated background service component designed to proactively populate the Redis cache with frequently accessed data before it is requested by end users. By executing pre-defined loading logic asynchronously, this worker minimizes cache misses during peak traffic periods and reduces latency for initial requests, ensuring that the distributed cache remains warm and optimized for performance.

## API

### `public CacheWarmerWorker`
Initializes a new instance of the `CacheWarmerWorker` class. This constructor sets up the internal state required for background execution but does not start the warming process immediately. No parameters are required for instantiation.

### `public void Start`
Initiates the background cache warming operation. Once called, the worker begins executing its configured loading strategy on a separate thread or task context.
*   **Parameters**: None.
*   **Return Value**: None.
*   **Exceptions**: Throws an `InvalidOperationException` if the worker has already been started or if the instance has been disposed.

### `public void Stop`
Signals the worker to cease its cache warming operations gracefully. This method requests cancellation of the current warming cycle and blocks until the active operation completes or times out, ensuring no partial data writes occur.
*   **Parameters**: None.
*   **Return Value**: None.
*   **Exceptions**: Throws an `InvalidOperationException` if the worker is not currently running.

### `public void Dispose`
Releases all unmanaged resources associated with the `CacheWarmerWorker` and ensures that any running background threads are terminated. If the worker is currently running, this method implicitly stops it before cleaning up resources.
*   **Parameters**: None.
*   **Return Value**: None.
*   **Exceptions**: None. This method is safe to call multiple times.

## Usage

### Example 1: Manual Lifecycle Management
This example demonstrates instantiating the worker, starting the warming process, and explicitly stopping it during application shutdown.

```csharp
using System;
using RedisCachePatterns;

public class ApplicationHost
{
    private readonly CacheWarmerWorker _warmer;

    public ApplicationHost()
    {
        _warmer = new CacheWarmerWorker();
    }

    public void Run()
    {
        Console.WriteLine("Starting cache warming...");
        _warmer.Start();

        // Simulate application runtime
        Console.WriteLine("Application is running with warmed cache.");
        
        // Application logic here...
    }

    public void Shutdown()
    {
        Console.WriteLine("Stopping cache warmer...");
        _warmer.Stop();
        _warmer.Dispose();
    }
}
```

### Example 2: Integration with a Using Statement
This example utilizes the `Dispose` pattern via a `using` block to ensure resources are cleaned up automatically, even if an exception occurs during the application's lifecycle.

```csharp
using System;
using RedisCachePatterns;

public class StartupService
{
    public void Execute()
    {
        using (var warmer = new CacheWarmerWorker())
        {
            try
            {
                warmer.Start();
                
                // Perform other startup tasks while cache warms in background
                InitializeWebServer();
                
                // Keep the service alive until a stop signal is received
                WaitForShutdownSignal();
            }
            finally
            {
                // Stop is called implicitly by Dispose if not called explicitly,
                // but explicit stopping allows for graceful completion logic.
                warmer.Stop(); 
            }
        }
    }

    private void InitializeWebServer() { /* ... */ }
    private void WaitForShutdownSignal() { /* ... */ }
}
```

## Notes

*   **Thread Safety**: The `Start` and `Stop` methods are not thread-safe relative to each other. Calling `Start` from multiple threads simultaneously may result in an `InvalidOperationException`. It is recommended to manage the lifecycle of this class from a single control thread or via synchronization primitives.
*   **State Transitions**: The worker enforces a strict state machine: `Created` → `Running` → `Stopped` → `Disposed`. Attempting to `Start` an instance that is already `Running` or has been `Disposed` will throw an exception. Similarly, calling `Stop` on an instance that was never started or is already stopped will throw an exception.
*   **Disposal Behavior**: Calling `Dispose` on a running instance will forcibly terminate the background operation. If graceful completion of the current warming cycle is required, `Stop` must be called explicitly before `Dispose`.
*   **Resource Leaks**: Failure to call `Dispose` may result in lingering background threads or unreleased network connections to the Redis server. Always ensure `Dispose` is called, preferably via a `using` statement or within a finalizer pattern.
