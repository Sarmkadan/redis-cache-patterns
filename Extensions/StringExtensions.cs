// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Extensions;

/// <summary>
/// Extension methods for string operations
/// </summary>
public static class StringExtensions
{
    public static string TruncateTo(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;
        return value.Substring(0, maxLength) + "...";
    }

    public static bool IsValidEmail(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return value.Contains("@") && value.Contains(".");
    }

    public static string ToUrlSlug(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value
            .ToLower()
            .Replace(" ", "-")
            .Replace("--", "-")
            .Trim('-');
    }

    public static string[] SplitAndTrim(this string value, char separator = ',')
    {
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<string>();

        return value
            .Split(separator)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
    }

    public static bool EqualsIgnoreCase(this string value, string? other)
    {
        return string.Equals(value, other, StringComparison.OrdinalIgnoreCase);
    }

    public static string SafeSubstring(this string value, int startIndex, int length = -1)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (startIndex >= value.Length)
            return string.Empty;

        if (length < 0)
            return value.Substring(startIndex);

        var actualLength = Math.Min(length, value.Length - startIndex);
        return value.Substring(startIndex, actualLength);
    }
}
