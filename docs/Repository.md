# Repository

The `Repository<T>` class provides a generic, asynchronous implementation of the repository pattern, specifically designed to abstract data access operations within applications utilizing Redis for caching. It standardizes basic CRUD (Create, Read, Update, Delete) functionality, ensuring consistent interaction with data stores while potentially leveraging Redis for performance optimizations in retrieving and managing entities of type `T`.

## API

*   **`public virtual async Task<T?> GetByIdAsync(string id)`**
    Retrieves an entity by its unique identifier. Returns the entity of type `T` if found; otherwise, returns `null`. Throws an exception if the underlying data store or cache is unreachable.

*   **`public virtual async Task<IEnumerable<T>> GetAllAsync()`**
    Retrieves all entities of type `T` from the data store. Returns an `IEnumerable<T>` containing all available records. Throws an exception on failure to communicate with the data store.

*   **`public virtual async Task<T> AddAsync(T item)`**
    Persists a new entity of type `T` to the data store. Returns the added entity, which may include generated identifiers or updated timestamps. Throws an exception if the operation fails due to validation errors or connectivity issues.

*   **`public virtual async Task<T> UpdateAsync(T item)`**
    Updates an existing entity of type `T` in the data store. Returns the updated entity. Throws an exception if the entity does not exist or if the update operation fails.

*   **`public virtual async Task<bool> DeleteAsync(string id)`**
    Removes an entity by its unique identifier. Returns `true` if the deletion was successful; `false` if the entity was not found. Throws an exception on connection failure.

*   **`public virtual async Task<int> CountAsync()`**
    Returns the total number of entities of type `T` currently stored. Throws an exception if the count operation cannot be performed.

*   **`public virtual async Task<bool> ExistsAsync(string id)`**
    Checks if an entity with the specified identifier exists. Returns `true` if found; `false` otherwise. Throws an exception on connectivity failure.

## Usage

### Basic CRUD Operations

```csharp
var repository = new Repository<Product>(redisDatabase);

// Add a new product
var newProduct = new Product { Id = "p1", Name = "Widget" };
await repository.AddAsync(newProduct);

// Retrieve the product
var product = await repository.GetByIdAsync("p1");
if (product != null)
{
    Console.WriteLine($"Found: {product.Name}");
}
```

### Checking Existence and Bulk Retrieval

```csharp
var repository = new Repository<Order>(redisDatabase);

// Check if an order exists before proceeding
bool exists = await repository.ExistsAsync("ord-123");

if (exists)
{
    // Retrieve all orders
    var allOrders = await repository.GetAllAsync();
    Console.WriteLine($"Total orders: {allOrders.Count()}");
}
```

## Notes

*   **Thread Safety:** The `Repository<T>` implementation is designed to be thread-safe, assuming the underlying Redis client (e.g., StackExchange.Redis) is correctly configured and utilized. Asynchronous methods should be awaited properly to avoid race conditions or deadlock scenarios in the calling code.
*   **Cache Consistency:** In scenarios where Redis is used as a cache, there is an inherent risk of data inconsistency between the primary database and the cache. Ensure that the implementation of `UpdateAsync` and `DeleteAsync` correctly invalidates or updates the corresponding cache keys to maintain data integrity.
*   **Serialization:** Since this repository interacts with Redis, ensure that type `T` is properly decorated with appropriate serialization attributes or that a custom serializer is configured to handle the mapping between C# objects and Redis data structures.
*   **Edge Cases:** Operations may fail due to network interruptions, Redis eviction policies, or serialization errors. Implement proper error handling (try-catch blocks) around repository calls to manage these scenarios gracefully.
