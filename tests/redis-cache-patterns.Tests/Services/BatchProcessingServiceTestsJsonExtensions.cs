#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedisCachePatterns.Services;

/// <summary>
/// Provides JSON serialization extensions for <see cref="BatchProcessingServiceTests"/>.
/// </summary>
public static class BatchProcessingServiceTestsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Serializes the <see cref="BatchProcessingServiceTests"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static string ToJson(this BatchProcessingServiceTests value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.Serialize(value, indented ? GetIndentedOptions() : _jsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="BatchProcessingServiceTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized instance, or null if the JSON is empty or whitespace.</returns>
    /// <exception cref="JsonException">Thrown if the JSON is invalid or cannot be deserialized.</exception>
    public static BatchProcessingServiceTests? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<BatchProcessingServiceTests>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="BatchProcessingServiceTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized instance if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out BatchProcessingServiceTests? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        try
        {
            value = JsonSerializer.Deserialize<BatchProcessingServiceTests>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    private static JsonSerializerOptions GetIndentedOptions()
    {
        var options = new JsonSerializerOptions(_jsonOptions)
        {
            WriteIndented = true,
        };
        return options;
    }
}
