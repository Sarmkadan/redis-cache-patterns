# StringExtensions

Provides common string manipulation and validation utilities for scenarios such as caching, URL generation, and input sanitization.

## API

### `public static string TruncateTo(string input, int maxLength)`

Truncates the input string to the specified maximum length and appends an ellipsis if truncation occurs.

- **Parameters**
  - `input`: The string to truncate. If `null`, returns `null`.
  - `maxLength`: The maximum allowed length of the resulting string. Must be non-negative.
- **Return value**: The truncated string, or the original string if its length is less than or equal to `maxLength`. If `maxLength` is zero, returns an empty string.
- **Exceptions**: Throws `ArgumentOutOfRangeException` if `maxLength` is negative.

---

### `public static bool IsValidEmail(string email)`

Determines whether the specified string is a valid email address.

- **Parameters**
  - `email`: The string to validate. If `null`, returns `false`.
- **Return value**: `true` if the string conforms to a basic email format; otherwise, `false`.
- **Exceptions**: None.

---

### `public static string ToUrlSlug(string input)`

Converts the input string into a URL-friendly slug by normalizing whitespace, removing diacritics, and replacing spaces with hyphens.

- **Parameters**
  - `input`: The string to convert. If `null`, returns `null`.
- **Return value**: A lowercase slug with non-alphanumeric characters removed and spaces replaced by hyphens.
- **Exceptions**: None.

---

### `public static string[] SplitAndTrim(string input, char separator)`

Splits the input string by the specified separator, trims whitespace from each resulting substring, and returns the array of non-empty strings.

- **Parameters**
  - `input`: The string to split. If `null`, returns an empty array.
  - `separator`: The character used to split the string.
- **Return value**: An array of trimmed substrings. Empty substrings are omitted.
- **Exceptions**: None.

---
### `public static bool EqualsIgnoreCase(string a, string b)`

Compares two strings for equality, ignoring case.

- **Parameters**
  - `a`: The first string to compare. If `null`, treated as an empty string.
  - `b`: The second string to compare. If `null`, treated as an empty string.
- **Return value**: `true` if the strings are equal ignoring case; otherwise, `false`.
- **Exceptions**: None.

---
### `public static string SafeSubstring(string input, int startIndex, int length)`

Returns a substring of the input string starting at `startIndex` with the specified `length`, or an empty string if the operation would otherwise fail.

- **Parameters**
  - `input`: The string to substring. If `null`, returns `null`.
  - `startIndex`: The zero-based starting character position.
  - `length`: The number of characters to return.
- **Return value**: The substring, or `null` if `input` is `null`. If the substring operation would fail, returns an empty string.
- **Exceptions**: None.

## Usage
