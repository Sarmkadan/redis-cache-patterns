#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using StackExchange.Redis;
using RedisCachePatterns.Exceptions;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Infrastructure.Cache;

/// <summary>
/// Manages Redis connection with retry logic and health checks
/// </summary>
public class RedisConnection : IRedisConnection
{
    private IConnectionMultiplexer? _connection;
    private readonly string _connectionString;
    private readonly ILogger<RedisConnection> _logger;
    private readonly ConfigurationOptions _configOptions;

    public RedisConnection(string connectionString, ILogger<RedisConnection> logger)
    {
        _connectionString = connectionString ?? "localhost:6379";
        _logger = logger;
        _configOptions = ConfigurationOptions.Parse(_connectionString);
        _configOptions.ConnectTimeout = 5000;
        _configOptions.SyncTimeout = 5000;
        _configOptions.AbortOnConnectFail = false;
    }

    public IConnectionMultiplexer GetConnection()
    {
        if (_connection != null && _connection.IsConnected)
            return _connection;

        try
        {
            _connection = ConnectionMultiplexer.Connect(_configOptions);
            _logger.LogInformation("Redis connection established successfully");
            return _connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish Redis connection");
            throw new CacheConnectionException("Unable to connect to Redis", ex);
        }
    }

    public IDatabase GetDatabase(int databaseId = 0)
    {
        var connection = GetConnection();
        if (databaseId < 0 || databaseId > 15)
            throw new ArgumentException("Database ID must be between 0 and 15");
        return connection.GetDatabase(databaseId);
    }

    public async Task<bool> IsConnectedAsync()
    {
        try
        {
            var connection = GetConnection();
            var server = connection.GetServer(connection.GetEndPoints().First());
            await server.PingAsync().ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync().ConfigureAwait(false);
            _connection?.Dispose();
            _connection = null;
            _logger.LogInformation("Redis connection closed");
        }
    }

    public string GetConnectionString() => _connectionString;
}
