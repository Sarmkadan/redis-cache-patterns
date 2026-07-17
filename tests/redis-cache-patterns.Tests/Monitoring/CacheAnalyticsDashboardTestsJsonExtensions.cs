#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using RedisCachePatterns.Monitoring;

namespace RedisCachePatterns.Tests.Monitoring;

/// <summary>
/// Provides System.Text.Json serialization/deserialization extensions for <see cref="CacheAnalyticsDashboardTests"/>.
/// </summary>
/// <remarks>
/// This class ensures consistent JSON serialization behavior for <see cref="CacheAnalyticsDashboardTests"/> instances
/// across test scenarios, using camelCase property naming and web defaults.
/// </remarks>
public static class CacheAnalyticsDashboardTestsJsonExtensions
{
    /// <summary>
    /// Options used for all JSON serialization operations in this class.
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a <see cref="CacheAnalyticsDashboardTests"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this CacheAnalyticsDashboardTests value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="CacheAnalyticsDashboardTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize. Empty or whitespace strings return <see langword="null"/>.</param>
    /// <returns>The deserialized instance, or <see langword="null"/> if the JSON is empty or whitespace.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static CacheAnalyticsDashboardTests? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<CacheAnalyticsDashboardTests>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="CacheAnalyticsDashboardTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized instance, or <see langword="null"/> if deserialization fails.</param>
    /// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
    public static bool TryFromJson(string json, out CacheAnalyticsDashboardTests? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            value = JsonSerializer.Deserialize<CacheAnalyticsDashboardTests>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}