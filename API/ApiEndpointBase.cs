// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using RedisCachePatterns.Middleware;
using RedisCachePatterns.Utilities;

namespace RedisCachePatterns.API;

/// <summary>
/// Base class for API endpoints with built-in validation, logging, and error handling
/// Provides consistent behavior across all API operations
/// </summary>
public abstract class ApiEndpointBase
{
    protected readonly ILogger Logger;
    protected readonly PerformanceMonitor PerformanceMonitor;

    protected ApiEndpointBase(ILogger logger, PerformanceMonitor performanceMonitor)
    {
        Logger = logger;
        PerformanceMonitor = performanceMonitor;
    }

    /// <summary>
    /// Handles endpoint execution with automatic error handling and metrics collection
    /// </summary>
    protected async Task<ApiResponse<T>> ExecuteAsync<T>(Func<Task<T>> operation, string operationName)
    {
        try
        {
            using (PerformanceMonitor.MeasureOperation(operationName))
            {
                var result = await operation();
                Logger.LogInformation("Operation succeeded: {Operation}", operationName);
                return ApiResponse<T>.Success(result);
            }
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Validation error in operation: {Operation}", operationName);
            return ApiResponse<T>.Failure(ex.Message, 400);
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning(ex, "Business logic error in operation: {Operation}", operationName);
            return ApiResponse<T>.Failure(ex.Message, 409);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error in operation: {Operation}", operationName);
            return ApiResponse<T>.Failure("Internal server error", 500);
        }
    }

    /// <summary>
    /// Validates required parameters
    /// </summary>
    protected void ValidateRequired(object? value, string paramName)
    {
        if (value == null)
            throw new ArgumentException($"{paramName} is required");

        if (value is string str && string.IsNullOrWhiteSpace(str))
            throw new ArgumentException($"{paramName} cannot be empty");
    }

    /// <summary>
    /// Validates numeric range
    /// </summary>
    protected void ValidateRange(int value, int min, int max, string paramName)
    {
        if (value < min || value > max)
            throw new ArgumentException($"{paramName} must be between {min} and {max}");
    }
}

/// <summary>
/// Standard API response format supporting both success and failure cases
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? RequestId { get; set; }

    public static ApiResponse<T> Success(T data)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            StatusCode = 200
        };
    }

    public static ApiResponse<T> Failure(string error, int statusCode = 500)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = error,
            StatusCode = statusCode
        };
    }

    public static ApiResponse<T> Unauthorized(string error = "Unauthorized")
    {
        return Failure(error, 401);
    }

    public static ApiResponse<T> NotFound(string error = "Not found")
    {
        return Failure(error, 404);
    }
}
