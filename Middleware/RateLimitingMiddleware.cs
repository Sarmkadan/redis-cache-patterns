#nullable enable
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
    /// <summary>
    /// The maximum number of requests allowed by the default preset.
    /// </summary>
    private const int DefaultMaxRequests = 100;

    /// <summary>
    /// The time window, in seconds, shared by the rate limit presets.
    /// </summary>
    private const int DefaultWindowSeconds = 60;

    /// <summary>
    /// The reduced maximum number of requests allowed by the strict preset.
    /// </summary>
    private const int StrictMaxRequests = 10;

    /// <summary>
    /// The increased maximum number of requests allowed by the lenient preset.
    /// </summary>
    private const int LenientMaxRequests = 1000;

    public int MaxRequests { get; set; } = DefaultMaxRequests;
    public int WindowSeconds { get; set; } = DefaultWindowSeconds;

    public static RateLimitPolicy Default() => new();

    public static RateLimitPolicy Strict() => new RateLimitPolicy
    {
        MaxRequests = StrictMaxRequests,
        WindowSeconds = DefaultWindowSeconds
    };

    public static RateLimitPolicy Lenient() => new RateLimitPolicy
    {
        MaxRequests = LenientMaxRequests,
        WindowSeconds = DefaultWindowSeconds
    };
}
