// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Cache key generation utility providing consistent key naming conventions
/// Helps prevent key collision and makes debugging easier
/// </summary>
public static class CacheKeyHelper
{
    private const string Separator = ":";
    private const string Wildcard = "*";

    /// <summary>
    /// Builds cache key with prefix and parameters
    /// </summary>
    public static string BuildKey(string prefix, params object?[] parameters)
    {
        var parts = new List<string> { prefix };
        parts.AddRange(parameters.Where(p => p != null).Select(p => p!.ToString() ?? ""));
        return string.Join(Separator, parts);
    }

    /// <summary>
    /// Builds cache key for entity by ID
    /// </summary>
    public static string BuildEntityKey<T>(int id) where T : class
    {
        return BuildKey(typeof(T).Name.ToLower(), "entity", id);
    }

    /// <summary>
    /// Builds cache key for entity collection
    /// </summary>
    public static string BuildCollectionKey<T>(string? filter = null) where T : class
    {
        var key = BuildKey(typeof(T).Name.ToLower(), "collection");
        return string.IsNullOrEmpty(filter) ? key : $"{key}:{filter}";
    }

    /// <summary>
    /// Builds pattern for wildcard matching
    /// </summary>
    public static string BuildPattern(string prefix, params object?[] parameters)
    {
        if (parameters.Length == 0)
            return $"{prefix}{Separator}{Wildcard}";

        var parts = new List<string> { prefix };
        parts.AddRange(parameters.Where(p => p != null).Select(p => p!.ToString() ?? ""));
        return string.Join(Separator, parts) + Separator + Wildcard;
    }

    /// <summary>
    /// Builds pattern for all entities of a type
    /// </summary>
    public static string BuildEntityPattern<T>() where T : class
    {
        return BuildPattern(typeof(T).Name.ToLower(), "entity");
    }

    /// <summary>
    /// Validates cache key format
    /// </summary>
    public static bool IsValidKey(string key)
    {
        return !string.IsNullOrWhiteSpace(key) && key.Length <= 512 && !key.Contains('\n') && !key.Contains('\r');
    }

    /// <summary>
    /// Normalizes cache key to ensure consistency
    /// </summary>
    public static string NormalizeKey(string key)
    {
        return key.ToLower().Trim();
    }

    /// <summary>
    /// Extracts components from cache key
    /// </summary>
    public static string[] ParseKey(string key)
    {
        return key.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Gets the prefix from a cache key
    /// </summary>
    public static string GetPrefix(string key)
    {
        var parts = ParseKey(key);
        return parts.Length > 0 ? parts[0] : string.Empty;
    }

    /// <summary>
    /// Creates cache key for distributed lock
    /// </summary>
    public static string BuildLockKey(string resourceId)
    {
        return BuildKey("lock", resourceId);
    }

    /// <summary>
    /// Creates pattern to match all locks
    /// </summary>
    public static string BuildLockPattern()
    {
        return BuildPattern("lock");
    }

    /// <summary>
    /// Creates cache key for temporary data
    /// </summary>
    public static string BuildTemporaryKey(string identifier)
    {
        return BuildKey("temp", identifier, Guid.NewGuid());
    }
}
