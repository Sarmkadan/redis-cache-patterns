# RepositoryExtensions

Extension methods for `IRepository<T>` providing common asynchronous LINQ-style operations on in-memory collections.
These methods mirror the behavior of `System.Linq.Enumerable` extension methods but operate asynchronously over repository data.

## API

### FirstOrDefaultAsync<T>(this IRepository<T> repository, int id)

Attempts to retrieve a single entity by its identifier.

- **Parameters:**
  - `repository`: The repository instance to query.
  - `id`: The identifier of the entity to retrieve.
- **Returns:** The entity if found; otherwise, `null`.
- **Exceptions:** Throws `ArgumentNullException` if `repository` is `null`.
- **Notes:** This method is an alias for `IRepository<T>.GetByIdAsync(int)` for convenience when chaining with other extension methods.

### AnyAsync<T>(this IRepository<T> repository, Func<T, bool> predicate)

Determines whether any entity in the repository satisfies the specified predicate.

- **Parameters:**
  - `repository`: The repository instance to query.
  - `predicate`: A function to test each entity for a condition.
- **Returns:** `true` if any entities match the predicate; otherwise, `false`.
- **Exceptions:** Throws `ArgumentNullException` if `repository` or `predicate` is `null`.
- **Notes:** Materializes all entities via `GetAllAsync()` before applying the predicate.

### FirstOrDefaultAsync<T>(this IRepository<T> repository, Func<T, bool> predicate)

Returns the first entity that satisfies the specified predicate, or `null` if no such entity exists.

- **Parameters:**
  - `repository`: The repository instance to query.
  - `predicate`: A function to test each entity for a condition.
- **Returns:** The first matching entity; otherwise, `null`.
- **Exceptions:** Throws `ArgumentNullException` if `repository` or `predicate` is `null`.
- **Notes:** Materializes all entities via `GetAllAsync()` before applying the predicate and `FirstOrDefault`.

### WhereAsync<T>(this IRepository<T> repository, Func<T, bool> predicate)

Returns all entities that satisfy the specified predicate.

- **Parameters:**
  - `repository`: The repository instance to query.
  - `predicate`: A function to test each entity for a condition.
- **Returns:** An `IEnumerable<T>` of entities matching the predicate.
- **Exceptions:** Throws `ArgumentNullException` if `repository` or `predicate` is `null`.
- **Notes:** Materializes all entities via `GetAllAsync()` before applying the predicate and `Where`.

### SingleAsync<T>(this IRepository<T> repository, Func<T, bool> predicate)

Returns the single entity that satisfies the specified predicate, or throws if not found.

- **Parameters:**
  - `repository`: The repository instance to query.
  - `predicate`: A function to test each entity for a condition.
- **Returns:** The single matching entity.
- **Exceptions:**
  - Throws `ArgumentNullException` if `repository` or `predicate` is `null`.
  - Throws `InvalidOperationException` if no entity or multiple entities match the predicate.
- **Notes:** Materializes all entities via `GetAllAsync()` before applying the predicate and `Single`.

### SingleOrDefaultAsync<T>(this IRepository<T> repository, Func<T, bool> predicate)

Returns the single entity that satisfies the specified predicate, or `null` if not found.

- **Parameters:**
  - `repository`: The repository instance to query.
  - `predicate`: A function to test each entity for a condition.
- **Returns:** The single matching entity, or `null` if not found.
- **Exceptions:**
  - Throws `ArgumentNullException` if `repository` or `predicate` is `null`.
  - Throws `InvalidOperationException` if multiple entities match the predicate.
- **Notes:** Materializes all entities via `GetAllAsync()` before applying the predicate and `SingleOrDefault`.

### SingleAsync<T>(this IRepository<T> repository)

Returns the only entity of the specified type, or throws if not found.

- **Parameters:**
  - `repository`: The repository instance to query.
- **Returns:** The only entity of type `T`.
- **Exceptions:**
  - Throws `ArgumentNullException` if `repository` is `null`.
  - Throws `InvalidOperationException` if no entity or multiple entities of type `T` exist.
- **Notes:** Materializes all entities via `GetAllAsync()` before applying `Single`.

### SingleOrDefaultAsync<T>(this IRepository<T> repository)

Returns the only entity of the specified type, or `null` if not found.

- **Parameters:**
  - `repository`: The repository instance to query.
- **Returns:** The only entity of type `T`, or `null` if not found.
- **Exceptions:** Throws `ArgumentNullException` if `repository` is `null`.
- **Notes:** Materializes all entities via `GetAllAsync()` before applying `SingleOrDefault`.

## Usage

### Example 1: Filtering and retrieving a single user

```csharp
var repository = new InMemoryRepository<User>();

// Add sample data
await repository.AddAsync(new User { Id = 1, Name = "Alice", Age = 30 });
await repository.AddAsync(new User { Id = 2, Name = "Bob", Age = 25 });
await repository.AddAsync(new User { Id = 3, Name = "Charlie", Age = 35 });

// Find the first user over 30
var user = await repository.FirstOrDefaultAsync(u => u.Age > 30);

Console.WriteLine(user?.Name); // Output: Charlie
```

### Example 2: Checking existence and retrieving all matching entities

```csharp
var productRepository = new InMemoryRepository<Product>();

// Add sample data
await productRepository.AddAsync(new Product { Id = 1, Name = "Laptop", Price = 999.99m, InStock = true });
await productRepository.AddAsync(new Product { Id = 2, Name = "Mouse", Price = 25.50m, InStock = true });
await productRepository.AddAsync(new Product { Id = 3, Name = "Keyboard", Price = 75.00m, InStock = false });

// Check if any in-stock products exist
bool hasInStock = await productRepository.AnyAsync(p => p.InStock);
Console.WriteLine(hasInStock); // Output: True

// Get all in-stock products
var inStockProducts = await productRepository.WhereAsync(p => p.InStock);
foreach (var product in inStockProducts)
{
    Console.WriteLine($"{product.Name}: {product.Price:C}");
}
```

## Notes

- **Materialization:** All methods materialize the entire repository collection via `GetAllAsync()` before applying LINQ operations. This ensures consistency but may impact performance for large datasets.
- **Thread Safety:** These methods are not thread-safe by design. Concurrent modifications to the underlying repository during execution may lead to inconsistent results or exceptions.
- **Null Handling:** Methods return `null` for "not found" cases where appropriate (`FirstOrDefaultAsync` with predicate, `SingleOrDefaultAsync` with/without predicate). Methods that require a single result throw `InvalidOperationException` when the result set is empty or contains multiple items.
- **Predicate Validation:** Methods accepting predicates validate the predicate parameter and throw `ArgumentNullException` if `null`.
- **Repository Validation:** All methods validate the repository parameter and throw `ArgumentNullException` if `null`.
- **Performance Considerations:** For large repositories, consider implementing server-side filtering or pagination instead of materializing all entities.