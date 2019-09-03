// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Domain;

/// <summary>
/// A contiguous range of Redis hash slots assigned to a single cluster node.
/// Slot indices follow the Redis Cluster specification (0–16 383).
/// </summary>
public sealed record ClusterSlotRange
{
    /// <summary>First hash slot in this range (inclusive).</summary>
    public required int Start { get; init; }

    /// <summary>Last hash slot in this range (inclusive).</summary>
    public required int End { get; init; }

    /// <summary>Number of slots covered by this range.</summary>
    public int Count => End - Start + 1;

    /// <summary>Returns <c>true</c> when <paramref name="slot"/> falls within this range.</summary>
    public bool Contains(int slot) => slot >= Start && slot <= End;

    /// <inheritdoc/>
    public override string ToString() => $"[{Start}–{End}]";
}

/// <summary>
/// Represents a single node in a Redis Cluster with its slot assignments and replication role.
/// A node may own multiple non-contiguous <see cref="ClusterSlotRange"/> entries (e.g., after partial resharding).
/// </summary>
public sealed record ClusterNode
{
    /// <summary>Unique 40-character hex identifier assigned by the cluster.</summary>
    public required string NodeId { get; init; }

    /// <summary>Network address of this node in <c>host:port</c> form.</summary>
    public required string EndPoint { get; init; }

    /// <summary>Whether this node is a master that accepts writes, or a read-only replica.</summary>
    public required ClusterNodeRole Role { get; init; }

    /// <summary>All hash-slot ranges currently owned by this node.</summary>
    public required IReadOnlyList<ClusterSlotRange> SlotRanges { get; init; }

    /// <summary>
    /// Whether the multiplexer currently considers this node reachable.
    /// Derived from the absence of the <c>noaddr</c> flag in <c>CLUSTER NODES</c> output.
    /// </summary>
    public bool IsConnected { get; init; }

    /// <summary>
    /// Node ID of the primary when <see cref="Role"/> is <see cref="ClusterNodeRole.Replica"/>;
    /// <c>null</c> for master nodes.
    /// </summary>
    public string? PrimaryNodeId { get; init; }

    /// <summary>Total number of hash slots this node owns, summed across all <see cref="SlotRanges"/>.</summary>
    public int TotalSlotCount => SlotRanges.Sum(r => r.Count);

    /// <summary>Returns <c>true</c> when this node is a master.</summary>
    public bool IsMaster => Role == ClusterNodeRole.Master;

    /// <summary>
    /// Returns <c>true</c> when <paramref name="slot"/> falls within any of this node's
    /// <see cref="SlotRanges"/>.
    /// </summary>
    public bool OwnsSlot(int slot) => SlotRanges.Any(r => r.Contains(slot));

    /// <inheritdoc/>
    public override string ToString() =>
        $"ClusterNode[{NodeId[..8]}… {EndPoint} {Role} slots={TotalSlotCount}]";
}

/// <summary>Role of a node within the Redis Cluster topology.</summary>
public enum ClusterNodeRole
{
    /// <summary>Node accepts writes and owns one or more slot ranges.</summary>
    Master,

    /// <summary>Node replicates a master; can serve reads when replica reads are enabled.</summary>
    Replica,

    /// <summary>Role could not be determined — typically during a handshake or failover election.</summary>
    Unknown
}

/// <summary>
/// Point-in-time snapshot of the Redis Cluster topology, aggregated from <c>CLUSTER NODES</c> output.
/// </summary>
public sealed record ClusterInfo
{
    /// <summary>Total node count, including replicas.</summary>
    public required int TotalNodes { get; init; }

    /// <summary>Number of master nodes that own at least one slot range.</summary>
    public required int MasterCount { get; init; }

    /// <summary>Number of replica nodes.</summary>
    public required int ReplicaCount { get; init; }

    /// <summary>
    /// Total possible hash slots per the Redis Cluster specification.
    /// This value is always 16 384.
    /// </summary>
    public required int TotalSlots { get; init; }

    /// <summary>Number of hash slots currently covered by a connected master.</summary>
    public required int CoveredSlots { get; init; }

    /// <summary>
    /// <c>true</c> when all 16 384 slots are covered by a connected master —
    /// indicating the cluster is fully operational with no slot gaps.
    /// </summary>
    public required bool IsHealthy { get; init; }

    /// <summary>UTC timestamp when this snapshot was captured.</summary>
    public required DateTime CapturedAt { get; init; }

    /// <summary>Percentage of total slots that are currently covered (0–100).</summary>
    public double SlotCoverage => TotalSlots == 0 ? 0d : (double)CoveredSlots / TotalSlots * 100d;

    /// <inheritdoc/>
    public override string ToString() =>
        $"ClusterInfo[nodes={TotalNodes} masters={MasterCount} slots={CoveredSlots}/{TotalSlots} " +
        $"({SlotCoverage:F1}%) healthy={IsHealthy}]";
}
