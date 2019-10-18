# UserRepository

The `UserRepository` class acts as a data access abstraction for retrieving `User` entities, designed to work within a `redis-cache-patterns` architecture. It manages the retrieval of user data from underlying persistent storage, providing methods to fetch users by unique identifiers or filter by specific attributes like role or status, often leveraging Redis as an intermediary cache to reduce database load and improve response times for frequently requested data.

## API

### GetByUsernameAsync
Retrieves a `User` record matching the provided `username`.
*   **Parameters:** `string username` - The unique username to search for.
*   **Returns:** A `Task` representing the asynchronous operation, containing the `User` object if found, or `null` if no record exists.
*   **Throws:** `ArgumentNullException` if `username` is null.

### GetByEmailAsync
Retrieves a `User` record matching the provided `email` address.
*   **Parameters:** `string email` - The unique email address to search for.
*   **Returns:** A `Task` representing the asynchronous operation, containing the `User` object if found, or `null` if no record exists.
*   **Throws:** `ArgumentNullException` if `email` is null.

### GetActiveUsersAsync
Retrieves a collection of all `User` records currently marked as active.
*   **Parameters:** None.
*   **Returns:** A `Task` representing the asynchronous operation, containing an `IEnumerable<User>` of all active users. Returns an empty collection if no active users are found.

### GetByRoleAsync
Retrieves a collection of `User` records associated with the specified `role`.
*   **Parameters:** `string role` - The role identifier used to filter the user list.
*   **Returns:** A `Task` representing the asynchronous operation, containing an `IEnumerable<User>` matching the specified role. Returns an empty collection if no users match the role.
*   **Throws:** `ArgumentNullException` if `role` is null.

## Usage

```csharp
// Retrieve a user by email and username
var userRepository = new UserRepository(dbContext, redisCache);

var userByEmail = await userRepository.GetByEmailAsync("user@example.com");
if (userByEmail != null)
{
    Console.WriteLine($"Found user: {userByEmail.Username}");
}

var userByUsername = await userRepository.GetByUsernameAsync("jdoe");
```

```csharp
// Retrieve all active users with the 'Admin' role
var userRepository = new UserRepository(dbContext, redisCache);

var activeUsers = await userRepository.GetActiveUsersAsync();
var admins = await userRepository.GetByRoleAsync("Admin");

var activeAdmins = activeUsers.IntersectBy(admins.Select(u => u.Id), u => u.Id);
foreach (var admin in activeAdmins)
{
    Console.WriteLine($"Active Admin: {admin.Username}");
}
```

## Notes

*   **Thread Safety:** The `UserRepository` is designed to be thread-safe for concurrent read operations. Thread safety for the underlying data store depends on the implementation of the database and cache providers used.
*   **Caching Implications:** Because this repository is part of a caching pattern, data retrieved may reflect the state of the cache rather than the primary database. Cache invalidation policies should be managed appropriately to ensure data consistency.
*   **Transient Failures:** As an asynchronous I/O-bound component interacting with external systems (Redis/Database), callers should implement robust error handling or retry policies to manage potential transient connection failures (e.g., `TimeoutException`).
