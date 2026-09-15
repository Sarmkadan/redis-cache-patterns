# IRepository

The repository interfaces in `Infrastructure/Repositories/IRepository.cs` define asynchronous data-access contracts for the application's domain entities. `IRepository<T>` supplies the common CRUD, count, and existence operations for reference types, while `IUserRepository`, `IProductRepository`, `IOrderRepository`, and `IInventoryRepository` extend that contract with entity-specific queries.

## API

### `IRepository<T>`

```csharp
public interface IRepository<T> where T : class
```

Defines the shared repository operations for an entity type `T`. The type argument must be a reference type.

#### `GetByIdAsync`

```csharp
Task<T?> GetByIdAsync(int id)
```

Retrieves the entity with the specified integer identifier. Returns the entity when found or `null` when no matching entity exists.

#### `GetAllAsync`

```csharp
Task<IEnumerable<T>> GetAllAsync()
```

Retrieves all entities available through the repository.

#### `AddAsync`

```csharp
Task<T> AddAsync(T entity)
```

Adds an entity and returns the added entity.

#### `UpdateAsync`

```csharp
Task<T> UpdateAsync(T entity)
```

Updates an entity and returns the updated entity.

#### `DeleteAsync`

```csharp
Task<bool> DeleteAsync(int id)
```

Deletes the entity with the specified identifier. Returns `true` when the entity is deleted; otherwise, returns `false`.

#### `CountAsync`

```csharp
Task<int> CountAsync()
```

Returns the total number of entities in the repository.

#### `ExistsAsync`

```csharp
Task<bool> ExistsAsync(int id)
```

Returns `true` when an entity with the specified identifier exists; otherwise, returns `false`.

### `IUserRepository`

```csharp
public interface IUserRepository : IRepository<User>
```

Adds user-specific queries to the generic repository contract.

- `Task<User?> GetByUsernameAsync(string username)` retrieves a user by username.
- `Task<User?> GetByEmailAsync(string email)` retrieves a user by email address.
- `Task<IEnumerable<User>> GetActiveUsersAsync()` retrieves active users.
- `Task<IEnumerable<User>> GetByRoleAsync(UserRole role)` retrieves users with the specified role.

### `IProductRepository`

```csharp
public interface IProductRepository : IRepository<Product>
```

Adds product-specific queries to the generic repository contract.

- `Task<IEnumerable<Product>> GetByCategoryAsync(string category)` retrieves products in a category.
- `Task<Product?> GetBySkuAsync(string sku)` retrieves a product by SKU.
- `Task<IEnumerable<Product>> GetLowStockProductsAsync()` retrieves products considered low in stock.
- `Task<IEnumerable<Product>> SearchByNameAsync(string searchTerm)` searches for products using the supplied term.

### `IOrderRepository`

```csharp
public interface IOrderRepository : IRepository<Order>
```

Adds order-specific queries to the generic repository contract.

- `Task<IEnumerable<Order>> GetByUserIdAsync(int userId)` retrieves orders belonging to a user.
- `Task<Order?> GetByOrderNumberAsync(string orderNumber)` retrieves an order by order number.
- `Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status)` retrieves orders with the specified status.
- `Task<IEnumerable<Order>> GetOrdersInDateRangeAsync(DateTime startDate, DateTime endDate)` retrieves orders within a date range.

### `IInventoryRepository`

```csharp
public interface IInventoryRepository : IRepository<InventoryItem>
```

Adds inventory-specific queries to the generic repository contract.

- `Task<InventoryItem?> GetByProductAndWarehouseAsync(int productId, string warehouse)` retrieves an inventory item for a product and warehouse.
- `Task<IEnumerable<InventoryItem>> GetByProductAsync(int productId)` retrieves inventory items for a product.
- `Task<IEnumerable<InventoryItem>> GetLowStockItemsAsync()` retrieves inventory items considered low in stock.
- `Task<int> GetTotalQuantityAsync(int productId)` returns the total inventory quantity for a product.

## Usage

Depend on the appropriate interface so application code remains independent of a particular repository implementation.

```csharp
public sealed class ProductLookupService
{
    private readonly IProductRepository _products;

    public ProductLookupService(IProductRepository products)
    {
        _products = products;
    }

    public async Task<Product?> FindBySkuAsync(string sku)
    {
        return await _products.GetBySkuAsync(sku);
    }

    public async Task<bool> RemoveAsync(int productId)
    {
        if (!await _products.ExistsAsync(productId))
        {
            return false;
        }

        return await _products.DeleteAsync(productId);
    }
}
```

The generic contract can also be used when no entity-specific query is required:

```csharp
public async Task<int> AddAndCountAsync<T>(IRepository<T> repository, T entity)
    where T : class
{
    await repository.AddAsync(entity);
    return await repository.CountAsync();
}
```

## Notes

- All members use the task-based asynchronous pattern and should be awaited by callers.
- Entity identifiers in the shared contract are `int` values.
- Nullable results from `GetByIdAsync`, `GetByUsernameAsync`, `GetByEmailAsync`, `GetBySkuAsync`, `GetByOrderNumberAsync`, and `GetByProductAndWarehouseAsync` indicate that no matching entity may exist.
- Collection-returning methods expose `IEnumerable<T>`; callers should not assume a particular collection implementation or ordering unless the selected repository implementation documents one.
- Validation, exception behavior, storage, caching, and concurrency guarantees are implementation concerns and are not specified by these interfaces.
