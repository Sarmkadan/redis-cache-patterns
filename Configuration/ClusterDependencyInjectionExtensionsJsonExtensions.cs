#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace RedisCachePatterns.Configuration;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="ClusterDependencyInjectionExtensions"/> type information.
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
    /// Serializes the <see cref="ClusterDependencyInjectionExtensions"/> type information to a JSON string.
    /// </summary>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the type information.</returns>
    public static string ToJson(bool indented = false)
    {
        var options = indented
            ? new JsonSerializerOptions(_jsonOptions)
            {
                WriteIndented = true,
            }
            : _jsonOptions;

        return JsonSerializer.Serialize(typeof(ClusterDependencyInjectionExtensions), options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="Type"/> representing <see cref="ClusterDependencyInjectionExtensions"/>.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized type information, or <c>null</c> if the JSON represents a null value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <c>null</c> or empty.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static Type? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        return JsonSerializer.Deserialize<Type>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="Type"/> representing <see cref="ClusterDependencyInjectionExtensions"/>.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized type information if successful; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <c>null</c> or empty.</exception>
    public static bool TryFromJson(string json, out Type? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<Type>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}