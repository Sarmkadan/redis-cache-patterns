# AuditingServiceExtensions

The `AuditingServiceExtensions` class is a static collection of extension methods for `AuditingService`. It adds three read-only query helpers: `GetRecentEntries` (newest entries first), `GetEntriesByUser` (filter by actor), and `GetAuditSummary` (a one-line text summary with a count per operation type). The class is in the `RedisCachePatterns.Services` namespace, the same as `AuditingService`, so no extra `using` is needed. None of the methods change the audit log.

Each method reads the log through the public `GetAuditLog()` method. They therefore see the same entries, and they are subject to the same log trimming, as that method.

## API

### GetRecentEntries

```csharp
public static IEnumerable<AuditingService.AuditEntry> GetRecentEntries(this AuditingService service, int count)
```

Returns up to `count` audit entries, ordered by `Timestamp` from newest to oldest.

- **service**: The auditing service instance.
- **count**: The maximum number of entries to return. Must be greater than zero.
- **Returns**: An enumerable of at most `count` entries. When the log holds fewer entries, all of them are returned.
- **Throws**: `ArgumentNullException` when `service` is null. `ArgumentOutOfRangeException` when `count` is zero or negative.

### GetEntriesByUser

```csharp
public static IEnumerable<AuditingService.AuditEntry> GetEntriesByUser(this AuditingService service, string userId)
```

Returns every audit entry whose `UserId` matches `userId`. The comparison uses `StringComparison.OrdinalIgnoreCase`, so `"Alice"` and `"alice"` select the same entries. The value is not trimmed, so `" alice "` matches nothing.

- **service**: The auditing service instance.
- **userId**: The user identifier to match. Must not be null, empty, or whitespace.
- **Returns**: The matching entries in the order they were logged (oldest first). The sequence is empty when no entry matches.
- **Throws**: `ArgumentNullException` when `service` or `userId` is null. `ArgumentException` when `userId` is empty or whitespace.

### GetAuditSummary

```csharp
public static string GetAuditSummary(this AuditingService service)
```

Returns a one-line summary of the log: the total number of entries, followed by the number of entries for each operation type.

- **service**: The auditing service instance.
- **Returns**: A string in one of these forms:
  - Empty log: `Audit Summary – Total Entries: 0`
  - Non-empty log: `Audit Summary – Total Entries: 3 ( Read: 2, Write: 1 )`

  The breakdown items are sorted alphabetically by their text. The separator before the total is an en dash (U+2013).
- **Throws**: `ArgumentNullException` when `service` is null.

## Usage

### Showing the most recent entries

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using RedisCachePatterns.Services;

var auditing = new AuditingService(NullLogger<AuditingService>.Instance);
auditing.LogOperation("Read", "product:1", "alice");
auditing.LogOperation("Write", "product:1", "bob");

foreach (var entry in auditing.GetRecentEntries(5))
{
    Console.WriteLine(entry);   // [2026-10-25T...Z] Write | Resource=product:1 | User=bob
}
```

### Filtering by user

```csharp
IEnumerable<AuditingService.AuditEntry> aliceActions = auditing.GetEntriesByUser("ALICE");
// Matches the entry logged with userId "alice"
```

### Getting a one-line summary

```csharp
string summary = auditing.GetAuditSummary();
// Audit Summary – Total Entries: 2 ( Read: 1, Write: 1 )
```

## Notes

*   **Snapshot at call time:** Each method calls `GetAuditLog()`, which copies the log into a new list under a lock. The returned sequences run over that copy. Entries logged after the call are not included, even if the sequence is enumerated later. Ordering and filtering run when the sequence is enumerated, not when the method is called.
*   **Ties in `GetRecentEntries`:** `OrderByDescending` is stable, so entries that share a `Timestamp` keep their logged order (the older entry comes first among them). Ties are possible because `Timestamp` comes from `DateTime.UtcNow`, and some platforms have a coarse clock.
*   **Trimmed entries are gone:** `AuditingService` keeps at most `maxLogEntries` entries (10,000 by default) and removes the oldest ones when the limit is passed. These extension methods only see the retained entries.
*   **Missing user defaults to `"system"`:** `LogOperation` stores `"system"` as the `UserId` when no user is given. `GetEntriesByUser("system")` therefore returns those entries.
*   **`GetAuditSummary` is not atomic:** The method reads the total with `GetAuditLogSize()` and the breakdown from `GetAuditLog()`. Each call takes the lock separately. If entries are logged between the two calls, the total and the breakdown can disagree.
*   **`GetEntriesByUser` documentation mismatch:** The XML comment says `ArgumentException` is thrown for a null or whitespace `userId`. In practice, null throws `ArgumentNullException` and whitespace throws `ArgumentException`. Catch both if you need to handle invalid input.
*   **Oversized `count` is not an error:** `GetRecentEntries` with a `count` larger than the log size returns every entry without throwing.
*   **In-memory only:** The log is held in memory by `AuditingService`. Nothing is persisted, so the entries are lost when the process restarts.
