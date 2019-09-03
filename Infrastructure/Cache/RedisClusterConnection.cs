// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using StackExchange.Redis;
using RedisCachePatterns.Configuration;
using RedisCachePatterns.Domain;
using RedisCachePatterns.Exceptions;
using Microsoft.Extensions.Logging;

// Alias to disambiguate our domain type from the StackExchange.Redis type of the same name.
using SxNode = StackExchange.Redis.ClusterNode;

namespace RedisCachePatterns.Infrastructure.Cache;

/// <summary>
/// Thread-safe Redis Cluster connection that wraps StackExchange.Redis with cluster-aware
/// operations: hash-slot computation, topology discovery, and fan-out across master nodes.
/// <para>
/// The underlying <see cref="IConnectionMultiplexer"/> handles <c>MOVED</c> and <c>ASK</c>
/// redirections automatically. This class adds higher-level cluster semantics on top.
/// </para>
/// </summary>
public sealed class RedisClusterConnection : IRedisClusterConnection, IAsyncDisposable
{
    private IConnectionMultiplexer? _connection;
    private readonly ClusterConfiguration _config;
    private readonly ILogger<RedisClusterConnection> _logger;

    // Guards the lazy-connect path only; hot-path reads are lock-free.
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    /// <summary>Total hash slots defined by the Redis Cluster specification.</summary>
    private const int TotalClusterSlots = 16_384;

    /// <inheritdoc/>
    public bool IsClusterMode => true;

    /// <summary>
    /// Initialises a new cluster connection using the supplied configuration.
    /// The actual TCP connection is established lazily on first use.
    /// </summary>
    public RedisClusterConnection(ClusterConfiguration config, ILogger<RedisClusterConnection> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger;
    }

