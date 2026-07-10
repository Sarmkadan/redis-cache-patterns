# RedisClusterConnection

The `RedisClusterConnection` class serves as a high-level wrapper for managing connections to a Redis Cluster, building upon the `StackExchange.Redis` library. It abstracts the complexities of cluster topology discovery, master-slave node management, and hash slot routing, providing a simplified interface for performing both general database operations and cluster-specific administrative tasks.

## API

- **`IConnectionMultiplexer GetConnection()`**
  Retrieves the underlying `StackExchange.Redis.IConnectionMultiplexer` instance used for communicating with the Redis Cluster.

- **`IDatabase GetDatabase()`**
  Provides access to the default `IDatabase` instance for performing standard key-value operations.

- **`async Task<bool> IsConnectedAsync()`**
  Checks the connectivity state of the cluster. Returns `true` if the connection is active and stable; otherwise, returns `false`.

- **`async Task DisconnectAsync()`**
  Closes the current connection to the Redis Cluster and performs necessary cleanup.

- **`string GetConnectionString()`**
  Returns the configuration string used to initialize the current connection.

- **`async Task<IReadOnlyList<Domain.ClusterNode>> GetClusterNodesAsync()`**
  Fetches a read-only list of all currently known nodes within the cluster topology.

- **`async Task<IReadOnlyList<Domain.ClusterNode>> GetMasterNodesAsync()`**
  Fetches a read-only list of nodes currently designated as masters in the cluster.

- **`int GetSlotForKey()`**
  Calculates and returns the hash slot associated with a key based on the cluster's current partitioning logic.

- **`async Task<IServer> GetNodeForKeyAsync()`**
  Identifies and returns the `IServer` instance responsible for the node holding the specified hash slot.

- **`Task ForEachMasterAsync()`**
  Executes a provided asynchronous action against every master node within the cluster.

- **`async Task<ClusterInfo> GetClusterInfoAsync()`**
  Retrieves detailed configuration and state information about the Redis Cluster.

- **`async ValueTask DisposeAsync()`**
  Asynchronously releases all resources associated with the connection, ensuring a clean shutdown of the underlying multiplexer.

## Usage

### Basic Database Operations and Cluster Information
```csharp
var clusterConnection = new RedisClusterConnection(configString);

// Perform standard database operations
var db = clusterConnection.GetDatabase();
await db.StringSetAsync("cache_key", "value");

// Retrieve cluster state
var info = await clusterConnection.GetClusterInfoAsync();
Console.WriteLine($"Cluster state: {info.State}");
```

### Administrative Task on Master Nodes
```csharp
var clusterConnection = new RedisClusterConnection(configString);

// Perform an administrative task on all master nodes
await clusterConnection.ForEachMasterAsync(async (server) =>
{
    // Execute command on each master server
    await server.SaveAsync(SaveType.BackgroundSave);
});
```

## Notes

- **Thread Safety**: This class is designed to be thread-safe. The underlying `IConnectionMultiplexer` is intended to be shared and reused across multiple threads within an application.
- **Connection Management**: While the class manages the lifecycle of the connection, it is recommended to maintain a single instance of `RedisClusterConnection` per cluster for the lifetime of the application to avoid unnecessary connection churn.
- **Topology Changes**: The cluster topology can change dynamically (e.g., node failures, rebalancing). Operations relying on specific node mappings (like `GetNodeForKeyAsync`) may need to be retried or refreshed if the cluster undergoes significant topology reconfiguration.
- **Disposal**: Always ensure `DisposeAsync` is called when the connection is no longer needed, typically during application shutdown, to properly release socket resources.
