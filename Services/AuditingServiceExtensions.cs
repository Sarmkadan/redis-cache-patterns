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
        /// <param name="count">The maximum number of entries to return.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of the latest audit entries.</returns>
        public static IEnumerable<AuditingService.AuditEntry> GetRecentEntries(this AuditingService service, int count)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (count <= 0) return Enumerable.Empty<AuditingService.AuditEntry>();

            return service
                .GetAuditLog()
                .OrderByDescending(entry => entry.Timestamp)
                .Take(count);
        }

        /// <summary>
        /// Retrieves all audit entries that were performed by a specific user.
        /// </summary>
        /// <param name="service">The auditing service instance.</param>
        /// <param name="userId">The identifier of the user.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of audit entries for the given user.</returns>
        public static IEnumerable<AuditingService.AuditEntry> GetEntriesByUser(this AuditingService service, string userId)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId cannot be null or whitespace.", nameof(userId));

            return service
                .GetAuditLog()
                .Where(entry => string.Equals(entry.UserId, userId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Produces a short textual summary of the audit log, including total entry count and a breakdown by operation type.
        /// </summary>
        /// <param name="service">The auditing service instance.</param>
        /// <returns>A string summarising the audit log.</returns>
        public static string GetAuditSummary(this AuditingService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

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
