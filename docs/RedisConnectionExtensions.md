# RedisConnectionExtensions

The `RedisConnectionExtensions` class provides a set of high-level asynchronous extension methods designed to simplify common operations for Redis databases using the `StackExchange.Redis` library. These methods abstract boilerplate logic for cache management, facilitating operations such as setting values with expiration, batch retrieval of multiple keys, and performing atomic existence checks and deletions.

## API

### GetWithExpirationAsync
Retrieves the string value associated with a specified key.
- **Parameters**: `IDatabase db` (the target database), `string key` (the key to retrieve).
- **Return Value**: `Task<string?>` representing the value if the key exists, otherwise `null`.
- **Throws**: `RedisException` if a communication error occurs with the Redis server.

### SetWithExpirationAsync
Sets a string value for a specified key with a defined time-to-live (TTL).
- **Parameters**: `IDatabase db`, `string key`, `string value`, `TimeSpan expiry` (the duration after which the key should expire).
- **Return Value**: `Task<bool>` returning `true` if the operation was successful, `false` otherwise.
- **Throws**: `RedisException` on connection failure.

### GetMultipleAsync
Retrieves the values for a collection of keys in a single batch operation.
- **Parameters**: `IDatabase db`, `IEnumerable<string> keys` (the collection of keys to fetch).
- **Return Value**: `Task<Dictionary<string, string?>>` where keys not found in Redis map to `null`.
- **Throws**: `RedisException` if the batch request fails.

### KeyExistsAsync
Determines if a specified key exists in the database.
- **Parameters**: `IDatabase db`, `string key`.
- **Return Value**: `Task<bool>` returning `true` if the key exists, `false` otherwise.
- **Throws**: `RedisException` if the check fails due to server connectivity issues.

### RemoveKeyAsync
Removes a specified key from the database.
- **Parameters**: `IDatabase db`, `string key`.
- **Return Value**: `Task<bool>` returning `true` if the key existed and was successfully removed, `false` if the key did not exist.
- **Throws**: `RedisException` if the deletion request fails.

## Usage

```csharp
// Example 1: Basic Set and Get with Expiration
public async Task CacheUserSession(IDatabase db, string userId, string token)
{
    await db.SetWithExpirationAsync($"user:{userId}", token, TimeSpan.FromMinutes(30));
    string? cachedToken = await db.GetWithExpirationAsync($"user:{userId}");
}

// Example 2: Batch Retrieval of Multiple Keys
public async Task<Dictionary<string, string?>> GetConfigValues(IDatabase db, IEnumerable<string> keys)
{
    // Retrieves multiple configuration keys in one network round-trip
    return await db.GetMultipleAsync(keys);
}
```

## Notes

- **Thread-Safety**: These methods are thread-safe, inheriting thread-safety guarantees from the underlying `IDatabase` implementation provided by `StackExchange.Redis`.
- **Edge Cases**:
  - Null or empty keys passed as parameters may result in `ArgumentException` or `RedisException`, depending on the specific implementation of `IDatabase`.
  - In scenarios with network instability, operations may throw a `RedisConnectionException` or `RedisTimeoutException`. Callers should implement appropriate retry policies.
  - `GetMultipleAsync` may perform better than sequential calls, but excessively large key collections can impact Redis performance; monitor batch sizes accordingly.
