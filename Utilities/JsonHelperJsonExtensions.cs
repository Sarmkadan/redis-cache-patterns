#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Extension methods for JsonHelper to provide fluent JSON serialization API
/// </summary>
public static class JsonHelperJsonExtensions
{
    private static readonly JsonSerializerOptions s_options = new(JsonHelper.DefaultOptions)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serializes the JsonHelper instance to a JSON string
    /// </summary>
    /// <param name="value">The JsonHelper instance to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>JSON string representation of the JsonHelper</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static string ToJson(this JsonHelper value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented ? JsonHelper.IndentedOptions : s_options;
        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a JsonHelper instance
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <returns>Deserialized JsonHelper instance, or null if JSON is invalid</returns>
    /// <exception cref="ArgumentException">Thrown when json is null or empty</exception>
    public static JsonHelper? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            return JsonSerializer.Deserialize<JsonHelper>(json, s_options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a JsonHelper instance
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <param name="value">Output parameter for the deserialized JsonHelper</param>
    /// <returns>True if deserialization succeeded, false otherwise</returns>
    /// <exception cref="ArgumentException">Thrown when json is null or empty</exception>
    public static bool TryFromJson(string json, out JsonHelper? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<JsonHelper>(json, s_options);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}