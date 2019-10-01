// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using StackExchange.Redis;
using RedisCachePatterns.Domain;

namespace RedisCachePatterns.Infrastructure.Cache;

/// <summary>
/// Extends <see cref="IRedisConnection"/> with Redis Cluster-specific capabilities:
/// topology discovery, hash-slot mapping, and fan-out execution across master nodes.
/// <para>
/// Implementations must handle <c>MOVED</c> and <c>ASK</c> redirections transparently
/// (StackExchange.Redis does this automatically when the underlying
/// <see cref="IConnectionMultiplexer"/> is configured for cluster mode).
/// </para>
/// </summary>
public interface IRedisClusterConnection : IRedisConnection
{
    /// <summary>
    /// Returns a snapshot of all nodes currently visible to the cluster,
    /// including both masters and replicas.
    /// Derived from <c>CLUSTER NODES</c> output.
    /// </summary>
    Task<IReadOnlyList<ClusterNode>> GetClusterNodesAsync();

    /// <summary>
    /// Returns only the master nodes that own at least one hash-slot range.
    /// Use this when you need to fan-out a write or scan to every shard.
    /// </summary>
    Task<IReadOnlyList<ClusterNode>> GetMasterNodesAsync();

    /// <summary>
    /// Computes the Redis hash slot (0–16 383) for <paramref name="key"/> using the
    /// XMODEM CRC16 algorithm defined in the Redis Cluster specification.
    /// When the key contains a hash tag <c>{tag}</c>, only the tag portion is hashed.
    /// </summary>
    int GetSlotForKey(string key);

    /// <summary>
    /// Resolves the <see cref="IServer"/> instance currently responsible for
    /// <paramref name="key"/>'s hash slot.
    /// Throws <see cref="Exceptions.CacheConnectionException"/> when no master owns that slot.
    /// </summary>
    Task<IServer> GetNodeForKeyAsync(string key);

    /// <summary>
    /// Executes <paramref name="action"/> against every master node concurrently
    /// via <c>Task.WhenAll</c>.
    /// <para>
    /// Use for cluster-wide operations that must touch all shards, such as:
    /// <list type="bullet">
    ///   <item>Pattern-based key scans (<c>SCAN</c>)</item>
    ///   <item>Full cache flush (<c>FLUSHDB</c>)</item>
    ///   <item>Aggregate statistics collection</item>
    /// </list>
    /// </para>
    /// </summary>
    Task ForEachMasterAsync(Func<IServer, Task> action);

    /// <summary>
    /// Builds a <see cref="ClusterInfo"/> snapshot by aggregating topology data
    /// from all known master nodes.
    /// </summary>
    Task<ClusterInfo> GetClusterInfoAsync();

    /// <summary>
    /// <c>true</c> for cluster-mode connections.
    /// Implementations backed by a standalone Redis instance should return <c>false</c>
    /// to allow callers to fall back to single-node behaviour.
    /// </summary>
    bool IsClusterMode { get; }
}
