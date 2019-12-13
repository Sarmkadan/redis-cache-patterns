#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// JSON serialization utilities with standard configuration for consistent behavior
/// Handles serialization, deserialization, and JSON manipulation
/// </summary>
public sealed class JsonHelper
{
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public static readonly JsonSerializerOptions IndentedOptions = new(DefaultOptions)
    {
        WriteIndented = true
    };

    /// <summary>
    /// Serializes object to JSON string
    /// </summary>
    public static string Serialize<T>(T? obj, bool indent = false) where T : class
    {
        try
        {
            var options = indent ? IndentedOptions : DefaultOptions;
            return JsonSerializer.Serialize(obj, options);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON serialization failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deserializes JSON string to object
    /// </summary>
    public static T? Deserialize<T>(string json) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON deserialization failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Safely deserializes with fallback to default value
    /// </summary>
    public static T? DeserializeSafe<T>(string json, T? defaultValue = null) where T : class
    {
        try
        {
            return Deserialize<T>(json) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Checks if string is valid JSON
    /// </summary>
    public static bool IsValidJson(string json)
    {
        try
        {
            using (var doc = JsonDocument.Parse(json))
            {
                return doc.RootElement.ValueKind != JsonValueKind.Undefined;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extracts value from JSON by path (simple dotted notation)
    /// </summary>
    public static object? GetValue(string json, string path)
    {
        try
        {
            using (var doc = JsonDocument.Parse(json))
            {
                var element = doc.RootElement;
                foreach (var segment in path.Split('.'))
                {
                    if (element.TryGetProperty(segment, out var next))
                    {
                        element = next;
                    }
                    else
                    {
                        return null;
                    }
                }
                return element.GetRawText();
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Merges two JSON objects
    /// </summary>
    public static string Merge(string json1, string json2)
    {
        try
        {
            var obj1 = Deserialize<Dictionary<string, object>>(json1) ?? new();
            var obj2 = Deserialize<Dictionary<string, object>>(json2) ?? new();

            foreach (var kvp in obj2)
            {
                obj1[kvp.Key] = kvp.Value;
            }

            return Serialize(obj1);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"JSON merge failed: {ex.Message}", ex);
        }
    }
}
