// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Configuration;

/// <summary>
/// Controls which Redis Cluster nodes are preferred for read-only commands.
/// </summary>
public enum ClusterReadPreference
{
    /// <summary>All reads are served by the slot-owning master (strongest consistency).</summary>
    Primary,

    /// <summary>Reads prefer a replica; falls back to the master when no replica is available.</summary>
    Replica,

    /// <summary>Read requests are distributed across both masters and replicas.</summary>
    Any
}

/// <summary>
/// Configuration for a Redis Cluster deployment.
/// Covers node endpoints, connection tunables, replica-read policy, Redlock parameters,
/// and failover behaviour.
/// </summary>
public sealed class ClusterConfiguration
{
    /// <summary>
    /// One or more cluster seed endpoints in <c>host:port</c> form.
    /// StackExchange.Redis discovers the full topology from these seeds — you do not need
    /// to list every node.
    /// </summary>
    public required string[] Endpoints { get; set; }

    /// <summary>Milliseconds to wait while establishing a TCP connection to any node.</summary>
    public int ConnectTimeoutMs { get; set; } = 5_000;

    /// <summary>Milliseconds to wait for a synchronous Redis command to complete.</summary>
    public int SyncTimeoutMs { get; set; } = 5_000;

    /// <summary>
    /// Governs which node type handles read-only cache operations.
    /// Default is <see cref="ClusterReadPreference.Primary"/> for strong read-after-write consistency.
    /// </summary>
    public ClusterReadPreference ReadPreference { get; set; } = ClusterReadPreference.Primary;

    /// <summary>
    /// When <c>true</c>, <c>READONLY</c> commands (GET, EXISTS, TTL, etc.) may be served
    /// by replica nodes, reducing load on masters at the cost of potential read-your-writes
    /// inconsistency during replication lag.
    /// </summary>
    public bool AllowReplicaReads { get; set; } = false;

    /// <summary>
    /// Page size passed to the <c>SCAN</c> cursor when iterating keys across cluster nodes.
    /// Larger values reduce round-trips but increase per-page latency.
    /// </summary>
    public int SlotScanPageSize { get; set; } = 250;

    // ── Distributed-lock (Redlock) parameters ────────────────────────────────

    /// <summary>
    /// Maximum number of lock-acquisition attempts before giving up.
    /// Applies to the retry loop inside <c>AcquireLockAsync</c>.
    /// </summary>
    public int RedlockRetryCount { get; set; } = 3;

    /// <summary>Delay between successive lock-acquisition attempts.</summary>
    public TimeSpan RedlockRetryDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Clock-drift allowance deducted from the elapsed time when deciding whether
    /// the lock was acquired within its valid window.
    /// </summary>
    public TimeSpan RedlockClockDrift { get; set; } = TimeSpan.FromMilliseconds(50);

    // ── Failover and reconnect ────────────────────────────────────────────────

    /// <summary>
    /// How long to wait for the cluster to elect a new primary after detecting a failure
    /// before surfacing a <see cref="Exceptions.CacheConnectionException"/>.
    /// </summary>
    public TimeSpan FailoverTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// When <c>true</c>, a fresh <c>ConnectionMultiplexer</c> is created automatically
    /// after a failover event, without requiring an application restart.
    /// </summary>
    public bool ReconnectOnFailover { get; set; } = true;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="ClusterConfiguration"/> populated from well-known environment variables:
    /// <list type="bullet">
    ///   <item><c>REDIS_CLUSTER_NODES</c> — comma-separated <c>host:port</c> seeds (default: localhost:7000,7001,7002)</item>
    ///   <item><c>REDIS_CONNECT_TIMEOUT</c> — connect timeout in milliseconds</item>
    ///   <item><c>REDIS_SYNC_TIMEOUT</c> — sync timeout in milliseconds</item>
    ///   <item><c>REDIS_REPLICA_READS</c> — <c>true</c> to enable replica reads</item>
    /// </list>
    /// </summary>
    public static ClusterConfiguration FromEnvironment()
    {
        var raw = Environment.GetEnvironmentVariable("REDIS_CLUSTER_NODES")
                  ?? "localhost:7000,localhost:7001,localhost:7002";

        var endpoints = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new ClusterConfiguration
        {
            Endpoints = endpoints,
            ConnectTimeoutMs = int.TryParse(
                Environment.GetEnvironmentVariable("REDIS_CONNECT_TIMEOUT"), out var ct) ? ct : 5_000,
            SyncTimeoutMs = int.TryParse(
                Environment.GetEnvironmentVariable("REDIS_SYNC_TIMEOUT"), out var st) ? st : 5_000,
            AllowReplicaReads = bool.TryParse(
                Environment.GetEnvironmentVariable("REDIS_REPLICA_READS"), out var rr) && rr
        };
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"ClusterConfig[nodes={Endpoints.Length} readPref={ReadPreference} " +
        $"replicaReads={AllowReplicaReads} connectTimeout={ConnectTimeoutMs}ms]";
}
