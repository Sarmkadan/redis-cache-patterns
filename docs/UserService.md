# UserService
The `UserService` class provides asynchronous operations for managing user entities, coordinating between the application’s data store and a Redis-backed cache to optimize read-heavy workloads while ensuring write operations remain consistent.

## API
### UserService()
Initializes a new instance of the `UserService`. Dependencies such as the Redis cache wrapper and the user repository are supplied via dependency injection; the constructor does not perform any I/O.

### GetUserByIdAsync
```csharp
public async Task<User?> GetUserByIdAsync(Guid userId);
```
- **Purpose:** Retrieves a user by its unique identifier, first checking the Redis cache and falling back to the persistent store if the entry is missing or stale.
- **Parameters:** `userId` – the identifier of the user to fetch; must not be `Guid.Empty`.
- **Return Value:** A `Task` that resolves to the `User` instance if found, or `null` when no user matches the identifier.
- **Exceptions:** 
  - `ArgumentException` if `userId` is `Guid.Empty`.
  - Propagates any `Exception` thrown by the underlying repository or cache layer.

### GetUserByUsernameAsync
```csharp
public async Task<User?> GetUserByUsernameAsync(string username);
```
- **Purpose:** Looks up a user by their username, utilizing the cache for rapid lookups.
- **Parameters:** `username` – the login name of the user; must not be `null`, empty, or whitespace.
- **Return Value:** A `Task` that yields the matching `User` or `null` if none exists.
- **Exceptions:** 
  - `ArgumentException` for invalid `username`.
  - Any exceptions from the data access or cache components are bubbled up.

### CreateUserAsync
```csharp
public async Task<User> CreateUserAsync(User user);
```
- **Purpose:** Persists a new user record and populates the cache with the created entity.
- **Parameters:** `user` – the user object to create; must not be `null` and must have a valid, unset identifier.
- **Return Value:** A `Task` that resolves to the created `User` with its identifier populated.
- **Exceptions:** 
  - `ArgumentNullException` if `user` is `null`.
  - `InvalidOperationException` if the user already exists (e.g., duplicate username/email).
  - Any storage‑ or cache‑related exceptions are propagated.

### UpdateUserAsync
```csharp
public async Task<User> UpdateUserAsync(User user);
```
- **Purpose:** Updates an existing user’s data, updating both the backing store and the corresponding cache entry.
- **Parameters:** `user` – the user instance containing the modified values; must not be `null` and must have a valid identifier.
- **Return Value:** A `Task` that yields the updated `User` object.
- **Exceptions:** 
  - `ArgumentNullException` for a `null` user.
  - `KeyNotFoundException` if no user with the given identifier exists.
  - Propagates any exceptions from the repository or cache layer.

### DeactivateUserAsync
```csharp
public async Task<bool> DeactivateUserAsync(Guid userId);
```
- **Purpose:** Marks a user as inactive (soft delete), removing the entry from the cache to prevent stale reads.
- **Parameters:** `userId` – the identifier of the user to deactivate; must not be `Guid.Empty`.
- **Return Value:** A `Task` that resolves to `true` if the user was found and deactivated, otherwise `false`.
- **Exceptions:** 
  - `ArgumentException` for an empty `userId`.
  - Any underlying store or cache exceptions are propagated.

### DeleteUserAsync
```csharp
public async Task<bool> DeleteUserAsync(Guid userId);
```
- **Purpose:** Permanently removes a user from the data store and evicts the corresponding cache entry.
- **Parameters:** `userId` – the identifier of the user to delete; must not be `Guid.Empty`.
- **Return Value:** A `Task` that yields `true` if a user was removed, `false` if none existed.
- **Exceptions:** 
  - `ArgumentException` for an empty `userId`.
  - Propagates any exceptions from the persistence or cache layers.

### GetActiveUsersAsync
```csharp
public async Task<IEnumerable<User>> GetActiveUsersAsync();
```
- **Purpose:** Returns all users whose `IsActive` flag is set, leveraging cached collections when available.
- **Parameters:** None.
- **Return Value:** A `Task` that resolves to an enumerable of `User` objects representing active users; may be empty.
- **Exceptions:** Propagates any exceptions from the repository or cache.

### GetUsersByRoleAsync
```csharp
public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role);
```
- **Purpose:** Retrieves users assigned to a specific role, using cached role‑based indexes if present.
- **Parameters:** `role` – the role name to filter by; must not be `null`, empty, or whitespace.
- **Return Value:** A `Task` that yields an enumerable of matching `User` objects; may be empty.
- **Exceptions:** 
  - `ArgumentException` for an invalid `role`.
  - Any exceptions from the data store or cache are propagated.

### GetAllUsersAsync
```csharp
public async Task<IEnumerable<User>> GetAllUsersAsync();
```
- **Purpose:** Fetches every user record, bypassing any soft‑delete filters; results may be served from a cached collection.
- **Parameters:** None.
- **Return Value:** A `Task` that resolves to an enumerable of all `User` objects; may be empty.
- **Exceptions:** Propagates any exceptions thrown by the underlying repository or cache.

### AuthenticateAsync
```csharp
public async Task<bool> AuthenticateAsync(string username, string password);
```
- **Purpose:** Verifies that the supplied credentials match a stored user account, checking the cache for the user record before validating the password hash.
- **Parameters:** 
  - `username` – the login name; must not be `null`, empty, or whitespace.
  - `password` – the plain‑text password to validate; must not be `null`.
- **Return Value:** A `Task` that yields `true` if credentials are valid, otherwise `false`.
- **Exceptions:** 
  - `ArgumentException` for invalid `username` or `null` `password`.
  - Any exceptions from the repository or cache are propagated.

## Usage
```csharp
// Example 1: Retrieve a user by ID and display their name.
var userId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
User? user = await userService.GetUserByIdAsync(userId);
if (user != null)
{
    Console.WriteLine($"Found user: {user.Username}");
}
else
{
    Console.WriteLine("User not found.");
}
```

```csharp
// Example 2: Create a new user and then attempt authentication.
var newUser = new User
{
    Username = "jdoe",
    Email = "jdoe@example.com",
    PasswordHash = /* hashed password */,
    IsActive = true
};
User created = await userService.CreateUserAsync(newUser);
bool isAuthenticated = await userService.AuthenticateAsync("jdoe", "plainPassword");
Console.WriteLine(isAuthenticated
    ? "Authentication succeeded."
    : "Authentication failed.");
```

## Notes
- All methods are thread‑safe assuming the injected dependencies (repository and cache) are thread‑safe; the service itself holds no mutable state.
- Passing `null` or invalid identifiers/strings will result in an `ArgumentException` or `ArgumentNullException` before any I/O occurs.
- Cache invalidation is performed implicitly on write operations (`CreateUserAsync`, `UpdateUserAsync`, `DeactivateUserAsync`, `DeleteUserAsync`) to avoid serving stale data.
- If the underlying cache is unavailable, the service will fall back to the persistent store; however, performance may degrade and exceptions from the cache layer will still be propagated.
- The `Get*` methods may return empty collections rather than `null` when no matching records exist. 
- Concurrent updates to the same user record may lead to race conditions; callers should consider optimistic concurrency handling if required.
