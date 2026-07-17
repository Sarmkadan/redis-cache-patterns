#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace RedisCachePatterns.Configuration;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="ClusterConfiguration"/> objects.
/// </summary>
public static class ClusterDependencyInjectionExtensionsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    /// <summary>
    /// Serializes a <see cref="ClusterConfiguration"/> instance to a JSON string.
    /// </summary>
    /// <param name="configuration">The cluster configuration to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the cluster configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is <c>null</c>.</exception>
    public static string ToJson(ClusterConfiguration configuration, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions)
            { WriteIndented = true, }
            : _jsonOptions;

        return JsonSerializer.Serialize(configuration, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="ClusterConfiguration"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized cluster configuration.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <c>null</c> or empty.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static ClusterConfiguration FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        return JsonSerializer.Deserialize<ClusterConfiguration>(json, _jsonOptions)
            ?? throw new JsonException("Deserialized JSON resulted in null configuration.");
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="ClusterConfiguration"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized cluster configuration if successful; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <c>null</c> or empty.</exception>
    public static bool TryFromJson(string json, out ClusterConfiguration? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<ClusterConfiguration>(json, _jsonOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}