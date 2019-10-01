#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Formatters;

/// <summary>
/// Base abstraction for output formatting supporting multiple formats
/// Provides strategy pattern for different serialization implementations
/// </summary>
public interface IOutputFormatter
{
    string Format(object data);
    string Format<T>(IEnumerable<T> data);
    string ContentType { get; }
}

/// <summary>
/// Registry for managing multiple output formatters
/// </summary>
public class FormatterRegistry
{
    private readonly Dictionary<string, IOutputFormatter> _formatters = new();

    public FormatterRegistry RegisterFormatter(string format, IOutputFormatter formatter)
    {
        _formatters[format.ToLower()] = formatter;
        return this;
    }

    public IOutputFormatter GetFormatter(string format)
    {
        if (_formatters.TryGetValue(format.ToLower(), out var formatter))
            return formatter;
        return _formatters["json"] ?? throw new InvalidOperationException("No default JSON formatter registered");
    }

    public bool HasFormatter(string format) => _formatters.ContainsKey(format.ToLower());

    public IEnumerable<string> GetAvailableFormats() => _formatters.Keys;
}

/// <summary>
/// Response wrapper supporting multiple output formats
/// </summary>
public class FormattedResponse<T>
{
    public T Data { get; set; }
    public string Format { get; set; }
    public DateTime GeneratedAt { get; set; }

    public FormattedResponse(T data, string format = "json")
    {
        Data = data;
        Format = format;
        GeneratedAt = DateTime.UtcNow;
    }

    public override string ToString() => $"[{Format}] {Data}";
}
