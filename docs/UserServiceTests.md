# UserServiceTests

`UserServiceTests` is the unit test suite for the `UserService` class in the `redis-cache-patterns` project. It validates caching behavior, data persistence, validation rules, and cache invalidation logic using a mocked repository and cache provider. Each test method exercises a specific scenario to ensure the service correctly implements read-through, write-through, and cache invalidation patterns for user entities.

## API

### UserServiceTests

```csharp
public UserServiceTests()
```

Default parameterless constructor. Initializes the test class and its shared test context (mocked dependencies, test data fixtures) before each test method executes. Does not throw.

---

### GetUserByIdAsync_WhenCacheHit_ReturnsUserWithoutRepositoryCall

```csharp
public async Task GetUserByIdAsync_WhenCacheHit_ReturnsUserWithoutRepositoryCall()
```

**Purpose:** Verifies that when a user is present in the cache, `GetUserByIdAsync` returns the cached instance without invoking the underlying repository.

**Parameters:** None (test method).

**Returns:** A completed `Task` representing the asynchronous test operation.

**Throws:** Assertion failures if the repository is unexpectedly called or the returned user does not match the cached value.

---

### GetUserByIdAsync_UsesCorrectCacheKey

```csharp
public async Task GetUserByIdAsync_UsesCorrectCacheKey()
```

**Purpose:** Confirms that `GetUserByIdAsync` constructs and queries the cache using the expected key format (e.g., `user:{id}`) rather than an arbitrary or malformed key.

**Parameters:** None.

**Returns:** A completed `Task`.

**Throws:** Assertion failures if the cache is accessed with an incorrect key pattern.

---

### GetUserByUsernameAsync_RetrievesUserByUsername

```csharp
public async Task GetUserByUsernameAsync_RetrievesUserByUsername()
```

**Purpose:** Ensures that `GetUserByUsernameAsync` correctly retrieves a user from the repository (or cache) when queried by username, and returns the matching entity.

**Parameters:** None.

**Returns:** A completed `Task`.

**Throws:** Assertion failures if the wrong user is returned or the lookup mechanism deviates from the expected path.

---

### CreateUserAsync_WithValidUser_PersistsAndCaches

```csharp
public async Task CreateUserAsync_WithValidUser_PersistsAndCaches()
```

**Purpose:** Validates that creating a user with valid data results in the user being persisted to the repository and subsequently stored in the cache.

**Parameters:** None.

**Returns:** A completed `Task`.

**Throws:** Assertion failures if the repository save or cache write is skipped.

---

### CreateUserAsync_WithInvalidEmail_ThrowsValidationException

```csharp
public async Task CreateUserAsync_WithInvalidEmail_ThrowsValidationException()
```

**Purpose:** Asserts that attempting to create a user with an invalid email format causes the service to throw a `ValidationException` before any persistence or caching occurs.

**Parameters:** None.

**Returns:** A completed `Task`.

**Throws:** The test expects a `ValidationException` to be thrown by the service method; the test itself throws assertion failures if the exception type is wrong or not thrown.

---

### CreateUserAsync_WhenUsernameExists_ThrowsValidationException

```csharp
public async Task CreateUserAsync_WhenUsernameExists_ThrowsValidationException()
```

**Purpose:** Verifies that duplicate username detection works correctly — when the username is already taken, the service throws a `ValidationException`.

**Parameters:** None.

**Returns:** A completed `Task`.

**Throws:** Expects a `ValidationException` from the service; assertion failures if the exception is missing or of the wrong type.

---

### CreateUserAsync_InvalidatesUserListCache

```csharp
public async Task CreateUserAsync_InvalidatesUserListCache()
```

**Purpose:** Confirms that after a new user is created, any cached list of users (e.g., “all users” or “active users”) is invalidated or removed so that subsequent list queries fetch fresh data.

**Parameters:** None.

**Returns:** A completed `Task`.

**Throws:** Assertion failures if the relevant list cache entries are not invalidated.

---

### UpdateUserAsync_WhenUserExists_UpdatesAndInvalidatesCache

```csharp
public async Task UpdateUserAsync_WhenUserExists_UpdatesAndInvalidatesCache()
```

**Purpose:** Ensures that updating an existing user persists the changes to the repository and invalidates both the individual user cache entry and any related list caches.

**Parameters:** None.

**Returns:** A completed `Task`.

**Throws:** Assertion failures if the repository update is skipped or cache invalidation is incomplete.

---

### UpdateUserAsync_WhenUserNotFound_ThrowsNotFoundException

```csharp
public async Task UpdateUserAsync_WhenUserNotFound_ThrowsNotFoundException()
```

**Purpose:** Tests that attempting to update a user that does not exist results in a `NotFoundException` being thrown, with no modifications made to the repository or cache.

**Parameters:** None.

**Returns:** A completed `Task`.

**Throws:** Expects a `NotFoundException` from the service; assertion failures if the exception is not thrown or is of the wrong type.

