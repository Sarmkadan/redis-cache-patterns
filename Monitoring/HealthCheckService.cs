#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using RedisCachePatterns.Infrastructure.Cache;
using System.Globalization;

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
            var isConnected = await _redisConnection.IsConnectedAsync();
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
            var pong = await db.PingAsync();
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
            return await _redisConnection.IsConnectedAsync();
        }
        catch
        {
            return false;
        }
    }
}

public static class HealthCheckServiceExtensions
{
    /// <summary>
    /// Gets the health status for a specific component only
    /// </summary>
    /// <param name="service">The health check service</param>
    /// <param name="componentName">Name of the component to check (Redis, Memory, etc.)</param>
    /// <returns>Health status string for the requested component</returns>
    /// <exception cref="ArgumentNullException">Thrown when service is null</exception>
    /// <exception cref="ArgumentException">Thrown when componentName is null or empty</exception>
    public static async Task<string> GetComponentStatusAsync(this HealthCheckService service, string componentName)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(componentName, nameof(componentName));

        var status = await service.CheckHealthAsync();
        return status.Components.TryGetValue(componentName, out var value) ? value : "Unknown";
    }

    /// <summary>
    /// Gets all health issues grouped by component
    /// </summary>
    /// <param name="service">The health check service</param>
    /// <returns>Dictionary mapping component names to their issues, or empty if none</returns>
    /// <exception cref="ArgumentNullException">Thrown when service is null</exception>
    public static async Task<Dictionary<string, List<string>>> GetComponentIssuesAsync(this HealthCheckService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        var status = await service.CheckHealthAsync();
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var component in status.Components)
        {
            if (component.Value.Equals("Error", StringComparison.OrdinalIgnoreCase) ||
                component.Value.Equals("Unhealthy", StringComparison.OrdinalIgnoreCase))
            {
                result[component.Key] = status.Issues
                    .Where(issue => issue.StartsWith(component.Key + ":", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if all components are healthy (no errors/unhealthy status)
    /// </summary>
    /// <param name="service">The health check service</param>
    /// <returns>True if all components are healthy, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when service is null</exception>
    public static async Task<bool> AreAllComponentsHealthyAsync(this HealthCheckService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        var status = await service.CheckHealthAsync();
        return status.Overall.Equals("Healthy", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the health check timestamp in ISO 8601 format for machine-readable logging
    /// </summary>
    /// <param name="service">The health check service</param>
    /// <returns>ISO 8601 formatted timestamp of last health check</returns>
    /// <exception cref="ArgumentNullException">Thrown when service is null</exception>
    public static async Task<string> GetHealthCheckTimestampAsync(this HealthCheckService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        var status = await service.CheckHealthAsync();
        return status.CheckedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
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
