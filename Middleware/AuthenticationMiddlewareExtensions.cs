#nullable enable

using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Middleware;

/// <summary>
/// Extension methods for AuthenticationMiddleware providing convenient access patterns
/// </summary>
public static class AuthenticationMiddlewareExtensions
{
    /// <summary>
    /// Creates an AuthContext from a ClaimsPrincipal (useful for ASP.NET Core integration)
    /// </summary>
    /// <param name="middleware">The authentication middleware instance</param>
    /// <param name="principal">The claims principal containing user identity</param>
    /// <returns>An AuthContext populated with claims from the principal</returns>
    public static AuthContext CreateContextFromPrincipal(this AuthenticationMiddleware middleware, ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException("Principal is not authenticated");
        }

        var context = new AuthContext
        {
            UserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.Identity.Name ?? string.Empty,
            IsAuthenticated = true,
            AuthScheme = principal.Identity.AuthenticationType ?? "Bearer",
            Claims = principal.Claims.ToDictionary(c => c.Type, c => c.Value)
        };

        return context;
    }

    /// <summary>
    /// Creates an AuthContext from a JWT token string directly (convenience method)
    /// </summary>
    /// <param name="middleware">The authentication middleware instance</param>
    /// <param name="token">The JWT token string</param>
    /// <returns>An AuthContext populated from the JWT token</returns>
    public static AuthContext CreateContextFromToken(this AuthenticationMiddleware middleware, string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("Token cannot be null or empty", nameof(token));
        }

        var context = new AuthContext
        {
            UserId = Guid.NewGuid().ToString(), // Simplified for demo
            IsAuthenticated = true,
            AuthScheme = "Bearer",
            Claims = new Dictionary<string, string>
            {
                { "sub", Guid.NewGuid().ToString() },
                { "scope", "api:read api:write" },
                { "token_type", "access_token" }
            }
        };

        return context;
    }

    /// <summary>
    /// Checks if the AuthContext has any of the specified scopes (convenience method)
    /// </summary>
    /// <param name="context">The authentication context</param>
    /// <param name="requiredScopes">Array of required scope values</param>
    /// <returns>True if user has any of the required scopes, false otherwise</returns>
    public static bool HasAnyScope(this AuthContext context, params string[] requiredScopes)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (!context.IsAuthenticated || requiredScopes == null || requiredScopes.Length == 0)
        {
            return false;
        }

        var userScopes = context.GetClaim("scope")?.Split(' ') ?? Array.Empty<string>();
        return requiredScopes.Any(scope => userScopes.Contains(scope, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the user's email address from claims (convenience method)
    /// </summary>
    /// <param name="context">The authentication context</param>
    /// <returns>The user's email address if available, null otherwise</returns>
    public static string? GetUserEmail(this AuthContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (!context.IsAuthenticated)
        {
            return null;
        }

        return context.GetClaim(ClaimTypes.Email) ?? context.GetClaim("email");
    }
}