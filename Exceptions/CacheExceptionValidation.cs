using System;
using System.Collections.Generic;

namespace RedisCachePatterns.Exceptions
{
    /// <summary>
    /// Provides validation methods for <see cref="CacheException"/> and its derived types.
    /// </summary>
    public static class CacheExceptionValidation
    {
        /// <summary>
        /// Validates the specified cache exception and returns a list of validation problems.
        /// </summary>
        /// <param name="value">The cache exception to validate. Cannot be null.</param>
        /// <returns>A read-only list of validation problem descriptions. Empty if validation succeeds.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this CacheException value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            // Validate ErrorCode (Base class property)
            if (string.IsNullOrWhiteSpace(value.ErrorCode))
            {
                problems.Add("ErrorCode cannot be null or empty.");
            }

            // Validate OccurredAt (Base class property) - should be in reasonable range
            var now = DateTime.UtcNow;
            if (value.OccurredAt > now.AddMinutes(5))
            {
                problems.Add("OccurredAt cannot be in the future.");
            }
            else if (value.OccurredAt < now.AddYears(-1))
            {
                problems.Add("OccurredAt appears to be too old to be valid.");
            }

            // Validate Timeout (CacheTimeoutException specific property)
            if (value is CacheTimeoutException timeoutException)
            {
                if (timeoutException.Timeout <= TimeSpan.Zero)
                {
                    problems.Add("Timeout must be a positive TimeSpan.");
                }
            }

            // Validate CacheKey (CacheKeyNotFoundException specific property)
            if (value is CacheKeyNotFoundException keyNotFoundException)
            {
                if (string.IsNullOrWhiteSpace(keyNotFoundException.CacheKey))
                {
                    problems.Add("CacheKey cannot be null or empty.");
                }
            }

            return problems;
        }

        /// <summary>
        /// Determines whether the specified cache exception is valid.
        /// </summary>
        /// <param name="value">The cache exception to check.</param>
        /// <returns>True if the exception is valid; otherwise, false.</returns>
        public static bool IsValid(this CacheException value) => value.Validate().Count == 0;

        /// <summary>
        /// Validates the specified cache exception and throws an exception if it is invalid.
        /// </summary>
        /// <param name="value">The cache exception to validate. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the exception is invalid.</exception>
        public static void EnsureValid(this CacheException value)
        {
            ArgumentNullException.ThrowIfNull(value);
            var validationProblems = value.Validate();
            if (validationProblems.Count > 0)
            {
                throw new ArgumentException($"CacheException is invalid: {string.Join("; ", validationProblems)}");
            }
        }
    }
}