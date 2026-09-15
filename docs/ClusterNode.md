# Cluster Node Types

Documentation for the `Domain` namespace cluster node types, which represent Redis Cluster topology elements including slot ranges, individual nodes, node roles, and cluster-wide information.

## Types

### `ClusterSlotRange`
A contiguous range of Redis hash slots assigned to a single cluster node. Slot indices follow the Redis Cluster specification (0–16 383).

| Property | Type | Description |
|----------|------|-------------|
| `Start` | `int` (required) | First hash slot in this range (inclusive). |
| `End` | `int` (required) | Last hash slot in this range (inclusive). |
| `Count` | `int` | Number of slots covered by this range. Calculated as `End - Start + 1`. |
| `Contains(int slot)` | `bool` | Returns <c>true</c> when <paramref name="slot"/> falls within this range. |
| `ToString()` | `string` | Returns a string representation in the format `"[Start–End]"`. |

### `ClusterNode`
Represents a single node in a Redis Cluster with its slot assignments and replication role. A node may own multiple non-contiguous <see cref="ClusterSlotRange"/> entries (e.g., after partial resharding).

| Property | Type | Description |
|----------|------|-------------|
| `NodeId` | `string` (required) | Unique 40-character hex identifier assigned by the cluster. |
| `EndPoint` | `string` (required) | Network address of this node in <c>host:port</c> form. |
| `Role` | `ClusterNodeRole` (required) | Whether this node is a master that accepts writes, or a read-only replica. |
| `SlotRanges` | `IReadOnlyList<ClusterSlotRange>` (required) | All hash-slot ranges currently owned by this node. |
| `IsConnected` | `bool` | Whether the multiplexer currently considers this node reachable. Derived from the absence of the <c>noaddr</c> flag in <c>CLUSTER NODES</c> output. |
| `PrimaryNodeId` | `string?` | Node ID of the primary when <see cref="Role"/> is <see cref="ClusterNodeRole.Replica"/>; <c>null</c> for master nodes. |
| `TotalSlotCount` | `int` | Total number of hash slots this node owns, summed across all <see cref="SlotRanges"/>. |
| `IsMaster` | `bool` | Returns <c>true</c> when this node is a master. |
| `OwnsSlot(int slot)` | `bool` | Returns <c>true</c> when <paramref name="slot"/> falls within any of this node's <see cref="SlotRanges"/>.
| `ToString()` | `string` | Returns a string representation in the format `"ClusterNode[{NodeId[..8]}… {EndPoint} {Role} slots={TotalSlotCount}]"`.

### `ClusterNodeRole`
Role of a node within the Redis Cluster topology.

| Member | Description |
|--------|-------------|
| `Master` | Node accepts writes and owns one or more slot ranges. |
| `Replica` | Node replicates a master; can serve reads when replica reads are enabled. |
| `Unknown` | Role could not be determined — typically during a handshake or failover election. |

### `ClusterInfo`
Point-in-time snapshot of the Redis Cluster topology, aggregated from <c>CLUSTER NODES</c> output.

| Property | Type | Description |
|----------|------|-------------|
| `TotalNodes` | `int` (required) | Total node count, including replicas. |
| `MasterCount` | `int` (required) | Number of master nodes that own at least one slot range. |
| `ReplicaCount` | `int` (required) | Number of replica nodes. |
| `TotalSlots` | `int` (required) | Total possible hash slots per the Redis Cluster specification. This value is always 16 384. |
| `CoveredSlots` | `int` (required) | Number of hash slots currently covered by a connected master. |
| `IsHealthy` | `bool` (required) | <c>true</c> when all 16 384 slots are covered by a connected master — indicating the cluster is fully operational with no slot gaps. |
| `CapturedAt` | `DateTime` (required) | UTC timestamp when this snapshot was captured. |
| `SlotCoverage` | `double` | Percentage of total slots that are currently covered (0–100). Calculated as `(double)CoveredSlots / TotalSlots * 100d`. |
| `ToString()` | `string` | Returns a string representation in the format `"ClusterInfo[nodes={TotalNodes} masters={MasterCount} slots={CoveredSlots}/{TotalSlots} ({SlotCoverage:F1}%) healthy={IsHealthy}]"`.

## Usage

### Creating a cluster slot range
```csharp
var slotRange = new ClusterSlotRange
{
    Start = 0,
    End = 5460
};

// Check if a slot is within the range
bool containsSlot = slotRange.Contains(2730); // Returns true
int slotCount = slotRange.Count; // Returns 5461
```

### Creating a cluster node
```csharp
var clusterNode = new ClusterNode
{
    NodeId = "a1b2c3d4e5f6789012345678901234567890abcd",
    EndPoint = "10.0.0.1:6379",
    Role = ClusterNodeRole.Master,
    SlotRanges = new List<ClusterSlotRange>
    {
        new ClusterSlotRange { Start = 0, End = 5460 },
        new ClusterSlotRange { Start = 10923, End = 16383 }
    },
    IsConnected = true,
    PrimaryNodeId = null
};

// Check node properties
bool isMaster = clusterNode.IsMaster; // Returns true
int totalSlots = clusterNode.TotalSlotCount; // Returns 10923
bool ownsSlot = clusterNode.OwnsSlot(8192); // Returns true
```

### Creating cluster info
```csharp
var clusterInfo = new ClusterInfo
{
    TotalNodes = 6,
    MasterCount = 3,
    ReplicaCount = 3,
    TotalSlots = 16384,
    CoveredSlots = 16384,
    IsHealthy = true,
    CapturedAt = DateTime.UtcNow
};

double coverage = clusterInfo.SlotCoverage; // Returns 100.0
```

## Notes

- All record types use required init-only properties, making them effectively immutable after creation.
- The `ClusterSlotRange.Count` property is calculated and does not require explicit setting.
- `ClusterNode.TotalSlotCount` sums the counts of all slot ranges owned by the node.
- `ClusterNode.IsMaster` provides a convenient way to check the node role without comparing to the enum directly.
- `ClusterNode.OwnsSlot` checks if the node is responsible for a specific hash slot across all its slot ranges.
- `ClusterInfo.SlotCoverage` provides a percentage value for monitoring cluster health.
- The `ToString()` methods provide human-readable representations suitable for logging and debugging.
- These types are designed to work with the output of the Redis `CLUSTER NODES` command.