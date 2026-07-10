using System;
using System.Collections.Generic;

namespace RedisCachePatterns.Exceptions
{
    public static class CacheExceptionValidation
    {
        public static IReadOnlyList<string> Validate(this CacheException value)
        {
            var problems = new List<string>();

            if (value == null)
            {
                problems.Add("Exception instance is null.");
                return problems;
            }

            // Validate ErrorCode (Base class property)
            if (string.IsNullOrWhiteSpace(value.ErrorCode))
            {
                problems.Add("ErrorCode cannot be null or empty.");
            }

            // Validate OccurredAt (Base class property)
            if (value.OccurredAt == default(DateTime))
            {
                problems.Add("OccurredAt cannot be the default DateTime value.");
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

        public static bool IsValid(this CacheException value)
        {
            var validationProblems = value.Validate();
            return validationProblems.Count == 0;
        }

        public static void EnsureValid(this CacheException value)
        {
            var validationProblems = value.Validate();
            if (validationProblems.Count > 0)
            {
                throw new ArgumentException($"CacheException is invalid: {string.Join("; ", validationProblems)}");
            }
        }
    }
}
