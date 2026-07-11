# ClusterConfiguration

`ClusterConfiguration` encapsulates all connection and behavioral settings required to interact with a Redis Cluster. It defines the cluster topology endpoints, timeouts, read routing preferences, slot discovery parameters, and Redlock-specific tuning values. The type is designed to be populated directly or via environment variables through its static `FromEnvironment` factory.

## API

### `Endpoints`
`public required string[] Endpoints`

The set of initial cluster seed endpoints. Each string must be a valid `host:port` pair (e.g., `"node1:6379"`). At least one endpoint is required; the client uses these seeds to discover the full cluster topology. This property is mandatory and must be set before the configuration is consumed.

### `ConnectTimeoutMs`
`public int ConnectTimeoutMs`

Maximum time in milliseconds allowed for establishing a socket connection to any individual cluster node. A value of zero or negative is treated as an infinite timeout. Exceeding this limit causes a connection attempt to fail with a timeout exception.

### `SyncTimeoutMs`
`public int SyncTimeoutMs`

Maximum time in milliseconds permitted for synchronous command execution, including the round-trip network latency and server processing. When exceeded, the command is cancelled and a timeout exception is thrown. A zero or negative value disables synchronous timeouts.

### `ReadPreference`
`public ClusterReadPreference ReadPreference`

Determines which nodes are eligible to serve read commands. Typical values route reads exclusively to primary nodes, exclusively to replicas, or prefer replicas with fallback to primaries. This setting is ignored unless `AllowReplicaReads` is `true`.

### `AllowReplicaReads`
`public bool AllowReplicaReads`

Master switch that enables or disables routing of read operations to replica nodes. When `false`, all reads are sent to primary nodes regardless of `ReadPreference`. Set to `true` to activate replica read routing according to the configured preference.

### `SlotScanPageSize`
`public int SlotScanPageSize`

Number of hash slots requested per iteration when scanning the cluster’s slot-to-node mapping. Smaller values reduce per-call payload size but increase the number of round-trips; larger values reduce round-trips at the cost of larger individual responses. Must be a positive integer.

### `RedlockRetryCount`
`public int RedlockRetryCount`

Maximum number of times the Redlock algorithm retries acquiring a distributed lock across all participating nodes before giving up. Each retry cycle attempts to lock all nodes again. A value of zero means no retries are performed after the first failed attempt.

### `RedlockRetryDelay`
`public TimeSpan RedlockRetryDelay`

Fixed delay applied between consecutive Redlock acquisition attempts. This delay is inserted after a failed attempt and before the next retry cycle begins. A zero or negative delay causes retries to execute immediately back-to-back.

### `RedlockClockDrift`
`public TimeSpan RedlockClockDrift`

Safety margin subtracted from the lock’s time-to-live to compensate for clock skew across cluster nodes. The effective lock duration is reduced by this amount to prevent premature lock expiry due to drifting clocks. Should be set based on observed maximum drift in the deployment environment.

### `FailoverTimeout`
`public TimeSpan FailoverTimeout`

Maximum duration the client waits for the cluster to complete an ongoing failover before timing out operations that are blocked on topology changes. During this window, commands targeting the affected shard may be queued or rejected depending on internal state.

### `ReconnectOnFailover`
`public bool ReconnectOnFailover`

Controls whether the client automatically tears down and rebuilds its internal connections when a failover is detected. When `true`, stale connections to demoted primaries are discarded and fresh connections to the new topology are established. When `false`, the client relies on existing connections and topology refresh alone.

### `FromEnvironment`
`public static ClusterConfiguration FromEnvironment`

Static factory method that constructs a `ClusterConfiguration` instance by reading well-known environment variables. It expects variables such as `REDIS_CLUSTER_ENDPOINTS` (comma-separated `host:port` pairs), `REDIS_CONNECT_TIMEOUT_MS`, `REDIS_SYNC_TIMEOUT_MS`, and others corresponding to each property. Missing variables fall back to documented defaults. Throws `FormatException` when a numeric or time-span variable cannot be parsed. Returns a fully populated, ready-to-use configuration object.

### `ToString`
`public override string ToString()`

Returns a string representation of the configuration, including all endpoint addresses and key timeout/preference values. Sensitive data such as passwords are never included. The format is intended for diagnostics and logging.

## Usage

### Basic manual construction
```csharp
var config = new ClusterConfiguration
{
    Endpoints = new[] { "10.0.1.10:6379", "10.0.1.11:6379", "10.0.1.12:6379" },
    ConnectTimeoutMs = 5000,
    SyncTimeoutMs = 2000,
    AllowReplicaReads = true,
    ReadPreference = ClusterReadPreference.PreferReplicas,
    SlotScanPageSize = 512,
    RedlockRetryCount = 3,
    RedlockRetryDelay = TimeSpan.FromMilliseconds(200),
    RedlockClockDrift = TimeSpan.FromMilliseconds(50),
    FailoverTimeout = TimeSpan.FromSeconds(30),
    ReconnectOnFailover = true
};

var clusterClient = new RedisClusterClient(config);
```

### Loading from environment
```csharp
// Environment variables expected:
//   REDIS_CLUSTER_ENDPOINTS=10.0.1.10:6379,10.0.1.11:6379
//   REDIS_CONNECT_TIMEOUT_MS=5000
//   REDIS_SYNC_TIMEOUT_MS=2000
//   REDIS_ALLOW_REPLICA_READS=true
//   REDIS_READ_PREFERENCE=PreferReplicas
//   REDIS_SLOT_SCAN_PAGE_SIZE=512
//   REDIS_REDLOCK_RETRY_COUNT=3
//   REDIS_REDLOCK_RETRY_DELAY_MS=200
//   REDIS_REDLOCK_CLOCK_DRIFT_MS=50
//   REDIS_FAILOVER_TIMEOUT_SECONDS=30
//   REDIS_RECONNECT_ON_FAILOVER=true

var config = ClusterConfiguration.FromEnvironment();
var clusterClient = new RedisClusterClient(config);
```

## Notes

- `Endpoints` is marked `required`; constructing the object without it triggers a compile-time error when using object initializers. At runtime, validation occurs on first use by the cluster client.
- `ConnectTimeoutMs` and `SyncTimeoutMs` apply per-node and per-command respectively. Setting both to zero effectively disables all timeout enforcement, which can cause indefinite hangs during network partitions.
- `AllowReplicaReads` acts as a global gate. Even if `ReadPreference` is set to a replica-aware value, reads will not reach replicas unless this flag is `true`. This design prevents accidental stale reads from misconfigured preferences.
- `SlotScanPageSize` must be positive. Values larger than the total number of hash slots (16384) are clamped internally by the cluster client but do not throw during configuration construction.
- Redlock properties (`RedlockRetryCount`, `RedlockRetryDelay`, `RedlockClockDrift`) are only relevant when the distributed lock pattern is used. They have no effect on standard key-value operations.
- `FailoverTimeout` and `ReconnectOnFailover` interact: a short timeout combined with `ReconnectOnFailover = false` may cause commands to fail quickly during failover rather than waiting for topology stabilization.
- `FromEnvironment` performs immediate parsing. Any unparseable value throws `FormatException` at construction time, not lazily during cluster access. The method is deterministic and does not cache or modify environment state.
- This type is not thread-safe for mutation. Once a configuration is passed to a cluster client, its properties should be treated as immutable. Concurrent reads from multiple threads are safe; concurrent writes are not synchronized and may produce inconsistent behavior.
