#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Reflection;
using System.Text;

namespace RedisCachePatterns.Formatters;

/// <summary>
/// CSV output formatter using reflection to dynamically generate column headers
/// Handles complex types by extracting public properties
/// </summary>
public class CsvFormatter : IOutputFormatter
{
    public string ContentType => "text/csv";
    private readonly string _delimiter;

    public CsvFormatter(string delimiter = ",")
    {
        _delimiter = delimiter;
    }

    public string Format(object data)
    {
        if (data == null)
            return string.Empty;

        var type = data.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.IgnoreCase);

        if (properties.Length == 0)
            return EscapeValue(data.ToString() ?? "");

        var sb = new StringBuilder();
        foreach (var prop in properties)
        {
            sb.Append(EscapeValue(prop.Name)).Append(_delimiter);
        }

        if (sb.Length > 0)
            sb.Length--;

        sb.AppendLine();

        foreach (var prop in properties)
        {
            var value = prop.GetValue(data)?.ToString() ?? "";
            sb.Append(EscapeValue(value)).Append(_delimiter);
        }

        if (sb.Length > 0)
            sb.Length--;

        return sb.ToString();
    }

    public string Format<T>(IEnumerable<T> data)
    {
        var list = data.ToList();
        if (list.Count == 0)
            return string.Empty;

        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.IgnoreCase);
        var sb = new StringBuilder();

        // Write headers
        for (int i = 0; i < properties.Length; i++)
        {
            if (i > 0) sb.Append(_delimiter);
            sb.Append(EscapeValue(properties[i].Name));
        }

        sb.AppendLine();

        // Write rows
        foreach (var item in list)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                if (i > 0) sb.Append(_delimiter);
                var value = properties[i].GetValue(item)?.ToString() ?? "";
                sb.Append(EscapeValue(value));
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string EscapeValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(_delimiter) || value.Contains("\"") || value.Contains("\n"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }
}
