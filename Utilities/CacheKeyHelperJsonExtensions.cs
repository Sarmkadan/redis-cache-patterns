#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="CacheKeyHelper"/> configuration.
/// </summary>
public static class CacheKeyHelperJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes cache key configuration to a JSON string.
    /// </summary>
    /// <param name="separator">The separator character used in cache keys by <see cref="CacheKeyHelper"/>.</param>
    /// <param name="wildcard">The wildcard pattern used for cache key matching by <see cref="CacheKeyHelper"/>.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the cache key configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="separator"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="wildcard"/> is null.</exception>
    public static string ToJson(string separator, string wildcard, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(separator);
        ArgumentNullException.ThrowIfNull(wildcard);

        var config = new CacheKeyConfiguration(separator, wildcard);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions)
            {
                WriteIndented = true
            }
            : _jsonOptions;

        return JsonSerializer.Serialize(config, options);
    }

    /// <summary>
    /// Deserializes cache key configuration from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="CacheKeyConfiguration"/> instance, or null if the JSON is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static CacheKeyConfiguration? FromJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<CacheKeyConfiguration>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize cache key configuration from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized <see cref="CacheKeyConfiguration"/> instance if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string? json, out CacheKeyConfiguration? value)
    {
        value = null;

        if (string.IsNullOrEmpty(json))
        {
            return true;
        }

        try
        {
            value = JsonSerializer.Deserialize<CacheKeyConfiguration>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Cache key configuration record for serialization.
    /// </summary>
    public sealed record CacheKeyConfiguration(string Separator, string Wildcard);
}
