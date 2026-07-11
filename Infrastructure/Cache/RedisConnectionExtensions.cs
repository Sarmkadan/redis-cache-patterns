#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using StackExchange.Redis;

namespace RedisCachePatterns.Infrastructure.Cache;

/// <summary>
/// Extension methods for RedisConnection providing common Redis operations
/// </summary>
public static class RedisConnectionExtensions
{
    /// <summary>
    /// Gets a value from Redis with optional expiration extension
    /// </summary>
    /// <param name="connection">Redis connection</param>
    /// <param name="key">The key to retrieve</param>
    /// <param name="databaseId">Database ID (0-15)</param>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/></exception>
    /// <exception cref="ArgumentException"><paramref name="key"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="databaseId"/></exception>
    /// <returns>The value if found, null otherwise</returns>
    public static async Task<string?> GetWithExpirationAsync(this RedisConnection connection, string key, int databaseId = 0)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentOutOfRangeException.ThrowIfLessThan(databaseId, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(databaseId, 15);

        var database = connection.GetDatabase(databaseId);
        return await database.StringGetAsync(key);
    }

    /// <summary>
    /// Sets a value in Redis with optional expiration
    /// </summary>
    /// <param name="connection">Redis connection</param>
    /// <param name="key">The key to set</param>
    /// <param name="value">The value to store</param>
    /// <param name="expiry">Optional expiration time</param>
    /// <param name="databaseId">Database ID (0-15)</param>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/></exception>
    /// <exception cref="ArgumentException"><paramref name="key"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="databaseId"/></exception>
    /// <returns>True if successful</returns>
    public static async Task<bool> SetWithExpirationAsync(this RedisConnection connection, string key, string value, TimeSpan? expiry = null, int databaseId = 0)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentOutOfRangeException.ThrowIfLessThan(databaseId, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(databaseId, 15);

        var database = connection.GetDatabase(databaseId);
        return await database.StringSetAsync(key, value, expiry);
    }

    /// <summary>
    /// Gets multiple values from Redis in a single round-trip
    /// </summary>
    /// <param name="connection">Redis connection</param>
    /// <param name="keys">Collection of keys to retrieve</param>
    /// <param name="databaseId">Database ID (0-15)</param>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="keys"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="databaseId"/></exception>
    /// <returns>Dictionary mapping keys to their values</returns>
    public static async Task<Dictionary<string, string?>> GetMultipleAsync(this RedisConnection connection, IEnumerable<string> keys, int databaseId = 0)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentOutOfRangeException.ThrowIfLessThan(databaseId, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(databaseId, 15);

        var database = connection.GetDatabase(databaseId);
        var result = new Dictionary<string, string?>();

        foreach (var key in keys)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                result[key] = await database.StringGetAsync(key);
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if a key exists in Redis
    /// </summary>
    /// <param name="connection">Redis connection</param>
    /// <param name="key">The key to check</param>
    /// <param name="databaseId">Database ID (0-15)</param>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/></exception>
    /// <exception cref="ArgumentException"><paramref name="key"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="databaseId"/></exception>
    /// <returns>True if the key exists</returns>
    public static async Task<bool> KeyExistsAsync(this RedisConnection connection, string key, int databaseId = 0)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentOutOfRangeException.ThrowIfLessThan(databaseId, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(databaseId, 15);

        var database = connection.GetDatabase(databaseId);
        return await database.KeyExistsAsync(key);
    }

    /// <summary>
    /// Removes a key from Redis
    /// </summary>
    /// <param name="connection">Redis connection</param>
    /// <param name="key">The key to remove</param>
    /// <param name="databaseId">Database ID (0-15)</param>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/></exception>
    /// <exception cref="ArgumentException"><paramref name="key"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="databaseId"/></exception>
    /// <returns>True if the key was removed</returns>
    public static async Task<bool> RemoveKeyAsync(this RedisConnection connection, string key, int databaseId = 0)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentOutOfRangeException.ThrowIfLessThan(databaseId, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(databaseId, 15);

        var database = connection.GetDatabase(databaseId);
        return await database.KeyDeleteAsync(key);
    }
}
