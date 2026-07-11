#nullable enable

using System.Threading.Tasks;
using StackExchange.Redis;
using RedisCachePatterns.Domain;

namespace RedisCachePatterns.Infrastructure.Cache;

/// <summary>
/// Extension methods for <see cref="RedisClusterConnection"/> that provide convenient cluster-aware
/// operations for common Redis patterns including key existence checks, bulk operations, and
/// conditional cache management.
/// </summary>
public static class RedisClusterConnectionExtensions
{
    /// <summary>
    /// Checks whether the specified key exists in the Redis cluster.
    /// </summary>
    /// <param name="connection">The cluster connection.</param>
    /// <param name="key">The key to check.</param>
    /// <returns><see langword="true"/> if the key exists; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public static async Task<bool> KeyExistsAsync(this RedisClusterConnection connection, string key)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(key);

        var db = connection.GetDatabase();
        return await db.KeyExistsAsync(key);
    }

    /// <summary>
    /// Checks whether any of the specified keys exist in the Redis cluster.
    /// </summary>
    /// <param name="connection">The cluster connection.</param>
    /// <param name="keys">The collection of keys to check.</param>
    /// <returns>A task that returns the number of keys that exist.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="keys"/> is <see langword="null"/>.</exception>
    public static async Task<long> KeyExistsAsync(this RedisClusterConnection connection, IReadOnlyCollection<string> keys)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(keys);

        if (keys.Count == 0)
            return 0;

        var tasks = keys.Select(async key =>
        {
            ArgumentNullException.ThrowIfNull(key);
            var db = connection.GetDatabase();
            return await db.KeyExistsAsync(key);
        });

        var results = await Task.WhenAll(tasks);
        return results.Count(r => r);
    }

    /// <summary>
    /// Gets the value of the specified key from the Redis cluster.
    /// </summary>
    /// <param name="connection">The cluster connection.</param>
    /// <param name="key">The key to get.</param>
    /// <returns>The value of the key, or <see langword="null"/> if the key does not exist.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public static async Task<string?> StringGetAsync(this RedisClusterConnection connection, string key)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(key);

        var db = connection.GetDatabase();
        return await db.StringGetAsync(key);
    }

    /// <summary>
    /// Sets the value of the specified key in the Redis cluster with an optional expiration.
    /// </summary>
    /// <param name="connection">The cluster connection.</param>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="expiry">Optional expiration time.</param>
    /// <returns><see langword="true"/> if the key was set; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static async Task<bool> StringSetAsync(
        this RedisClusterConnection connection,
        string key,
        string value,
        TimeSpan? expiry = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        var db = connection.GetDatabase();
        return await db.StringSetAsync(key, value, expiry);
    }

    /// <summary>
    /// Performs a bulk delete operation across all master nodes in the cluster.
    /// This is useful for cache invalidation scenarios where you need to remove keys
    /// that may be distributed across multiple shards.
    /// </summary>
    /// <param name="connection">The cluster connection.</param>
    /// <param name="pattern">The pattern to match keys against (e.g., "user:*").</param>
    /// <returns>The total number of keys deleted across all nodes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    public static async Task<long> DeleteByPatternAsync(this RedisClusterConnection connection, string pattern)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pattern);

        var masters = await connection.GetMasterNodesAsync();
        var tasks = masters.Select(async master =>
        {
            var server = connection.GetConnection().GetServer(master.EndPoint);
            var keys = server.Keys(pattern: pattern).ToArray();
            if (keys.Length == 0)
                return 0L;

            var db = connection.GetDatabase();
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            return await db.KeyDeleteAsync(redisKeys);
        });

        var results = await Task.WhenAll(tasks);
        return results.Sum();
    }

    /// <summary>
    /// Gets information about the cluster health and topology.
    /// </summary>
    /// <param name="connection">The cluster connection.</param>
    /// <returns>A <see cref="ClusterInfo"/> object containing cluster statistics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
    public static async Task<ClusterInfo> GetClusterHealthAsync(this RedisClusterConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return await connection.GetClusterInfoAsync();
    }

    /// <summary>
    /// Executes a command on all master nodes and returns the aggregated results.
    /// </summary>
    /// <param name="connection">The cluster connection.</param>
    /// <param name="command">The Redis command to execute.</param>
    /// <param name="args">Optional arguments for the command.</param>
    /// <returns>A dictionary mapping node endpoints to command results.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> is <see langword="null"/>.</exception>
    public static async Task<Dictionary<string, string>> ExecuteOnAllMastersAsync(
        this RedisClusterConnection connection,
        string command,
        params string[] args)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(command);

        var masters = await connection.GetMasterNodesAsync();
        var tasks = masters.Select(async master =>
        {
            var server = connection.GetConnection().GetServer(master.EndPoint);
            var db = connection.GetDatabase();

            // Use conditional logic based on command type
            object resultObj = command.ToUpperInvariant() switch
            {
                "INFO" => await server.InfoAsync(),
                "DBSIZE" => "N/A",
                "MEMORY USAGE" when args.Length > 0 => (await db.StringLengthAsync(args[0])).ToString(),
                _ => throw new NotSupportedException($"Command '{command}' is not supported")
            };

            return new KeyValuePair<string, string>(master.EndPoint ?? string.Empty, resultObj?.ToString() ?? string.Empty);
        });

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}