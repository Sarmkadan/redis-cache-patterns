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
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
    Task<int> CountAsync();
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
