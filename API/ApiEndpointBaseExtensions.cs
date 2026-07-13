#nullable enable
using System;

namespace RedisCachePatterns.API;

/// <summary>
/// Extension methods that simplify creating <see cref="ApiResponse{T}"/> instances from an <see cref="ApiEndpointBase"/>.
/// </summary>
public static class ApiEndpointBaseExtensions
{
    /// <summary>
    /// Creates a successful <see cref="ApiResponse{T}"/> containing the supplied <paramref name="data"/>.
    /// </summary>
    /// <typeparam name="T">The type of the response payload.</typeparam>
    /// <param name="endpoint">The endpoint instance used as the extension target.</param>
    /// <param name="data">The payload to include in the response.</param>
    /// <returns>An <see cref="ApiResponse{T}"/> marked as successful.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="endpoint"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <c>null</c>.</exception>
    public static ApiResponse<T> ToSuccessResponse<T>(this ApiEndpointBase endpoint, T data)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(data);
        return ApiResponse<T>.Success(data);
    }

    /// <summary>
    /// Creates a failure <see cref="ApiResponse{T}"/> with the specified <paramref name="error"/> message
    /// and optional <paramref name="statusCode"/>.
    /// </summary>
    /// <typeparam name="T">The type of the response payload.</typeparam>
    /// <param name="endpoint">The endpoint instance used as the extension target.</param>
    /// <param name="error">The error message describing the failure.</param>
    /// <param name="statusCode">The HTTP status code to associate with the failure. Defaults to 500.</param>
    /// <returns>An <see cref="ApiResponse{T}"/> representing a failed operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="endpoint"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="error"/> is <c>null</c> or empty.</exception>
    public static ApiResponse<T> ToFailureResponse<T>(this ApiEndpointBase endpoint, string error, int statusCode = 500)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrEmpty(error);
        return ApiResponse<T>.Failure(error, statusCode);
    }

    /// <summary>
    /// Creates an unauthorized <see cref="ApiResponse{T}"/> with an optional custom <paramref name="error"/> message.
    /// </summary>
    /// <typeparam name="T">The type of the response payload.</typeparam>
    /// <param name="endpoint">The endpoint instance used as the extension target.</param>
    /// <param name="error">The error message; defaults to “Unauthorized”.</param>
    /// <returns>An <see cref="ApiResponse{T}"/> representing an unauthorized result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="endpoint"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="error"/> is <c>null</c> or empty.</exception>
    public static ApiResponse<T> ToUnauthorizedResponse<T>(this ApiEndpointBase endpoint, string error = "Unauthorized")
        where T : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrEmpty(error);
        return ApiResponse<T>.Unauthorized(error);
    }

    /// <summary>
    /// Creates a not‑found <see cref="ApiResponse{T}"/> with an optional custom <paramref name="error"/> message.
    /// </summary>
    /// <typeparam name="T">The type of the response payload.</typeparam>
    /// <param name="endpoint">The endpoint instance used as the extension target.</param>
    /// <param name="error">The error message; defaults to “Not found”.</param>
    /// <returns>An <see cref="ApiResponse{T}"/> representing a not‑found result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="endpoint"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="error"/> is <c>null</c> or empty.</exception>
    public static ApiResponse<T> ToNotFoundResponse<T>(this ApiEndpointBase endpoint, string error = "Not found")
        where T : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrEmpty(error);
        return ApiResponse<T>.NotFound(error);
    }
}
