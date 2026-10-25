# RepositoryExtensions

The `RepositoryExtensions` class is a static collection of generic extension methods for `IRepository<T>`. It adds LINQ-style query helpers (`AnyAsync`, `FirstOrDefaultAsync`, `WhereAsync`, `SingleAsync`, `SingleOrDefaultAsync`) that work on any repository, including `Repository<T>`, `ProductRepository`, `OrderRepository`, and `UserRepository`. The class is in the `RedisCachePatterns.Infrastructure.Repositories` namespace, the same as `IRepository<T>`, so no extra `using` is needed for it. The methods only read data and do not change the repository.

Unlike the entity-specific extensions such as `ProductRepositoryExtensions`, these methods are declared on the generic interface, so a variable typed as `IRepository<T>` (or any `IXxxRepository` that extends it) can call them directly without a cast.

## API

### FirstOrDefaultAsync(int id)

```csharp
public static Task<T?> FirstOrDefaultAsync<T>(this IRepository<T> repository, int id) where T : class
```

Gets an entity by its identifier, or `null` when no entity has that identifier. This is a convenience alias for `IRepository<T>.GetByIdAsync`. Despite the name, it does not take a predicate.

- **repository**: The repository instance.
- **id**: The identifier of the entity to retrieve.
- **Returns**: The entity if found; otherwise, `null`.
- **Throws**: `ArgumentNullException` when `repository` is null. This check runs when the method is called, not when the task is awaited; see the notes below.

### AnyAsync

```csharp
public static async Task<bool> AnyAsync<T>(this IRepository<T> repository, Func<T, bool> predicate) where T : class
```

Returns `true` when at least one entity satisfies the predicate. The method calls `GetAllAsync` and evaluates the predicate in memory.

- **repository**: The repository instance.
- **predicate**: A function to test each entity for a condition.
- **Returns**: `true` if any entity matches; otherwise, `false`.
- **Throws**: `ArgumentNullException` when `repository` or `predicate` is null.

### FirstOrDefaultAsync(predicate)

```csharp
public static async Task<T?> FirstOrDefaultAsync<T>(this IRepository<T> repository, Func<T, bool> predicate) where T : class
```

Returns the first entity that satisfies the predicate, in the order `GetAllAsync` returns entities, or `null` when none matches.

- **repository**: The repository instance.
- **predicate**: A function to test each entity for a condition.
- **Returns**: The first matching entity; otherwise, `null`.
- **Throws**: `ArgumentNullException` when `repository` or `predicate` is null.

### WhereAsync

```csharp
public static async Task<IEnumerable<T>> WhereAsync<T>(this IRepository<T> repository, Func<T, bool> predicate) where T : class
```

Returns all entities that satisfy the predicate. The method calls `GetAllAsync` and applies `Where` to the result. The filter is deferred, so the predicate runs each time the returned sequence is enumerated.

- **repository**: The repository instance.
- **predicate**: A function to test each entity for a condition.
- **Returns**: An enumerable of matching entities. The enumerable is empty when nothing matches.
- **Throws**: `ArgumentNullException` when `repository` or `predicate` is null.

### SingleAsync(predicate)

```csharp
public static async Task<T> SingleAsync<T>(this IRepository<T> repository, Func<T, bool> predicate) where T : class
```

Returns the one entity that satisfies the predicate. The method calls `Enumerable.Single`, so it fails when zero or several entities match.

- **repository**: The repository instance.
- **predicate**: A function to test each entity for a condition.
- **Returns**: The single matching entity.
- **Throws**: `ArgumentNullException` when `repository` or `predicate` is null. `InvalidOperationException` when no entity or more than one entity matches.

### SingleOrDefaultAsync(predicate)

```csharp
public static async Task<T?> SingleOrDefaultAsync<T>(this IRepository<T> repository, Func<T, bool> predicate) where T : class
```

Returns the one entity that satisfies the predicate, or `null` when none matches. The method calls `Enumerable.SingleOrDefault`, so it still fails when more than one entity matches.

