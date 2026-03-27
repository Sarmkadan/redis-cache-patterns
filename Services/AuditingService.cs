// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace RedisCachePatterns.Services;

/// <summary>
/// Auditing service for tracking and logging system operations
/// Maintains audit trail for compliance and debugging purposes
/// </summary>
public class AuditingService
{
    private readonly ILogger<AuditingService> _logger;
    private readonly List<AuditEntry> _auditLog = new();
    private readonly object _lockObject = new();
    private readonly int _maxLogEntries;

    public AuditingService(ILogger<AuditingService> logger, int maxLogEntries = 10000)
    {
        _logger = logger;
        _maxLogEntries = maxLogEntries;
    }

    public void LogOperation(string operationType, string resourceId, string? userId = null, string? details = null)
    {
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            OperationType = operationType,
            ResourceId = resourceId,
            UserId = userId ?? "system",
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        lock (_lockObject)
        {
            _auditLog.Add(entry);

            // Trim log if it exceeds max size
            if (_auditLog.Count > _maxLogEntries)
            {
                _auditLog.RemoveRange(0, _auditLog.Count - _maxLogEntries);
            }
        }

        _logger.LogInformation(
            "Audit: {OperationType} | Resource: {ResourceId} | User: {UserId}",
            operationType, resourceId, entry.UserId);
    }

    public IEnumerable<AuditEntry> GetAuditLog(string? operationType = null, string? resourceId = null, int? lastNEntries = null)
    {
        lock (_lockObject)
        {
            var query = _auditLog.AsEnumerable();

            if (!string.IsNullOrEmpty(operationType))
                query = query.Where(x => x.OperationType == operationType);

            if (!string.IsNullOrEmpty(resourceId))
                query = query.Where(x => x.ResourceId == resourceId);

            if (lastNEntries.HasValue)
                query = query.TakeLast(lastNEntries.Value);

            return query.ToList();
        }
    }

    public void ClearAuditLog()
    {
        lock (_lockObject)
        {
            _auditLog.Clear();
        }
        _logger.LogWarning("Audit log cleared");
    }

    public int GetAuditLogSize()
    {
        lock (_lockObject)
        {
            return _auditLog.Count;
        }
    }

    public class AuditEntry
    {
        public string Id { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }

        public override string ToString() =>
            $"[{Timestamp:O}] {OperationType} | Resource={ResourceId} | User={UserId}";
    }
}
