#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using RedisCachePatterns.Exceptions;

namespace RedisCachePatterns.Middleware;

/// <summary>
/// Centralized error handling middleware that intercepts and processes exceptions
/// Converts domain exceptions to appropriate error responses with proper logging
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly Dictionary<Type, int> _exceptionStatusCodes;

    public ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger)
    {
        _logger = logger;
        _exceptionStatusCodes = new Dictionary<Type, int>
        {
            { typeof(ArgumentNullException), 400 },
            { typeof(ArgumentException), 400 },
            { typeof(BusinessException), 409 },
            { typeof(CacheException), 500 },
            { typeof(InvalidOperationException), 400 },
        };
    }

    public async Task InvokeAsync(Func<Task> next)
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    private Task HandleExceptionAsync(Exception exception)
    {
        var statusCode = GetStatusCode(exception.GetType());
        var errorId = Guid.NewGuid().ToString();

        _logger.LogError(
            exception,
            "Unhandled exception occurred | ErrorId: {ErrorId} | StatusCode: {StatusCode} | Message: {Message}",
            errorId, statusCode, exception.Message);

        var errorResponse = new ErrorResponse
        {
            ErrorId = errorId,
            StatusCode = statusCode,
            Message = exception.Message,
            Details = exception.InnerException?.Message,
            Timestamp = DateTime.UtcNow
        };

        return Task.CompletedTask;
    }

    private int GetStatusCode(Type exceptionType)
    {
        return _exceptionStatusCodes.TryGetValue(exceptionType, out var code) ? code : 500;
    }
}

/// <summary>
/// Standard error response structure for API responses
/// </summary>
public class ErrorResponse
{
    public string ErrorId { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
}