- **repository**: The repository instance.
- **predicate**: A function to test each entity for a condition.
- **Returns**: The single matching entity, or `null` if no entity matches.
- **Throws**: `ArgumentNullException` when `repository` or `predicate` is null. `InvalidOperationException` when more than one entity matches.

### SingleAsync()

```csharp
public static async Task<T> SingleAsync<T>(this IRepository<T> repository) where T : class
```

Returns the only entity in the repository. Use it for entity types that are expected to hold exactly one row, such as a settings record.

- **repository**: The repository instance.
- **Returns**: The only entity of type `T`.
- **Throws**: `ArgumentNullException` when `repository` is null. `InvalidOperationException` when the repository is empty or holds more than one entity.

### SingleOrDefaultAsync()

```csharp
public static async Task<T?> SingleOrDefaultAsync<T>(this IRepository<T> repository) where T : class
```

Returns the only entity in the repository, or `null` when the repository is empty.

- **repository**: The repository instance.
- **Returns**: The only entity of type `T`, or `null` if the repository is empty.
- **Throws**: `ArgumentNullException` when `repository` is null. `InvalidOperationException` when the repository holds more than one entity.

## Usage

### Checking whether an email is already registered

```csharp
using RedisCachePatterns.Infrastructure.Repositories;

public class RegistrationValidator(IUserRepository users)
{
    public Task<bool> IsEmailTakenAsync(string email)
    {
        return users.AnyAsync(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
    }
}
```

`IUserRepository` already has `GetByEmailAsync` for this lookup. Use `AnyAsync` when only the existence of a match matters, or when the match needs a condition the repository does not offer.

### Finding a product by SKU with a guard against duplicates

```csharp
Product? product = await productRepository.SingleOrDefaultAsync(p => p.Sku == sku);
```

`SingleOrDefaultAsync` returns `null` for an unknown SKU and throws `InvalidOperationException` when two products share the SKU. That makes it a useful check when the SKU is meant to be unique. Catch the exception if a duplicate SKU is a data error you want to report rather than crash on.

### Listing active users with a deferred filter

```csharp
IEnumerable<User> activeUsers = await users.WhereAsync(u => u.IsActive);
List<User> snapshot = activeUsers.ToList();
```

Materialize the result with `ToList()` when the caller needs a stable list. Otherwise each enumeration re-runs the predicate.

## Notes

*   **Every predicate method loads the full collection:** `AnyAsync`, `FirstOrDefaultAsync(predicate)`, `WhereAsync`, `SingleAsync(predicate)`, and `SingleOrDefaultAsync(predicate)` all call `GetAllAsync`, which copies the whole in-memory list under the repository lock. The predicate then runs over that copy. Cost grows with the number of stored entities, not with the number of matches.
*   **`FirstOrDefaultAsync(int)` is not an `async` method:** It is a plain method that returns the `Task` from `GetByIdAsync`. The `ArgumentNullException` for a null `repository` is therefore thrown synchronously when the method is called. The other methods are `async`, so their argument checks are stored in the returned `Task` and thrown when it is awaited.
*   **The name `FirstOrDefaultAsync(int)` is misleading:** It looks up by identifier, not by a predicate. To find the first entity by a condition, use the `Func<T, bool>` overload.
*   **Predicates are `Func`, not expressions:** The methods take `Func<T, bool>`, so they run in memory. They do not translate to a query language, and a repository backed by a real database would load all rows before filtering.
*   **Null predicates throw `ArgumentNullException`:** Each predicate overload checks its argument with `ArgumentNullException.ThrowIfNull`.
*   **`Single` and `SingleOrDefault` are strict:** They throw `InvalidOperationException` on duplicates, not only when no entity is found. Use `FirstOrDefaultAsync` when the first match is acceptable.
*   **Prefer entity-specific lookups when they exist:** `ProductRepository`, `UserRepository`, and the other repositories have their own methods such as `GetBySkuAsync` and `GetByEmailAsync`. Use those in preference to these generic helpers, because they are written for the entity.
*   **No caching:** The extensions do not read from or write to Redis.
*   **Ordering is not guaranteed by the contract:** Methods that return a single entity without an explicit order depend on the order of `GetAllAsync`. For the in-memory repositories this is insertion order, but the interface does not promise it.
