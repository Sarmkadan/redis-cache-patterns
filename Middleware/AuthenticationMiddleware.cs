#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Claims;
using System.Text;
using System.Text.Json;
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

    /// <summary>
    /// Parses a bearer token directly and builds an AuthContext from its claims,
    /// without requiring a full "Bearer &lt;token&gt;" authorization header.
    /// </summary>
    /// <param name="token">The raw JWT token string.</param>
    /// <exception cref="ArgumentException"><paramref name="token"/> is null or whitespace</exception>
    /// <exception cref="InvalidOperationException">the token is not a well-formed JWT or is missing required claims</exception>
    public AuthContext CreateContextFromBearerToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return ValidateBearer(token);
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
        // Note: this parses and reads the JWT payload but does not verify the
        // signature. Signature verification requires the issuer's signing key
        // and is out of scope for this cache-patterns sample.
        var claims = ExtractClaimsFromToken(token);
        if (!claims.TryGetValue("sub", out var subject) || string.IsNullOrEmpty(subject))
            throw new InvalidOperationException("Token is missing required 'sub' claim");

        return new AuthContext
        {
            UserId = subject,
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
        var segments = token.Split('.');
        if (segments.Length != 3)
            throw new InvalidOperationException("Invalid JWT format: expected 3 dot-separated segments");

        string payloadJson;
        try
        {
            payloadJson = Encoding.UTF8.GetString(DecodeBase64Url(segments[1]));
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Invalid JWT payload encoding", ex);
        }

        using var document = JsonDocument.Parse(payloadJson);
        var claims = new Dictionary<string, string>();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            claims[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                _ => property.Value.GetRawText()
            };
        }

        return claims;
    }

    private static byte[] DecodeBase64Url(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        var padding = (4 - base64.Length % 4) % 4;
        base64 = base64.PadRight(base64.Length + padding, '=');
        return Convert.FromBase64String(base64);
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
