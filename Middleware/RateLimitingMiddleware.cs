// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Middleware;

/// <summary>
/// Rate limiting middleware that enforces request quotas per client/operation
/// Uses sliding window algorithm to prevent abuse while allowing bursts
/// </summary>
public class RateLimitingMiddleware
{
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitPolicy _policy;
    private readonly ConcurrentDictionary<string, RequestHistory> _requestHistory;

    public RateLimitingMiddleware(
        ILogger<RateLimitingMiddleware> logger,
        RateLimitPolicy? policy = null)
    {
        _logger = logger;
        _policy = policy ?? RateLimitPolicy.Default();
        _requestHistory = new();
    }

    public async Task InvokeAsync(string clientId, Func<Task> next)
    {
        if (!IsRequestAllowed(clientId))
        {
            _logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);
            throw new InvalidOperationException("Rate limit exceeded");
        }

        RecordRequest(clientId);
        await next();
    }

    private bool IsRequestAllowed(string clientId)
    {
        var history = _requestHistory.GetOrAdd(clientId, _ => new RequestHistory());
        var now = DateTime.UtcNow;

        // Clean old entries outside the window
        history.Timestamps.RemoveAll(t => (now - t).TotalSeconds > _policy.WindowSeconds);

        return history.Timestamps.Count < _policy.MaxRequests;
    }

    private void RecordRequest(string clientId)
    {
        var history = _requestHistory.GetOrAdd(clientId, _ => new RequestHistory());
        history.Timestamps.Add(DateTime.UtcNow);
    }

    private class RequestHistory
    {
        public List<DateTime> Timestamps { get; } = new();
    }
}

/// <summary>
/// Configuration for rate limiting behavior
/// </summary>
public class RateLimitPolicy
{
    public int MaxRequests { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;

    public static RateLimitPolicy Default() => new();

    public static RateLimitPolicy Strict() => new
    {
        MaxRequests = 10,
        WindowSeconds = 60
    };

    public static RateLimitPolicy Lenient() => new
    {
        MaxRequests = 1000,
        WindowSeconds = 60
    };
}
