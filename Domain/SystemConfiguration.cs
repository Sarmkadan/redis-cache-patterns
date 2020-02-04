#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Domain;

/// <summary>
/// System-wide configuration settings and feature flags
/// </summary>
public class SystemConfiguration
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DataType { get; set; } = "string"; // string, int, bool, json
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string Category { get; set; } = "general";

    public T GetValue<T>()
    {
        try
        {
            return DataType.ToLower() switch
            {
                "bool" => (T)(object)bool.Parse(Value),
                "int" => (T)(object)int.Parse(Value, System.Globalization.CultureInfo.InvariantCulture),
                "double" => (T)(object)double.Parse(Value, System.Globalization.CultureInfo.InvariantCulture),
                "json" => System.Text.Json.JsonSerializer.Deserialize<T>(Value) ?? throw new InvalidOperationException(),
                _ => (T)(object)Value
            };
        }
        catch
        {
            throw new InvalidCastException($"Cannot convert '{Value}' to type {typeof(T).Name}");
        }
    }

    public void SetValue<T>(T value)
    {
        Value = value switch
        {
            null => typeof(T).Name.ToLower() switch
            {
                "boolean" => "false",
                "int32" or "int64" or "double" or "single" => "0",
                _ => string.Empty
            },
            IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetJsonValue<T>(T value)
    {
        DataType = "json";
        Value = System.Text.Json.JsonSerializer.Serialize(value);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Enable()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public override string ToString() => $"{Key}: {Value} ({DataType})";
}
