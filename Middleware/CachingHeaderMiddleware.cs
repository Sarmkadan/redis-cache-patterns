// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Middleware;

/// <summary>
/// Middleware for setting cache control headers based on response type
/// Enforces cache policies at HTTP level
/// </summary>
public class CachingHeaderMiddleware
{
    private readonly ILogger<CachingHeaderMiddleware> _logger;
    private readonly Dictionary<string, CacheControlPolicy> _policies = new();

    public CachingHeaderMiddleware(ILogger<CachingHeaderMiddleware> logger)
    {
        _logger = logger;
        InitializeDefaultPolicies();
    }

    public async Task InvokeAsync(string path, Func<Task> next)
    {
        var policy = GetPolicyForPath(path);
        _logger.LogDebug("Cache policy applied: {Path} | MaxAge: {MaxAgeSeconds}s",
            path, policy.MaxAgeSeconds);

        await next();
    }

    public void RegisterPolicy(string pathPattern, CacheControlPolicy policy)
    {
        _policies[pathPattern] = policy;
        _logger.LogDebug("Cache policy registered: {Pattern}", pathPattern);
    }

    private void InitializeDefaultPolicies()
    {
        // Public, cacheable responses
        _policies["/api/products/*"] = new CacheControlPolicy { MaxAgeSeconds = 3600, IsPublic = true };
        _policies["/api/users/*"] = new CacheControlPolicy { MaxAgeSeconds = 1800, IsPublic = false };

        // Never cache administrative endpoints
        _policies["/admin/*"] = new CacheControlPolicy { NoCache = true, NoStore = true };
    }

    private CacheControlPolicy GetPolicyForPath(string path)
    {
        foreach (var (pattern, policy) in _policies)
        {
            if (MatchesPattern(path, pattern))
                return policy;
        }

        // Default: no caching
        return new CacheControlPolicy { NoCache = true };
    }

    private bool MatchesPattern(string path, string pattern)
    {
        if (pattern == path) return true;
        if (pattern.EndsWith("*"))
        {
            var prefix = pattern[..^1];
            return path.StartsWith(prefix);
        }
        return false;
    }

    public string GenerateHeaderValue(CacheControlPolicy policy)
    {
        var directives = new List<string>();

        if (policy.NoStore) directives.Add("no-store");
        if (policy.NoCache) directives.Add("no-cache");
        if (policy.IsPublic) directives.Add("public");
        if (!policy.IsPublic && policy.MaxAgeSeconds > 0) directives.Add("private");

        if (policy.MaxAgeSeconds > 0)
            directives.Add($"max-age={policy.MaxAgeSeconds}");

        if (policy.SMaxAgeSeconds > 0)
            directives.Add($"s-maxage={policy.SMaxAgeSeconds}");

        return string.Join(", ", directives);
    }
}

/// <summary>
/// Cache control policy configuration
/// </summary>
public class CacheControlPolicy
{
    public int MaxAgeSeconds { get; set; }
    public int SMaxAgeSeconds { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool NoCache { get; set; }
    public bool NoStore { get; set; }
    public bool MustRevalidate { get; set; }
}
