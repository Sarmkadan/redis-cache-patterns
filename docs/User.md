# User

Represents a registered user in the system with authentication, profile, and order management capabilities. This entity encapsulates user identity, credentials, activity status, and audit timestamps while providing methods for profile updates, login tracking, and account lifecycle management.

## API

### Properties

**`public int Id`**  
Unique identifier for the user. Assigned on creation and immutable thereafter.

**`public string Username`**  
Unique username chosen by the user. Used for authentication and display purposes.

**`public string Email`**  
User's email address. Must be a valid email format; used for communication and password recovery.

**`public string FirstName`**  
User's given name. Can be updated via `UpdateProfile`.

**`public string LastName`**  
User's family name. Can be updated via `UpdateProfile`.

**`public string FullName`**  
Computed property returning the concatenation of `FirstName` and `LastName`. Equivalent to calling `GetFullName()`.

**`public string PasswordHash`**  
BCrypt or equivalent hash of the user's password. Never stores plaintext. Set during registration or password reset.

**`public bool IsActive`**  
Indicates whether the user account is active. Inactive users cannot authenticate. Modified by `Activate()` and `Deactivate()`.

**`public DateTime CreatedAt`**  
Timestamp when the user record was created. Set once on construction; immutable.

**`public DateTime? LastLoginAt`**  
Timestamp of the most recent successful login. Null if the user has never logged in. Updated by `SetLastLogin()`.

**`public string? Phone`**  
Optional phone number for the user. Can be updated via `UpdateProfile`.

**`public string? Address`**  
Optional physical address for the user. Can be updated via `UpdateProfile`.

**`public UserRole Role`**  
Enumeration value representing the user's authorization level (e.g., Customer, Admin). Determines permission scope.

**`public List<Order> Orders`**  
Collection of orders placed by this user. Populated by the data layer; not intended for direct mutation.

### Methods

**`public string GetFullName()`**  
Returns the user's full name by concatenating `FirstName` and `LastName` with a space.  
**Returns:** `string` — formatted full name.  
**Throws:** None.

**`public void UpdateProfile(string firstName, string lastName, string? phone = null, string? address = null)`**  
Updates the user's profile information.  
**Parameters:**  
- `firstName` — new given name; must not be null or whitespace.  
- `lastName` — new family name; must not be null or whitespace.  
- `phone` — optional phone number; may be null to clear existing value.  
- `address` — optional address; may be null to clear existing value.  
**Throws:** `ArgumentException` if `firstName` or `lastName` is null or whitespace.

**`public void SetLastLogin()`**  
Sets `LastLoginAt` to the current UTC timestamp. Call after successful authentication.  
**Throws:** None.

**`public bool IsValidEmail(string email)`**  
Validates an email address format using a standard regex pattern.  
**Parameters:**  
- `email` — email string to validate.  
**Returns:** `bool` — true if format is valid; false otherwise.  
**Throws:** None.

**`public void Deactivate()`**  
Sets `IsActive` to false, preventing further authentication. Does not delete data.  
**Throws:** None.

**`public void Activate()`**  
Sets `IsActive` to true, restoring authentication capability.  
**Throws:** None.

## Usage

### Example 1: User Registration and Initial Login

```csharp
var user = new User
{
    Username = "jdoe",
    Email = "john.doe@example.com",
    FirstName = "John",
    LastName = "Doe",
    PasswordHash = BCrypt.Net.BCrypt.HashPassword("securePassword123"),
    Role = UserRole.Customer,
    IsActive = true,
    CreatedAt = DateTime.UtcNow
};

if (!user.IsValidEmail(user.Email))
{
    throw new InvalidOperationException("Invalid email format");
}

user.SetLastLogin();
// Persist user via repository
await userRepository.AddAsync(user);
```

### Example 2: Profile Update and Account Deactivation

```csharp
var user = await userRepository.GetByIdAsync(userId);
if (user == null || !user.IsActive)
{
    return NotFound();
}

user.UpdateProfile(
    firstName: "Jonathan",
    lastName: "Doe",
    phone: "+1-555-0199",
    address: "123 Main St, Springfield, IL"
);

if (request.DeactivateAccount)
{
    user.Deactivate();
}

await userRepository.UpdateAsync(user);
```

## Notes

- **Thread Safety:** This type is not thread-safe. Concurrent calls to `UpdateProfile`, `SetLastLogin`, `Activate`, or `Deactivate` on the same instance from multiple threads may result in lost updates or inconsistent state. Synchronize externally if shared across threads.
- **Email Validation:** `IsValidEmail` performs syntactic validation only; it does not verify domain existence or deliverability. Call before assigning to `Email` property.
- **PasswordHash:** Never assign plaintext passwords. Always hash using a strong algorithm (BCrypt, Argon2) before setting.
- **Orders Collection:** The `Orders` list is populated by the ORM/data layer. Direct modification (Add/Remove) may not persist correctly; use the order repository for mutations.
- **LastLoginAt:** Remains null until `SetLastLogin` is called. Treat null as "never logged in" in reporting queries.
- **Deactivation:** `Deactivate()` is a soft delete. The record remains in the database with `IsActive = false`. Consider filtering by `IsActive` in all authentication and listing queries.
