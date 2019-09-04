#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Middleware;

/// <summary>
/// Authentication middleware that validates API keys and JWTs
/// Enriches request context with user identity information
/// </summary>
public class AuthenticationMiddleware
{
    private readonly ILogger<AuthenticationMiddleware> _logger;
    private readonly HashSet<string> _validApiKeys;

    public AuthenticationMiddleware(ILogger<AuthenticationMiddleware> logger, IEnumerable<string>? validApiKeys = null)
    {
        _logger = logger;
        _validApiKeys = new HashSet<string>(validApiKeys ?? Enumerable.Empty<string>());
    }

    public async Task InvokeAsync(string authHeader, Func<AuthContext, Task> next)
    {
        var context = new AuthContext();

        try
        {
            if (string.IsNullOrEmpty(authHeader))
            {
                _logger.LogWarning("Missing authentication header");
                throw new InvalidOperationException("Authentication required");
            }

            var (scheme, credentials) = ParseAuthHeader(authHeader);

            context = scheme.ToLower() switch
            {
                "bearer" => ValidateBearer(credentials),
                "apikey" => ValidateApiKey(credentials),
                _ => throw new InvalidOperationException($"Unsupported auth scheme: {scheme}")
            };

            _logger.LogInformation("Authentication successful for user: {UserId}", context.UserId);
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Authentication failed");
            throw;
        }
    }

    private (string Scheme, string Credentials) ParseAuthHeader(string header)
    {
        var parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            throw new InvalidOperationException("Invalid authorization header format");
        return (parts[0], parts[1]);
    }

    private AuthContext ValidateBearer(string token)
    {
        // In production, validate JWT signature and claims
        // For demo, accept any Bearer token
        var claims = ExtractClaimsFromToken(token);
        return new AuthContext
        {
            UserId = claims["sub"],
            IsAuthenticated = true,
            AuthScheme = "Bearer",
            Claims = claims
        };
    }

    private AuthContext ValidateApiKey(string apiKey)
    {
        if (!_validApiKeys.Contains(apiKey))
            throw new InvalidOperationException("Invalid API key");

        return new AuthContext
        {
            UserId = apiKey,
            IsAuthenticated = true,
            AuthScheme = "ApiKey"
        };
    }

    private Dictionary<string, string> ExtractClaimsFromToken(string token)
    {
        // Simplified token parsing - in production use JWT library
        return new Dictionary<string, string>
        {
            { "sub", Guid.NewGuid().ToString() },
            { "scope", "api:read api:write" }
        };
    }
}

/// <summary>
/// Authentication context containing user identity and claims
/// </summary>
public class AuthContext
{
    public string UserId { get; set; } = string.Empty;
    public bool IsAuthenticated { get; set; }
    public string AuthScheme { get; set; } = string.Empty;
    public Dictionary<string, string> Claims { get; set; } = new();

    public bool HasClaim(string claim) => Claims.ContainsKey(claim);
    public string? GetClaim(string claim) => Claims.TryGetValue(claim, out var value) ? value : null;
}
