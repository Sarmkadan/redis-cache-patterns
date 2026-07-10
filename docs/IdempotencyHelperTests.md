# IdempotencyHelperTests

The `IdempotencyHelperTests` class serves as the comprehensive test suite for validating the behavior of the idempotency helper component within the `redis-cache-patterns` project. It verifies core functionalities including key existence checks, result storage and retrieval, data type handling, retention policy enforcement, and the independent tracking of multiple keys. By covering scenarios ranging from default constructor behavior to complex object serialization and expiration logic, this class ensures the reliability of idempotency operations in distributed caching environments.

## API

The following members are exposed by the `IdempotencyHelperTests` class:

### Test Methods

*   **`public void IsProcessed_WhenKeyNeverProcessed_ReturnsFalse`**
    Validates that the `IsProcessed` check returns `false` when queried with a key that has never been registered in the cache. This method takes no parameters and returns no value; it throws an assertion exception if the result is not `false`.

*   **`public void MarkAsProcessed_WithValidKey_StoresResult`**
    Verifies that calling the mark operation with a valid key successfully persists the associated result data. It accepts no explicit parameters (using internal test data) and returns void. It throws if the data is not found upon immediate subsequent retrieval.

*   **`public void GetResult_AfterMarkedAsProcessed_ReturnsStoredResult`**
    Ensures that retrieving a result after a key has been marked as processed returns the exact object originally stored. Returns void; throws if the retrieved object does not match the stored reference or values.

*   **`public void GetResult_WhenKeyNotProcessed_ReturnsNull`**
    Confirms that attempting to retrieve a result for a key that has not been marked as processed yields `null`. Returns void; throws if the result is non-null.

*   **`public void IsProcessed_WithDifferentKeys_TracksIndependently`**
    Tests the isolation of cache entries by marking one key as processed and verifying that a different key remains unprocessed. Returns void; throws if the state of either key is incorrect.

*   **`public void MarkAsProcessed_WithDifferentTypes_StoresCorrectly`**
    Validates the serialization and deserialization logic by storing results of varying data types under different keys. Returns void; throws if type fidelity is lost during storage or retrieval.

*   **`public void MarkAsProcessed_UpdatesExistingKey_OverwritesPreviousResult`**
    Verifies that marking an already processed key with a new result overwrites the previous entry rather than appending or ignoring the update. Returns void; throws if the old value persists or the new value is not present.

*   **`public void IsProcessed_WithExpiredRecord_ReturnsFalse`**
    Ensures that `IsProcessed` returns `false` for a key whose retention period has elapsed. This test typically involves manipulating time or waiting for expiration. Returns void; throws if the expired record is still considered processed.

*   **`public void GetResult_WithExpiredRecord_ReturnsNull`**
    Confirms that `GetResult` returns `null` when queried against a key that has passed its expiration threshold. Returns void; throws if data is returned for an expired key.

*   **`public void IsProcessed_WithNearExpiryTime_ReturnsTrue`**
    Validates that a record is still considered processed immediately before its expiration time is reached. Returns void; throws if the record is prematurely invalidated.

*   **`public void MarkAsProcessed_WithComplexObject_StoresAndRetrieves`**
    Tests the handling of complex object graphs (nested properties, collections) to ensure deep cloning or serialization preserves data integrity. Returns void; throws if the retrieved complex object differs from the source.

*   **`public void IsProcessed_WithMultipleRecords_ChecksSpecificKey`**
    Verifies that in a cache populated with multiple records, checking a specific key returns the correct status without interference from other entries. Returns void; throws if the status is inaccurate.

*   **`public void Constructor_WithDefaultRetention_Uses24Hours`**
    Asserts that instantiating the helper without explicit retention arguments defaults the time-to-live (TTL) to 24 hours. Returns void; throws if the default configuration differs.

*   **`public void Constructor_WithCustomRetention_AppliesCorrectly`**
    Validates that providing a custom retention duration to the constructor correctly configures the expiration policy for stored keys. Returns void; throws if the custom duration is not applied.

*   **`public void GetResult_WithTypeConversionNeeded_ReturnsCorrectType`**
    Ensures that the retrieval mechanism correctly handles type conversion or generic resolution when the stored object requires specific typing upon return. Returns void; throws if the returned type is invalid or conversion fails.

### Test Data Members

*   **`public int Id`**
    A public integer property used as sample data within test cases to verify value type handling.

*   **`public string? Name`**
    A nullable string property used as sample data within test cases to verify reference type and null-handling logic.

*   **`public int[]? Values`**
    A nullable integer array property used as sample data to verify collection serialization and retrieval.

## Usage

The following examples demonstrate how the test methods validate specific behaviors of the idempotency system.

### Example 1: Validating Basic Idempotency Flow
This scenario verifies that a key transitions from unprocessed to processed and that the result is retrievable.

```csharp
var testSuite = new IdempotencyHelperTests();

// Initially, the key should not be marked as processed
testSuite.IsProcessed_WhenKeyNeverProcessed_ReturnsFalse();

// Mark the operation as processed with a result
testSuite.MarkAsProcessed_WithValidKey_StoresResult();

// Verify the result can be retrieved exactly as stored
testSuite.GetResult_AfterMarkedAsProcessed_ReturnsStoredResult();

// Confirm the key is now flagged as processed
// (Implicitly validated within the retrieval test logic)
```

### Example 2: Testing Expiration and Overwrite Logic
This scenario ensures that records expire correctly after the retention period and that updating an existing key overwrites previous data.

```csharp
var testSuite = new IdempotencyHelperTests();

// Store an initial result
testSuite.MarkAsProcessed_WithValidKey_StoresResult();

// Simulate time passing beyond the retention window
// The test method handles the time simulation internally
testSuite.IsProcessed_WithExpiredRecord_ReturnsFalse();

// Verify retrieval returns null after expiration
testSuite.GetResult_WithExpiredRecord_ReturnsNull();

// Re-mark the same key with new data to test overwrite behavior
testSuite.MarkAsProcessed_UpdatesExistingKey_OverwritesPreviousResult();
```

## Notes

*   **Thread Safety**: As this is a test class, the methods themselves are not designed for concurrent execution against a shared static state unless the underlying test framework isolates instances. The underlying system being tested should be verified for thread safety separately; these tests assume sequential execution per test case to avoid race conditions during expiration checks.
*   **Expiration Precision**: The tests `IsProcessed_WithNearExpiryTime_ReturnsTrue` and `IsProcessed_WithExpiredRecord_ReturnsFalse` rely on precise time measurement. In production environments, clock skew between the application server and the Redis server may cause slight deviations in expiration behavior compared to the deterministic environment of these unit tests.
*   **Serialization Limits**: The `MarkAsProcessed_WithComplexObject_StoresAndRetrieves` test validates standard serializable objects. If the implementation relies on specific serializers (e.g., JSON vs. Binary), objects containing non-serializable members or circular references not covered by this test may still cause runtime failures.
*   **Null Handling**: The presence of nullable properties (`Name`, `Values`) and the `GetResult_WhenKeyNotProcessed_ReturnsNull` test explicitly verify null safety. Consumers of the helper should expect `null` returns for missing or expired keys rather than exceptions.
*   **Data Isolation**: The `IsProcessed_WithDifferentKeys_TracksIndependently` test confirms that key collisions do not occur. However, care must be taken in the actual implementation to ensure key naming conventions prevent accidental overlaps in shared Redis instances.
