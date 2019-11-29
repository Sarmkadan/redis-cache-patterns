# ClusterSlotRange

Represents a contiguous range of hash slots within a Redis cluster, together with metadata about the node that owns the range and summary statistics for the cluster topology.

## API

### Start
**Type:** `int` (required)  
**Purpose:** Gets the inclusive lower bound of the slot range.  
**Remarks:** Must be non‑negative and less than or equal to `End`. The value is set during object initialization and cannot be changed thereafter.

### End
**Type:** `int` (required)  
**Purpose:** Gets the inclusive upper bound of the slot range.  
**Remarks:** Must be greater than or equal to `Start`. The value is set during object initialization and cannot be changed thereafter.

### Contains
**Type:** `bool`  
**Purpose:** Indicates whether the slot range is valid and non‑empty.  
**Remarks:** Returns `true` when `Start <= End`; otherwise returns `false`. This property does not accept parameters and never throws.

### ToString
**Type:** `string` (override)  
**Purpose:** Returns a human‑readable representation of the slot range.  
**Return Value:** A string in the format `"[Start-End] (NodeId:Endpoint)"`.  
**Remarks:** The method is guaranteed to return a non‑null string and does not throw.

### NodeId
**Type:** `string` (required)  
**Purpose:** Gets the unique identifier of the Redis node that owns this slot range.  
**Remarks:** The value is set during initialization and cannot be changed.

### EndPoint
**Type:** `string` (required)  
**Purpose:** Gets the network endpoint (host:port) of the node that owns this slot range.  
**Remarks:** The value is set during initialization and cannot be changed.

### Role
**Type:** `ClusterNodeRole` (required)  
**Purpose:** Gets the role of the owning node (e.g., Master, Replica).  
**Remarks:** The value is set during initialization and cannot be changed.

### SlotRanges
**Type:** `IReadOnlyList<ClusterSlotRange>` (required)  
**Purpose:** Gets a list of sub‑ranges that further partition this slot range (useful for hierarchical representations).  
**Remarks:** The list is immutable after initialization; it may be empty but never null.

### IsConnected
**Type:** `bool`  
**Purpose:** Indicates whether the owning node is currently reachable.  
**Remarks:** The value can change at runtime based on network conditions; reading the property is thread‑safe.

### PrimaryNodeId
**Type:** `string?`  
**Purpose:** Gets the identifier of the primary node for this range, if the owning node is a replica; otherwise `null`.  
**Remarks:** The value may change if replica re‑parenting occurs; reading is thread‑safe.

### OwnsSlot
**Type:** `bool`  
**Purpose:** Indicates whether the owning node is responsible for at least one slot in the range.  
**Remarks:** Typically `true` when `Contains` is `true` and the node is healthy; the property reflects current state and is thread‑safe.

### TotalNodes
**Type:** `int` (required)  
**Purpose:** Gets the total number of nodes in the cluster at the time the snapshot was taken.  
**Remarks:** Set during initialization; does not change after object creation.

### MasterCount
**Type:** `int` (required)  
**Purpose:** Gets the number of master nodes in the cluster snapshot.  
**Remarks:** Set during initialization; does not change after object creation.

### ReplicaCount
**Type:** `int` (required)  
**Purpose:** Gets the number of replica nodes in the cluster snapshot.  
**Remarks:** Set during initialization; does not change after object creation.

### TotalSlots
**Type:** `int` (required)  
**Purpose:** Gets the total number of hash slots configured for the cluster (usually 16384).  
**Remarks:** Set during initialization; does not change after object creation.

### CoveredSlots
**Type:** `int` (required)  
**Purpose:** Gets the number of slots covered by this range (i.e., `End - Start + 1` when valid).  
**Remarks:** Set during initialization; does not change after object creation.

### IsHealthy
**Type:** `bool` (required)  
**Purpose:** Indicates whether the owning node passed the last health check.  
**Remarks:** Set during initialization; does not change after object creation.

### CapturedAt
**Type:** `DateTime` (required)  
**Purpose:** Gets the timestamp when the cluster topology snapshot was taken.  
**Remarks:** Set during initialization; does not change after object creation.

## Usage

### Example 1: Inspecting a slot range
```csharp
var range = new ClusterSlotRange
{
    Start = 5461,
    End = 10922,
    NodeId = "node-3",
    EndPoint = "10.0.0.3:6379",
    Role = ClusterNodeRole.Master,
    SlotRanges = Array.Empty<ClusterSlotRange>(),
    IsConnected = true,
    PrimaryNodeId = null,
    OwnsSlot = true,
    TotalNodes = 6,
    MasterCount = 3,
    ReplicaCount = 3,
    TotalSlots = 16384,
    CoveredSlots = 5462,
    IsHealthy = true,
    CapturedAt = DateTime.UtcNow
};

if (range.Contains)
{
    Console.WriteLine($"Range {range} is healthy: {range.IsHealthy}");
}
else
{
    Console.WriteLine("Invalid slot range detected.");
}
```

### Example 2: Aggregating covered slots across nodes
```csharp
int totalCovered = 0;
foreach (var range in clusterSnapshot.SlotRanges)
{
    if (range.IsConnected && range.OwnsSlot)
    {
        totalCovered += range.CoveredSlots;
    }
}

Console.WriteLine($"Connected nodes collectively cover {totalCovered} of {clusterSnapshot.TotalSlots} slots.");
```

## Notes

- The `Start` and `End` properties define an inclusive interval; a range where `Start > End` is considered invalid, causing `Contains` to return `false`.  
- `SlotRanges` is always supplied as an `IReadOnlyList<ClusterSlotRange>`; consumers should treat it as immutable and safe for concurrent reads.  
- Boolean state properties (`IsConnected`, `OwnsSlot`, `IsHealthy`) may reflect runtime conditions that change after the object is created; reading them is thread‑safe, but callers should not rely on their values remaining constant across successive reads.  
- `PrimaryNodeId` is non‑null only for replica nodes; for masters it will be `null`.  
- All required init‑only properties are set at object construction and thereafter remain constant, making the instance effectively immutable with respect to those fields.  
- The `ToString` override is intended for logging and debugging; it does not guarantee a particular culture‑specific format and should not be parsed for programmatic logic.
