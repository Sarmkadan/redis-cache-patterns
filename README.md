// ... (rest of the file remains the same)

## AuditingService

The `AuditingService` class provides a centralized logging mechanism for tracking system operations. It maintains an audit trail of events, allowing for easy retrieval and analysis of historical data. The service can be used to log various types of operations, including user actions and system events.

### Usage Example

```csharp
using Microsoft.Extensions.Logging;
using RedisCachePatterns.Services;

// Create logger (typically from DI container)
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<AuditingService>();

// Create auditing service
var auditingService = new AuditingService(logger);

// Log an operation
auditingService.LogOperation("User login", "user123", "John Doe");

// Retrieve audit log entries
var auditLog = auditingService.GetAuditLog("User login");

// Clear audit log
auditingService.ClearAuditLog();

// Get audit log size
var logSize = auditingService.GetAuditLogSize();
