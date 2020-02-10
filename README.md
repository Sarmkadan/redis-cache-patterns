// existing content ...

## RedisClusterConnection

The `RedisClusterConnection` class provides a thread-safe connection to a Redis Cluster, 
enabling cluster-aware operations such as hash-slot computation, topology discovery, 
and fan-out across master nodes.

### Usage Example
```csharp
var clusterConfig = new ClusterConfiguration
{
    Endpoints = new[] { "localhost:6379", "localhost:6380", "localhost:6381" }
};

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<RedisClusterConnection>();

var clusterConnection = new RedisClusterConnection(clusterConfig, logger);

// Check connection status
var isConnected = await clusterConnection.IsConnectedAsync();
Console.WriteLine($"Is connected: {isConnected}");

// Get cluster nodes
var nodes = await clusterConnection.GetClusterNodesAsync();
foreach (var node in nodes)
{
    Console.WriteLine($"Node: {node.EndPoint}, Role: {node.Role}");
}

// Get master nodes
var masterNodes = await clusterConnection.GetMasterNodesAsync();
Console.WriteLine($"Master nodes count: {masterNodes.Count}");

// Get slot for key
var key = "my_key";
var slot = clusterConnection.GetSlotForKey(key);
Console.WriteLine($"Slot for key '{key}': {slot}");

// Get node for key
var nodeForKey = await clusterConnection.GetNodeForKeyAsync(key);
Console.WriteLine($"Node for key '{key}': {nodeForKey.EndPoint}");

// Perform action on each master node
await clusterConnection.ForEachMasterAsync(async server =>
{
    await server.PingAsync();
    Console.WriteLine($"Pinged master node: {server.EndPoint}");
});

// Get cluster info
var clusterInfo = await clusterConnection.GetClusterInfoAsync();
Console.WriteLine($"Cluster info: {clusterInfo}");

// Dispose connection
await clusterConnection.DisposeAsync();
```