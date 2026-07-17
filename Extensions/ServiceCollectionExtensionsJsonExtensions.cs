#nullable enable

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace RedisCachePatterns.Extensions;

/// <summary>
/// Provides System.Text.Json serialization extensions for service collection configuration patterns.
/// </summary>
public static class ServiceCollectionExtensionsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes an object to a JSON string using System.Text.Json.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this object? value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes service collection extension patterns from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A deserialized patterns instance, or null if the JSON is empty or whitespace.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static ServiceCollectionPatterns? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ServiceCollectionPatterns>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize service collection extension patterns from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized instance if successful, otherwise null.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static bool TryFromJson(string json, out ServiceCollectionPatterns? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        value = null;

        try
        {
            value = JsonSerializer.Deserialize<ServiceCollectionPatterns>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Container for service collection extension method configuration patterns.
    /// </summary>
    public sealed class ServiceCollectionPatterns
    {
        /// <summary>
        /// Gets or sets whether auditing is enabled.
        /// </summary>
        public bool AuditingEnabled { get; set; }

        /// <summary>
        /// Gets or sets whether batch processing is configured.
        /// </summary>
        public bool BatchProcessingConfigured { get; set; }

        /// <summary>
        /// Gets or sets whether idempotency is enabled.
        /// </summary>
        public bool IdempotencyEnabled { get; set; }

        /// <summary>
        /// Gets or sets whether performance monitoring is enabled.
        /// </summary>
        public bool PerformanceMonitoringEnabled { get; set; }
    }
}