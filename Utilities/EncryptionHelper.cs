#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Cryptography;
using System.Text;

namespace RedisCachePatterns.Utilities;

/// <summary>
/// Encryption and hashing utilities for securing sensitive data
/// Provides SHA256 hashing and basic encryption/decryption operations
/// </summary>
public static class EncryptionHelper
{
    /// <summary>
    /// Creates SHA256 hash of input string
    /// </summary>
    public static string HashSha256(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hashedBytes);
        }
    }

    /// <summary>
    /// Verifies string against SHA256 hash
    /// </summary>
    public static bool VerifyHash(string input, string hash)
    {
        var hashOfInput = HashSha256(input);
        return hashOfInput.Equals(hash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates MD5 hash (use only for non-security-critical operations like cache keys)
    /// </summary>
    public static string HashMd5(string input)
    {
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            var hashedBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hashedBytes);
        }
    }

    /// <summary>
    /// Generates cryptographically secure random bytes
    /// </summary>
    public static byte[] GenerateRandomBytes(int length)
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            return bytes;
        }
    }

    /// <summary>
    /// Generates cryptographically secure random string
    /// </summary>
    public static string GenerateRandomString(int length = 32)
    {
        const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var bytes = GenerateRandomBytes(length);
        var result = new StringBuilder(length);

        foreach (var b in bytes)
        {
            result.Append(validChars[b % validChars.Length]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Masks sensitive data for logging (e.g., "pa****rd")
    /// </summary>
    public static string MaskSensitiveData(string data, int visibleChars = 2)
    {
        if (string.IsNullOrEmpty(data) || data.Length <= visibleChars)
            return new string('*', Math.Max(data?.Length ?? 0, 4));

        var visible = data[..visibleChars];
        var masked = new string('*', data.Length - visibleChars);
        return visible + masked;
    }
}
