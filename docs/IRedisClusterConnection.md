# IRedisClusterConnection

The `IRedisClusterConnection` interface defines the cluster-aware Redis connection contract in the `RedisCachePatterns.Infrastructure.Cache` namespace. It extends `IRedisConnection` with topology discovery, hash-slot routing, master-node fan-out, and cluster health information while retaining the standard connection and database operations.

Implementations are expected to use a cluster-capable `IConnectionMultiplexer`. Redis `MOVED` and `ASK` redirections are handled by StackExchange.Redis when the multiplexer is configured for cluster mode.

## Inherited Connection API

As an `IRedisConnection`, the interface also exposes the following members:

| Member | Description |
|--------|-------------|
| `IConnectionMultiplexer GetConnection()` | Returns the active Redis connection multiplexer. |
| `IDatabase GetDatabase(int databaseId = 0)` | Returns a Redis database instance. Redis Cluster implementations normally support database index `0` only. |
| `Task<bool> IsConnectedAsync()` | Checks whether the Redis connection is available. |
| `Task DisconnectAsync()` | Disconnects from Redis. |
| `string GetConnectionString()` | Returns the configured Redis connection string. |

## Cluster API

### `Task<IReadOnlyList<ClusterNode>> GetClusterNodesAsync()`

Returns a point-in-time view of all nodes visible in the cluster topology, including masters and replicas. The node information is derived from Redis `CLUSTER NODES` data.

### `Task<IReadOnlyList<ClusterNode>> GetMasterNodesAsync()`

Returns the master nodes that own at least one hash-slot range. This list is useful for operations that must run once on every shard, such as scans, administrative commands, or statistics collection.

### `int GetSlotForKey(string key)`

Computes the Redis Cluster hash slot for `key`. Slots range from `0` through `16383` and are calculated with the XMODEM CRC16 algorithm. If a key contains a non-empty hash tag, such as `{customer}` in `orders:{customer}:recent`, only the text inside the braces is hashed. Keys with the same hash tag therefore map to the same slot.

### `Task<IServer> GetNodeForKeyAsync(string key)`

Resolves the server currently responsible for the key's hash slot. Implementations throw `CacheConnectionException` when no master owns the calculated slot, which can occur while the cluster topology is incomplete or changing.

### `Task ForEachMasterAsync(Func<IServer, Task> action)`

Executes `action` against every master node concurrently. The returned task completes after all node operations complete and propagates failures from the fan-out operation.

### `Task<ClusterInfo> GetClusterInfoAsync()`

Builds a point-in-time `ClusterInfo` snapshot from the known topology. The snapshot reports node and role counts, covered and total hash slots, health status, slot coverage, and its capture time.

### `bool IsClusterMode { get; }`

Indicates whether the connection operates in Redis Cluster mode. Cluster implementations return `true`; callers can use `false` to select a standalone Redis fallback path when an alternative implementation supports both modes.

## Usage

### Route Work by Key

```csharp
public async Task LogKeyOwnerAsync(
    IRedisClusterConnection connection,
    string key,
    ILogger logger)
{
    var slot = connection.GetSlotForKey(key);
    var server = await connection.GetNodeForKeyAsync(key);

    logger.LogInformation(
        "Key {Key} maps to slot {Slot} on {Endpoint}",
        key,
        slot,
        server.EndPoint);
}
```

### Inspect Cluster Health

```csharp
public async Task<bool> HasFullSlotCoverageAsync(
    IRedisClusterConnection connection)
{
    if (!connection.IsClusterMode || !await connection.IsConnectedAsync())
        return false;

    var info = await connection.GetClusterInfoAsync();
    return info.IsHealthy;
}
```

### Run an Operation on Every Shard

```csharp
await clusterConnection.ForEachMasterAsync(async server =>
{
    var latency = await server.PingAsync();
    Console.WriteLine($"{server.EndPoint}: {latency.TotalMilliseconds:F1} ms");
});
```

## Notes

- **Topology snapshots:** Results from topology and health methods describe the cluster at the time they are collected. Failover or resharding can make previously returned node ownership stale.
- **Hash tags:** Use the same non-empty `{tag}` in related keys when an operation requires them to occupy one slot. Empty braces do not form a hash tag.
- **Fan-out cost:** `ForEachMasterAsync` runs once per master and should be used carefully for expensive commands on large clusters.
- **Error handling:** Callers should handle connection failures and topology changes. A key-owner lookup can fail temporarily when no master is known to own its slot.
- **Connection lifetime:** Implementations should generally be registered and reused as long-lived services because `IConnectionMultiplexer` is designed for sharing rather than per-operation creation.
