// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Middleware;

/// <summary>
/// Intercepts and logs all incoming requests with timing and performance metrics
/// Captures request/response details for diagnostics and performance analysis
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(ILogContext context, Func<Task> next)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = context.RequestId ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Request started: {RequestId} | Method: {Method} | Operation: {Operation}",
            requestId, context.Method, context.OperationName);

        try
        {
            await next();
            stopwatch.Stop();

            _logger.LogInformation(
                "Request completed: {RequestId} | Duration: {DurationMs}ms | Status: Success",
                requestId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Request failed: {RequestId} | Duration: {DurationMs}ms | Error: {Error}",
                requestId, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}

/// <summary>
/// Provides context for request operations
/// </summary>
public interface ILogContext
{
    string? RequestId { get; }
    string Method { get; }
    string OperationName { get; }
}

/// <summary>
/// Default implementation of request logging context
/// </summary>
public class LogContext : ILogContext
{
    public string? RequestId { get; set; }
    public string Method { get; set; } = "Unknown";
    public string OperationName { get; set; } = "Unknown";
}
