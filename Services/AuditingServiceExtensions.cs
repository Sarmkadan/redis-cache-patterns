using System;
using System.Collections.Generic;
using System.Linq;

namespace RedisCachePatterns.Services
{
    /// <summary>
    /// Extension methods that add useful querying capabilities to <see cref="AuditingService"/>.
    /// </summary>
    public static class AuditingServiceExtensions
    {
        /// <summary>
        /// Returns the most recent audit entries, ordered by <see cref="AuditingService.AuditEntry.Timestamp"/> descending.
        /// </summary>
        /// <param name="service">The auditing service instance.</param>
        /// <param name="count">The maximum number of entries to return. Must be greater than zero.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of the latest audit entries.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than or equal to zero.</exception>
        public static IEnumerable<AuditingService.AuditEntry> GetRecentEntries(this AuditingService service, int count)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(count, 0);

            return service
                .GetAuditLog()
                .OrderByDescending(entry => entry.Timestamp)
                .Take(count);
        }

        /// <summary>
        /// Retrieves all audit entries that were performed by a specific user.
        /// </summary>
        /// <param name="service">The auditing service instance.</param>
        /// <param name="userId">The identifier of the user. Must not be null or whitespace.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of audit entries for the given user.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="userId"/> is null or whitespace.</exception>
        public static IEnumerable<AuditingService.AuditEntry> GetEntriesByUser(this AuditingService service, string userId)
        {
            ArgumentNullException.ThrowIfNull(service);
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            return service
                .GetAuditLog()
                .Where(entry => string.Equals(entry.UserId, userId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Produces a short textual summary of the audit log, including total entry count and a breakdown by operation type.
        /// </summary>
        /// <param name="service">The auditing service instance.</param>
        /// <returns>A string summarising the audit log.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
        public static string GetAuditSummary(this AuditingService service)
        {
            ArgumentNullException.ThrowIfNull(service);

            var total = service.GetAuditLogSize();

            var breakdown = service
                .GetAuditLog()
                .GroupBy(entry => entry.OperationType)
                .Select(g => $"{g.Key}: {g.Count()}")
                .OrderBy(s => s);

            var breakdownText = string.Join(", ", breakdown);
            return $"Audit Summary – Total Entries: {total}" + (breakdown.Any() ? $" ( {breakdownText} )" : string.Empty);
        }
    }
}
