#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedisCachePatterns.Formatters;

/// <summary>
/// JSON output formatter with configurable serialization options
/// Handles both single objects and collections with proper formatting
/// </summary>
public class JsonFormatter : IOutputFormatter
{
    private readonly JsonSerializerOptions _options;

    public string ContentType => "application/json";

    public JsonFormatter(bool indent = true, JsonUnknownTypeHandling unknownHandling = JsonUnknownTypeHandling.JsonElement)
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = indent,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            UnknownTypeHandling = unknownHandling,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new JsonStringEnumConverter()
            }
        };
    }

    public string Format(object data)
    {
        try
        {
            return JsonSerializer.Serialize(data, _options);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(
                new { error = "Serialization failed", message = ex.Message },
                _options);
        }
    }

    public string Format<T>(IEnumerable<T> data)
    {
        try
        {
            var list = data.ToList();
            return JsonSerializer.Serialize(list, _options);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(
                new { error = "Serialization failed", message = ex.Message },
                _options);
        }
    }
}
