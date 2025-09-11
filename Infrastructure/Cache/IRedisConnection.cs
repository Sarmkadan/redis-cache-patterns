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
    IConnectionMultiplexer GetConnection();
    IDatabase GetDatabase(int databaseId = 0);
    Task<bool> IsConnectedAsync();
    Task DisconnectAsync();
    string GetConnectionString();
}
