# UserRepositoryExtensions

The `UserRepositoryExtensions` class is a static collection of extension methods for `UserRepository`. It adds four helpers: two lookups that throw when no user is found (by username and by email), a list of active usernames, and a lookup by role name. The class is in the `RedisCachePatterns.Infrastructure.Repositories` namespace, the same as `UserRepository`, so no extra `using` is needed for it. The methods only read data and do not change the repository.

The methods are declared on the concrete `UserRepository` type, not on `IUserRepository`. A variable typed as `IUserRepository` must be cast to `UserRepository` before these methods can be called. `UserService` depends on `IUserRepository`, so it cannot use these helpers without that cast.

## API

### GetByUsernameOrThrowAsync

```csharp
public static async Task<User> GetByUsernameOrThrowAsync(this UserRepository repository, string username)
```

Returns the user with the given username. The method calls `GetByUsernameAsync`, so the match ignores case. Unlike `GetByUsernameAsync`, it does not return `null` for a missing user.

- **repository**: The `UserRepository` instance.
- **username**: The username to look up.
- **Returns**: The matching `User`.
- **Throws**: `ArgumentNullException` when `repository` is null. `ArgumentNullException` when `username` is null. `ArgumentException` when `username` is empty. `BusinessException` when no user has this username.

### GetByEmailOrThrowAsync

```csharp
public static async Task<User> GetByEmailOrThrowAsync(this UserRepository repository, string email)
```

Returns the user with the given email address. The method calls `GetByEmailAsync`, so the match ignores case. Like the username variant, it throws instead of returning `null`.

- **repository**: The `UserRepository` instance.
- **email**: The email address to look up.
- **Returns**: The matching `User`.
- **Throws**: `ArgumentNullException` when `repository` is null. `ArgumentNullException` when `email` is null. `ArgumentException` when `email` is empty. `BusinessException` when no user has this email address.

### GetActiveUsernamesAsync

```csharp
public static async Task<IReadOnlyList<string>> GetActiveUsernamesAsync(this UserRepository repository)
```

Returns the usernames of all users whose `IsActive` flag is `true`. The method calls `GetActiveUsersAsync` and projects each user to its `Username`.

- **repository**: The `UserRepository` instance.
- **Returns**: A read-only list of usernames. The list is empty when no user is active. The order is the order the repository returns the users in.
- **Throws**: `ArgumentNullException` when `repository` is null.

### GetByRoleAsync

```csharp
public static async Task<IReadOnlyList<User>> GetByRoleAsync(this UserRepository repository, string roleName)
```

Returns the users that have the named role. The name is parsed with `Enum.TryParse<UserRole>` and ignores case, so `"administrator"` and `"Administrator"` both work. The method then calls the repository's `GetByRoleAsync(UserRole)` overload.

- **repository**: The `UserRepository` instance.
- **roleName**: The name of the role. The valid names are `User`, `Moderator`, and `Administrator`.
- **Returns**: A read-only list of users with the role. The list is empty when no user has it.
- **Throws**: `ArgumentNullException` when `repository` is null. `ArgumentNullException` when `roleName` is null. `ArgumentException` when `roleName` is empty. `ArgumentException` (with `roleName` as the parameter name) when the name is not a valid role.

## Usage

### Looking up a user at login

```csharp
using RedisCachePatterns.Domain;
using RedisCachePatterns.Exceptions;
using RedisCachePatterns.Infrastructure.Repositories;

public class LoginHandler(UserRepository repository)
{
    public async Task<User?> FindForLoginAsync(string username)
    {
        try
        {
            return await repository.GetByUsernameOrThrowAsync(username);
        }
        catch (BusinessException)
        {
            return null;
        }
    }
}
```

The method throws for a missing user, so callers that expect "not found" as a normal result have to catch `BusinessException`. Callers that want `null` should use `GetByUsernameAsync` instead.

### Listing the administrators

```csharp
IReadOnlyList<User> admins = await repository.GetByRoleAsync("Administrator");
```

### Listing active usernames

```csharp
IReadOnlyList<string> names = await repository.GetActiveUsernamesAsync();
```

## Notes

*   **Exceptions are deferred:** The methods are `async`, so argument checks do not throw when the method is called. The exception is stored in the returned `Task` and thrown when the task is awaited.
*   **Null arguments throw `ArgumentNullException`:** The XML documentation of `GetByUsernameOrThrowAsync`, `GetByEmailOrThrowAsync`, and `GetByRoleAsync` says that a null argument throws `ArgumentException`. The code calls `ArgumentException.ThrowIfNullOrEmpty`, which throws `ArgumentNullException` for null and `ArgumentException` for empty. Callers that catch only `ArgumentException` will miss the null case.
*   **Lookups ignore case:** `GetByUsernameAsync` and `GetByEmailAsync` compare with `StringComparison.OrdinalIgnoreCase`, so `Alice` and `alice` find the same user.
*   **Role names are parsed, not validated against a list:** `Enum.TryParse` also accepts numeric strings. `"2"` is treated as `Administrator`. A number that is not a defined role, such as `"9"`, parses successfully and returns an empty list rather than throwing.
*   **Filtering happens in memory:** The repository keeps its data in memory (`_data`, guarded by `_lock`). `GetActiveUsernamesAsync` and `GetByRoleAsync` load the matching users and filter them with LINQ, so the extensions add no I/O of their own.
*   **No caching:** The extensions do not read from or write to Redis. Nothing in the current tree calls them, so they have no caller yet.
