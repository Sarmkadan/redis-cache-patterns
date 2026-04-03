#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Helper for JSON serialization with consistent options
/// </summary>
public static class SerializationHelper
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static string Serialize<T>(T value, bool pretty = false)
    {
        try
        {
            return JsonSerializer.Serialize(value, pretty ? PrettyOptions : DefaultOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Serialization failed for type {typeof(T).Name}", ex);
        }
    }

    public static T? Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Deserialization failed for type {typeof(T).Name}", ex);
        }
    }

    public static object? Deserialize(string json, Type type)
    {
        try
        {
            return JsonSerializer.Deserialize(json, type, DefaultOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Deserialization failed for type {type.Name}", ex);
        }
    }
}
