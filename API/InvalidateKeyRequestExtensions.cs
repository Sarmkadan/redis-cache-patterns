#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cacheKey"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public static InvalidateKeyRequest WithCacheKey(
        this string cacheKey,
        InvalidationReason reason = InvalidationReason.DataUpdate,
        string source = "system")
    {
        ArgumentNullException.ThrowIfNull(cacheKey);
        ArgumentNullException.ThrowIfNull(source);

        return new InvalidateKeyRequest
        {
            CacheKey = cacheKey,
            Reason = reason,
            Source = source
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
    /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public static InvalidateKeyRequest ForUser(
        this string userId,
        string keyPrefix = "user:",
        InvalidationReason reason = InvalidationReason.DataUpdate,
        string source = "user-service")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(source);

        return new InvalidateKeyRequest
        {
            CacheKey = $"{keyPrefix}{userId}",
            Reason = reason,
            Source = source
        };
    }

    /// <summary>
    /// Creates a new <see cref="InvalidateKeyRequest"/> for a product-related cache key.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="reason">The reason for invalidation. Defaults to <see cref="InvalidationReason.DataUpdate"/>.</param>
    /// <param name="source">The name of the requesting service. Defaults to "product-service".</param>
    /// <returns>A configured <see cref="InvalidateKeyRequest"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public static InvalidateKeyRequest ForProduct(
        this int productId,
        InvalidationReason reason = InvalidationReason.DataUpdate,
        string source = "product-service")
    {
        ArgumentNullException.ThrowIfNull(source);

        return new InvalidateKeyRequest
        {
            CacheKey = $"product:{productId}",
            Reason = reason,
            Source = source
        };
    }

    /// <summary>
    /// Creates a new <see cref="InvalidateKeyRequest"/> for a session-related cache key.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="reason">The reason for invalidation. Defaults to <see cref="InvalidationReason.DataUpdate"/>.</param>
    /// <param name="source">The name of the requesting service. Defaults to "auth-service".</param>
    /// <returns>A configured <see cref="InvalidateKeyRequest"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sessionId"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public static InvalidateKeyRequest ForSession(
        this string sessionId,
        InvalidationReason reason = InvalidationReason.DataUpdate,
        string source = "auth-service")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(source);

        return new InvalidateKeyRequest
        {
            CacheKey = $"session:{sessionId}",
            Reason = reason,
            Source = source
        };
    }

    /// <summary>
    /// Updates the <see cref="InvalidationReason"/> of an existing <see cref="InvalidateKeyRequest"/>.
    /// </summary>
    /// <param name="request">The request to update.</param>
    /// <param name="reason">The new invalidation reason.</param>
    /// <returns>The updated <see cref="InvalidateKeyRequest"/> (enables method chaining).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    public static InvalidateKeyRequest WithReason(
        this InvalidateKeyRequest request,
        InvalidationReason reason)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Reason = reason;
        return request;
    }

    /// <summary>
    /// Updates the <see cref="Source"/> of an existing <see cref="InvalidateKeyRequest"/>.
    /// </summary>
    /// <param name="request">The request to update.</param>
    /// <param name="source">The new source identifier.</param>
    /// <returns>The updated <see cref="InvalidateKeyRequest"/> (enables method chaining).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source"/> is null or whitespace.</exception>
    public static InvalidateKeyRequest WithSource(
        this InvalidateKeyRequest request,
        string source)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        request.Source = source;
        return request;
    }

    /// <summary>
    /// Determines whether the <see cref="InvalidateKeyRequest"/> represents a manual purge operation.
    /// </summary>
    /// <param name="request">The request to check.</param>
    /// <returns><c>true</c> if the reason is <see cref="InvalidationReason.ManualPurge"/>; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    public static bool IsManualPurge(this InvalidateKeyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.Reason == InvalidationReason.ManualPurge;
    }

    /// <summary>
    /// Determines whether the <see cref="InvalidateKeyRequest"/> represents a data update operation.
    /// </summary>
    /// <param name="request">The request to check.</param>
    /// <returns><c>true</c> if the reason is <see cref="InvalidationReason.DataUpdate"/>; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    public static bool IsDataUpdate(this InvalidateKeyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.Reason == InvalidationReason.DataUpdate;
    }
}