#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Domain;

/// <summary>
/// Helper extension methods for <see cref="ClusterNode"/> and <see cref="ClusterSlotRange"/>
/// used when inspecting a Redis Cluster topology snapshot.
/// </summary>
public static class ClusterNodeExtensions
{
    /// <summary>Number of leading characters of a node ID shown in <see cref="ToSummary(ClusterNode)"/>.</summary>
    private const int ShortNodeIdLength = 8;

    /// <summary>
    /// Filters a sequence of cluster nodes down to masters (nodes with <see cref="ClusterNodeRole.Master"/>).
    /// </summary>
    /// <param name="nodes">The nodes to filter.</param>
    /// <returns>A lazily evaluated sequence containing only master nodes, in their original order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="nodes"/> is null.</exception>
    public static IEnumerable<ClusterNode> Masters(this IEnumerable<ClusterNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        return nodes.Where(n => n.IsMaster);
    }

    /// <summary>
    /// Filters a sequence of cluster nodes down to replicas (nodes with <see cref="ClusterNodeRole.Replica"/>).
    /// </summary>
    /// <param name="nodes">The nodes to filter.</param>
    /// <returns>A lazily evaluated sequence containing only replica nodes, in their original order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="nodes"/> is null.</exception>
    public static IEnumerable<ClusterNode> Replicas(this IEnumerable<ClusterNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        return nodes.Where(n => n.Role == ClusterNodeRole.Replica);
    }

    /// <summary>
    /// Finds the master node that owns the given hash slot.
    /// </summary>
    /// <param name="nodes">The cluster nodes to search.</param>
    /// <param name="slot">The hash slot index (0–16 383) to look up.</param>
    /// <returns>The owning master node, or <c>null</c> when no master covers <paramref name="slot"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="nodes"/> is null.</exception>
    public static ClusterNode? FindSlotOwner(this IEnumerable<ClusterNode> nodes, int slot)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        return nodes.FirstOrDefault(n => n.IsMaster && n.OwnsSlot(slot));
    }

    /// <summary>
    /// Builds a short, single-line, human-readable summary of a cluster node for logs and diagnostics.
    /// </summary>
    /// <param name="node">The node to summarize.</param>
    /// <returns>
    /// A string such as <c>"1a2b3c4d master 127.0.0.1:6379 slots=5461 (2 ranges)"</c>,
    /// with a <c>" disconnected"</c> suffix when <see cref="ClusterNode.IsConnected"/> is <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
    public static string ToSummary(this ClusterNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var shortId = node.NodeId.Length > ShortNodeIdLength ? node.NodeId[..ShortNodeIdLength] : node.NodeId;
        var connection = node.IsConnected ? string.Empty : " disconnected";
        return $"{shortId} {node.Role.ToString().ToLowerInvariant()} {node.EndPoint} " +
               $"slots={node.TotalSlotCount} ({node.SlotRanges.Count} ranges){connection}";
    }

    /// <summary>
    /// Determines whether this slot range shares at least one hash slot with another range.
    /// </summary>
    /// <param name="range">The range to check.</param>
    /// <param name="other">The range to compare against.</param>
    /// <returns><c>true</c> when the two ranges share at least one slot; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
    public static bool Overlaps(this ClusterSlotRange range, ClusterSlotRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return range.Start <= other.End && other.Start <= range.End;
    }

    /// <summary>
    /// Determines whether this slot range directly precedes or follows another range with no gap and no overlap.
    /// Useful for merging contiguous ranges reported by <c>CLUSTER NODES</c>.
    /// </summary>
    /// <param name="range">The range to check.</param>
    /// <param name="other">The range to compare against.</param>
    /// <returns><c>true</c> when the ranges touch end-to-start in either direction; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
    public static bool IsAdjacentTo(this ClusterSlotRange range, ClusterSlotRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return range.End + 1 == other.Start || other.End + 1 == range.Start;
    }
}
