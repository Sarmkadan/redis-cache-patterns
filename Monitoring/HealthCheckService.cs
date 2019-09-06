#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using RedisCachePatterns.Infrastructure.Cache;

namespace RedisCachePatterns.Monitoring;

/// <summary>
/// Health check service for monitoring application and cache system status
/// Provides diagnostics for all critical components
/// </summary>
public class HealthCheckService
{
    private readonly IRedisConnection _redisConnection;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(IRedisConnection redisConnection, ILogger<HealthCheckService> logger)
    {
        _redisConnection = redisConnection;
        _logger = logger;
    }

    public async Task<HealthStatus> CheckHealthAsync()
    {
        var status = new HealthStatus { CheckedAt = DateTime.UtcNow };

        // Check Redis connection
        try
        {
            var isConnected = await _redisConnection.IsConnectedAsync().ConfigureAwait(false);
            status.RedisConnected = isConnected;
            if (isConnected)
            {
                status.Components.Add("Redis", "Healthy");
                _logger.LogInformation("Health check: Redis connection OK");
            }
            else
            {
                status.Components.Add("Redis", "Unhealthy");
                status.Issues.Add("Redis connection failed");
                _logger.LogWarning("Health check: Redis connection failed");
            }
        }
        catch (Exception ex)
        {
            status.Components.Add("Redis", "Error");
            status.Issues.Add($"Redis error: {ex.Message}");
            _logger.LogError(ex, "Health check: Redis error");
        }

        // Check memory usage
        try
        {
            var db = _redisConnection.GetDatabase();
            var pong = await db.PingAsync().ConfigureAwait(false);
            status.Components.Add("Memory", pong.TotalMilliseconds >= 0 ? "Healthy" : "Unknown");
        }
        catch (Exception ex)
        {
            status.Components.Add("Memory", "Error");
            _logger.LogError(ex, "Health check: Memory info error");
        }

        // Determine overall status
        status.Overall = status.Issues.Count == 0 ? "Healthy" : "Unhealthy";

        _logger.LogInformation("Health check completed: Overall={Status} | Issues={Count}",
            status.Overall, status.Issues.Count);

        return status;
    }

    public async Task<bool> IsReadyAsync()
    {
        try
        {
            return await _redisConnection.IsConnectedAsync().ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Health status report
/// </summary>
public class HealthStatus
{
    public string Overall { get; set; } = "Unknown";
    public bool RedisConnected { get; set; }
    public Dictionary<string, string> Components { get; set; } = new();
    public List<string> Issues { get; set; } = new();
    public DateTime CheckedAt { get; set; }

    public override string ToString() =>
        $"Status={Overall} | Redis={RedisConnected} | Components={Components.Count} | Issues={Issues.Count}";
}
