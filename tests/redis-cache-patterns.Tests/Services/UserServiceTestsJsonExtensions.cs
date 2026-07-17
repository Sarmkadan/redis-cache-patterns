#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedisCachePatterns.Tests.Services;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="UserServiceTests"/>.
/// These methods enable serialization and deserialization of test fixtures for snapshot testing,
/// test data persistence, and test fixture exchange between test runs.
/// </summary>
public static class UserServiceTestsJsonExtensions
{
    /// <summary>
    /// Gets the JSON serialization options configured for camelCase property naming and proper handling of test data.
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// Serializes the <see cref="UserServiceTests"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The test fixture instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the test fixture.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this UserServiceTests value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="UserServiceTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="UserServiceTests"/> instance, or null if JSON is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static UserServiceTests? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<UserServiceTests>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="UserServiceTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized instance if successful; otherwise, null.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out UserServiceTests? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        value = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                value = JsonSerializer.Deserialize<UserServiceTests>(json, _jsonOptions);
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