    // ── IRedisConnection ──────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// Uses double-checked locking so that only one thread pays the connection cost
    /// while subsequent callers return the cached multiplexer lock-free.
    /// </remarks>
    public IConnectionMultiplexer GetConnection()
    {
        if (_connection is { IsConnected: true })
            return _connection;

        _connectLock.Wait();
        try
        {
            if (_connection is { IsConnected: true })
                return _connection;

            _connection = ConnectionMultiplexer.Connect(BuildOptions());
            _logger.LogInformation(
                "Redis Cluster connection established to {SeedCount} seed node(s): {Seeds}",
                _config.Endpoints.Length,
                string.Join(", ", _config.Endpoints));
            return _connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Redis Cluster");
            throw new CacheConnectionException("Unable to connect to Redis Cluster", ex);
        }
        finally
        {
            _connectLock.Release();
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Redis Cluster only supports database index 0. Passing any other value throws
    /// <see cref="ArgumentException"/> rather than silently routing to the wrong dataset.
    /// </remarks>
    public IDatabase GetDatabase(int databaseId = 0)
    {
        if (databaseId != 0)
            throw new ArgumentException(
                "Redis Cluster only supports database index 0. " +
                "Use hash tags to logically partition keys instead.", nameof(databaseId));

        return GetConnection().GetDatabase(0);
    }

    /// <inheritdoc/>
    public async Task<bool> IsConnectedAsync()
    {
        try
        {
            var conn = GetConnection();
            var endpoint = conn.GetEndPoints().FirstOrDefault();
            if (endpoint is null) return false;

            await conn.GetServer(endpoint).PingAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
            _connection = null;
            _logger.LogInformation("Redis Cluster connection closed");
        }
    }

    /// <inheritdoc/>
    public string GetConnectionString() => string.Join(',', _config.Endpoints);

    // ── IRedisClusterConnection ───────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ClusterNode>> GetClusterNodesAsync()
    {
        var conn = GetConnection();
        var server = conn.GetEndPoints()
            .Select(ep => conn.GetServer(ep))
            .FirstOrDefault(s => !s.IsReplica && s.IsConnected);

        if (server is null)
            throw new CacheConnectionException("No reachable master node found in the cluster.");

        var clusterConfig = await server.ClusterNodesAsync();
        if (clusterConfig?.Nodes is null)
            return Array.Empty<ClusterNode>();

        return clusterConfig.Nodes
            .Select(MapClusterNode)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ClusterNode>> GetMasterNodesAsync()
    {
        var all = await GetClusterNodesAsync();
        return all.Where(n => n.IsMaster).ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public int GetSlotForKey(string key) => ComputeHashSlot(key);

    /// <inheritdoc/>
    public async Task<IServer> GetNodeForKeyAsync(string key)
    {
        var slot = GetSlotForKey(key);
        var nodes = await GetClusterNodesAsync();
        var owner = nodes.FirstOrDefault(n => n.IsMaster && n.OwnsSlot(slot));

        if (owner is null)
            throw new CacheConnectionException(
                $"No master node is currently responsible for hash slot {slot} (key: '{key}'). " +
                "The cluster may be undergoing a resharding operation.");

        return GetConnection().GetServer(owner.EndPoint);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Each action is dispatched concurrently with <c>Task.WhenAll</c>.
    /// Failures on individual nodes are propagated as an <see cref="AggregateException"/>.
    /// </remarks>
    public Task ForEachMasterAsync(Func<IServer, Task> action)
    {
        var conn = GetConnection();
        var tasks = conn.GetEndPoints()
            .Select(ep => conn.GetServer(ep))
            .Where(s => !s.IsReplica && s.IsConnected)
            .Select(action);

        return Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public async Task<ClusterInfo> GetClusterInfoAsync()
    {
        var nodes = await GetClusterNodesAsync();
        var masters = nodes.Where(n => n.IsMaster).ToList();
        var replicas = nodes.Where(n => n.Role == ClusterNodeRole.Replica).ToList();
        var coveredSlots = masters.Sum(n => n.TotalSlotCount);

        return new ClusterInfo
        {
            TotalNodes = nodes.Count,
            MasterCount = masters.Count,
            ReplicaCount = replicas.Count,
            TotalSlots = TotalClusterSlots,
            CoveredSlots = coveredSlots,
            IsHealthy = coveredSlots == TotalClusterSlots,
            CapturedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() => await DisconnectAsync();

    // ── Private helpers ───────────────────────────────────────────────────────

    private ConfigurationOptions BuildOptions()
    {
        var opts = new ConfigurationOptions
        {
            ConnectTimeout = _config.ConnectTimeoutMs,
            SyncTimeout = _config.SyncTimeoutMs,
            AbortOnConnectFail = false,
            ReconnectRetryPolicy = new ExponentialRetry(5_000),
        };

        foreach (var ep in _config.Endpoints)
            opts.EndPoints.Add(ep);

        return opts;
    }

    private static ClusterNode MapClusterNode(SxNode node)
    {
        var role = node.IsReplica ? ClusterNodeRole.Replica : ClusterNodeRole.Master;

        var slotRanges = node.Slots
            .Select(s => new ClusterSlotRange { Start = s.From, End = s.To })
            .ToList()
            .AsReadOnly();

        return new ClusterNode
        {
            NodeId = node.NodeId ?? string.Empty,
            EndPoint = node.EndPoint?.ToString() ?? string.Empty,
            Role = role,
            SlotRanges = slotRanges,
            // noaddr flag means the node has no address yet (handshake / leaving cluster)
            IsConnected = !node.IsNoAddr,
            PrimaryNodeId = node.IsReplica ? node.ParentNodeId : null
        };
    }

    /// <summary>
    /// Computes the Redis Cluster hash slot for <paramref name="key"/> using the XMODEM CRC16
    /// algorithm. When the key contains a hash tag <c>{tag}</c>, only the tag content is hashed,
    /// enabling co-location of related keys on the same shard.
    /// </summary>
    private static int ComputeHashSlot(string key)
    {
        ReadOnlySpan<char> span = key;

        var tagOpen = span.IndexOf('{');
        if (tagOpen >= 0)
        {
            var inner = span[(tagOpen + 1)..];
            var tagClose = inner.IndexOf('}');
            // Only apply hash tag when the braces enclose at least one character.
            if (tagClose > 0)
                span = inner[..tagClose];
        }

        var bytes = Encoding.UTF8.GetBytes(span.ToString());
        return (int)(Crc16Xmodem(bytes) % TotalClusterSlots);
    }

    /// <summary>
    /// XMODEM CRC16 — the exact variant specified by the Redis Cluster hash-slot algorithm.
    /// Polynomial: 0x1021, initial value: 0x0000, no input/output reflection.
    /// </summary>
    private static ushort Crc16Xmodem(ReadOnlySpan<byte> data)
    {
        const ushort Poly = 0x1021;
        ushort crc = 0;

        foreach (var b in data)
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
                crc = (crc & 0x8000) != 0
                    ? (ushort)((crc << 1) ^ Poly)
                    : (ushort)(crc << 1);
        }

        return crc;
    }
}
