#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Logging helper for structured logging with consistent format and context
/// Provides utilities for performance logging and correlation tracking
/// </summary>
public static class LoggingHelper
{
    /// <summary>
    /// Logs operation performance metrics
    /// </summary>
    public static void LogOperationPerformance(
        ILogger logger,
        string operationName,
        long elapsedMs,
        long itemCount = 0,
        LogLevel level = LogLevel.Information)
    {
        var message = itemCount > 0
            ? $"Operation completed: {operationName} | Duration: {elapsedMs}ms | Items: {itemCount} | Throughput: {itemCount / Math.Max(elapsedMs / 1000.0, 0.001):F0} items/sec"
            : $"Operation completed: {operationName} | Duration: {elapsedMs}ms";

        logger.Log(level, message);
    }

    /// <summary>
    /// Logs cache operation with metrics
    /// </summary>
    public static void LogCacheOperation(
        ILogger logger,
        string operation,
        string key,
        bool success,
        long? elapsedMs = null)
    {
        var status = success ? "Success" : "Failed";
        var duration = elapsedMs.HasValue ? $" | Duration: {elapsedMs}ms" : "";
        logger.LogInformation("Cache operation: {Operation} | Key: {Key} | Status: {Status}{Duration}",
            operation, key, status, duration);
    }

    /// <summary>
    /// Logs business operation with structured data
    /// </summary>
    public static void LogBusinessOperation(
        ILogger logger,
        string operationType,
        string resourceId,
        Dictionary<string, object> context)
    {
        var contextStr = string.Join(", ", context.Select(x => $"{x.Key}={x.Value}"));
        logger.LogInformation("Business operation: {OperationType} | Resource: {ResourceId} | Context: {Context}",
            operationType, resourceId, contextStr);
    }

    /// <summary>
    /// Creates a correlation ID for request tracking
    /// </summary>
    public static string GenerateCorrelationId()
    {
        return $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 8)}";
    }

    /// <summary>
    /// Logs exception with sanitization
    /// </summary>
    public static void LogException(
        ILogger logger,
        Exception ex,
        string context,
        Dictionary<string, string>? sensitiveFields = null)
    {
        var message = SanitizeExceptionMessage(ex.Message, sensitiveFields);
        logger.LogError(ex, "Exception in {Context}: {Message}", context, message);
    }

    private static string SanitizeExceptionMessage(string message, Dictionary<string, string>? sensitiveFields)
    {
        if (sensitiveFields == null) return message;

        var sanitized = message;
        foreach (var (field, pattern) in sensitiveFields)
        {
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized, pattern, $"[{field}]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        return sanitized;
    }
}
