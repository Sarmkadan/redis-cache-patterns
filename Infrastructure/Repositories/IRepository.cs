#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace RedisCachePatterns.Infrastructure.Repositories;

/// <summary>
/// Generic repository interface providing CRUD operations
/// </summary>
public interface IRepository<T> where T : class
{
    /// <summary>Gets an entity by ID.</summary>
    /// <param name="id">The entity ID.</param>
    /// <returns>The entity if found; otherwise null.</returns>
    Task<T?> GetByIdAsync(int id);
    /// <summary>Gets all entities.</summary>
    /// <returns>A collection of all entities.</returns>
    Task<IEnumerable<T>> GetAllAsync();
    /// <summary>Adds a new entity.</summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>The added entity.</returns>
    Task<T> AddAsync(T entity);
    /// <summary>Updates an existing entity.</summary>
    /// <param name="entity">The entity to update.</param>
    /// <returns>The updated entity.</returns>
    Task<T> UpdateAsync(T entity);
    /// <summary>Deletes an entity by ID.</summary>
    /// <param name="id">The entity ID.</param>
    /// <returns><c>true</c> if deleted; otherwise <c>false</c>.</returns>
    Task<bool> DeleteAsync(int id);
    /// <summary>Counts total entities.</summary>
    /// <returns>The total count of entities.</returns>
    Task<int> CountAsync();
    /// <summary>Checks if an entity exists.</summary>
    /// <param name="id">The entity ID.</param>
    /// <returns><c>true</c> if exists; otherwise <c>false</c>.</returns>
    Task<bool> ExistsAsync(int id);
}

/// <summary>
/// Repository interface for User entities
/// </summary>
public interface IUserRepository : IRepository<RedisCachePatterns.Domain.User>
{
    Task<RedisCachePatterns.Domain.User?> GetByUsernameAsync(string username);
    Task<RedisCachePatterns.Domain.User?> GetByEmailAsync(string email);
    Task<IEnumerable<RedisCachePatterns.Domain.User>> GetActiveUsersAsync();
    Task<IEnumerable<RedisCachePatterns.Domain.User>> GetByRoleAsync(RedisCachePatterns.Domain.UserRole role);
}

/// <summary>
/// Repository interface for Product entities
/// </summary>
public interface IProductRepository : IRepository<RedisCachePatterns.Domain.Product>
{
    Task<IEnumerable<RedisCachePatterns.Domain.Product>> GetByCategoryAsync(string category);
    Task<RedisCachePatterns.Domain.Product?> GetBySkuAsync(string sku);
    Task<IEnumerable<RedisCachePatterns.Domain.Product>> GetLowStockProductsAsync();
    Task<IEnumerable<RedisCachePatterns.Domain.Product>> SearchByNameAsync(string searchTerm);
}

/// <summary>
/// Repository interface for Order entities
/// </summary>
public interface IOrderRepository : IRepository<RedisCachePatterns.Domain.Order>
{
    Task<IEnumerable<RedisCachePatterns.Domain.Order>> GetByUserIdAsync(int userId);
    Task<RedisCachePatterns.Domain.Order?> GetByOrderNumberAsync(string orderNumber);
    Task<IEnumerable<RedisCachePatterns.Domain.Order>> GetByStatusAsync(RedisCachePatterns.Domain.OrderStatus status);
    Task<IEnumerable<RedisCachePatterns.Domain.Order>> GetOrdersInDateRangeAsync(DateTime startDate, DateTime endDate);
}

/// <summary>
/// Repository interface for InventoryItem entities
/// </summary>
public interface IInventoryRepository : IRepository<RedisCachePatterns.Domain.InventoryItem>
{
    Task<RedisCachePatterns.Domain.InventoryItem?> GetByProductAndWarehouseAsync(int productId, string warehouse);
    Task<IEnumerable<RedisCachePatterns.Domain.InventoryItem>> GetByProductAsync(int productId);
    Task<IEnumerable<RedisCachePatterns.Domain.InventoryItem>> GetLowStockItemsAsync();
    Task<int> GetTotalQuantityAsync(int productId);
}
