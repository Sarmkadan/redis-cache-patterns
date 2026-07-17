#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace RedisCachePatterns.Utilities.JsonExtensions;

/// <summary>
/// Instance-based cache key builder for JSON serialization support.
/// </summary>
[JsonSerializable(typeof(CacheKeyBuilder))]
public sealed class CacheKeyBuilder
{
    [JsonInclude]
    private List<string> _parts = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheKeyBuilder"/> class.
    /// </summary>
    public CacheKeyBuilder()
    {
    }

    /// <summary>
    /// Adds a part to the cache key.
    /// </summary>
    /// <param name="part">The part to add.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="part"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="part"/> is empty or whitespace.</exception>
    public CacheKeyBuilder Add(string part)
    {
        ArgumentException.ThrowIfNullOrEmpty(part);
        _parts.Add(part);
        return this;
    }

    /// <summary>
    /// Adds a formatted part to the cache key.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="args">The format arguments.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="format"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="format"/> is empty or whitespace.</exception>
    public CacheKeyBuilder AddFormat(string format, params object?[] args)
    {
        ArgumentException.ThrowIfNullOrEmpty(format);
        _parts.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args));
        return this;
    }

    /// <summary>
    /// Builds the final cache key.
    /// </summary>
    /// <returns>The constructed cache key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the builder is in an invalid state.</exception>
    public string Build()
    {
        if (_parts.Count == 0)
            return string.Empty;

        if (_parts.Count == 1)
            return _parts[0];

        int totalLen = _parts.Count - 1; // one separator between each segment
        for (int i = 0; i < _parts.Count; i++)
        {
            totalLen += _parts[i].Length;
        }

        return string.Create(totalLen, _parts, static (span, parts) =>
        {
            int pos = 0;
            for (int i = 0; i < parts.Count; i++)
            {
                if (i > 0) span[pos++] = ':';
                parts[i].AsSpan().CopyTo(span[pos..]);
                pos += parts[i].Length;
            }
        });
    }

    /// <summary>
    /// Returns a string representation of the builder (the cache key).
    /// </summary>
    /// <returns>The constructed cache key.</returns>
    public override string ToString() => Build();
}

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="CacheKeyBuilder"/>.
/// </summary>
public static class CacheKeyBuilderJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        WriteIndented = false
    };

    /// <summary>
    /// Serializes the <see cref="CacheKeyBuilder"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this CacheKeyBuilder value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="CacheKeyBuilder"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized instance, or null if the JSON represents a null value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null, empty, or whitespace.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static CacheKeyBuilder? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize<CacheKeyBuilder>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="CacheKeyBuilder"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized instance if successful; otherwise, null.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or whitespace.</exception>
    public static bool TryFromJson(string json, out CacheKeyBuilder? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        try
        {
            value = JsonSerializer.Deserialize<CacheKeyBuilder>(json, _jsonOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
