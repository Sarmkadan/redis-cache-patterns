// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.ObjectPool;
using System.Text;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Cache key generation utility providing consistent key naming conventions.
/// Uses ObjectPool&lt;StringBuilder&gt; for pattern construction so that the
/// StringBuilder buffer is reused across calls instead of re-allocated each time.
/// </summary>
public static class CacheKeyHelper
{
    private const string Separator = ":";
    private const string Wildcard = "*";

    // Shared pool so the StringBuilder internal char buffer is reused across calls.
    private static readonly ObjectPool<StringBuilder> _sbPool =
        new DefaultObjectPoolProvider().Create<StringBuilder>();

    /// <summary>
    /// Builds a cache key from a prefix and parameters using string.Create to
    /// fill the result in a single allocation.
    /// </summary>
    public static string BuildKey(string prefix, params object?[] parameters)
    {
        if (parameters.Length == 0) return prefix;

        var nonNull = parameters.Where(p => p != null).Select(p => p!.ToString()!).ToArray();
        if (nonNull.Length == 0) return prefix;

        int totalLen = prefix.Length + nonNull.Length; // separators between prefix+each param
        foreach (var s in nonNull) totalLen += s.Length;

        return string.Create(totalLen, (prefix, nonNull), static (span, state) =>
        {
            int pos = 0;
            state.prefix.AsSpan().CopyTo(span);
            pos += state.prefix.Length;
            foreach (var s in state.nonNull)
            {
                span[pos++] = ':';
                s.AsSpan().CopyTo(span[pos..]);
                pos += s.Length;
            }
        });
    }

    /// <summary>
    /// Builds cache key for entity by ID.
    /// </summary>
    public static string BuildEntityKey<T>(int id) where T : class =>
        BuildKey(typeof(T).Name.ToLowerInvariant(), "entity", id);

    /// <summary>
    /// Builds cache key for entity collection.
    /// </summary>
    public static string BuildCollectionKey<T>(string? filter = null) where T : class
    {
        var key = BuildKey(typeof(T).Name.ToLowerInvariant(), "collection");
        return string.IsNullOrEmpty(filter) ? key : $"{key}:{filter}";
    }

    /// <summary>
    /// Builds a wildcard pattern using a pooled StringBuilder to avoid per-call allocation.
    /// </summary>
    public static string BuildPattern(string prefix, params object?[] parameters)
    {
        var sb = _sbPool.Get();
        try
        {
            sb.Append(prefix);
            foreach (var p in parameters)
            {
                if (p == null) continue;
                sb.Append(Separator);
                sb.Append(p.ToString());
            }
            sb.Append(Separator).Append(Wildcard);
            return sb.ToString();
        }
        finally
        {
            sb.Clear();
            _sbPool.Return(sb);
        }
    }

    /// <summary>
    /// Builds pattern for all entities of a type.
    /// </summary>
    public static string BuildEntityPattern<T>() where T : class =>
        BuildPattern(typeof(T).Name.ToLowerInvariant(), "entity");

    /// <summary>
    /// Validates cache key format.
    /// </summary>
    public static bool IsValidKey(string key) =>
        !string.IsNullOrWhiteSpace(key)
        && key.Length <= 512
        && !key.Contains('\n')
        && !key.Contains('\r');

    /// <summary>
    /// Normalizes cache key to ensure consistency.
    /// </summary>
    public static string NormalizeKey(string key) => key.ToLowerInvariant().Trim();

    /// <summary>
    /// Extracts components from cache key.
    /// </summary>
    public static string[] ParseKey(string key) =>
        key.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// Gets the prefix from a cache key.
    /// </summary>
    public static string GetPrefix(string key)
    {
        var parts = ParseKey(key);
        return parts.Length > 0 ? parts[0] : string.Empty;
    }

    /// <summary>
    /// Creates cache key for distributed lock.
    /// </summary>
    public static string BuildLockKey(string resourceId) => $"lock:{resourceId}";

    /// <summary>
    /// Creates pattern to match all locks.
    /// </summary>
    public static string BuildLockPattern() => BuildPattern("lock");

    /// <summary>
    /// Creates cache key for temporary data.
    /// </summary>
    public static string BuildTemporaryKey(string identifier) =>
        $"temp:{identifier}:{Guid.NewGuid()}";
}
