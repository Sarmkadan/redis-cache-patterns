# AuditingService

The `AuditingService` component provides a simple in‑memory mechanism for recording and retrieving audit entries. It enables applications to capture operational events (such as reads, writes, or deletions) together with contextual information like the actor, target resource, and optional details, and to query or clear the accumulated log as needed.

## API

### AuditingService()
Initializes a new instance of the `AuditingService` class with an empty internal audit log.  
- **Parameters:** none  
- **Return value:** none (constructor)  
- **Exceptions:** none  

### void LogOperation()
Records a single audit entry using the current values of the `Id`, `OperationType`, `ResourceId`, `UserId`, `Details`, and `Timestamp` properties.  
- **Parameters:** none  
- **Return value:** none  
- **Exceptions:**  
  - `InvalidOperationException` – if `Id`, `OperationType`, `ResourceId`, or `UserId` is `null` or empty when the method is invoked.  

### IEnumerable<AuditEntry> GetAuditLog()
Returns a snapshot of all audit entries that have been logged since the service was instantiated or last cleared.  
- **Parameters:** none  
- **Return value:** an `IEnumerable<AuditEntry>` representing the immutable log entries; enumeration does not expose the internal collection for modification.  
- **Exceptions:** none  

### void ClearAuditLog()
Removes all audit entries from the internal log.  
- **Parameters:** none  
- **Return value:** none  
- **Exceptions:** none  

### int GetAuditLogSize()
Gets the number of audit entries currently stored in the internal log.  
- **Parameters:** none  
- **Return value:** the count of logged entries as an `Int32`.  
- **Exceptions:** none  

### string Id
Gets or sets the identifier associated with the audit entry.  
- **Return value:** the identifier string.  
- **Exceptions:**  
  - `ArgumentNullException` – when setting the property to `null`.  

### string OperationType
Gets or sets the type of operation being audited (e.g., "Read", "Write", "Delete").  
- **Return value:** the operation type string.  
- **Exceptions:**  
  - `ArgumentNullException` – when setting the property to `null`.  

### string ResourceId
Gets or sets the identifier of the resource that the operation targets.  
- **Return value:** the resource identifier string.  
- **Exceptions:**  
  - `ArgumentNullException` – when setting the property to `null`.  

### string UserId
Gets or sets the identifier of the user or service principal that performed the operation.  
- **Return value:** the user identifier string.  
- **Exceptions:**  
  - `ArgumentNullException` – when setting the property to `null`.  

### string? Details
Gets or sets optional supplemental information about the operation. May be `null`.  
- **Return value:** the details string or `null` if none was provided.  
- **Exceptions:** none  

### DateTime Timestamp
Gets or sets the date and time (in UTC) when the operation occurred.  
- **Return value:** a `DateTime` value.  
- **Exceptions:** none  

### override string ToString()
Provides a human‑readable representation of the current audit entry, including `Id`, `OperationType`, `ResourceId`, `UserId`, `Timestamp`, and `Details` when present.  
- **Parameters:** none  
- **Return value:** a formatted `string`.  
- **Exceptions:** none  

## Usage

### Basic logging and retrieval
```csharp
var audit = new AuditingService
{
    Id = Guid.NewGuid().ToString(),
    OperationType = "Write",
    ResourceId = "cache:key:123",
    UserId = "alice@example.com",
    Details = "Updated value to 42",
    Timestamp = DateTime.UtcNow
};

audit.LogOperation();

foreach (var entry in audit.GetAuditLog())
{
    Console.WriteLine(entry.ToString());
}
```

### Checking log size and clearing
```csharp
var audit = new AuditingService();
// ... perform several LogOperation calls ...

int count = audit.GetAuditLogSize(); // e.g., 5
Console.WriteLine($"Logged {count} audit entries.");

audit.ClearAuditLog();
Console.WriteLine($"Log size after clear: {audit.GetAuditLogSize()}"); // prints 0
```

## Notes
- The class is **not thread‑safe**. Concurrent calls to `LogOperation`, `ClearAuditLog`, or property setters from multiple threads may result in undefined behavior. External synchronization (e.g., locking) is required when the instance is shared across threads.  
- `GetAuditLog` returns a snapshot; enumerating the returned collection does not block modifications to the internal log, but the snapshot itself reflects the state of the log at the moment the method was called.  
- Setting any of the identifier‑related properties (`Id`, `OperationType`, `ResourceId`, `UserId`) to `null` will throw an `ArgumentNullException`. The service considers these fields mandatory for a valid audit entry.  
- The `Timestamp` property is expected to be supplied in UTC; the service does not perform any conversion.  
- After invoking `ClearAuditLog`, subsequent calls to `GetAuditLogSize` will return `0` until new entries are logged.  
- The `ToString` override is intended for diagnostic or logging purposes and may evolve; consumers should not rely on a specific exact format beyond the inclusion of the core fields.
