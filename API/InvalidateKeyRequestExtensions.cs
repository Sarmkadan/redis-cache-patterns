#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using RedisCachePatterns.Domain;

namespace RedisCachePatterns.API;

/// <summary>
/// Provides extension methods for <see cref="InvalidateKeyRequest"/> to facilitate common
/// invalidation scenarios and simplify request construction.
/// </summary>
public static class InvalidateKeyRequestExtensions
{
    /// <summary>
    /// Creates a new <see cref="InvalidateKeyRequest"/> with the specified cache key.
    /// </summary>
    /// <param name="cacheKey">The exact Redis key to invalidate.</param>
    /// <param name="reason">The reason for invalidation. Defaults to <see cref="InvalidationReason.DataUpdate"/>.</param>
    /// <param name="source">The name of the requesting service. Defaults to "system".</param>
    /// <returns>A configured <see cref="InvalidateKeyRequest"/> instance.</returns>
    public static InvalidateKeyRequest WithCacheKey(
        this string cacheKey,
        InvalidationReason reason = InvalidationReason.DataUpdate,
        string source = "system")
    {
        return new InvalidateKeyRequest
        {
            CacheKey = cacheKey ?? throw new ArgumentNullException(nameof(cacheKey)),
            Reason = reason,
            Source = source ?? throw new ArgumentNullException(nameof(source))
        };
    }

    /// <summary>
    /// Creates a new <see cref="InvalidateKeyRequest"/> for a user-related cache key.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="keyPrefix">The cache key prefix (e.g., "user:", "session:"). Defaults to "user:".</param>
    /// <param name="reason">The reason for invalidation. Defaults to <see cref="InvalidationReason.DataUpdate"/>.</param>
    /// <param name="source">The name of the requesting service. Defaults to "user-service".</param>
    /// <returns>A configured <see cref="InvalidateKeyRequest"/> instance.</returns>
    public static InvalidateKeyRequest ForUser(
        this string userId,
        string keyPrefix = "user:",
        InvalidationReason reason = InvalidationReason.DataUpdate,
        string source = "user-service")
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or whitespace.", nameof(userId));
        }

        return new InvalidateKeyRequest
        {
            CacheKey = $"{keyPrefix}{userId}",
            Reason = reason,
            Source = source ?? throw new ArgumentNullException(nameof(source))
        };
    }

    /// <summary>
    /// Creates a new <see cref="InvalidateKeyRequest"/> for a product-related cache key.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="reason">The reason for invalidation. Defaults to <see cref="InvalidationReason.DataUpdate"/>.</param>
    /// <param name="source">The name of the requesting service. Defaults to "product-service".</param>
    /// <returns>A configured <see cref="InvalidateKeyRequest"/> instance.</returns>
    public static InvalidateKeyRequest ForProduct(
        this int productId,
        InvalidationReason reason = InvalidationReason.DataUpdate,
        string source = "product-service")
    {
        return new InvalidateKeyRequest
        {
            CacheKey = $"product:{productId}",
            Reason = reason,
            Source = source ?? throw new ArgumentNullException(nameof(source))
        };
    }

    /// <summary>
    /// Creates a new <see cref="InvalidateKeyRequest"/> for a session-related cache key.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="reason">The reason for invalidation. Defaults to <see cref="InvalidationReason.DataUpdate"/>.</param>
    /// <param name="source">The name of the requesting service. Defaults to "auth-service".</param>
    /// <returns>A configured <see cref="InvalidateKeyRequest"/> instance.</returns>
    public static InvalidateKeyRequest ForSession(
        this string sessionId,
        InvalidationReason reason = InvalidationReason.DataUpdate,
        string source = "auth-service")
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));
        }

        return new InvalidateKeyRequest
        {
            CacheKey = $"session:{sessionId}",
            Reason = reason,
            Source = source ?? throw new ArgumentNullException(nameof(source))
        };
    }

    /// <summary>
    /// Updates the <see cref="InvalidationReason"/> of an existing <see cref="InvalidateKeyRequest"/>.
    /// </summary>
    /// <param name="request">The request to update.</param>
    /// <param name="reason">The new invalidation reason.</param>
    /// <returns>The updated <see cref="InvalidateKeyRequest"/> (enables method chaining).</returns>
    public static InvalidateKeyRequest WithReason(
        this InvalidateKeyRequest request,
        InvalidationReason reason)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        request.Reason = reason;
        return request;
    }

    /// <summary>
    /// Updates the <see cref="Source"/> of an existing <see cref="InvalidateKeyRequest"/>.
    /// </summary>
    /// <param name="request">The request to update.</param>
    /// <param name="source">The new source identifier.</param>
    /// <returns>The updated <see cref="InvalidateKeyRequest"/> (enables method chaining).</returns>
    public static InvalidateKeyRequest WithSource(
        this InvalidateKeyRequest request,
        string source)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source cannot be null or whitespace.", nameof(source));
        }

        request.Source = source;
        return request;
    }

    /// <summary>
    /// Determines whether the <see cref="InvalidateKeyRequest"/> represents a manual purge operation.
    /// </summary>
    /// <param name="request">The request to check.</param>
    /// <returns><c>true</c> if the reason is <see cref="InvalidationReason.ManualPurge"/>; otherwise, <c>false</c>.</returns>
    public static bool IsManualPurge(this InvalidateKeyRequest request)
    {
        return request?.Reason == InvalidationReason.ManualPurge;
    }

    /// <summary>
    /// Determines whether the <see cref="InvalidateKeyRequest"/> represents a data update operation.
    /// </summary>
    /// <param name="request">The request to check.</param>
    /// <returns><c>true</c> if the reason is <see cref="InvalidationReason.DataUpdate"/>; otherwise, <c>false</c>.</returns>
    public static bool IsDataUpdate(this InvalidateKeyRequest request)
    {
        return request?.Reason == InvalidationReason.DataUpdate;
    }
}