---

### DeactivateUserAsync_WhenUserExists_DeactivatesAndInvalidatesCache

```csharp
public async Task DeactivateUserAsync_WhenUserExists_DeactivatesAndInvalidatesCache()
```

**Purpose:** Verifies that deactivating an existing user updates their status in the repository and invalidates the relevant cache entries (individual and list caches).

**Parameters:** None.

**Returns:** A completed `Task`.

**Throws:** Assertion failures if deactivation is not persisted or cache entries remain stale.

---

### GetActiveUsersAsync_ReturnsActiveUsersFromCache

```csharp
public async Task GetActiveUsersAsync_ReturnsActiveUsersFromCache()
```

**Purpose:** Confirms that `GetActiveUsersAsync` returns the set of active users from the cache when a cached entry exists, without falling back to the repository.

**Parameters:** None.

**Returns:** A completed `Task`.

**Throws:** Assertion failures if the repository is queried despite a cache hit, or the returned collection is incorrect.

---

### GetAllUsersAsync_ReturnsAllUsersFromCache

```csharp
public async Task GetAllUsersAsync_ReturnsAllUsersFromCache()
```

**Purpose:** Validates that `GetAllUsersAsync` serves results directly from the cache when available, avoiding an unnecessary repository call.

**Parameters:** None.

**Returns:** A completed `Task`.

**Throws:** Assertion failures if the repository is invoked or the cached data is not returned.

---

### DeleteUserAsync_WhenUserExists_DeletesAndInvalidatesCache

```csharp
public async Task DeleteUserAsync_WhenUserExists_DeletesAndInvalidatesCache()
```

**Purpose:** Ensures that deleting an existing user removes them from the repository and invalidates both the individual cache entry and any list caches that may contain the user.

**Parameters:** None.

**Returns:** A completed `Task`.

**Throws:** Assertion failures if the repository deletion or cache invalidation does not occur.

---

### DeleteUserAsync_WhenUserNotFound_ReturnsFalse

```csharp
public async Task DeleteUserAsync_WhenUserNotFound_ReturnsFalse()
```

**Purpose:** Tests that attempting to delete a non-existent user returns `false` and does not throw an exception or modify cache state.

**Parameters:** None.

**Returns:** A completed `Task`.

**Throws:** Assertion failures if the return value is not `false` or if an exception is thrown.

## Usage

### Example 1: Arranging a cache hit and asserting no repository call

```csharp
// Arrange
var cachedUser = new User { Id = 1, Username = "jdoe" };
cacheMock.Setup(c => c.GetAsync<User>("user:1"))
          .ReturnsAsync(cachedUser);

var service = new UserService(repositoryMock.Object, cacheMock.Object);

// Act
var result = await service.GetUserByIdAsync(1);

// Assert
Assert.Equal(cachedUser, result);
repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
```

### Example 2: Testing validation on duplicate username during creation

```csharp
// Arrange
var newUser = new User { Username = "existinguser", Email = "valid@example.com" };
repositoryMock.Setup(r => r.ExistsByUsernameAsync("existinguser"))
              .ReturnsAsync(true);

var service = new UserService(repositoryMock.Object, cacheMock.Object);

// Act & Assert
await Assert.ThrowsAsync<ValidationException>(
    () => service.CreateUserAsync(newUser)
);

repositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<User>(), It.IsAny<TimeSpan?>()), Times.Never);
```

## Notes

- **Cache key consistency:** Tests such as `GetUserByIdAsync_UsesCorrectCacheKey` rely on a deterministic key format. Any change to the key generation logic in `UserService` will cause these tests to fail, serving as a guard against accidental key drift.
- **Validation ordering:** `CreateUserAsync_WithInvalidEmail_ThrowsValidationException` and `CreateUserAsync_WhenUsernameExists_ThrowsValidationException` assume validation runs before any persistence or cache operation. If validation order changes, these tests must be updated to reflect the new sequence.
- **Cache invalidation scope:** Methods like `CreateUserAsync_InvalidatesUserListCache` and `UpdateUserAsync_WhenUserExists_UpdatesAndInvalidatesCache` verify that list-level cache entries are invalidated. The exact set of invalidated keys (e.g., `users:all`, `users:active`) depends on the service implementation and must be kept in sync with the test expectations.
- **Thread safety:** These tests are single-threaded and do not exercise concurrent access scenarios. They validate correctness of caching logic under sequential execution. Production thread-safety concerns (e.g., race conditions between cache read and repository fetch in a cache-aside pattern) are not covered here.
- **Exception types:** Tests expect specific exception types (`ValidationException`, `NotFoundException`). If the service layer introduces new exception hierarchies or changes exception types, the corresponding tests will break and require adjustment.
- **Mock verification strictness:** Tests use `Times.Never` and `Times.Once` verifications on mocked dependencies. Overly strict setups may cause false negatives if the service implementation changes its internal call pattern without altering the externally observable contract.
