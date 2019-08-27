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
                "int" => (T)(object)int.Parse(Value),
                "double" => (T)(object)double.Parse(Value),
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
        Value = typeof(T).Name.ToLower() switch
        {
            "boolean" => value?.ToString() ?? "false",
            "int32" or "int64" => value?.ToString() ?? "0",
            "double" or "single" => value?.ToString() ?? "0",
            _ => value?.ToString() ?? string.Empty
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
