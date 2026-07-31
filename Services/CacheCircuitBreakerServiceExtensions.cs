#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using RedisCachePatterns.Services;

namespace RedisCachePatterns.Services;

/// <summary>
/// Extension methods for <see cref="CacheCircuitBreakerService"/>.
/// </summary>
public static class CacheCircuitBreakerServiceExtensions
{
    /// <summary>
    /// Checks if the circuit is currently in the Open state.
    /// </summary>
    public static bool IsOpen(this CacheCircuitBreakerService service)
    {
        return service.State == CacheCircuitState.Open;
    }

    /// <summary>
    /// Returns the time remaining until the circuit attempts to transition to the Half-Open state.
    /// Returns <see cref="TimeSpan.Zero"/> if the circuit is not Open.
    /// </summary>
    public static TimeSpan TimeUntilHalfOpen(this CacheCircuitBreakerService service)
    {
        if (service.State != CacheCircuitState.Open || !service.OpenedAtUtc.HasValue)
        {
            return TimeSpan.Zero;
        }

        var elapsed = DateTime.UtcNow - service.OpenedAtUtc.Value;
        var remaining = service.BreakDuration - elapsed;

        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// Returns a human-readable string representing the current status of the circuit breaker.
    /// </summary>
    public static string ToStatusString(this CacheCircuitBreakerService service)
    {
        var state = service.State.ToString();
        return service.State switch
        {
            CacheCircuitState.Open => $"{state} (Opened at {service.OpenedAtUtc?.ToString("O") ?? "unknown"}, Remaining: {service.TimeUntilHalfOpen():hh\\:mm\\:ss})",
            _ => state
        };
    }
}
