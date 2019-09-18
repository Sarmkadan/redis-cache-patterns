#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using StackExchange.Redis;

namespace RedisCachePatterns.Infrastructure.Cache;

/// <summary>
/// Interface for managing Redis connections
/// </summary>
public interface IRedisConnection
{
    /// <summary>Gets the current connection multiplexer.</summary>
    /// <returns>The active <see cref="IConnectionMultiplexer"/>.</returns>
    IConnectionMultiplexer GetConnection();
    /// <summary>Gets a database instance.</summary>
    /// <param name="databaseId">The Redis database index.</param>
    /// <returns>The <see cref="IDatabase"/> instance.</returns>
    IDatabase GetDatabase(int databaseId = 0);
    /// <summary>Checks if connected.</summary>
    /// <returns><c>true</c> if connected; otherwise <c>false</c>.</returns>
    Task<bool> IsConnectedAsync();
    /// <summary>Disconnects from Redis.</summary>
    Task DisconnectAsync();
    /// <summary>Gets the connection string.</summary>
    /// <returns>The Redis connection string.</returns>
    string GetConnectionString();
}
