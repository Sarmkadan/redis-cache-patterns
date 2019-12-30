# ModuleRegistration

A lightweight orchestrator that manages the lifecycle of background workers in a Redis-backed caching system. It provides controlled startup and shutdown of worker instances, supports generic worker types, and ensures clean disposal of resources.

## API

### `public ModuleRegistration()`

Initializes a new instance of the `ModuleRegistration` class. No background workers are started automatically; workers must be explicitly registered and started.

### `public void StartBackgroundWorkers()`

Starts all background workers that have been registered via `StartWorker<T>`. This method is idempotent—subsequent calls have no effect if workers are already running.

_Parameters:_ None
_Returns:_ `void`
_Throws:_ `InvalidOperationException` if workers are already running or if the module is disposed.

### `public void StopBackgroundWorkers()`

Stops all active background workers gracefully. Pending tasks may complete before termination, depending on worker implementation. This method is safe to call multiple times.

_Parameters:_ None
_Returns:_ `void`
_Throws:_ `ObjectDisposedException` if the module has been disposed.

### `public void StartWorker<T>()`

Registers and starts a background worker of type `T`, where `T` implements a Redis-aware worker interface. The worker runs asynchronously and is managed by the module.

_Parameters:_
- `T` (generic type parameter): The worker type to instantiate and start.

_Returns:_ `void`
_Throws:_
- `ArgumentException` if `T` does not implement the required worker interface.
- `InvalidOperationException` if workers are already running or if the module is disposed.
- `InvalidOperationException` if a worker of type `T` is already registered.

### `public void Dispose()`

Releases all managed resources, stops all background workers, and prevents further worker registration or startup. This method is thread-safe and idempotent.

_Parameters:_ None
_Returns:_ `void`
_Throws:_ None

## Usage

### Example 1: Basic Worker Registration and Lifecycle
