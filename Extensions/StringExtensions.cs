#nullable enable
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
    /// <summary>
    /// Truncates the string to the specified maximum length.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="maxLength">The maximum length of the resulting string.</param>
    /// <returns>The truncated string with ellipsis if longer than maxLength; otherwise the original string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxLength"/> is negative.</exception>
    public static string TruncateTo(this string value, int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..maxLength] + "...";
    }

    /// <summary>
    /// Determines whether the specified string is a valid email address.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <returns><see langword="true"/> if the string appears to be a valid email address; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// This is a basic validation that checks for the presence of '@' and '.' characters.
    /// For production use, consider using a more robust email validation library.
    /// </remarks>
    public static bool IsValidEmail(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var atIndex = value.IndexOf('@');
        if (atIndex <= 0 || atIndex >= value.Length - 1)
            return false;

        var dotIndex = value.LastIndexOf('.');
        return dotIndex > atIndex + 1 && dotIndex < value.Length - 1;
    }

    /// <summary>
    /// Converts the string to a URL-friendly slug format.
    /// </summary>
    /// <param name="value">The string to convert to a slug.</param>
    /// <returns>A URL-friendly slug representation of the input string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToUrlSlug(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-")
            .Trim('-');
    }

    /// <summary>
    /// Splits the string by the specified separator and trims whitespace from each element.
    /// </summary>
    /// <param name="value">The string to split.</param>
    /// <param name="separator">The character used to separate substrings.</param>
    /// <returns>An array of trimmed, non-empty strings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string[] SplitAndTrim(this string value, char separator = ',')
    {
        ArgumentNullException.ThrowIfNull(value);

        if (string.IsNullOrWhiteSpace(value))
            return [];


        return value
            .Split(separator)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
    }

    /// <summary>
    /// Determines whether this string equals another string, ignoring case.
    /// </summary>
    /// <param name="value">The current string instance.</param>
    /// <param name="other">The string to compare with.</param>
    /// <returns><see langword="true"/> if the strings are equal ignoring case; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool EqualsIgnoreCase(this string value, string? other)
    {
        ArgumentNullException.ThrowIfNull(value);

        return string.Equals(value, other, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Safely extracts a substring from the specified position.
    /// </summary>
    /// <param name="value">The string to extract from.</param>
    /// <param name="startIndex">The starting index.</param>
    /// <param name="length">The length of the substring, or -1 for the remainder of the string.</param>
    /// <returns>The extracted substring, or an empty string if the operation would fail.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="startIndex"/> is negative.</exception>
    public static string SafeSubstring(this string value, int startIndex, int length = -1)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);

        if (string.IsNullOrEmpty(value) || startIndex >= value.Length)
            return string.Empty;

        if (length < 0)
            return value[startIndex..];

        var actualLength = Math.Min(length, value.Length - startIndex);
        return value.Substring(startIndex, actualLength);
    }
}
